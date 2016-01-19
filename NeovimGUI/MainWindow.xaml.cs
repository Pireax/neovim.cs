using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MsgPack;
using Neovim;

namespace NeovimGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NeovimClient _neovim;
        private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _neovim = new NeovimClient(@"C:\Program Files\Neovim\bin\nvim.exe");

            // Event is asynchronous so we need to handle the redraw event in the UI thread
            _neovim.Redraw += (o, args) => _uiContext.Post((x) => OnRedraw(o, args), null);

            _neovim.ui_attach(80, 24, true);

            this.KeyDown += term_KeyDown;
            Grid.SizeChanged += Window_SizeChanged;
        }

        private void OnRedraw(object sender, NeovimRedrawEventArgs e)
        {
            var v = _term.Dispatcher.DisableProcessing(); // using() {}?
            foreach (var f in e.Functions)
            {
                var list = f.AsList();
                string function = list[0].AsString(Encoding.Default);

                IList<IList<MessagePackObject>> args = new List<IList<MessagePackObject>>();
                for (var i = 1; i < list.Count; i++)
                    args.Add(list[i].AsList());

                switch (function)
                {
                    case "clear":
                        _term.Clear();
                        break;
                    case "resize":
                        _term.Resize(args[0][1].AsInt32(), args[0][0].AsInt32());
                        break;

                    case "update_fg":
                        break;

                    case "update_bg":
                        break;

                    case "highlight_set":
                        Color fg = Colors.White;
                        bool bold = false;
                        bool italic = false;

                        foreach (var arg in args)
                        {
                            var dict = arg[0].AsDictionary();

                            foreach (var entry in dict)
                            {
                                var str = entry.Key.AsString(Encoding.Default);
                                if (str == "foreground")
                                {
                                    uint c = entry.Value.AsUInt32();
                                    byte r = (byte)(c >> 16);
                                    byte g = (byte)(c >> 8);
                                    byte b = (byte)(c >> 0);
                                    fg = Color.FromRgb(r, g, b);
                                }
                                else if (str == "bold")
                                    bold = entry.Value.AsBoolean();
                                else if (str == "italic")
                                    italic = entry.Value.AsBoolean();

                            }
                        }

                        _term.Highlight(fg, bold, italic);
                        break;

                    case "eol_clear":
                        _term.ClearToEnd();
                        break;

                    case "set_title":
                        this.Title = args[0][0].AsString(Encoding.Default);
                        break;

                    case "put":
                        List<byte> bytes = new List<byte>();

                        foreach (var arg in args)
                            bytes.AddRange(arg[0].AsBinary());

                        _term.PutText(Encoding.Default.GetString(bytes.ToArray()));
                        break;

                    case "cursor_goto":
                        _term.TCursor.MoveCaret(args[0][0].AsInt32(), args[0][1].AsInt32());
                        break;

                    case "scroll":
                        _term.Scroll(args[0][0].AsSByte());
                        break;

                    case "set_scroll_region":
                        break;

                    case "normal_mode":
                        //UpdateTerminal((t) => t.TCursor.Width = t._cellWidth);
                        break;

                    case "insert_mode":
                        //UpdateTerminal((t) => t.TCursor.Width = 3);
                        break;

                    case "busy_start":
                        break;

                    case "busy_stop":
                        break;

                    case "mouse_on":
                        break;

                    case "mouse_off":
                        break;
                }
            }
            v.Dispose();
        }

        private void term_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift || e.Key == Key.LeftAlt ||
                e.Key == Key.RightAlt || e.Key == Key.CapsLock || e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                return;

            string keys = Input.Encode(e.Key);
            if (keys != null)
                _neovim.vim_input(keys);

            e.Handled = true;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //int rows = (int)Math.Round(e.NewSize.Height / _term.Cell.Height);
            //int columns = (int) Math.Round(e.NewSize.Width/_term.Cell.Width);

            //if (_term.Cells.Count != rows && _term.Cells[0].Count != columns)
            //    _neovim.ui_try_resize(columns, rows);
        }
    }
}
