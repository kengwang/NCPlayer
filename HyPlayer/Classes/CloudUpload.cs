#region

using HyPlayer.NeteaseApi.ApiContracts;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

#endregion

namespace HyPlayer.Classes;

/// <summary>
///     网易云音乐云盘上载
///     @copyright Kengwang
///     @refer https://github.com/Binaryify/NeteaseCloudMusicApi
/// </summary>
internal class CloudUpload
{
    public static async Task UploadMusic(StorageFile file)
    {
        throw new NotImplementedException();
        Common.AddToTeachingTipLists("上传本地音乐至音乐云盘中", "正在上传: " + file.DisplayName);
        //首先获取基本信息
        //var tagfile = File.Create(new UwpStorageFileAbstraction(file));
        var basicprop = await file.GetBasicPropertiesAsync();
        var musicprop = await file.Properties.GetMusicPropertiesAsync();
        var bytes = await FileIO.ReadBufferAsync(file);
        //再获取上传所需要的信息
        var computedHash = new MD5CryptoServiceProvider().ComputeHash(bytes.ToArray());
        var sBuilder = new StringBuilder();
        foreach (var b in computedHash) sBuilder.Append(b.ToString("x2").ToLower());
        var md5 = sBuilder.ToString();
        var checkAPIRequest = new CloudUploadCheckRequest
        {
            Ext = file.FileType,
            Md5 = md5,
            Bitrate = (int)musicprop.Bitrate,
        };
        var checkResult = await Common.NeteaseAPI.RequestAsync(NeteaseApis.CloudUploadCheckApi, checkAPIRequest);
        if (checkResult.IsSuccess)
        {
            if (checkResult.Value.NeedUpload)
            {
                // 文件需要上传
                var tokenRequest = new CloudUploadTokenAllocRequest() { FileName = file.Name, Md5 = md5 };
                var tokenRes = await Common.NeteaseAPI.RequestAsync(NeteaseApis.CloudUploadTokenAllocApi, tokenRequest);
                if (tokenRes.IsSuccess)
                {
                    var objkey = tokenRes.Value.Data.ObjectKey;
                    var targetLink = "http://45.127.129.8/jd-musicrep-privatecloud-audio-public/" + objkey + "?offset=0&complete=true&version=1.0";
                    using var request = new HttpRequestMessage(HttpMethod.Post,
                        new Uri(targetLink));
                    using var fileStream = await file.OpenAsync(FileAccessMode.Read);
                    using var content = new HttpStreamContent(fileStream);
                    content.Headers.ContentLength = basicprop.Size;
                    content.Headers.Add("Content-MD5", md5);
                    request.Headers.Add("x-nos-token", tokenRes.Value.Data.Token);
                    content.Headers.ContentType = new HttpMediaTypeHeaderValue(file.ContentType);
                    request.Content = content;
                    await Common.HttpClient.SendRequestAsync(request);
                    var title = string.IsNullOrEmpty(musicprop.Title)
                ? Path.GetFileNameWithoutExtension(file.Path)
                : musicprop.Title;
                    var infoReq = new CloudUploadInfoRequest()
                    {
                        Md5 = md5,
                        SongId = checkResult.Value.SongId,
                        FileName = file.Name,
                        Song = title,
                        Album = musicprop.Album,
                        Artist = musicprop.Artist,
                        Bitrate = (int)musicprop.Bitrate,
                        ResourceId = tokenRes.Value.Data.ResourceId,
                        ObjectKey = tokenRes.Value.Data.ObjectKey,
                    };
                    var infoRes = await Common.NeteaseAPI.RequestAsync(NeteaseApis.CloudUploadInfoApi, infoReq);
                    var cloudPubReq = new CloudPubRequest() { SongId = infoRes.Value.SongId };
                    var cloudPubRes = await Common.NeteaseAPI.RequestAsync(NeteaseApis.CloudPubApi, cloudPubReq);
                    if (infoRes.IsSuccess)
                    {
                        Common.AddToTeachingTipLists("上传本地音乐至音乐云盘成功", "成功上传: " + file.DisplayName);
                    }

                }
            }
        }
    }
}