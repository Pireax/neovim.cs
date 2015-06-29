using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NeovimGUI
{
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
                    //Cells[i][j].UseLayoutRounding = true;
                    Cells[i][j].Text = "X";
                    this.Children.Add(Cells[i][j]);
                    Canvas.SetLeft(Cells[i][j], j * Cell.Width);
                    Canvas.SetTop(Cells[i][j], i * Cell.Height);
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

        public void Scroll(sbyte direction)
        {
            _renderProperties.Enabled = false;
            // Scroll up
            if (direction == 1)
            {
                for (int i = 0; i < Cells[Cells.Count - 1].Count; i++)
                {
                    Cells[Cells.Count - 1][i] = new Cell(_renderProperties);
                    Cells[Cells.Count - 1][i].Background = _renderProperties.Background;
                    
                }

                for (int i = 0; i < Cells.Count - 1; i++)
                {
                    for (int j = 0; j < Cells[i].Count; j++)
                    {
                        Cells[i][j].Background = Cells[i + 1][j].Background;
                        Cells[i][j].Foreground = Cells[i + 1][j].Foreground;
                        Cells[i][j].TypeFace = Cells[i + 1][j].TypeFace;
                        Cells[i][j].Text = Cells[i + 1][j].Text;
                    }
                }


            }

            // Scroll down
            if (direction == -1)
            {
                for (int i = Cells.Count - 1; i > 0; i--)
                {
                    for (int j = 0; j < Cells[i].Count; j++)
                    {
                        Cells[i][j].Background = Cells[i - 1][j].Background;
                        Cells[i][j].Foreground = Cells[i - 1][j].Foreground;
                        Cells[i][j].TypeFace = Cells[i - 1][j].TypeFace;
                        Cells[i][j].Text = Cells[i - 1][j].Text;
                    }
                }
                for (int i = 0; i < Cells[0].Count; i++)
                {
                    Cells[0][i] = new Cell(_renderProperties);
                    Cells[0][i].Background = _renderProperties.Background;
                }
            }
            _renderProperties.Enabled = true;
        }
        public void Highlight(Color foreground, bool bold, bool italic)
        {
            _renderProperties.Foreground = new SolidColorBrush(foreground);
            _renderProperties.TypeFace = new Typeface(new FontFamily("Courier"), italic ? FontStyles.Italic : FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
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
        public bool Enabled { get; set; }

        public RenderProperties()
        {
            this.Enabled = true;
            this.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            this.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            this.TypeFace = new Typeface(new FontFamily("Courier"), FontStyles.Normal, FontWeights.Normal, FontStretches.Condensed);
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
                if (RenderProps.Enabled == true)
                {
                    Background = RenderProps.Background;
                    Foreground = RenderProps.Foreground;
                    TypeFace = RenderProps.TypeFace;
                }
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
            this.TypeFace = new Typeface(new FontFamily("Courier"), FontStyles.Normal, FontWeights.Normal, FontStretches.Condensed);
            this.FontSize = 12;
            this.Width = this.FontSize;
            this.Height = Math.Ceiling(this.FontSize * this.TypeFace.FontFamily.LineSpacing);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(Background, null, new Rect(0, 0, this.Width, this.Height));
            var formattedText = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, TypeFace, FontSize, Foreground);
            drawingContext.DrawText(formattedText, new Point(0, 0));
        }
    }
}
