#region

using NeteaseCloudMusicApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace HyPlayer.Classes;

internal class Api
{
    public static async Task<bool> LikeSong(string songid, bool like)
    {
        try
        {
            var requestResult = await Common.ncapi?.RequestAsync(CloudMusicApiProviders.Like,
                new Dictionary<string, object> { { "id", songid }, { "like", like ? "true" : "false" } });
            if (requestResult["code"].ToString() != "200")
                throw new Exception(requestResult.ToString());
        }
        catch (Exception ex)
        {
            Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
            return false;
        }
        return true;
    }
}