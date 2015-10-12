using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MsgPack;

namespace Neovim
{
    public class NeovimClient
    {
        MsgPackIO _io;

        /// <summary>
        /// Initializes the Client
        /// </summary>
        /// <param name="path">Path to Neovim.exe</param>
        public NeovimClient(string path)
        {
            _io = new MsgPackIO(path, @"--embed --headless");
            _io.NotificationReceived += OnNotificationReceived;
            _io.StartReadingOutput();
        }

        public event EventHandler<NeovimRedrawEventArgs> Redraw;

        protected virtual void OnRedraw(NeovimRedrawEventArgs e)
        {
            EventHandler<NeovimRedrawEventArgs> handler = Redraw;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Fires a redraw event after getting a notification from the MsgPackIO class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msgPackEventArgs"></param>
        private void OnNotificationReceived(object sender, MsgPackEventArgs msgPackEventArgs)
        {
            if (msgPackEventArgs.Function != "redraw")
                return;

            var list = msgPackEventArgs.Result.AsList();
            NeovimRedrawEventArgs args = new NeovimRedrawEventArgs();
            args.Functions = list;
            OnRedraw(args);
        }

        #region Neovim RPC requests

        #region buffer
        public long buffer_line_count(long buffer)
        {
            return _io.Request((int)MessageType.Request, "buffer_line_count", new object[] { buffer })[1].AsInt64();
        }
        public string buffer_get_line(long buffer, long index)
        {
            return _io.Request((int)MessageType.Request, "buffer_get_line", new object[] { buffer, index })[1].AsString(Encoding.Default);
        }
        public void buffer_set_line(long buffer, long index, string line)
        {
            _io.Request((int)MessageType.Request, "buffer_set_line", new object[] { buffer, index, line });
        }
        public void buffer_del_line(long buffer, long index)
        {
            _io.Request((int)MessageType.Request, "buffer_del_line", new object[] { buffer, index });
        }
        public string[] buffer_get_line_slice(long buffer, long start, long end, bool include_start, bool include_end)
        {
            var list = _io.Request((int)MessageType.Request, "buffer_get_line_slice", new object[] { buffer, start, end, include_start, include_end })[1].AsList();
            return list.Select(i => i.AsString()).ToArray();
        }
        public void buffer_set_line_slice(long buffer, long start, long end, bool include_start, bool include_end, string[] replacement)
        {
            _io.Request((int)MessageType.Request, "buffer_set_line_slice", new object[] { buffer, start, end, include_start, include_end, replacement });
        }
        public object buffer_get_var(long buffer, string name)
        {
            return _io.Request((int)MessageType.Request, "buffer_get_var", new object[] { buffer, name })[1].ToObject();
        }
        public void buffer_set_var(long buffer, string name, object value)
        {
            _io.Request((int)MessageType.Request, "buffer_set_var", new object[] { buffer, name, value });
        }
        public object buffer_get_option(long buffer, string name)
        {
            return _io.Request((int)MessageType.Request, "buffer_get_option", new object[] { buffer, name })[1].ToObject();
        }
        public void buffer_set_option(long buffer, string name, object value)
        {
            _io.Request((int)MessageType.Request, "buffer_set_option", new [] { buffer, name, value });
        }
        public long buffer_get_number(long buffer)
        {
            return _io.Request((int)MessageType.Request, "buffer_get_number", new object[] { buffer })[1].AsInt64();
        }
        public string buffer_get_name(long buffer)
        {
            return _io.Request((int)MessageType.Request, "buffer_get_name", new object[] { buffer })[1].AsString(Encoding.Default);
        }
        public bool buffer_is_valid(long buffer)
        {
            return _io.Request((int)MessageType.Request, "buffer_is_valid", new object[] { buffer })[1].AsBoolean();
        }
        public void buffer_insert(long buffer, long lnum, string[] lines)
        {
            _io.Request((int)MessageType.Request, "buffer_insert", new object[] { buffer, lines });
        }
        public long[] buffer_get_mark(long buffer, string name)
        {
            var list = _io.Request((int)MessageType.Request, "buffer_get_mark", new object[] { buffer, name })[1].AsList();
            return list.Select(i => i.AsInt64()).ToArray();
        }
        #endregion

        #region tabpage

        #endregion

        #region ui
        public void ui_attach(long width, long height, bool rgb)
        {
            _io.Request((int)MessageType.Request, "ui_attach", new object[] { width, height, rgb });
        }
        public void ui_detach()
        {
            _io.Request((int)MessageType.Request, "ui_detach", new object[] { });
        }
        public void ui_try_resize(long width, long height)
        {
            _io.Request((int)MessageType.Request, "ui_try_resize", new object[] { width, height });
        }
        #endregion

        #region vim
        public void vim_command(string str)
        {
            _io.Request((int)MessageType.Request, "vim_command", new object[] { str });
        }
        public void vim_feedkeys(string keys, string mode, bool escape_csi)
        {
            _io.Request((int)MessageType.Request, "vim_feedkeys", new object[] { keys, mode, escape_csi });
        }
        public long vim_input(string keys)
        {
            return _io.Request((int)MessageType.Request, "vim_input", new object[] { keys }, true)[1].AsInt64();
        }
        public string vim_replace_termcodes(string str, bool from_part, bool do_lt, bool special)
        {
            return _io.Request((int)MessageType.Request, "vim_replace_termcodes", new object[] { str, from_part, do_lt, special })[1].AsString();
        }
        public string vim_command_output(string str)
        {
            return _io.Request((int)MessageType.Request, "vim_command_output", new object[] { str })[1].AsString();
        }
        public object vim_eval(string str)
        {
            return _io.Request((int)MessageType.Request, "vim_eval", new object[] { str })[1].ToObject();
        }
        public long vim_strwidth(string str)
        {
            return _io.Request((int)MessageType.Request, "vim_strwidth", new object[] { str })[1].AsInt64();
        }
        public string[] vim_list_runtime_paths(string str, bool from_part, bool do_lt, bool special)
        {
            var list = _io.Request((int)MessageType.Request, "vim_list_runtime_paths", new object[] { str, from_part, do_lt, special })[1].AsList();
            return list.Select(i => i.AsString()).ToArray();
        }
        public void vim_change_directory(string dir)
        {
            _io.Request((int)MessageType.Request, "vim_change_directory", new object[] { dir });
        }
        public string vim_get_current_line()
        {
            return _io.Request((int)MessageType.Request, "vim_get_current_line", new object[] {})[1].AsString(Encoding.Default);
        }
        public void vim_set_current_line(string line)
        {
            _io.Request((int)MessageType.Request, "vim_set_current_line", new object[] { line });
        }
        public void vim_del_current_line()
        {
            _io.Request((int)MessageType.Request, "vim_del_current_line", new object[] { });
        }
        public object vim_get_var(string name)
        {
            return _io.Request((int)MessageType.Request, "vim_get_var", new object[] { name })[1].ToObject();
        }
        public void vim_set_var(string name, object value)
        {
            _io.Request((int)MessageType.Request, "vim_set_var", new [] { name, value });
        }
        public object vim_get_vvar(string name)
        {
            return _io.Request((int)MessageType.Request, "vim_get_vvar", new object[] { name })[1].ToObject();
        }
        public string vim_get_option(string name)
        {
            return _io.Request((int)MessageType.Request, "vim_get_option", new object[] { name })[1].AsString(Encoding.Default);
        }
        public void vim_set_option(string name, object value)
        {
            _io.Request((int)MessageType.Request, "vim_set_option", new [] { name, value });
        }
        public void vim_out_write(string str)
        {
            _io.Request((int)MessageType.Request, "vim_out_write", new object[] { str });
        }
        public void vim_err_write(string str)
        {
            _io.Request((int)MessageType.Request, "vim_err_write", new object[] { str });
        }
        public void vim_report_error(string str)
        {
            _io.Request((int)MessageType.Request, "vim_report_error", new object[] { str });
        }
        public long[] vim_get_buffers()
        {
            var list = _io.Request((int) MessageType.Request, "vim_get_buffers", new object[] {})[1].AsList();
            return list.Select(i => ExtToInt64(i.AsMessagePackExtendedTypeObject().GetBody())).ToArray();
        }
        public long vim_get_current_buffer()
        {
            return ExtToInt64(_io.Request((int)MessageType.Request, "vim_get_current_buffer", new object[] { })[1].AsMessagePackExtendedTypeObject().GetBody());
        }
        public void vim_set_current_buffer(long buffer)
        {
            _io.Request((int) MessageType.Request, "vim_set_current_buffer", new object[] {buffer});
        }
        public long[] vim_get_windows()
        {
            var list = _io.Request((int)MessageType.Request, "vim_get_windows", new object[] { })[1].AsList();
            return list.Select(i => ExtToInt64(i.AsMessagePackExtendedTypeObject().GetBody())).ToArray();
        }
        public long vim_get_current_window()
        {
            return ExtToInt64(_io.Request((int)MessageType.Request, "vim_get_current_window", new object[] { })[1].AsMessagePackExtendedTypeObject().GetBody());
        }
        public void vim_set_current_window(long window)
        {
            _io.Request((int)MessageType.Request, "vim_set_current_window", new object[] { window });
        }
        public long[] vim_get_tabpages()
        {
            var list = _io.Request((int)MessageType.Request, "vim_get_tabpages", new object[] { })[1].AsList();
            return list.Select(i => ExtToInt64(i.AsMessagePackExtendedTypeObject().GetBody())).ToArray();
        }
        public long vim_get_current_tabpage()
        {
            return ExtToInt64(_io.Request((int)MessageType.Request, "vim_get_current_tabpage", new object[] { })[1].AsMessagePackExtendedTypeObject().GetBody());
        }
        public void vim_set_current_tabpage(long tabpage)
        {
            _io.Request((int)MessageType.Request, "vim_set_current_tabpage", new object[] { tabpage });
        }
        public void vim_subscribe(string _event)
        {
            _io.Request((int)MessageType.Request, "vim_subscribe", new object[] { _event });
        }
        public void vim_unsubscribe(string _event)
        {
            _io.Request((int)MessageType.Request, "vim_unsubscribe", new object[] { _event });
        }
        public long vim_name_to_color(string name)
        {
            return _io.Request((int) MessageType.Request, "vim_name_to_color", new object[] {name})[1].AsInt64();
        }
        public MessagePackObject vim_get_color_map()
        {
            return _io.Request((int) MessageType.Request, "vim_get_color_map", new object[] {})[1];
        }
        public MessagePackObject vim_get_api_info()
        {
            return _io.Request((int) MessageType.Request, "vim_get_api_info", new object[] {})[1];
        }
        #endregion

        #region window
        public long window_get_buffer(long window)
        {
            return ExtToInt64(_io.Request((int)MessageType.Request, "window_get_buffer", new object[] { window })[1].AsMessagePackExtendedTypeObject().GetBody());
        }
        public long[] window_get_cursor(long window)
        {
            var list = _io.Request((int) MessageType.Request, "window_get_cursor", new object[] {window})[1].AsList();
            return list.Select(i => i.AsInt64()).ToArray();
        }
        public void window_set_cursor(long window, long[] pos)
        {
            _io.Request((int) MessageType.Request, "window_set_cursor", new object[] {window, pos});
        }
        public long window_get_height(long window)
        {
            return _io.Request((int)MessageType.Request, "window_get_height", new object[] { window })[1].AsInt64();
        }
        public void window_set_height(long window, long height)
        {
            _io.Request((int)MessageType.Request, "window_set_height", new object[] { window, height });
        }
        public long window_get_width(long window)
        {
            return _io.Request((int)MessageType.Request, "window_get_width", new object[] { window })[1].AsInt64();
        }
        public void window_set_width(long window, long width)
        {
            _io.Request((int)MessageType.Request, "window_set_width", new object[] { window, width });
        }
        public object window_get_var(long window, string name)
        {
            return _io.Request((int)MessageType.Request, "window_get_var", new object[] { window, name })[1].ToObject();
        }
        public void window_set_var(long window, string name, object value)
        {
            _io.Request((int)MessageType.Request, "window_set_var", new object[] { window, name, value });
        }
        public object window_get_option(long window, string name)
        {
            return _io.Request((int)MessageType.Request, "window_get_option", new object[] { window, name })[1].ToObject();
        }
        public void window_set_option(long window, string name, object value)
        {
            _io.Request((int)MessageType.Request, "window_set_option", new object[] { window, name, value });
        }
        public long[] window_get_position(long window)
        {
            var list = _io.Request((int)MessageType.Request, "window_get_position", new object[] { window })[1].AsList();
            return list.Select(i => i.AsInt64()).ToArray();
        }
        public long window_get_tabpage(long window)
        {
            return ExtToInt64(_io.Request((int)MessageType.Request, "window_get_tabpage", new object[] { window })[1].AsMessagePackExtendedTypeObject().GetBody());
        }
        public bool window_is_valid(long window)
        {
            return _io.Request((int)MessageType.Request, "window_is_valid", new object[] { window })[1].AsBoolean();
        }
        #endregion

        #endregion

        /// <summary>
        /// Converts any of the Ext types of Neovim to a long
        /// </summary>
        /// <param name="bytes">The Ext type objects body</param>
        /// <returns>the value of the Ext type</returns>
        private long ExtToInt64(byte[] bytes)
        {
            if (bytes.Length < 8)
                Array.Resize(ref bytes, 8);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }
    }

    /// <summary>
    /// EventArgs for the redraw notification from Neovim
    /// </summary>
    public class NeovimRedrawEventArgs : EventArgs
    {
        public IList<MessagePackObject> Functions;
    }
}
