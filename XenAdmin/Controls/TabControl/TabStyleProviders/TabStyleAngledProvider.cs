/* Copyright (c) XCP-ng Project. 
 * 
 * Redistribution and use in source and binary forms, 
 * with or without modification, are permitted provided 
 * that the following conditions are met: 
 * 
 * *   Redistributions of source code must retain the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer. 
 * *   Redistributions in binary form must reproduce the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer in the documentation and/or other 
 *     materials provided with the distribution. 
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
 * SUCH DAMAGE.
 */

using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace XenAdmin.Controls.TabControl.TabStyleProviders
{
    [ToolboxItem(false)]
    public class TabStyleAngledProvider : TabStyleProvider
    {
        public TabStyleAngledProvider(CustomTabControl tabControl) : base(tabControl)
        {
            _ImageAlign = ContentAlignment.MiddleRight;
            _Overlap = 7;
            _Radius = 10;

            //	Must set after the _Radius as this is used in the calculations of the actual padding
            Padding = new Point(10, 3);
        }

        public override void AddTabBorder(GraphicsPath path, Rectangle tabBounds)
        {
            switch (_TabControl.Alignment)
            {
                case TabAlignment.Top:
                    path.AddLine(tabBounds.X, tabBounds.Bottom, tabBounds.X + _Radius - 2, tabBounds.Y + 2);
                    path.AddLine(tabBounds.X + _Radius, tabBounds.Y, tabBounds.Right - _Radius, tabBounds.Y);
                    path.AddLine(tabBounds.Right - _Radius + 2, tabBounds.Y + 2, tabBounds.Right, tabBounds.Bottom);
                    break;
                case TabAlignment.Bottom:
                    path.AddLine(tabBounds.Right, tabBounds.Y, tabBounds.Right - _Radius + 2, tabBounds.Bottom - 2);
                    path.AddLine(tabBounds.Right - _Radius, tabBounds.Bottom, tabBounds.X + _Radius, tabBounds.Bottom);
                    path.AddLine(tabBounds.X + _Radius - 2, tabBounds.Bottom - 2, tabBounds.X, tabBounds.Y);
                    break;
                case TabAlignment.Left:
                    path.AddLine(tabBounds.Right, tabBounds.Bottom, tabBounds.X + 2, tabBounds.Bottom - _Radius + 2);
                    path.AddLine(tabBounds.X, tabBounds.Bottom - _Radius, tabBounds.X, tabBounds.Y + _Radius);
                    path.AddLine(tabBounds.X + 2, tabBounds.Y + _Radius - 2, tabBounds.Right, tabBounds.Y);
                    break;
                case TabAlignment.Right:
                    path.AddLine(tabBounds.X, tabBounds.Y, tabBounds.Right - 2, tabBounds.Y + _Radius - 2);
                    path.AddLine(tabBounds.Right, tabBounds.Y + _Radius, tabBounds.Right, tabBounds.Bottom - _Radius);
                    path.AddLine(tabBounds.Right - 2, tabBounds.Bottom - _Radius + 2, tabBounds.X, tabBounds.Bottom);
                    break;
            }
        }
    }
}