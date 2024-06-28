using HyPlayer.HyPlayControl;
using HyPlayer.LyricRenderer.RollingCalculators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HyPlayer.LyricRenderer.Converters;
using Microsoft.Gaming.XboxGameBar;
using Windows.UI;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace HyPlayer.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WidgetPage : Page
    {

        private XboxGameBarWidget _widget;
        public WidgetPage()
        {
            this.InitializeComponent();
        }

        private bool _positionChangedBySeeking = false;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HyPlayList.OnPlayItemChange += OnSongChanged;
            _widget = e.Parameter as XboxGameBarWidget;
            InitializeLyricView();
            LoadLyrics();
        }

        private void OnSongChanged(Classes.HyPlayItem playItem)
        {
            LoadLyrics();
        }

        public void InitializeLyricView()
        {
            LyricView.Context.LineRollingEaseCalculator = new ElasticEaseRollingCalculator();
            LyricView.OnBeforeRender += LyricView_OnBeforeRender;
            LyricView.OnRequestSeek += LyricView_OnRequestSeek;
            LyricView.Context.LyricWidthRatio = 1;
            LyricView.Context.LyricPaddingTopRatio = Common.Setting.lyricPaddingTopRatio / 100f;
            LyricView.Context.CurrentLyricTime = 0;
            LyricView.Context.Debug = Common.Setting.LyricRendererDebugMode;
            LyricView.Context.Effects.Blur = Common.Setting.lyricRenderBlur;
            LyricView.Context.LineRollingEaseCalculator = Common.Setting.LineRollingCalculator switch
            {
                1 => new SinRollingCalculator(),
                2 => new LyricifyRollingCalculator(),
                3 => new SyncRollingCalculator(),
                _ => new ElasticEaseRollingCalculator()
            };
            LyricView.Context.Effects.ScaleWhenFocusing = Common.Setting.lyricRenderScaleWhenFocusing;
            LyricView.Context.Effects.FocusHighlighting = Common.Setting.lyricRenderFocusHighlighting;
            LyricView.Context.Effects.TransliterationScanning = Common.Setting.lyricRenderTransliterationScanning;
            LyricView.Context.Effects.SimpleLineScanning = Common.Setting.lyricRenderSimpleLineScanning;
            LyricView.Context.PreferTypography.Font = Common.Setting.lyricFontFamily;
            LyricView.Context.LineSpacing = Common.Setting.lyricLineSpacing;
        }

        private void LyricView_OnRequestSeek(long time)
        {
            HyPlayList.Player.PlaybackSession.Position = TimeSpan.FromMilliseconds(time);
        }

        private void LyricView_OnBeforeRender(LyricRenderer.LyricRenderView view)
        {
            view.Context.IsPlaying = HyPlayList.Player.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing;
            if (HyPlayList.Player.PlaybackSession.Position.TotalMilliseconds < view.Context.CurrentLyricTime)
            {
                view.Context.CurrentLyricTime = (long)HyPlayList.Player.PlaybackSession.Position.TotalMilliseconds;
                LyricView.ReflowTime(0);
            }
            else
            {
                view.Context.CurrentLyricTime = (long)HyPlayList.Player.PlaybackSession.Position.TotalMilliseconds;
            }
            view.Context.IsSeek = _positionChangedBySeeking;
            _positionChangedBySeeking = false;
        }
        public void LoadLyrics()
        {
            //_lyricIsReadyToGo = true;
            //if (_lyricIsCleaning) return;
            LyricView.SetLyricLines(LrcConverter.Convert(ExpandedPlayer.ConvertToALRC(HyPlayList.Lyrics)));
            LyricView.ChangeAlignment(Common.Setting.lyricAlignment switch
            {
                1 => TextAlignment.Center,
                2 => TextAlignment.Right,
                _ => TextAlignment.Left
            });
            LyricView.ReflowTime(0);
            //lastlrcid = HyPlayList.NowPlayingHashCode;
            if (HyPlayList.NowPlayingItem == null) return;
            LyricView.Width = _widget.WindowBounds.Width;
            LyricView.ChangeRenderColor(Colors.Gray, Colors.White);
            LyricView.ChangeRenderFontSize(32, (Common.Setting.translationSize > 0) ? Common.Setting.translationSize : (float)Common.Setting.lyricSize / 2, Common.Setting.romajiSize);
        }
        private SolidColorBrush GetAccentBrush()
        {
            return Application.Current.Resources["SystemControlPageTextBaseHighBrush"] as SolidColorBrush;
        }

        private SolidColorBrush GetIdleBrush()
        {
            return Application.Current.Resources["TextFillColorTertiaryBrush"] as SolidColorBrush;
        }

    }

}
