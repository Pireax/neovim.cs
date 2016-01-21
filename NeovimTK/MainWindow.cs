using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MsgPack;
using Neovim;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK.Platform.Windows;
using QuickFont;

namespace NeovimTK
{
    public partial class MainWindow : Form
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

        private readonly SynchronizationContext _uiContext;
        private NeovimClient _neovim;

        private RectangleF _cursor;
        private RectangleF _scrollRegion;
        private int _rows = 24;
        private int _columns = 80;
        private float _width;
        private float _height;
        private FontGroup _font;
        private Color _fgColor = Color.White;
        private Color _bgColor = Color.DarkSlateGray;

        private FrameBuffer _backBuffer;
        private FrameBuffer _pingPongBuffer;
        private char[] charBuffer;

        public MainWindow()
        {
            InitializeComponent();

            _uiContext = SynchronizationContext.Current;
            _neovim = new NeovimClient(@"C:\Program Files\Neovim\bin\nvim.exe");
            // Event is asynchronous so we need to handle the redraw event in the UI thread
            _neovim.Redraw += (o, args) => _uiContext.Post((x) => NeovimOnRedraw(o, args), null);
            _neovim.ui_attach(_columns, _rows, true);
        }

        private Color ColorFromRgb(int rgb)
        {
            byte r = (byte)(rgb >> 16);
            byte g = (byte)(rgb >> 8);
            byte b = (byte)(rgb >> 0);
            return Color.FromArgb(r, g, b);
        }

        private void NeovimOnRedraw(object sender, NeovimRedrawEventArgs e)
        {
            bool shouldInvalidate = false;
     
            _backBuffer.Bind();
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
                        shouldInvalidate = true;
                        GL.Clear(ClearBufferMask.ColorBufferBit);
                        break;

            //        case "resize":
            //            _term.Resize(args[0][1].AsInt32(), args[0][0].AsInt32());
            //            break;

                    case "update_fg":
                        _font.Color = ColorFromRgb(args[0][0].AsInt32());
                        break;

                    case "update_bg":
                        _bgColor = ColorFromRgb(args[0][0].AsInt32());
                        GL.ClearColor(_bgColor);
                        break;

                    case "highlight_set":
                        foreach (var arg in args)
                        {
                            var dict = arg[0].AsDictionary();

                            foreach (var entry in dict)
                            {
                                var str = entry.Key.AsString(Encoding.Default);
                                if (str == "foreground")
                                    _font.Color = ColorFromRgb(entry.Value.AsInt32());
                                else if (str == "bold")
                                    if (entry.Value.AsBoolean())
                                        _font.FontStyle |= FontStyle.Bold;
                                    else _font.FontStyle &= ~FontStyle.Bold;
                                else if (str == "italic")
                                    if (entry.Value.AsBoolean())
                                        _font.FontStyle |= FontStyle.Italic;
                                    else _font.FontStyle &= FontStyle.Italic;
                            }
                        }
                        break;

                    case "eol_clear":
                        shouldInvalidate = true;
                        DrawRectangle(new RectangleF(_cursor.X, _cursor.Y, _columns * _width - _cursor.X, _height), _bgColor);
                        break;

                    case "set_title":
                        Text = args[0][0].AsString(Encoding.Default);
                        break;

                    case "put":
                        shouldInvalidate = true;
                        List<byte> bytes = new List<byte>();

                        foreach (var arg in args)
                            bytes.AddRange(arg[0].AsBinary());

                        var text = Encoding.Default.GetString(bytes.ToArray());
                        var tSize = _font.Measure(text);
                        
                        DrawRectangle(new RectangleF(_cursor.Location, tSize), _bgColor);

                        GL.Enable(EnableCap.Blend);
                        _font.Print(text, new Vector2(_cursor.X, _cursor.Y));
                        GL.Disable(EnableCap.Blend);
                        GL.Color4(Color.White);

                        _cursor.X += tSize.Width;
                        if (_cursor.X >= _columns*_width) // Dont know if this is needed
                        {
                            _cursor.X = 0;
                            _cursor.Y += _height;
                        }
                        break;

                    case "cursor_goto":
                        shouldInvalidate = true;
                        _cursor.Y = args[0][0].AsInt32() * _height;
                        _cursor.X = args[0][1].AsInt32() * _width;
                        break;

                    case "scroll":
                        // Amount to scroll
                        var count = args[0][0].AsSByte();

                        // Scroll up
                        if (count == 1)
                        {
                            _pingPongBuffer.Bind();

                            _backBuffer.Texture.Bind();

                            // DrawTexturedRectangle is bugged
                            DrawTexturedRectangle(new RectangleF(0, 0, glControl.Width, glControl.Height - _height),
                                                  new RectangleF(0, 0, glControl.Width, glControl.Height - _height));
                            Texture2D.Unbind();

                            DrawRectangle(new RectangleF(0, glControl.Height - _height * count, glControl.Width, _height), _bgColor);

                            // Swap buffers
                            var buffer = _backBuffer;
                            _backBuffer = _pingPongBuffer;
                            _pingPongBuffer = buffer;
                        }
                        // Scroll down
                        else if (count == -1)
                        {
                            _pingPongBuffer.Bind();

                            DrawRectangle(new RectangleF(0, 0, glControl.Width, _height), _bgColor);

                            _backBuffer.Texture.Bind();
                            DrawTexturedRectangle(new RectangleF(0, _height, glControl.Width, glControl.Height - _height),
                                                  new RectangleF(0, _height, glControl.Width, glControl.Height - _height));
                            Texture2D.Unbind();

                            // Swap buffers
                            var buffer = _backBuffer;
                            _backBuffer = _pingPongBuffer;
                            _pingPongBuffer = buffer;
                        }
                        break;

                    case "set_scroll_region":
                        var x = args[0][2].AsUInt32() * _width;
                        var y = args[0][0].AsUInt32() * _height;
                        var width = (args[0][3].AsUInt32() + 1) * _width;
                        var height = (args[0][1].AsUInt32() + 1) * _height;

                        _scrollRegion = new RectangleF(x, y, width, height);
                        break;

                    case "mode_change":
                        shouldInvalidate = true;
                        var mode = args[0][0].AsString(Encoding.Default);
                        if (mode == "insert")
                            _cursor.Width = _width / 4;
                        else if (mode == "normal")
                            _cursor.Width = _width;
                        break;

            //        case "busy_start":
            //            break;

            //        case "busy_stop":
            //            break;

            //        case "mouse_on":
            //            break;

            //        case "mouse_off":
            //            break;
                }
            }
            FrameBuffer.Unbind();
            if (shouldInvalidate)
                glControl.Invalidate();
        }


        private void glControl_Load(object sender, EventArgs e)
        {
            _font = new FontGroup(glControl.Font);
            _font.Color = _fgColor;

            _width = _font.MonoSpaceWidth;
            _height = _font.LineSpacing;
            glControl.Width = Convert.ToInt32(_width * _columns);
            glControl.Height = Convert.ToInt32(_height * _rows);

            _cursor = new RectangleF(0, 0, _width, _height);

            _backBuffer = new FrameBuffer(glControl.Width, glControl.Height);
            _pingPongBuffer = new FrameBuffer(glControl.Width, glControl.Height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, glControl.Width, glControl.Height, 0, -1, 1);
            GL.Viewport(0, 0, glControl.Width, glControl.Height);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Lighting);
            GL.ClearColor(_bgColor);
        }

        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            _backBuffer.Texture.Bind();

            GL.Begin(PrimitiveType.Quads);

            // Backbuffer needs inverted TexCoords, origin of TexCoords is bottom-left corner
            GL.TexCoord2(0, 1); GL.Vertex2(0, 0);
            GL.TexCoord2(1, 1); GL.Vertex2(glControl.Width, 0);
            GL.TexCoord2(1, 0); GL.Vertex2(glControl.Width, glControl.Height);
            GL.TexCoord2(0, 0); GL.Vertex2(0, glControl.Height);

            GL.End();

            Texture2D.Unbind();

            // Invert cursor color depending on the background for now
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.OneMinusDstColor, BlendingFactorDest.Zero);
            DrawRectangle(_cursor, Color.White);
            GL.Disable(EnableCap.Blend);

            glControl.SwapBuffers();
        }

        private void DrawRectangle(RectangleF rect, Color color)
        {
            GL.Color3(color);

            GL.Begin(PrimitiveType.Quads);

            GL.Vertex2(rect.X, rect.Y);
            GL.Vertex2(rect.X + rect.Width, rect.Y);
            GL.Vertex2(rect.X + rect.Width, rect.Y + rect.Height);
            GL.Vertex2(rect.X, rect.Y + rect.Height);

            GL.End();

            GL.Color4(Color.White);
        }

        private void DrawTexturedRectangle(RectangleF rectSrc, RectangleF rectDst)
        {
            GL.Begin(PrimitiveType.Quads);

            var wScale = 1.0f/glControl.Width;
            var hScale = 1.0f/glControl.Height;

            GL.TexCoord2(wScale * rectSrc.X, hScale * (rectSrc.Y + rectSrc.Height));              GL.Vertex2(rectDst.X, rectDst.Y);
            GL.TexCoord2(wScale * (rectSrc.X + rectSrc.Width), hScale * (rectSrc.Y + rectSrc.Height)); GL.Vertex2(rectDst.X + rectDst.Width, rectDst.Y);
            GL.TexCoord2(wScale * (rectSrc.X + rectSrc.Width), hScale * rectSrc.Y);               GL.Vertex2(rectDst.X + rectDst.Width, rectDst.Y + rectDst.Height);
            GL.TexCoord2(wScale * rectSrc.X, hScale * rectSrc.Y);                            GL.Vertex2(rectDst.X, rectDst.Y + rectDst.Height);

            GL.End();
        }

        private void glControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Alt || e.KeyCode == Keys.ControlKey)
                return;

            string keys = Input.Encode((int)e.KeyCode);
            if (keys != null)
                _neovim.vim_input(keys);

            e.Handled = true;
        }
    }
}
