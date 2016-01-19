using System;
using System.Collections.Generic;
using System.Globalization;
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
                Row = row;
                Column = col;
                CaretRectangle.Text = _term.Cells[row][col].Text;
                SetLeft(CaretRectangle, GetLeft(_term.Cells[row][col]));
                SetTop(CaretRectangle, GetTop(_term.Cells[row][col]));
            }

            public void MoveCaretRight()
            {
                if (Column == _term.Cells[0].Count - 1)
                    MoveCaret(Row + 1, 0);
                else
                    MoveCaret(Row, Column + 1);
            }

            public void MoveCaretLeft()
            {
                if (Column == 0)
                    MoveCaret(Row - 1, _term.Cells[0].Count - 1);
                else
                    MoveCaret(Row, Column - 1);
            }
        }

        public Window ParentWindow { get; set; }
        public List<List<Cell>> Cells;
        public Caret TCursor;
        private RenderProperties _renderProperties;

        public Cell Cell
        {
            get { return Cells[0][0]; }
        }

        public Terminal()
        {
            _renderProperties = new RenderProperties();

            Cells = new List<List<Cell>>(24);
            for (int i = 0; i < 24; i++)
            {
                Cells.Add(new List<Cell>(80));
                for (int j = 0; j < 80; j++)
                {
                    Cells[i].Add(new Cell(_renderProperties));
                    //Cells[i][j].UseLayoutRounding = true;
                    Cells[i][j].Text = "X";
                    Children.Add(Cells[i][j]);
                    SetLeft(Cells[i][j], j * Cell.Width);
                    SetTop(Cells[i][j], i * Cell.Height);
                    SetZIndex(Cells[i][j], 0);
                }
            }

            TCursor = new Caret(this);
            TCursor.CaretRectangle.Width = Cells[0][0].Width;
            TCursor.CaretRectangle.Height = Cells[0][0].Height;
            Children.Add(TCursor.CaretRectangle);
            TCursor.MoveCaret(0, 0);
            SetZIndex(TCursor.CaretRectangle, 1);
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
            _renderProperties.TypeFace = new Typeface(new FontFamily("Courier New"), italic ? FontStyles.Italic : FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
            Cells[TCursor.Row][TCursor.Column].InvalidateVisual();
        }

        public void PutText(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                Cells[TCursor.Row][TCursor.Column].Text = text[i].ToString();
                TCursor.MoveCaretRight();
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
            for (int i = TCursor.Column; i < Cells[0].Count; i++)
            {
                Cells[TCursor.Row][i].Text = "";
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
            Enabled = true;
            Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            TypeFace = new Typeface(new FontFamily("Courier New"), FontStyles.Normal, FontWeights.Normal, FontStretches.Condensed);
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
                if (RenderProps.Enabled)
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
            RenderProps = renderProperties;
            _text = " ";
            TypeFace = new Typeface(new FontFamily("Courier New"), FontStyles.Normal, FontWeights.Normal, FontStretches.Condensed);
            FontSize = 12;
            Width = FontSize;
            Height = Math.Ceiling(FontSize * TypeFace.FontFamily.LineSpacing);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(Background, null, new Rect(0, 0, Width, Height));
            if (Text != " " && Text != "")
            {
                var formattedText = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, TypeFace, FontSize, Foreground);
                drawingContext.DrawText(formattedText, new Point(0, 0));
            }
        }
    }
}
