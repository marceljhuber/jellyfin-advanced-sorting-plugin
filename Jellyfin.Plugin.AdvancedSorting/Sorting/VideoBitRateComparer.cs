using System;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;

namespace Jellyfin.Plugin.AdvancedSorting.Sorting;

/// <summary>
/// Comparer that sorts items by their video stream bitrate (highest first by default).
/// </summary>
public class VideoBitRateComparer : IBaseItemComparer
{
    /// <inheritdoc />
    public ItemSortBy Type => ItemSortBy.VideoBitRate;

    /// <inheritdoc />
    public int Compare(BaseItem? x, BaseItem? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        return GetVideoBitRate(x).CompareTo(GetVideoBitRate(y));
    }

    private static int GetVideoBitRate(BaseItem item)
    {
        var mediaSource = item.GetMediaSources(false)?.FirstOrDefault();
        if (mediaSource == null)
        {
            return 0;
        }

        // Get the video stream bitrate
        var videoStream = mediaSource.MediaStreams?
            .FirstOrDefault(s => s.Type == MediaBrowser.Model.Entities.MediaStreamType.Video);

        return videoStream?.BitRate ?? mediaSource.Bitrate ?? 0;
    }
}
