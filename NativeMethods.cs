//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//using vm = Windows.UI.ViewManagement;

//namespace DigitalClock
//{
//    internal class NativeMethods
//    {
//        [StructLayout(LayoutKind.Sequential)]
//        private struct IMMERSIVE_COLOR_PREFERENCE
//        {
//            public uint dwColorSetIndex;
//            public uint crStartColor;
//            public uint crAccentColor;
//        }

//        [DllImport("uxtheme.dll", EntryPoint = "#122")]
//        static extern int SetUserColorPreference(ref IMMERSIVE_COLOR_PREFERENCE pcpPreference, bool fForceSetting);

//        [DllImport("uxtheme.dll", EntryPoint = "#120")]
//        static extern IntPtr GetUserColorPreference(ref IMMERSIVE_COLOR_PREFERENCE pcpPreference, bool fForceReload);

//        private static uint ToUint(Color c)
//        {
//            return (uint)((c.B << 16) | (c.G << 8) | c.R);
//        }

//        private static Color ToColor(uint c)
//        {
//            int R = (int)(c & 0xFF) % 256;
//            int G = (int)((c >> 8) & 0xFFFF) % 256;
//            int B = (int)(c >> 16) % 256;
//            return Color.FromArgb(R, G, B);
//        }

//        private Color GetAccent()
//        {
//            IMMERSIVE_COLOR_PREFERENCE get = new();
//            GetUserColorPreference(ref get, true);
//            return ToColor(get.crStartColor);
//        }

//        private void SetAccent(Color c)
//        {
//            IMMERSIVE_COLOR_PREFERENCE set = new IMMERSIVE_COLOR_PREFERENCE
//            {
//                dwColorSetIndex = 0,
//                crStartColor = ToUint(c),
//                crAccentColor = ToUint(c)
//            };
//            SetUserColorPreference(ref set, true);
//        }

//        private Color InvertColor(Color c)
//        {
//            //bit-wise not
//            return ToColor(~ToUint(c));
//        }

//        void GetAccentColor()
//        {
//            var settings = new vm.UISettings();
//            var color = settings.GetColorValue(vm.UIColorType.Accent);
//            // color.A, color.R, color.G, and color.B are the color channels.
//        }
//    }
//}
