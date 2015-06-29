using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Policy;
using System.Text;
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

namespace Neovim
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            _renderProperties = new RenderProperties();
            _term = new Terminal(_renderProperties);
            Grid.Children.Add(_term);
        }

        private Terminal _term;
        private NeovimClient _neovim;
        private RenderProperties _renderProperties;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _neovim = new NeovimClient(@"C:\Program Files\Neovim\nvim.exe");
            _neovim.Redraw += OnRedraw;
            _neovim.ui_attach(80, 24, true);

            this.KeyDown += term_KeyDown;
            Grid.SizeChanged += Window_SizeChanged;
        }

        private delegate void UpdateTerminalWindowDelegate(Action<Terminal> message);
        private void UpdateTerminal(Action<Terminal> message)
        {
            if (!_term.Dispatcher.CheckAccess())
            {
                UpdateTerminalWindowDelegate update = UpdateTerminal;
                _term.Dispatcher.BeginInvoke(update, message);
            }
            else
            {
                message(_term);
            }
        }

        private void OnRedraw(object sender, NeovimRedrawEventArgs e)
        {
            foreach (var f in e.Functions)
            {
                var list = f.AsList();
                string function = list[0].AsString(Encoding.Default);


                IList<IList<MessagePackObject>> args = new List<IList<MessagePackObject>>();
                for (int i = 1; i < list.Count; i++)
                {
                    args.Add(list[i].AsList());
                }

                switch (function)
                {
                    case "clear":
                        UpdateTerminal((t) => t.Clear());
                        break;
                    case "resize":
                        UpdateTerminal((t) => t.Resize(args[0][1].AsInt32(), args[0][0].AsInt32()));
                        break;

                    case "update_fg":
                        break;

                    case "update_bg":
                        break;

                    case "highlight_set":
                        Color fg = Color.FromRgb(255, 255, 255);
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

                        UpdateTerminal((t) => t.Highlight(fg, bold, italic));
                        break;

                    case "eol_clear":
                        UpdateTerminal((t) => t.ClearToEnd());
                        break;

                    case "set_title":
                        this.Title = args[0][0].AsString(Encoding.Default);
                        break;

                    case "put":
                        List<byte> bytes = new List<byte>();

                        foreach (var arg in args)
                            bytes.AddRange(arg[0].AsBinary());

                        UpdateTerminal((t) => t.PutText(Encoding.Default.GetString(bytes.ToArray())));
                        break;

                    case "cursor_goto":
                        UpdateTerminal((t) => t.TerminalCursor.MoveCaret(args[0][0].AsInt32(), args[0][1].AsInt32()));
                        break;

                    case "scroll":
                        UpdateTerminal((t) => t.Scroll(args[0][0].AsByte()));
                        break;

                    case "set_scroll_region":
                        break;

                    case "normal_mode":
                        UpdateTerminal((t) => t.TerminalCursor.CaretRectangle.Width = t.Cells[0][0].Width);
                        break;

                    case "insert_mode":
                        UpdateTerminal((t) => t.TerminalCursor.CaretRectangle.Width = 5);
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
        }

        private void term_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift || e.Key == Key.LeftAlt|| 
                e.Key == Key.RightAlt || e.Key == Key.CapsLock || e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                return;

            string keys = Input.Encode(e.Key);
            if (keys != null)
                _neovim.vim_input(keys);

            e.Handled = true;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int rows = (int)Math.Round(e.NewSize.Height / _term.Cell.Height);
            int columns = (int) Math.Round(e.NewSize.Width/_term.Cell.Width);

            if (_term.Cells.Count != rows && _term.Cells[0].Count != columns)
                _neovim.ui_try_resize(columns, rows);
        }
    }

    public class Terminal : Canvas
    {
        public class Caret
        {
            public Cell CaretRectangle { get; set; }
            public int Row { get; set; }
            public int Column { get; set; }

            private Terminal _term; 

            public Caret(Terminal term)
            {
                _term = term;

                RenderProperties renderProperties = new RenderProperties();
                renderProperties.Background = new SolidColorBrush(Color.FromRgb(248, 248, 240));
                renderProperties.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                CaretRectangle = new Cell(renderProperties);
                Row = 0;
                Column = 0;
            }

            public void MoveCaret(int row, int col)
            {
                this.Row = row;
                this.Column = col;
                this.CaretRectangle.Text = _term.Cells[row][col].Text;
                Canvas.SetLeft(this.CaretRectangle, Canvas.GetLeft(_term.Cells[row][col]));
                Canvas.SetTop(this.CaretRectangle, Canvas.GetTop(_term.Cells[row][col]));
            }

            public void MoveCaretRight()
            {
                if (this.Column == _term.Cells[0].Count - 1)
                    MoveCaret(this.Row + 1, 0);
                else
                    MoveCaret(this.Row, this.Column + 1);
            }

            public void MoveCaretLeft()
            {
                if (this.Column == 0)
                    MoveCaret(this.Row - 1, _term.Cells[0].Count - 1);
                else
                    MoveCaret(this.Row, this.Column - 1);
            }
        }

        public Window ParentWindow { get; set; }
        public List<List<Cell>> Cells;
        public Caret TerminalCursor;

        private RenderProperties _renderProperties;
        
        public Cell Cell
        {
            get { return Cells[0][0]; }
        }

        public Terminal(RenderProperties renderProperties)
        {
            _renderProperties = renderProperties;

            Cells = new List<List<Cell>>(24);
            for (int i = 0; i < 24; i++)
            {
                Cells.Add(new List<Cell>(80));
                for (int j = 0; j < 80; j++)
                {
                    Cells[i].Add(new Cell(renderProperties));
                    Cells[i][j].UseLayoutRounding = true;
                    Cells[i][j].Text = "X";
                    this.Children.Add(Cells[i][j]);
                    Canvas.SetLeft(Cells[i][j], j * Cells[i][j].Width);
                    Canvas.SetTop(Cells[i][j], i * Cells[i][j].Height);
                    Panel.SetZIndex(Cells[i][j], 0);
                }
            }

            TerminalCursor = new Caret(this);
            TerminalCursor.CaretRectangle.Width = Cells[0][0].Width;
            TerminalCursor.CaretRectangle.Height = Cells[0][0].Height;
            this.Children.Add(TerminalCursor.CaretRectangle);
            TerminalCursor.MoveCaret(0, 0);
            Panel.SetZIndex(TerminalCursor.CaretRectangle, 1);
        }

        public void Scroll(byte direction)
        {
            // Scroll up
            if (direction == 1)
            {
                for (int i = 0; i < Cells.Count - 1; i++)
                {
                    Cells[i] = Cells[i + 1];
                }

                for (int i = 0; i < Cells[Cells.Count - 1].Count; i++)
                {
                    Cells[Cells.Count-1][i] = new Cell(_renderProperties);
                }
            }

            // Scroll down
            if (direction == -1)
            {
            }
        }
        public void Highlight(Color foreground, bool bold, bool italic)
        {
            _renderProperties.Foreground = new SolidColorBrush(foreground);
            _renderProperties.TypeFace = new Typeface(new FontFamily("Anonymice_Powerline"), italic ? FontStyles.Italic : FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
            Cells[TerminalCursor.Row][TerminalCursor.Column].InvalidateVisual();
        }

        public void PutText(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                Cells[TerminalCursor.Row][TerminalCursor.Column].Text = text[i].ToString();
                TerminalCursor.MoveCaretRight();
            }
        }

        public void Clear()
        {
            for (int i = 0; i < Cells.Count; i++)
            {
                for (int j = 0; j < Cells[i].Count; j++)
                    Cells[i][j].Text = "";
            }
        }

        public void ClearToEnd()
        {
            for (int i = TerminalCursor.Column; i < Cells[0].Count; i++)
            {
                Cells[TerminalCursor.Row][i].Text = "";
            }
        }

        public void Resize(int rows, int columns)
        {
            if (Cells.Count == rows && Cells[0].Count == columns)
                return;

            if (Cells.Count > rows)
            {
                Cells.RemoveRange(rows, Cells.Count - rows);
            }

            else if (Cells.Count < rows)
            {
                for (int i = rows - Cells.Count; i > 0; i--)
                {
                    Cells.Add(new List<Cell>(Cells[0].Count));
                }
            }

            if (Cells[0].Count > columns)
            {
                int max = Cells.Count;
                for (int i = 0; i < max; i++)
                {
                    Cells[i].RemoveRange(columns, Cells[i].Count - columns);
                }
            }

            else if (Cells[0].Count < columns)
            {
                int max = columns - Cells[0].Count;
                for (int i = 0; i < Cells.Count; i++)
                {
                    for (int j = max; j > 0; j--)
                    {
                        var cell = new Cell(_renderProperties);
                        cell.Text = " ";
                        Cells[i].Add(cell);
                    }
                   
                }
            }
        }
    }

    public class RenderProperties
    {
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
        public Typeface TypeFace { get; set; }

        public RenderProperties()
        {
            this.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            this.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            this.TypeFace = new Typeface(new FontFamily("Anonymice_Powerline"), FontStyles.Normal, FontWeights.Normal, FontStretches.Condensed);
        }
    }

    public class Cell : FrameworkElement
    {
        private string _text;

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                Background = RenderProps.Background;
                Foreground = RenderProps.Foreground;
                TypeFace = RenderProps.TypeFace;
                InvalidateVisual();
            }
        }

        public RenderProperties RenderProps { get; set; }
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
        public Typeface TypeFace { get; set; }
        public double FontSize { get; set; }

        public Cell(RenderProperties renderProperties)
        {
            this.RenderProps = renderProperties;
            this._text = " ";
            this.TypeFace = new Typeface(new FontFamily("Anonymice_Powerline"), FontStyles.Normal, FontWeights.Normal, FontStretches.Condensed);
            this.FontSize = 12;
            this.Width = this.FontSize - 4;
            this.Height = Math.Round(this.FontSize * this.TypeFace.FontFamily.LineSpacing);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(Background, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            var formattedText = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, TypeFace, FontSize, Foreground);
            drawingContext.DrawText(formattedText, new Point(0, 0));
        }
    }
}
