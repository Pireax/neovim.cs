using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using MsgPack;

namespace Neovim
{
    public enum MessageType
    {
        Request = 0,
        Response = 1,
        Notification = 2
    }

    internal class MsgPackIO
    {
        /* Some of the code in this class is borrowed from http://odetocode.com/articles/97.aspx */
        private readonly StreamWriter _standardInput;
        private readonly StreamReader _standardOutput;
        private readonly StreamReader _standardError;
        private readonly byte[] _errorBuffer = new byte[8192];
        private readonly byte[] _outputBuffer = new byte[16384];
        private readonly Dictionary<long, TaskCompletionSource<MessagePackObject[]>> _tcs = new Dictionary<long, TaskCompletionSource<MessagePackObject[]>>();

        private int _msgId;

        private AsyncCallback _outputReady;
        private AsyncState _outputState;
        private AsyncCallback _errorReady;
        private AsyncState _errorState;

        public MsgPackIO(StreamWriter input, StreamReader output, StreamReader error)
        {
            _standardInput = input;
            _standardOutput = output;
            _standardError = error;

            PrepareAsyncState(true);
        }

        public MsgPackIO(StreamWriter input, StreamReader output)
        {
            _standardInput = input;
            _standardOutput = output;

            PrepareAsyncState();
        }

        private void PrepareAsyncState(bool errorStream = false)
        {
            _outputReady = OutputCallback;
            _outputState = new AsyncState(_standardOutput, _outputBuffer);

            if (!errorStream) return;

            _errorReady = ErrorCallback;
            _errorState = new AsyncState(_standardError, _errorBuffer);
        }

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
            var result = request.Result;
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

            var packer = Packer.Create(package);
            packer.PackArrayHeader(4);
            packer.Pack(type);
            packer.Pack(_msgId);
            packer.PackString(method);
            packer.PackArrayHeader(parameters.Length);
            foreach (var param in parameters)
            {
                packer.Pack(param);
            }

            string temp;
            package.Position = 0;
            using (StreamReader sr = new StreamReader(package, Encoding.Default))
            {
                temp = sr.ReadToEnd();
            }

            int id = _msgId;
            if (returnValue)
                _tcs.Add(id, new TaskCompletionSource<MessagePackObject[]>());

            _standardInput.Write(temp);
            _msgId++;

            if (!returnValue)
                return null;

            MessagePackObject[] result = await _tcs[id].Task.ConfigureAwait(false);
            _tcs.Remove(id);
            return result;
        }

        public delegate void MsgPackNotificationEventHandler(object sender, MsgPackNotificationEventArgs e);
        public event MsgPackNotificationEventHandler NotificationReceived;

        protected virtual void OnNotificationReceived(MsgPackNotificationEventArgs e)
        {
            var handler = NotificationReceived;
            handler?.Invoke(this, e);
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

                    var dataList = data.Value.AsList().Select(x => new MessagePackObject(x)).ToList();

                    // Response message has 4 items in the array
                    if (dataList.Count == 4)
                    {
                        var type = (MessageType)dataList[0].AsInteger();
                        if (type != MessageType.Response)
                            Debugger.Break();

                        var msgId = dataList[1].AsInteger();

                        var err = dataList[2];
                        var res = dataList[3];

                        _tcs[msgId].SetResult(new[] { err, res });
                    }

                    // Notification message has 3 items in the array
                    else if (dataList.Count == 3)
                    {
                        var type = (MessageType)dataList[0].AsInteger();
                        if (type != MessageType.Notification)
                            Debugger.Break();

                        var func = dataList[1].AsString(Encoding.Default);

                        var res = dataList[2];

                        MsgPackNotificationEventArgs args = new MsgPackNotificationEventArgs
                        {
                            Method = func,
                            Params = res
                        };
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
    }

    public class MsgPackNotificationEventArgs : EventArgs
    {
        public MessagePackObject Params { get; set; }
        public string Method { get; set; }
    }

    internal class AsyncState
    {
        public AsyncState(StreamReader stream, byte[] buffer)
        {
            Stream = stream;
            Buffer = buffer;
        }

        public StreamReader Stream { get; }
        public byte[] Buffer { get; }
    }
}
