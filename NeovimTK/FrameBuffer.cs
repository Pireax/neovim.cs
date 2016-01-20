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

            GL.Ext.GenFramebuffers(1, out Handle);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, Handle);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.Texture2D, Texture.Handle, 0);

            FramebufferErrorCode errorCode = GL.Ext.CheckFramebufferStatus(FramebufferTarget.FramebufferExt);

            if (errorCode != FramebufferErrorCode.FramebufferCompleteExt)
                throw new Exception($"Framebuffer construction failed with error: {errorCode}");

            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
        }

        public void Bind()
        {
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, Handle);
        }

        public void Dispose()
        {
            var value = Handle;
            GL.Ext.DeleteFramebuffers(1, ref value);
        }
    }
}
