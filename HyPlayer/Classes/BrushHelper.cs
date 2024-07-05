using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace HyPlayer.Classes
{
    public static class BrushHelper
    {
        public static Windows.UI.Color? GetKaraokAccentBrush()
        {
            if (Common.Setting.karaokLyricFocusingColor is not null)
            {
                return Common.Setting.karaokLyricFocusingColor;
            }

            return Common.PageExpandedPlayer != null
                ? Common.PageExpandedPlayer.ForegroundAccentTextBrush.Color
                : (Application.Current.Resources["SystemControlPageTextBaseHighBrush"] as SolidColorBrush)!.Color;
        }

        public static SolidColorBrush GetAccentBrush()
        {
            if (Common.Setting.pureLyricFocusingColor is not null)
            {
                return new SolidColorBrush(Common.Setting.pureLyricFocusingColor.Value);
            }

            return (Common.PageExpandedPlayer != null
                ? Common.PageExpandedPlayer.ForegroundAccentTextBrush
                : Application.Current.Resources["SystemControlPageTextBaseHighBrush"] as SolidColorBrush)!;
        }

        public static SolidColorBrush GetIdleBrush()
        {
            if (Common.Setting.pureLyricIdleColor is not null)
            {
                return  new SolidColorBrush(Common.Setting.pureLyricIdleColor.Value);
            }

            return (Common.PageExpandedPlayer != null
                ? Common.PageExpandedPlayer.ForegroundIdleTextBrush
                : Application.Current.Resources["TextFillColorTertiaryBrush"] as SolidColorBrush)!;
        }
    }
}
