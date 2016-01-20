using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace NeovimTK
{
    public class FrameBuffer : IDisposable
    {
        public readonly uint Handle;
        public readonly Texture2D Texture;

        public readonly int Width;
        public readonly int Height;

        public FrameBuffer(int width, int height)
        {
            Width = width;
            Height = height;

            Texture = new Texture2D(Width, Height);

            GL.GenFramebuffers(1, out Handle);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture.Handle, 0);

            FramebufferErrorCode errorCode = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            if (errorCode != FramebufferErrorCode.FramebufferComplete)
                throw new Exception($"Framebuffer construction failed with error: {errorCode}");

            GL.DrawBuffer((DrawBufferMode)FramebufferAttachment.ColorAttachment0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Bind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
            GL.PushAttrib(AttribMask.ViewportBit);
            GL.Viewport(0, 0, Width, Height);
        }

        public static void Unbind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.DrawBuffer(DrawBufferMode.Back);
            GL.PopAttrib();
        }

        public void Dispose()
        {
            var value = Handle;
            GL.DeleteFramebuffers(1, ref value);
        }
    }
}
