//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace DigitalClock
//{
//    internal class ColorMaster
//    {
//        //https://stackoverflow.com/a/78883062/561256
//        const int accuracy = 24; //24 is an upper-bound estimate depending on the ColorModel
//        public static Color[] GetContrastingColors<ColorModel>(Color backgroundColor, Color textColor, double contrast, ChangingMode changingMode) where ColorModel : IContrastAdjustable<ColorModel>
//        {
//            Color[] colors = new Color[2];
//            colors[0] = backgroundColor;
//            colors[1] = textColor;

//            Color[] lightAndDarkAsColors = new Color[2];

//            double backL = RelativeLuminance(backgroundColor);
//            double textL = RelativeLuminance(textColor);

//            bool preferChangingLight = false;
//            bool darkFixed, lightFixed;
//            bool textIsLight = textL > backL;

//            // See if we have good enough contrast already
//            if (!(ColorContrast(backgroundColor, textColor) < contrast))
//            {
//                return colors;
//            }
//            ColorModel light, dark;

//            ColorModel verydark, verylight;

//            if (changingMode == ChangingMode.ForegroundFixed || changingMode == ChangingMode.BackgroundFixed)
//            {
//                //decide which colors we change
//                lightAndDarkAsColors[0] = textIsLight ? textColor : backgroundColor;
//                lightAndDarkAsColors[1] = textIsLight ? backgroundColor : textColor;
//                Color colorFixed = changingMode == ChangingMode.ForegroundFixed ? textColor : backgroundColor;
//                Color colorChangeable = changingMode == ChangingMode.ForegroundFixed ? backgroundColor : textColor;
//                double rw = ColorContrast(Color.White, colorFixed);
//                double rb = ColorContrast(colorFixed, Color.Black);
//                preferChangingLight = rw > rb;

//                if ((!(preferChangingLight ^ (changingMode == ChangingMode.ForegroundFixed))) == textIsLight) //avoid testing the same colors twice
//                {
//                    //if one color is fixed, high contrast can be achieve by increasing or by decreasing the Luminosity of the other Color //Jochla
//                    //so we test if we can achieve a good result while keeping the dark color dark and the light color light
//                    lightFixed = !(changingMode == ChangingMode.ForegroundFixed ^ textIsLight);
//                    darkFixed = !(changingMode == ChangingMode.BackgroundFixed ^ textIsLight);

//                    light = ColorModel.FromColor(lightAndDarkAsColors[0]);
//                    dark = ColorModel.FromColor(lightAndDarkAsColors[1]);

//                    verydark = dark.Clone();
//                    verylight = light.Clone();
//                    if (!lightFixed)
//                    {
//                        verylight.Luminosity = ColorModel.MaxLuminosity;
//                    }
//                    if (!darkFixed)
//                    {
//                        verydark.Luminosity = ColorModel.MinLuminosity;
//                    }

//                    //test if we can achieve a solution with the darkest and the lightest the color model offers
//                    if (ColorContrast(ColorModel.ToColor(verydark), ColorModel.ToColor(verylight)) >= contrast)
//                    {
//                        //use logarithmic search to find a close solution
//                        double lightDiff = ColorModel.MaxLuminosity - light.Luminosity;
//                        double darkDiff = ColorModel.MinLuminosity + dark.Luminosity;

//                        double LDiff = ColorModel.MinLuminosity;
//                        double RDiff = Math.Max(darkDiff, lightDiff);

//                        double orignialDark = dark.Luminosity;
//                        double orignialLight = light.Luminosity;

//                        for (int i = 0; i < accuracy; i++)
//                        {
//                            double MDiff = (LDiff + RDiff) / 2;
//                            if (!darkFixed)
//                            {
//                                dark.Luminosity = orignialDark - MDiff;
//                            }
//                            if (!lightFixed)
//                            {
//                                light.Luminosity = orignialLight + MDiff;
//                            }

//                            if (ColorContrast(ColorModel.ToColor(light), ColorModel.ToColor(dark)) >= contrast)
//                            {
//                                RDiff = MDiff;
//                            }
//                            else
//                            {
//                                LDiff = MDiff;
//                            }
//                        }
//                        //RDiff is the smallest Luminosity we found with our accuracy that gives a result above contrast
//                        if (!darkFixed)
//                        {
//                            dark.Luminosity = orignialDark - RDiff;
//                        }
//                        if (!lightFixed)
//                        {
//                            light.Luminosity = orignialLight + RDiff;
//                        }

//                        lightAndDarkAsColors[0] = ColorModel.ToColor(light);
//                        lightAndDarkAsColors[1] = ColorModel.ToColor(dark);
//                    }
//                }

//                //decide which colors we change
//                textIsLight = changingMode == ChangingMode.BackgroundFixed ? preferChangingLight : !preferChangingLight;
//                lightAndDarkAsColors[0] = preferChangingLight ? colorChangeable : colorFixed;
//                lightAndDarkAsColors[1] = preferChangingLight ? colorFixed : colorChangeable;
//                darkFixed = preferChangingLight;
//                lightFixed = !preferChangingLight;

//            }
//            else
//            {
//                //decide which colors we change
//                lightAndDarkAsColors[0] = textIsLight ? textColor : backgroundColor;
//                lightAndDarkAsColors[1] = textIsLight ? backgroundColor : textColor;
//                darkFixed = false;
//                lightFixed = false;
//            }

//            light = ColorModel.FromColor(lightAndDarkAsColors[0]);
//            dark = ColorModel.FromColor(lightAndDarkAsColors[1]);

//            verydark = dark.Clone();
//            verylight = light.Clone();

//            if (!lightFixed)
//            {
//                verylight.Luminosity = ColorModel.MaxLuminosity;
//            }
//            if (!darkFixed)
//            {
//                verydark.Luminosity = ColorModel.MinLuminosity;
//            }

//            //test if we can achieve a solution with the darkest and the lightest the color model offers 
//            if (ColorContrast(ColorModel.ToColor(verydark), ColorModel.ToColor(verylight)) >= contrast)
//            {
//                //use logarithmic search to find a close solution //Jochla
//                double lightDiff = ColorModel.MaxLuminosity - light.Luminosity;
//                double darkDiff = ColorModel.MinLuminosity + dark.Luminosity;

//                double LDiff = ColorModel.MinLuminosity;
//                double RDiff = Math.Max(darkDiff, lightDiff);

//                double orignialDark = dark.Luminosity;
//                double orignialLight = light.Luminosity;

//                for (int i = 0; i < accuracy; i++)
//                {
//                    double MDiff = (LDiff + RDiff) / 2;
//                    if (!darkFixed)
//                    {
//                        dark.Luminosity = orignialDark - MDiff;
//                    }
//                    if (!lightFixed)
//                    {
//                        light.Luminosity = orignialLight + MDiff;
//                    }

//                    if (ColorContrast(ColorModel.ToColor(light), ColorModel.ToColor(dark)) >= contrast)
//                    {
//                        RDiff = MDiff;
//                    }
//                    else
//                    {
//                        LDiff = MDiff;
//                    }
//                }
//                //RDiff is the smallest Luminosity we found with our accuracy that gives a result above contrast
//                if (!darkFixed)
//                {
//                    dark.Luminosity = orignialDark - RDiff;
//                }
//                if (!lightFixed)
//                {
//                    light.Luminosity = orignialLight + RDiff;
//                }

//                lightAndDarkAsColors[0] = ColorModel.ToColor(light);
//                lightAndDarkAsColors[1] = ColorModel.ToColor(dark);
//            }
//            else
//            {
//                //fall back to black or white 

//                if (changingMode == ChangingMode.PreferCloseness)
//                {
//                    //we determine whether both colors are closer to white or black
//                    //average = sum / 2 < (whiteL + blackL) / 2 
//                    changingMode = backL + textL < ColorModel.MinLuminosity + ColorModel.MaxLuminosity ? ChangingMode.PreferBlack : ChangingMode.PreferWhite;
//                }
//                switch (changingMode)
//                {
//                    //if we prefer white we use white
//                    case ChangingMode.PreferWhite:
//                        lightAndDarkAsColors[0] = Color.White;

//                        //Can we achieve any better than black and white?
//                        if (ColorContrast(Color.White, ColorModel.ToColor(verydark)) < contrast)
//                        {
//                            lightAndDarkAsColors[1] = Color.Black;
//                        }
//                        else
//                        {
//                            //use logarithmic search to find a close solution
//                            double LDiff = ColorModel.MinLuminosity;
//                            double RDiff = dark.Luminosity;

//                            double orignialDark = dark.Luminosity;

//                            for (int i = 0; i < accuracy; i++)
//                            {
//                                double MDiff = (LDiff + RDiff) / 2;
//                                dark.Luminosity = orignialDark - MDiff;
//                                if (ColorContrast(ColorModel.ToColor(light), ColorModel.ToColor(dark)) >= contrast)
//                                {
//                                    RDiff = MDiff;
//                                }
//                                else
//                                {
//                                    LDiff = MDiff;
//                                }
//                            }
//                            //RDiff is the smallest Luminosity we found with our accuracy that gives a result above contrast
//                            dark.Luminosity = orignialDark - RDiff;

//                            lightAndDarkAsColors[1] = ColorModel.ToColor(dark);
//                        }
//                        break;
//                    //if we prefer black we use black 
//                    case ChangingMode.PreferBlack:
//                        lightAndDarkAsColors[1] = Color.Black;

//                        //Can we achieve any better than black and white?
//                        if (ColorContrast(ColorModel.ToColor(verylight), Color.Black) < contrast)
//                        {
//                            lightAndDarkAsColors[0] = Color.White;
//                        }
//                        else
//                        {
//                            //use logarithmic search to find a close solution
//                            double LDiff = ColorModel.MinLuminosity;
//                            double RDiff = ColorModel.MaxLuminosity - light.Luminosity;

//                            double orignialLight = light.Luminosity;

//                            for (int i = 0; i < accuracy; i++)
//                            {
//                                double MDiff = (LDiff + RDiff) / 2;
//                                light.Luminosity = orignialLight + MDiff;

//                                if (ColorContrast(ColorModel.ToColor(light), ColorModel.ToColor(dark)) >= contrast)
//                                {
//                                    RDiff = MDiff;
//                                }
//                                else
//                                {
//                                    LDiff = MDiff;
//                                }
//                            }
//                            //RDiff is the smallest Luminosity we found with our accuracy that gives a result above contrast
//                            light.Luminosity = orignialLight + RDiff;

//                            lightAndDarkAsColors[0] = ColorModel.ToColor(light);
//                        }
//                        break;
//                    case ChangingMode.ForegroundFixed:
//                        colors[0] = preferChangingLight ? Color.White : Color.Black;
//                        return colors;

//                    case ChangingMode.BackgroundFixed:
//                        colors[1] = preferChangingLight ? Color.White : Color.Black;
//                        return colors;
//                }
//            }
//            colors[0] = textIsLight ? lightAndDarkAsColors[1] : lightAndDarkAsColors[0];
//            colors[1] = textIsLight ? lightAndDarkAsColors[0] : lightAndDarkAsColors[1];
//            return colors;
//        }
//        public static double RelativeLuminance(Color color)
//        {

//            var r = GetComposantValue(color.R);
//            var g = GetComposantValue(color.G);
//            var b = GetComposantValue(color.B);

//            var l = 0.2126 * r + 0.7152 * g + 0.0722 * b;
//            return l;
//        }
//        public static double GetComposantValue(double a)
//        {
//            var b = a / 255;
//            if (b <= 0.03928)
//            {
//                return b / 12.92;
//            }
//            else
//            {
//                return Math.Pow(((b + 0.055) / 1.055), 2.4);
//            }
//        }
//        public enum ChangingMode
//        {
//            PreferCloseness,
//            PreferWhite,
//            PreferBlack,
//            BackgroundFixed,
//            ForegroundFixed
//        }
//        public interface IContrastAdjustable<T>
//        {
//            abstract double Luminosity { get; set; }
//            abstract static double MinLuminosity { get; }
//            abstract static double MaxLuminosity { get; }

//            abstract static T FromColor(Color color);
//            abstract static Color ToColor(T color);
//            T Clone();
//        }








//        internal class OklabColor : IContrastAdjustable<OklabColor>
//        {
//            private double luminosity = 0;
//            private double a = 0;
//            private double b = 0;

//            public double Luminosity
//            {
//                get { return luminosity; }
//                set { luminosity = value; }
//            }
//            public double A
//            {
//                get { return a; }
//                set { a = value; }
//            }
//            public double B
//            {
//                get { return b; }
//                set { b = value; }
//            }

//            public static double MinLuminosity => 0;

//            public static double MaxLuminosity => 2;

//            public static implicit operator OklabColor(Color color)
//            {
//                OklabColor oklabColor = new OklabColor();

//                double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;
//                double l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b;
//                double m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b;
//                double s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b;

//                double l_ = Math.Cbrt(l);
//                double m_ = Math.Cbrt(m);
//                double s_ = Math.Cbrt(s);

//                oklabColor.luminosity = 0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_;
//                oklabColor.a = 1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_;
//                oklabColor.b = 0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_;
//                return oklabColor;
//            }

//            public static implicit operator Color(OklabColor oklabColor)
//            {
//                double l_ = oklabColor.luminosity + 0.3963377774f * oklabColor.a + 0.2158037573f * oklabColor.b;
//                double m_ = oklabColor.luminosity - 0.1055613458f * oklabColor.a - 0.0638541728f * oklabColor.b;
//                double s_ = oklabColor.luminosity - 0.0894841775f * oklabColor.a - 1.2914855480f * oklabColor.b;
//                double l = l_ * l_ * l_;
//                double m = m_ * m_ * m_;
//                double s = s_ * s_ * s_;

//                double r_ = 0, g_ = 0, b_ = 0;
//                r_ = +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s;
//                g_ = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s;
//                b_ = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s;

//                //gamut mapping
//                double r = 0, g = 0, b = 0;
//                r = r_ < 0 ? 0 : r_ > 1 ? 1 : r_;
//                g = g_ < 0 ? 0 : g_ > 1 ? 1 : g_;
//                b = b_ < 0 ? 0 : b_ > 1 ? 1 : b_;

//                return Color.FromArgb((int)(255 * r), (int)(255 * g), (int)(255 * b));
//            }

//            public void SetRGB(int red, int green, int blue)
//            {
//                OklabColor oklabColor = (OklabColor)Color.FromArgb(red, green, blue);
//                this.luminosity = oklabColor.luminosity;
//                this.a = oklabColor.a;
//                this.b = oklabColor.b;
//            }

//            public static OklabColor FromColor(Color color)
//            {
//                return (OklabColor)color;
//            }

//            public static Color ToColor(OklabColor color)
//            {
//                return (Color)color;
//            }

//            public OklabColor Clone()
//            {
//                return new OklabColor(this.luminosity, this.a, this.b);
//            }

//            public OklabColor() { }
//            public OklabColor(double luminosity, double a, double b)
//            {
//                this.luminosity = luminosity;
//                this.a = a;
//                this.b = b;
//            }
//        }
//    }
//}
