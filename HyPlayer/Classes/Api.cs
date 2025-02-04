#region

using HyPlayer.NeteaseApi.ApiContracts;
using System.Threading.Tasks;

#endregion

namespace HyPlayer.Classes;

internal class Api
{
    public static async Task<bool> LikeSong(string songid, bool like)
    {
        var requestResult = await Common.NeteaseAPI.RequestAsync(NeteaseApis.LikeApi,
            new LikeRequest() { TrackId = songid, Like = like });
        if (requestResult.IsSuccess)
        {
            return true;
        }
        else
        {
            Common.AddToTeachingTipLists(requestResult.Error.Message);
            return false;
        }
    }
}