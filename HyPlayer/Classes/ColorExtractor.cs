using Impressionist.Abstractions;
using Impressionist.Implementations;
using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace HyPlayer.Classes
{
    public static class ColorExtractor
    {
        public static async Task<Color> ExtractColorFromStream(IRandomAccessStream stream)
        {
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var colors = await ImageDecoder.GetPixelColor(decoder);
            ThemeColorResult color;

            if (Common.Setting.ColorGeneratorType is 0)
            {
                color = await PaletteGenerators.KMeansPaletteGenerator.CreateThemeColor(colors, Common.Setting.ImpressionistIgnoreWhite, Common.Setting.ImpressionistLABSpace);
            }
            else
            {
                color = await PaletteGenerators.OctTreePaletteGenerator.CreateThemeColor(colors, Common.Setting.ImpressionistIgnoreWhite);
            }
            return Color.FromArgb(255, (byte)color.Color.X, (byte)color.Color.Y, (byte)color.Color.Z);
        }
    }
}
