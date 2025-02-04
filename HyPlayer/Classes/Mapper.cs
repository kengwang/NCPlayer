using System;
using System.Linq;
using HyPlayer.NeteaseApi.Models;
using HyPlayer.NeteaseApi.Models.ResponseModels;

namespace HyPlayer.Classes;

public static class Mapper
{
    public static NCSong MapToNcSong(this SongDto song)
    {
        return new NCSong
        {
            Album = song.Album.MapToNcAlbum(),
            alias = song.Alias is not null ? string.Join(",", song.Alias) : null,
            Artist = song.Artists?.Select(artist => artist.MapToNcArtist())
                         .ToList() ??
                     [],
            CDName = song.CdName,
            IsCloud = song.Sid is not "0",
            IsVip = false,
            LengthInMilliseconds = song.Duration,
            mvid = song.MvId,
            sid = song.Id,
            songname = song.Name,
            TrackId = song.TrackNumber,
            transname = song.Translation,
            IsAvailable = true,
            Type = HyPlayItemType.Netease,
        };
    }

    public static NCSong MapToNcSong(this EmittedSongDto song)
    {
        return new NCSong
        {
            Album = song.Album.MapToNcAlbum(),
            alias = song.Alias is not null ? string.Join(",", song.Alias) : null,
            Artist = song.Artists?.Select(artist => artist.MapToNcArtist())
                         .ToList() ??
                     [],
            CDName = song.CdName,
            IsCloud = song.Sid is not "0",
            IsVip = song.Fee is 1,
            LengthInMilliseconds = song.Duration,
            mvid = song.MvId,
            sid = song.Id,
            songname = song.Name,
            TrackId = song.TrackNumber,
            transname = song.Translations is not null ? string.Join(",", song.Translations) : null,
            IsAvailable = true,
            Type = HyPlayItemType.Netease,
        };
    }
    
    public static Comment MapToComment(this CommentDto comment)
    {
        return new Comment
        {
            cid = comment.CommentId,
            content = comment.Content,
            HasLiked = comment.Liked,
            likedCount = comment.LikedCount,
            ReplyCount = comment.ReplyCount,
            SendTime = new DateTime(comment.Time * 10000 + 621355968000000000),
            CommentUser = comment.User.MapToNcUser(),
        };
    }
    
    public static NCUser MapToNcUser(this UserInfoDto user)
    {
        return new NCUser
        {
            avatar = user.AvatarUrl,
            id = user.UserId,
            name = user.Nickname,
            signature = user.Signature
        };
    }
    
    public static NCSong MapNcSong(this EmittedSongDtoWithPrivilege song)
    {
        return new NCSong
        {
            Album = song.Album.MapToNcAlbum(),
            alias = song.Alias is not null ? string.Join(",", song.Alias) : null,
            Artist = song.Artists?.Select(artist => artist.MapToNcArtist())
                         .ToList() ??
                     [],
            CDName = song.CdName,
            IsCloud = song.Sid is not "0",
            IsVip = song.Fee is 1,
            LengthInMilliseconds = song.Duration,
            mvid = song.MvId,
            sid = song.Id,
            songname = song.Name,
            TrackId = song.TrackNumber,
            transname = song.Translations is not null ? string.Join(",", song.Translations) : null,
            IsAvailable = song.Privilege?.St is 0,
            Type = HyPlayItemType.Netease,
        };
    }
    
    public static NCSong MapToNcSong(this SongWithPrivilegeDto song)
    {
        return new NCSong
        {
            Album = song.Album.MapToNcAlbum(),
            alias = song.Alias is not null ? string.Join(",", song.Alias) : null,
            Artist = song.Artists?.Select(artist => artist.MapToNcArtist())
                         .ToList() ??
                     [],
            CDName = song.CdName,
            IsCloud = song.Sid is not "0",
            IsVip = false,
            LengthInMilliseconds = song.Duration,
            mvid = song.MvId,
            sid = song.Id,
            songname = song.Name,
            TrackId = song.TrackNumber,
            transname = song.Translation,
            IsAvailable = song.Privilege?.St is 0,
            Type = HyPlayItemType.Netease,
        };
    }
    
    public static NCAlbum MapToNcAlbum(this AlbumDto album)
    {
        return new NCAlbum
        {
            AlbumType = HyPlayItemType.Netease,
            alias = album.Translation,
            cover = album.PictureUrl,
            description = album.Description,
            id = album.Id,
            name = album.Name
        };
    }

    public static NCArtist MapToNcArtist(this ArtistDto artist)
    {
        return new NCArtist
        {
            alias = artist.Translation,
            avatar = artist.Img1v1Url,
            id = artist.Id,
            name = artist.Name,
            transname = artist.Translation,
            Type = HyPlayItemType.Netease
        };
    }
}