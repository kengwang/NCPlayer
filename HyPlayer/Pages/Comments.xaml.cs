﻿#region

using HyPlayer.Classes;
using HyPlayer.NeteaseApi.ApiContracts;
using HyPlayer.NeteaseApi.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Point = Windows.Foundation.Point;

#endregion

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace HyPlayer.Pages;

/// <summary>
///     可用于自身或导航至 Frame 内部的空白页。
/// </summary>
public sealed partial class Comments : Page, IDisposable
{
    private string cursor;
    private int page = 1;
    private string resourceid;
    private NeteaseResourceType resourcetype;
    private int sortType = 1;
    private bool IsShiftingPage = false;
    private ScrollViewer MainScroll, HotCommentsScroll;
    private ObservableCollection<Comment> hotComments = new ObservableCollection<Comment>();
    private ObservableCollection<Comment> normalComments = new ObservableCollection<Comment>();
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private CancellationToken _cancellationToken;
    private Task _commentLoaderTask;
    private Task _hotCommentLoaderTask;
    private bool disposedValue = false;

    public Comments()
    {
        InitializeComponent();
        _cancellationToken = _cancellationTokenSource.Token;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string resstr)
        {
            resourceid = resstr.Substring(2);
            switch (resstr.Substring(0, 2))
            {
                case "sg":
                    resourcetype = NeteaseResourceType.Song;
                    break;
                case "mv":
                    resourcetype = NeteaseResourceType.MV;
                    break;
                case "fm":
                    resourcetype = NeteaseResourceType.RadioProgram;
                    break;
                case "mb":
                    resourcetype = NeteaseResourceType.MLog;
                    break;
                case "al":
                    resourcetype = NeteaseResourceType.Album;
                    break;
                case "pl":
                    resourcetype = NeteaseResourceType.Playlist;
                    break;
            }
        }

        LoadHotComments();
        _commentLoaderTask = LoadComments(sortType);
    }

    protected override async void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        if (_commentLoaderTask != null && !_commentLoaderTask.IsCompleted)
        {
            try
            {
                _cancellationTokenSource.Cancel();
                await _commentLoaderTask;
            }
            catch
            {
            }
        }
        if (_hotCommentLoaderTask != null && !_hotCommentLoaderTask.IsCompleted)
        {
            try
            {
                _cancellationTokenSource.Cancel();
                await _hotCommentLoaderTask;
            }
            catch
            {
            }
        }
        Dispose();
    }


    private void LoadHotComments()
    {
        _hotCommentLoaderTask = LoadComments(2);
    }

    private async Task LoadComments(int type)
    {
        if (string.IsNullOrEmpty(resourceid)) return;
        if (IsShiftingPage) return;
        _cancellationToken.ThrowIfCancellationRequested();
        var isHotCommentsPage = HotCommentsContainer.Visibility == Visibility.Visible;
        var result = await Common.NeteaseAPI.RequestAsync(NeteaseApis.CommentsApi, new CommentsRequest
        {
            ResourceType = resourcetype,
            ResourceId = resourceid,
            CommentSortType = type switch
            {
                2 => CommentSortType.Hot,
                3 => CommentSortType.Time,
                _ => CommentSortType.Recommend
            },
            PageSize = 20,
            PageNo = page,
            Cursor = page != 1 && type == 3 ? cursor : null
        }, _cancellationToken);

        if (result.IsError)
        {
            Common.AddToTeachingTipLists("加载评论时出错", result.Error.Message);
            return;
        }

        if (type == 2 && isHotCommentsPage)
            hotComments.Clear();
        else normalComments.Clear();

        foreach (var comment in result.Value?.Data?.Comments ?? [])
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var cmt = comment.MapToComment();
            cmt.resourceType = resourcetype;
            cmt.resourceId = resourceid;
            if (type == 2 && isHotCommentsPage)
                hotComments.Add(cmt);
            else normalComments.Add(cmt);
        }

        if (type == 3)
            cursor = result.Value?.Data?.Cursor;

        if (result.Value?.Data?.HasMore == true)
            NextPage.IsEnabled = true;
        else
            NextPage.IsEnabled = false;

        if (page > 1)
            PrevPage.IsEnabled = true;
        else
            PrevPage.IsEnabled = false;
    }


    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        page++;
        _commentLoaderTask = LoadComments(sortType);
        ScrollTop();
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        page--;
        _commentLoaderTask = LoadComments(sortType);
        ScrollTop();
    }

    private void SendComment_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 评论功能风控
        Common.AddToTeachingTipLists("评论功能暂时关闭", "由于网易云音乐风控策略，评论功能暂时关闭");
        /*
        if (!string.IsNullOrWhiteSpace(CommentEdit.Text) && Common.Logined)
        {
            try
            {
                var result = await Common.ncapi?.RequestAsync(CloudMusicApiProviders.Comment,
                    new Dictionary<string, object>
                    {
                        {
                            "id", resourceid
                        },
                        {
                            "type", resourcetype
                        },
                        {
                            "t", "1"
                        },
                        {
                            "content", CommentEdit.Text
                        }
                    });

                CommentEdit.Text = string.Empty;
                await Task.Delay(1000);
                _commentLoaderTask = LoadComments(3);
                Common.AddToTeachingTipLists("评论成功");
                Common.RollTeachingTip();
            }
            catch (Exception ex)
            {
                Common.AddToTeachingTipLists("出现问题，评论失败", ex.Message);
                Common.RollTeachingTip();
            }
        }

        else if (string.IsNullOrWhiteSpace(CommentEdit.Text))
        {
            Common.AddToTeachingTipLists("评论不能为空");
            Common.RollTeachingTip();
        }
        else
        {
            var dlg = new MessageDialog("请先登录");
            await dlg.ShowAsync();
        }
        */
    }

    private void ComboBoxSortType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        sortType = ComboBoxSortType.SelectedIndex + 1;
        _commentLoaderTask = LoadComments(sortType);
    }

    private void SkipPage_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(PageSelect.Text, out page))
        {
            _commentLoaderTask = LoadComments(sortType);
            ScrollTop();
        }
    }

    private void ScrollTop()
    {
        var transform = AllCmtsTB.TransformToVisual(MainScroll);
        var point = transform.TransformPoint(new Point(0, -1000000));//一定要这么大
        var y = point.Y + MainScroll.VerticalOffset;
        MainScroll.ChangeView(null, y, null, false);
        TimeSpan delay = TimeSpan.FromMilliseconds(320);//稍微等等再滚回去，免得回到热评区域
        ThreadPoolTimer DelayTimer = ThreadPoolTimer.CreateTimer(
    (source) =>

    {
        _ = Dispatcher.RunAsync(
        CoreDispatcherPriority.Low,
        () =>
        {
            point = transform.TransformPoint(new Point(0, 25));//要超过判定区域，还要预留一点
            y = point.Y + MainScroll.VerticalOffset;
            MainScroll.ChangeView(null, y, null, false);
        });

    }, delay);

    }

    private void BackToTop_Click(object sender, RoutedEventArgs e)
    {
        ScrollTop();
    }

    private void MainScroll_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        var transform = AllCmtsTB.TransformToVisual(MainScroll);
        var point = transform.TransformPoint(new Point(0, 0));
        var y = point.Y + MainScroll.VerticalOffset;
        if ((sender as ScrollViewer).VerticalOffset > y + 25)
            BackToTop.Visibility = Visibility.Visible;
        else BackToTop.Visibility = Visibility.Collapsed;
        if ((sender as ScrollViewer).VerticalOffset < 15)
        {
            TimeSpan delay = TimeSpan.FromMilliseconds(90);//先别急，如果是回到顶部触发的会滚回去一点
            ThreadPoolTimer DelayTimer = ThreadPoolTimer.CreateTimer(
        (source) =>

        {
            _ = Dispatcher.RunAsync(
            CoreDispatcherPriority.Low,
            () =>
            {
                if ((sender as ScrollViewer).VerticalOffset < 15)
                    ShiftCommentList(false);//回到热评
            });

        }, delay);
        }
    }

    private void PageSelect_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (int.TryParse(PageSelect.Text, out page))
        {
            _commentLoaderTask = LoadComments(sortType);
            ScrollTop();
        }
    }

    private void HotComments_Loaded(object sender, RoutedEventArgs e)
    {
        TimeSpan delay = TimeSpan.FromMilliseconds(500);
        ThreadPoolTimer DelayTimer = ThreadPoolTimer.CreateTimer(
    (source) =>

        {
            _ = Dispatcher.RunAsync(
            CoreDispatcherPriority.Low,
            () =>
            {
                HotCommentsScroll = HotComments.CommentPresentScrollViewer;
                HotCommentsScroll.ViewChanged += HotCommentsScroll_ViewChanged;
            });

        }, delay);//缓一会再加载，要不然获取不到

    }

    private void HotCommentsScroll_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (HotCommentsScroll.ScrollableHeight - HotCommentsScroll.VerticalOffset <= 14)
            ShiftCommentList(true);
    }

    private void ShiftCommentList(bool direction)
    {
        IsShiftingPage = true;
        if (direction)
        {
            AllCommentsContainer.Visibility = Visibility.Visible;
            var animation = (Storyboard)Resources["CommentFlyUp"];
            animation.Begin();
            HotCommentsContainer.Visibility = Visibility.Collapsed;
            TimeSpan delay = TimeSpan.FromMilliseconds(500);
            ThreadPoolTimer DelayTimer = ThreadPoolTimer.CreateTimer(
            (source) =>

            {
                _ = Dispatcher.RunAsync(
                CoreDispatcherPriority.Low,
                () =>
                {
                    MainScroll = NormalComments.CommentPresentScrollViewer;
                    var transform = AllCmtsTB.TransformToVisual(MainScroll);
                    var point = transform.TransformPoint(new Point(0, 25));//要超过判定区域，还要预留一点
                    var y = point.Y + MainScroll.VerticalOffset;
                    MainScroll.ChangeView(null, y, null, false);
                    MainScroll.ViewChanged += MainScroll_ViewChanged;
                });

            }, delay);
        }
        else
        {
            HotCommentsContainer.Visibility = Visibility.Visible;
            var animation = (Storyboard)Resources["CommentFlyDown"];
            animation.Begin();
            AllCommentsContainer.Visibility = Visibility.Collapsed;
            BackToTop.Visibility = Visibility.Collapsed;
        }
        IsShiftingPage = false;
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                hotComments.Clear();
                normalComments.Clear();
                _cancellationTokenSource.Dispose();
                cursor = null;
                resourceid = null;
            }
            disposedValue = true;
        }
    }
    ~Comments()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}