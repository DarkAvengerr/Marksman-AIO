using System;
using EloBuddy;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;

namespace Simple_Marksmans.Utils.PermaShow
{
    internal sealed class Text : IDisposable
    {
        public Font Font { get; set; }

        public uint Height { get; set; }
        public ColorBGRA Color { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Message { get; set; }

        public Text(string message, uint height, int x, int y, ColorBGRA color, bool italic = false,
            bool bold = false, FontQuality quality = FontQuality.Antialiased)
        {
            Height = height;
            Color = color;
            X = x;
            Y = y;
            Message = message;

            Font = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Gill Sans MT Pro Medium",
                    Height = (int) height,
                    Quality = quality,
                    Italic = italic,
                    Weight = bold ? FontWeight.Bold : FontWeight.Regular
                });

            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
        }

        public Text(string message, string faceName, uint height, int x, int y, ColorBGRA color,
            bool italic = false, bool bold = false, FontQuality quality = FontQuality.Antialiased)
        {
            Height = height;
            Color = color;
            X = x;
            Y = y;
            Message = message;

            Font = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = faceName,
                    Height = (int) height,
                    Quality = quality,
                    Italic = italic,
                    Weight = bold ? FontWeight.Bold : FontWeight.Regular
                });

            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
        }

        ~Text()
        {
            Dispose();
        }

        public void Dispose()
        {
            Font?.Dispose();
            GC.SuppressFinalize(this);

            Drawing.OnPreReset -= Drawing_OnPreReset;
            Drawing.OnPostReset -= Drawing_OnPostReset;
        }
        /*
        public void Draw()
        {
            if (Font == null || Font.IsDisposed || Device == null || Device.IsDisposed || Height < 1 || string.IsNullOrEmpty(Message))
                return;

            using (this)
            {
                Font.DrawText(null, Message, X, Y, Color);
            }
        }*/

        public SharpDX.Rectangle GetTextRectangle()
        {
            return Font.MeasureText(null, Message, FontDrawFlags.Right);
        }

        private void Drawing_OnPostReset(EventArgs args)
        {
            if (Font == null || Font.IsDisposed)
                return;

            Font?.OnResetDevice();
        }

        private void Drawing_OnPreReset(EventArgs args)
        {
            if (Font == null || Font.IsDisposed)
                return;

            Dispose();
            Font?.OnLostDevice();
        }
    }
}