using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using MsgPack;

namespace Neovim
{
    public enum MessageType
    {
        Request = 0,
        Response = 1,
        Notification = 2
    }
    class MsgPackIO
    {
        /* Some of the code in this class is borrowed from http://odetocode.com/articles/97.aspx */

        private bool _executing = false;
        private Process _process;
        private StreamWriter _standardInput;
        private StreamReader _standardOutput;
        private StreamReader _standardError;
        private byte[] _errorBuffer = new byte[8192];
        private byte[] _outputBuffer = new byte[16384];
        private Dictionary<int, TaskCompletionSource<MessagePackObject[]>> tcs = new Dictionary<int, TaskCompletionSource<MessagePackObject[]>>();

        private int _msgId = 0;

        private AsyncCallback _outputReady;
        private AsyncState _outputState;
        private AsyncCallback _errorReady;
        private AsyncState _errorState;

        public MsgPackIO(string filename, string args) { InitializeProcess(filename, args); }

        /// <summary>
        /// Start reading asynchronously from the output and error streams
        /// </summary>
        public void StartReadingOutput()
        {
            _standardOutput.BaseStream.BeginRead(
               _outputBuffer, 0,
               _outputBuffer.Length,
               _outputReady,
               _outputState
               );

            _standardError.BaseStream.BeginRead(
               _errorBuffer, 0,
               _errorBuffer.Length,
               _errorReady,
               _errorState
               );
        }

        /// <summary>
        /// This function makes a RPC call to the process and returns the reply
        /// </summary>
        /// <param name="type">Type of request</param>
        /// <param name="method">Method name</param>
        /// <param name="parameters">Method parameters</param>
        /// <param name="returnValue">Does it return?</param>
        /// <returns></returns>
        public MessagePackObject[] Request(int type, string method, object[] parameters, bool returnValue = true)
        {
            var request = RequestAsync(type, method, parameters, returnValue);
            MessagePackObject[] result = request.Result;
            return result;
        }

        /// <summary>
        /// Makes an async request
        /// </summary>
        /// <param name="type">Type of request</param>
        /// <param name="method">Method name</param>
        /// <param name="parameters">Method parameters</param>
        /// <param name="returnValue">Does it return?</param>
        /// <returns></returns>
        public async Task<MessagePackObject[]> RequestAsync(int type, string method, object[] parameters, bool returnValue = true)
        {
            MemoryStream package = new MemoryStream();

            Packer packer = Packer.Create(package);
            packer.PackArrayHeader(4);
            packer.Pack(type);
            packer.Pack(_msgId);
            packer.PackString(method);
            packer.PackArrayHeader(parameters.Length);
            if (parameters.Length > 0)
            {
                foreach (var param in parameters)
                {
                    packer.Pack(param);
                }
            }

            string temp;
            package.Position = 0;
            using (StreamReader sr = new StreamReader(package, Encoding.Default))
            {
                temp = sr.ReadToEnd();
            }

            int id = _msgId;
            if (returnValue)
                tcs.Add(id, new TaskCompletionSource<MessagePackObject[]>());

            _standardInput.Write(temp);
            _msgId++;

            if (!returnValue)
                return null;

            MessagePackObject[] result = await tcs[id].Task.ConfigureAwait(false);
            tcs.Remove(id);
            return result;
        }

        public delegate void MsgPackNotificationEventHandler(object sender, MsgPackEventArgs e);
        public event MsgPackNotificationEventHandler NotificationReceived;

        protected virtual void OnNotificationReceived(MsgPackEventArgs e)
        {
            MsgPackNotificationEventHandler handler = NotificationReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected ProcessStartInfo GetStartInfo(string filename, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = filename;
            psi.Arguments = args;
            psi.UseShellExecute = false;
            psi.StandardErrorEncoding = Encoding.Default;
            psi.StandardOutputEncoding = Encoding.Default;

            psi.RedirectStandardInput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            psi.CreateNoWindow = true;

            return psi;
        }

        protected void InitializeProcess(string filename, string args)
        {
            if (_executing)
            {
                // don't allow client to start another process while one is 
                // currently executing
                throw new ApplicationException("A Process is currently executing");
            }

            _process = new Process();
            _process.StartInfo = GetStartInfo(filename, args);
            _process.EnableRaisingEvents = true;
            _process.Exited += new EventHandler(_process_Exited);
            _process.Start();

            AttachStreams();
            PrepareAsyncState();
        }

        private void AttachStreams()
        {
            _standardInput = _process.StandardInput;
            _standardOutput = _process.StandardOutput;
            _standardError = _process.StandardError;
        }

        private void _process_Exited(object sender, EventArgs e)
        {
            _process.Dispose();
            _process = null;
            _executing = false;
        }


        /// <summary>
        /// Gets executed if BeginRead() has red some data
        /// </summary>
        /// <param name="ar">Async Result</param>
        private void OutputCallback(IAsyncResult ar)
        {
            AsyncState state = (AsyncState)ar.AsyncState;

            int count = state.Stream.BaseStream.EndRead(ar);

            if (count > 0)
            {
                int readCount = 0;
                while (count > readCount)
                {
                    var data = Unpacking.UnpackObject(state.Buffer, readCount);
                    readCount += data.ReadCount;

                    if (!data.Value.IsArray)
                        Debugger.Break();

                    var dataList = data.Value.AsList();

                    // Response message has 4 items in the array
                    if (dataList.Count == 4)
                    {
                        var type = (MessageType)dataList[0].AsInt64();
                        if (type != MessageType.Response)
                            Debugger.Break();

                        var msgId = dataList[1].AsInt32();

                        var err = dataList[2];
                        var res = dataList[3];

                        tcs[msgId].SetResult(new[] { err, res });
                    }

                    // Notification message has 3 items in the array
                    else if (dataList.Count == 3)
                    {
                        var type = (MessageType)dataList[0].AsInt64();
                        if (type != MessageType.Notification)
                            Debugger.Break();

                        var func = dataList[1].AsString(Encoding.Default);

                        var res = dataList[2];

                        MsgPackEventArgs args = new MsgPackEventArgs();
                        args.Function = func;
                        args.Result = res;
                        OnNotificationReceived(args);
                    }

                    else Debugger.Break();
                }

                Array.Clear(state.Buffer, 0, state.Buffer.Length);

                _standardOutput.BaseStream.BeginRead(
                    _outputBuffer, 0,
                    _outputBuffer.Length,
                    _outputReady,
                    _outputState
                    );
            }
        }

        /// <summary>
        /// Callback for the error stream
        /// </summary>
        /// <param name="ar"></param>
        private void ErrorCallback(IAsyncResult ar)
        {
            AsyncState state = (AsyncState)ar.AsyncState;

            int count = state.Stream.BaseStream.EndRead(ar);

            if (count > 0)
            {
                Debug.WriteLine(Encoding.Default.GetString(state.Buffer));
                Debugger.Break();

                _standardError.BaseStream.BeginRead(
                    _errorBuffer, 0,
                    _errorBuffer.Length,
                    _errorReady,
                    _errorState
                    );
            }
        }
        private void PrepareAsyncState()
        {
            _outputReady = new AsyncCallback(OutputCallback);
            _outputState = new AsyncState(_standardOutput, _outputBuffer);
            _errorReady = new AsyncCallback(ErrorCallback);
            _errorState = new AsyncState(_standardError, _errorBuffer);
        }
    }

    public class MsgPackEventArgs : EventArgs
    {
        public MessagePackObject Result { get; set; }
        public string Function { get; set; }
    }

    internal class AsyncState
    {
        public AsyncState(StreamReader stream, byte[] buffer)
        {
            _stream = stream;
            _buffer = buffer;
        }

        public StreamReader Stream
        {
            get { return _stream; }
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }

        protected StreamReader _stream;
        protected byte[] _buffer;
    }
}
