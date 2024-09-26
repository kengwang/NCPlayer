using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace HyPlayer.Classes
{
    public static class ColorExtractor
    {
        public static async Task<Color> ExtractColorFromStream(InMemoryRandomAccessStream stream)
        {
            var buffer = new Windows.Storage.Streams.Buffer(MIMEHelper.PICTURE_FILE_HEADER_CAPACITY);
            stream.Seek(0);
            await stream.ReadAsync(buffer, MIMEHelper.PICTURE_FILE_HEADER_CAPACITY, InputStreamOptions.None);
            var mime = MIMEHelper.GetPictureCodecFromBuffer(buffer);
            var decoder = await BitmapDecoder.CreateAsync(mime, stream);
            var colors = await ImageDecoder.GetPixelColor(decoder);
            var color = await Common.PaletteGenerator.CreateThemeColor(colors);
            return Color.FromArgb(255, (byte)color.Color.X, (byte)color.Color.Y, (byte)color.Color.Z);
        }
    }
}
