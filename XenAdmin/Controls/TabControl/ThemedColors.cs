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

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace XenAdmin.Controls.TabControl
{
    internal sealed class ThemedColors
    {
        public enum ColorScheme
        {
            NormalColor = 0,
            HomeStead = 1,
            Metallic = 2,
            NoTheme = 3
        }

        private static ColorScheme GetCurrentThemeIndex()
        {
            var theme = ColorScheme.NoTheme;

            if (VisualStyleInformation.IsSupportedByOS && VisualStyleInformation.IsEnabledByUser &&
                Application.RenderWithVisualStyles)
                switch (VisualStyleInformation.ColorScheme)
                {
                    case NormalColor:
                        theme = ColorScheme.NormalColor;
                        break;
                    case HomeStead:
                        theme = ColorScheme.HomeStead;
                        break;
                    case Metallic:
                        theme = ColorScheme.Metallic;
                        break;
                    default:
                        theme = ColorScheme.NoTheme;
                        break;
                }

            return theme;
        }

        #region "    Variables and Constants "

        private const string NormalColor = "NormalColor";
        private const string HomeStead = "HomeStead";
        private const string Metallic = "Metallic";
        private const string NoTheme = "NoTheme";

        private static readonly Color[] _toolBorder;

        #endregion

        #region "    Properties "

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static ColorScheme CurrentThemeIndex => GetCurrentThemeIndex();

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static Color ToolBorder => _toolBorder[(int)CurrentThemeIndex];

        #endregion

        #region "    Constructors "

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static ThemedColors()
        {
            _toolBorder = new[]
            {
                Color.FromArgb(127, 157, 185), Color.FromArgb(164, 185, 127), Color.FromArgb(165, 172, 178),
                Color.FromArgb(132, 130, 132)
            };
        }

        private ThemedColors()
        {
        }

        #endregion
    }
}