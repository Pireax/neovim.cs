using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using QuickFont;

namespace NeovimDemo
{
    public class FontGroup
    {
        private QFont _normal;
        private QFont _bold;
        private QFont _italic;

        public FontStyle FontStyle;

        public FontGroup(Font font)
        {
            _normal = new QFont(new Font(font, FontStyle.Regular));
            _normal.Options.Monospacing = QFontMonospacing.Yes;

            _bold = new QFont(new Font(font, FontStyle.Bold));
            _bold.Options.Monospacing = QFontMonospacing.Yes;

            _italic = new QFont(new Font(font, FontStyle.Italic));
            _italic.Options.Monospacing = QFontMonospacing.Yes;
        }

        public float MonoSpaceWidth => _normal.MonoSpaceWidth;
        public float LineSpacing => _normal.LineSpacing;

        public void Print(string text, Vector2 position)
        {
            if (FontStyle == FontStyle.Regular)
                _normal.Print(text, position);
            else if ((FontStyle & FontStyle.Bold) > 0)
                _bold.Print(text, position);
            else if ((FontStyle & FontStyle.Italic) > 0)
                _italic.Print(text, position);
        }

        public SizeF Measure(string text)
        {
            return _normal.Measure(text);
        }

        public Color4 Color
        {
            set
            {
                _normal.Options.Colour = value;
                _bold.Options.Colour = value;
                _italic.Options.Colour = value;
            }
            get { return _normal.Options.Colour; }
        }
    }
}
