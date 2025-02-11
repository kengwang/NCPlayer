#region

using HyPlayer.HyPlayControl;
using HyPlayer.NeteaseApi.ApiContracts;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using Windows.Storage;
using File = TagLib.File;

#endregion

namespace HyPlayer.Classes;

/// <summary>
///     网易云音乐云盘上载
///     @copyright Kengwang
///     @refer https://github.com/Binaryify/NeteaseCloudMusicApi
/// </summary>
internal class CloudUpload
{
#nullable enable
    public static async Task UploadMusic(StorageFile file)
    {
        Common.AddToTeachingTipLists("上传本地音乐至音乐云盘中", "正在上传: " + file.DisplayName);
        var musicprop = await file.Properties.GetMusicPropertiesAsync();
        //首先获取基本信息
        using var abstraction = new UwpStorageFileAbstraction(file);
        var album = string.Empty;
        var duration = (long)musicprop.Duration.TotalMilliseconds;
        var bitrate = musicprop.Bitrate;
        var name = file.DisplayName;
        var artist = string.Empty;
        byte[]? coverBytes = null;
        try
        {
            using var tagFile = File.Create(abstraction);
            var tag = tagFile?.Tag;
            album = tag?.Album;
            name = tag?.Title;
            artist = string.Join("; ", tag?.Performers ?? []);
            coverBytes = tag?.Pictures.FirstOrDefault().Data?.Data;
        }
        catch
        {
            //Ignore
        }
        
        var bytes = await FileIO.ReadBufferAsync(file);
        //再获取上传所需要的信息
        var computedHash = new MD5CryptoServiceProvider().ComputeHash(bytes.ToArray());
        var sBuilder = new StringBuilder();
        foreach (var b in computedHash) sBuilder.Append(b.ToString("x2").ToLower());
        var md5 = sBuilder.ToString();
        var checkResult = await Common.NeteaseAPI!.RequestAsync(NeteaseApis.CloudUploadCheckApi,
            new CloudUploadCheckRequest()
            {
                Ext = file.FileType,
                Md5 = md5,
                Bitrate = (int)bitrate,
            });
        if (checkResult.IsError)
        {
            Common.AddToTeachingTipLists($"上传失败: {file.DisplayName}", checkResult.Error!.Message);
            return;
        }

        if (checkResult.Value?.NeedUpload is not false)
        {
            // 文件需要上传
            var tokenRequest = new CloudUploadTokenAllocRequest
            {
                FileName = file.Name,
                Md5 = md5
            };
            var tokenRes = await Common.NeteaseAPI.RequestAsync(NeteaseApis.CloudUploadTokenAllocApi, tokenRequest);
            if (tokenRes.IsError)
            {
                Common.AddToTeachingTipLists($"上传失败: {file.DisplayName}", tokenRes.Error!.Message);
                return;
            }

            var objkey = tokenRes.Value!.Data!.ObjectKey;
            // fetch load balancer
            var lb = "http://45.127.129.8";
            var loadBalancerReq = new NeteaseUploadLoadBalancerGetRequest()
            {
                Bucket = "jd-musicrep-privatecloud-audio-public"
            };
            var loadBalancerRes = await Common.NeteaseAPI.RequestAsync(NeteaseApis.NeteaseUploadLoadBalancerGetApi,
                loadBalancerReq);
            if (loadBalancerRes.IsSuccess)
            {
                lb = loadBalancerRes.Value!.Upload?.FirstOrDefault() ?? lb;
            }

            var targetLink = $"{lb}/jd-musicrep-privatecloud-audio-public/{objkey}?offset=0&complete=true&version=1.0";
            using var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri(targetLink));
            using var fileStream = await file.OpenAsync(FileAccessMode.Read);
            using var stream = fileStream.AsStream();
            using var content = new StreamContent(stream);
            content.Headers.Add("Content-MD5", md5);
            request.Headers.Add("x-nos-token", tokenRes.Value.Data.Token);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            request.Content = content;
            await Common.HttpClient!.SendAsync(request);
            var title = string.IsNullOrEmpty(name)
                ? Path.GetFileNameWithoutExtension(file.Path)
                : name;
            string coverId = string.Empty;
            // upload cover
            if (coverBytes != null)
            {
                var imgcomputedHash = new MD5CryptoServiceProvider().ComputeHash(coverBytes);
                var imgsBuilder = new StringBuilder();
                foreach (var b in imgcomputedHash) imgsBuilder.Append(b.ToString("x2").ToLower());
                var imgmd5 = imgsBuilder.ToString();

                var coverAllocRes = await Common.NeteaseAPI.RequestAsync(NeteaseApis.CloudUploadCoverTokenAllocApi,
                    new CloudUploadCoverTokenAllocRequest
                    {
                        Ext = "png",
                        Filename = $"{file.DisplayName}_cover",
                    });
                if (coverAllocRes.IsError)
                {
                    Common.AddToTeachingTipLists($"上传失败(封面): {file.DisplayName}", coverAllocRes.Error!.Message);
                }
                coverId = coverAllocRes.Value?.Result?.DocId!;
                var imglb = "http://45.127.129.8";
                var imgloadBalancerReq = new NeteaseUploadLoadBalancerGetRequest()
                {
                    Bucket = "yyimg"
                };
                var imgloadBalancerRes = await Common.NeteaseAPI.RequestAsync(NeteaseApis.NeteaseUploadLoadBalancerGetApi,
                    imgloadBalancerReq);
                if (imgloadBalancerRes.IsSuccess)
                {
                    imglb = imgloadBalancerRes.Value!.Upload?.FirstOrDefault() ?? lb;
                }
                targetLink = $"{imglb}/yyimg/{coverAllocRes.Value?.Result?.ObjectKey}?offset=0&complete=true&version=1.0";

                using var imgReq = new HttpRequestMessage(HttpMethod.Post,
                    new Uri(targetLink));
                using var imgContent = new ByteArrayContent(coverBytes);
                imgContent.Headers.Add("Content-MD5", md5);
                imgReq.Headers.Add("x-nos-token", coverAllocRes.Value?.Result?.Token);
                imgContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                imgReq.Content = imgContent;
                await Common.HttpClient.SendAsync(imgReq);
            }


            var infoReq = new CloudUploadInfoRequest
            {
                Md5 = md5,
                SongId = checkResult.Value!.SongId!,
                FileName = file.Name,
                Song = title ?? file.DisplayName,
                Album = album ?? "",
                Artist = artist,
                Bitrate = (int)bitrate,
                CoverId = coverId!,
                ResourceId = tokenRes.Value.Data!.ResourceId!,
                ObjectKey = $"jd-musicrep-privatecloud-audio-public/{tokenRes.Value.Data!.ObjectKey}",
            };
            var infoRes = await Common.NeteaseAPI.RequestAsync(NeteaseApis.CloudUploadInfoApi, infoReq);
            if (infoRes.IsError)
            {
                Common.AddToTeachingTipLists($"上传失败: {file.DisplayName}", infoRes.Error!.Message);
                return;
            }
            var cloudPubReq = new CloudPubRequest()
            {
                SongId = infoRes.Value!.SongId!
            };
            var cloudPubRes = await Common.NeteaseAPI.RequestAsync(NeteaseApis.CloudPubApi, cloudPubReq);
            if (cloudPubRes.IsError)
            {
                Common.AddToTeachingTipLists($"上传失败: {file.DisplayName}", cloudPubRes.Error!.Message);
            }
            else
            {
                Common.AddToTeachingTipLists("上传本地音乐至音乐云盘成功", "成功上传: " + file.DisplayName);
            }
        }
    }
#nullable restore
}