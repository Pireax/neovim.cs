using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using Brushes = System.Drawing.Brushes;
using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;
using FontStyle = System.Drawing.FontStyle;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace NeovimGUI
{
    class TerminalTst : Image
    {
        private Bitmap bmp;
        private Bitmap _caret;
        private Bitmap final;

        private Graphics g;
        public Rectangle Caret;
        public Color Background { get; set; }
        public Color Foreground { get; set; }
        public Font Font { get; set; }

        public int _cellWidth = 12;
        private int _cellHeight = 16;
        private int rows = 25;
        private int columns = 80;
        public TerminalTst()
        {
            Font = new Font(new FontFamily("Arial"), 12);
            _cellWidth = 12;
            _cellHeight = 16;

            Caret = new Rectangle();
            Caret.Width = _cellWidth;
            Caret.Height = _cellHeight;
            bmp = new Bitmap(80*_cellWidth, 24*_cellHeight);


            g = Graphics.FromImage(bmp);

            Background = Color.DarkSlateGray;
            Foreground = Color.White;

            Width = _cellWidth*columns;
            Height = _cellHeight*rows;
        }

        public void SwapBuffers()
        {
            final = new Bitmap(bmp);
            var f = Graphics.FromImage(final);

            var attrs = new ImageAttributes();
            ColorMatrix m = new ColorMatrix(new float[][]
            {
                new float[] {-1, 0, 0, 0, 0},
                new float[] {0, -1, 0, 0, 0},
                new float[] {0, 0, -1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {1, 1, 1, 0, 1}
            }); 
            attrs.SetColorMatrix(m);

            f.DrawImage(final, new Rectangle((int)Caret.X, (int)Caret.Y, _cellWidth, _cellHeight), Caret.X, Caret.Y, (float)_cellWidth, (float)_cellHeight, GraphicsUnit.Pixel, attrs);

            f.Dispose();

            ImageSource src = Imaging.CreateBitmapSourceFromHBitmap(final.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            this.Source = src;
        }

        public void MoveCaret(int row, int col)
        {
            Caret.X = col*_cellWidth;
            Caret.Y = row*_cellHeight;
        }

        public void PutText(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                g.FillRectangle(new SolidBrush(Background), Caret.X, Caret.Y, _cellWidth, _cellHeight);
                g.DrawString(text[i].ToString(), new Font(new System.Drawing.FontFamily("Arial"), 8), new SolidBrush(Foreground), Caret.X, Caret.Y);
                Caret.X += _cellWidth;
            }
        }

        public void Scroll(sbyte direction)
        {
            if (direction == -1)
            {
                g.DrawImage(bmp, 0, _cellHeight);
                g.FillRectangle(new SolidBrush(Background), Caret.X, Caret.Y, (float)this.Width, _cellHeight);
            }

            else if (direction == 1)
            {
                g.DrawImage(bmp, 0, -_cellHeight);
                g.FillRectangle(new SolidBrush(Background), Caret.X, Caret.Y, (float)this.Width, _cellHeight);
            }
        }

        public void Clear()
        {
            g.Clear(Background);
        }

        public void ClearToEnd()
        {
            g.FillRectangle(new SolidBrush(Background), Caret.X, Caret.Y, (float)this.Width, _cellHeight);
        }

        public void Highlight(Color foreground, bool bold, bool italic)
        {
            Foreground = foreground;
        }

        public void Resize(int rows, int columns)
        {
            
        }
    }
}
