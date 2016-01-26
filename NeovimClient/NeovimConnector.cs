using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neovim
{
    internal abstract class NeovimConnector
    {
        private MsgPackIO _msgPackIo;

        public void StartReading()
        {
            _msgPackIo.StartReadingOutput();
        }

        public MessagePackObject[] Request(int type, string method, object[] parameters, bool returnValue = true)
        {
            return _msgPackIo.Request(type, method, parameters, returnValue);
        }

        public Task<MessagePackObject[]> RequestAsync(int type, string method, object[] parameters, bool returnValue = true)
        {
            return _msgPackIo.RequestAsync(type, method, parameters, returnValue);
        }

        public event MsgPackIO.MsgPackNotificationEventHandler NotificationReceived;

        protected virtual void OnNotificationReceived(object sender, MsgPackNotificationEventArgs e)
        {
            var handler = NotificationReceived;
            handler?.Invoke(this, e);
        }

        internal class Embed : NeovimConnector
        {
            private Process _neovimProcess;
            public bool IsRunning { get; private set; } = false;

            public Embed(string filename, string args)
            {
                _neovimProcess = new Process();
                _neovimProcess.StartInfo = GetStartInfo(filename, args);
                _neovimProcess.EnableRaisingEvents = true;
                _neovimProcess.Exited += OnProcessExited;
                _neovimProcess.Start();

                _msgPackIo = new MsgPackIO(_neovimProcess.StandardInput, _neovimProcess.StandardOutput,
                    _neovimProcess.StandardError);
                _msgPackIo.NotificationReceived += OnNotificationReceived;
                IsRunning = true;
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

            private void OnProcessExited(object sender, EventArgs e)
            {
                _neovimProcess.Dispose();
                IsRunning = false;
            }
        }
    }
}
