﻿using HyPlayer.Classes;
using HyPlayer.HyPlayControl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using HyPlayer.NeteaseApi;


// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace HyPlayer.Pages;

/// <summary>
///     可用于自身或导航至 Frame 内部的空白页。
/// </summary>
public sealed partial class TestPage : Page
{
    public static readonly DependencyProperty ResourceIdProperty =
        DependencyProperty.Register(nameof(ResourceId), typeof(string), typeof(TestPage), new PropertyMetadata(""));

    private int _teachingTipIndex;


    public TestPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        TbAdditionalApiParameters.Text = JsonConvert.SerializeObject(Common.Setting.ApiAdditionalParameters, Formatting.Indented);
    }

    public string ResourceId
    {
        get => (string)GetValue(ResourceIdProperty);
        set => SetValue(ResourceIdProperty, value);
    }

    private void TestTeachingTip_OnClick(object sender, RoutedEventArgs e)
    {
        Common.AddToTeachingTipLists("TestTeachingTip", _teachingTipIndex++.ToString());
    }

    private void NavigateResourceId(object sender, RoutedEventArgs e)
    {
        _ = Common.NavigatePageResource(ResourceId);
    }

    private async void PlayResourceId(object sender, RoutedEventArgs e)
    {
        await HyPlayList.AppendNcSource(ResourceId);
    }

    private async void DumpDebugInfo_Click(object sender, RoutedEventArgs e)
    {
        var info = JsonConvert.SerializeObject(new DumpInfo
        {
            CurrentSong = HyPlayList.NowPlayingItem,
            CurrentPlaySource = HyPlayList.PlaySourceId,
            CurrentUser = Common.LoginedUser,
            DeviceId = new EasClientDeviceInformation().Id.ToString(),
            IsInBackground = Common.IsInBackground,
            IsUsingCache = Common.Setting.enableCache,
            IsLowCache = Common.Setting.forceMemoryGarbage,
            ErrorMessageList = Common.ErrorMessageList.TakeLast(15).ToList()
        }, Formatting.Indented);
        var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("dump-" +
            DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid() + ".txt");
        await FileIO.WriteTextAsync(file, info);
        _ = Launcher.LaunchFileAsync(file);
    }

    private void DisablePopUpButton_Click(object sender, RoutedEventArgs e)
    {
        Common.Setting.DisablePopUp = true;
    }

    private class DumpInfo
    {
        public HyPlayItem CurrentSong { get; set; }
        public string CurrentPlaySource { get; set; }
        public NCUser CurrentUser { get; set; }
        public string DeviceId { get; set; }
        public bool IsInBackground { get; set; }
        public bool IsUsingCache { get; set; }
        public bool IsLowCache { get; set; }
        public List<string> ErrorMessageList { get; set; }
    }

    private void ForceGC_Click(object sender, RoutedEventArgs e)
    {
        GC.Collect();
    }

    private void SaveApiAdditionalParameters_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = JsonConvert.DeserializeObject<AdditionalParameters>(TbAdditionalApiParameters.Text);
            if (result == null)
            {
                throw new Exception("Invalid JSON");
            }
            Common.Setting.ApiAdditionalParameters = result;
            Common.NeteaseAPI!.Option.AdditionalParameters = result;
            HyPlayList.LoginDoneCall();
            Common.AddToTeachingTipLists("成功设置API附加参数", "请重启应用以使更改生效");
        }
        catch (Exception ex)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Error",
                Content = ex.Message,
                CloseButtonText = "OK"
            };
            dialog.ShowAsync();
        }
    }
}