#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Rectangle.cs" company="EloBuddy">
// // 
// //  Marksman AIO
// // 
// //  Copyright (C) 2016 Krystian Tenerowicz
// // 
// //  This program is free software: you can redistribute it and/or modify
// //  it under the terms of the GNU General Public License as published by
// //  the Free Software Foundation, either version 3 of the License, or
// //  (at your option) any later version.
// // 
// //  This program is distributed in the hope that it will be useful,
// //  but WITHOUT ANY WARRANTY; without even the implied warranty of
// //  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// //  GNU General Public License for more details.
// // 
// //  You should have received a copy of the GNU General Public License
// //  along with this program.  If not, see http://www.gnu.org/licenses/. 
// //  </copyright>
// //  <summary>
// // 
// //  Email: geroelobuddy@gmail.com
// //  PayPal: geroelobuddy@gmail.com
// //  </summary>
// //  ---------------------------------------------------------------------
#endregion
using System;
using EloBuddy;
using SharpDX;
using SharpDX.Direct3D9;

namespace Simple_Marksmans.Utils.PermaShow
{
    internal sealed class Rectangle : IDisposable
    {
        private readonly Line _rectangle;
        private Device Device => Drawing.Direct3DDevice;
        public int Width { get; set; }
        public ColorBGRA Color { get; set; }
        public Vector2[] Vectors { get; set; }

        public Rectangle(int width, Vector2[] vectors, ColorBGRA color)
        {
            Width = width;
            Color = color;
            Vectors = vectors;

            _rectangle = new Line(Device) {Width = width > 0 ? width : 1};

            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
        }

        ~Rectangle()
        {
            Dispose();
        }

        public void Dispose()
        {
            _rectangle?.Dispose();
            GC.SuppressFinalize(this);

            Drawing.OnPreReset -= Drawing_OnPreReset;
            Drawing.OnPostReset -= Drawing_OnPostReset;
        }

        public void Draw()
        {
            if (_rectangle == null || _rectangle.IsDisposed || Device == null || Device.IsDisposed || Width < 1)
                return;

            using (this)
            {
                _rectangle.Begin();
                _rectangle.Draw(Vectors, Color);
                _rectangle.End();
            }
        }

        private void Drawing_OnPostReset(EventArgs args)
        {
            if (_rectangle == null || _rectangle.IsDisposed)
                return;

            _rectangle?.OnResetDevice();
        }

        private void Drawing_OnPreReset(EventArgs args)
        {
            if (_rectangle == null || _rectangle.IsDisposed)
                return;

            Dispose();
            _rectangle.OnLostDevice();
        }
    }
}