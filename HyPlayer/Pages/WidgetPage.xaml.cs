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
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using HyPlayer.Classes;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Helpers;

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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);


        HyPlayList.OnLyricLoaded += OnPlaylistLyricLoaded;
        _widget = e.Parameter as XboxGameBarWidget;
        _widget.WindowBoundsChanged += OnResized;       
        
        _hotkeyWatcher = XboxGameBarHotkeyWatcher.CreateWatcher(_widget, [VirtualKey.Control, VirtualKey.LeftMenu, VirtualKey.A]);//全局热键
        _hotkeyWatcher.Start();
        _hotkeyWatcher.HotkeySetStateChanged += OnHotkeySetStateChanged;
        InitializeLyricView();
        LoadLyrics();

        this.PointerEntered += WidgetPage_PointerEntered;
        this.PointerExited += WidgetPage_PointerExited;
        HyPlayList.OnPlayItemChange += HyPlayList_OnPlayItemChange;
        HyPlayList.OnPlayPositionChange += HyPlayList_OnPlayPositionChange;
        ChangePlayStateButton.Click += ChangePlayStateButton_Click;
        MoveNextButton.Click += MoveNextButton_Click; 
        MovePreviousButton.Click += MovePreviousButton_Click;
    }


    private void HyPlayList_OnPlayPositionChange(TimeSpan position)
    {
        var view = CoreApplication.Views[0];

        view.ExecuteOnUIThreadAsync(() =>
        {
            var txt = HyPlayList.NowPlayingItem.PlayItem.Name;
            PositionProgressBar.Value = position.TotalMilliseconds / HyPlayList.NowPlayingItem.PlayItem.LengthInMilliseconds * 100;
            CurrentPositionText.Text = $"{position.ToString(@"mm\:ss")}/{TimeSpan.FromMilliseconds(HyPlayList.NowPlayingItem.PlayItem.LengthInMilliseconds).ToString(@"mm\:ss")}";
        });
    }

    private void HyPlayList_OnPlayItemChange(HyPlayItem playItem)
    {
        var view = CoreApplication.Views[0];

        view.ExecuteOnUIThreadAsync(() =>
        {
            var txt = HyPlayList.NowPlayingItem.PlayItem.Name;
            SongNameText.Text = HyPlayList.NowPlayingItem.PlayItem.Name;
            ArtistText.Text = HyPlayList.NowPlayingItem.PlayItem.ArtistString;
        });
    }

    private async void MovePreviousButton_Click(object sender, RoutedEventArgs e)
    {
        await HyPlayList.SongFadeRequest(HyPlayList.SongFadeEffectType.UserNextFadeOut, HyPlayList.SongChangeType.Previous);
    }

    private async void MoveNextButton_Click(object sender, RoutedEventArgs e)
    {
        await HyPlayList.SongFadeRequest(HyPlayList.SongFadeEffectType.UserNextFadeOut, HyPlayList.SongChangeType.Next);
    }

    private async void ChangePlayStateButton_Click(object sender, RoutedEventArgs e)
    {
        await ChangePlayState();
    }

    private void WidgetPage_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        BorderBackground.Visibility = PlayBar.Visibility = Visibility.Collapsed;
    }

    private void WidgetPage_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        BorderBackground.Visibility = PlayBar.Visibility = Visibility.Visible;
    }

    private void OnPlaylistLyricLoaded()
    {
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
            await ChangePlayState();
        }
    }
    public async Task ChangePlayState()
    {
        if (HyPlayList.IsPlaying) await HyPlayList.SongFadeRequest(HyPlayList.SongFadeEffectType.PauseFadeOut);
        else await HyPlayList.SongFadeRequest(HyPlayList.SongFadeEffectType.PlayFadeIn);
        var view = CoreApplication.Views[0];
        view.ExecuteOnUIThreadAsync(() =>
        {
            PlayStateIcon.Glyph = HyPlayList.IsPlaying ? "\uEDB4" : "\uEDB5";
        });
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
        HyPlayList_OnPlayItemChange(null);
    }

    private void UpdateLyricSize()
    {
        if (HyPlayList.NowPlayingItem == null) return;
        var lyricSize = Common.Setting.lyricSize <= 0
            ? Math.Max(_widget.WindowBounds.Width / 20, 40)
            : Common.Setting.lyricSize;
        var translationSize = (Common.Setting.translationSize > 0) ? Common.Setting.translationSize : lyricSize / 1.8;
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