using HyPlayer.HyPlayControl;
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
using Microsoft.Gaming.XboxGameBar.Input;
using Windows.System;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace HyPlayer.Pages;

public sealed partial class WidgetPage : Page
{

    private XboxGameBarWidget _widget;
    private XboxGameBarHotkeyWatcher _hotkeyWatcher;

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
        _widget.WindowBoundsChanged += OnResized;       
        
        _hotkeyWatcher = XboxGameBarHotkeyWatcher.CreateWatcher(_widget, [VirtualKey.Control, VirtualKey.LeftMenu, VirtualKey.A]);

        _hotkeyWatcher.Start();
        _hotkeyWatcher.HotkeySetStateChanged += OnHotkeySetStateChanged;
        InitializeLyricView();
        LoadLyrics();
    }



    private void OnResized(XboxGameBarWidget sender, object args)
    {
        UpdateLyricSize();
    }

    private async void OnHotkeySetStateChanged(XboxGameBarHotkeyWatcher sender, HotkeySetStateChangedArgs args)
    {
        if (args.HotkeySetDown)
        {
            if (HyPlayList.IsPlaying) await HyPlayList.SongFadeRequest(HyPlayList.SongFadeEffectType.PauseFadeOut);
            else await HyPlayList.SongFadeRequest(HyPlayList.SongFadeEffectType.PlayFadeIn);
        }
    }

    private void OnSongChanged(Classes.HyPlayItem playItem)
    {
        LoadLyrics();
    }

    private void InitializeLyricView()
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
        LyricView.ChangeRenderColor(GetIdleBrush().Color, GetAccentBrush().Color);
        UpdateLyricSize();
    }

    private void UpdateLyricSize()
    {
        if (HyPlayList.NowPlayingItem == null) return;
        var lyricSize = Common.Setting.lyricSize <= 0
            ? Math.Max(_widget.WindowBounds.Width / 20, 40)
            : Common.Setting.lyricSize;
        var translationSize = (Common.Setting.translationSize > 0) ? Common.Setting.translationSize : lyricSize / 1.6;
        LyricView.ChangeRenderFontSize((float)lyricSize, (float)translationSize, Common.Setting.romajiSize);
        LyricView.ChangeAlignment(Common.Setting.lyricAlignment switch
        {
            1 => TextAlignment.Center,
            2 => TextAlignment.Right,
            _ => TextAlignment.Left
        });
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

    private void LoadLyrics()
    {
        //_lyricIsReadyToGo = true;
        //if (_lyricIsCleaning) return;
        LyricView.SetLyricLines(LrcConverter.Convert(ExpandedPlayer.ConvertToALRC(HyPlayList.Lyrics)));
        LyricView.ReflowTime(0);
        //lastlrcid = HyPlayList.NowPlayingHashCode;

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