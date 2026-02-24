using System;
using System.Linq;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AdvancedSorting.Sorting;

/// <summary>
/// Utility class for comparing items by file size.
/// Used internally by the API controller; not registered as an IBaseItemComparer
/// to avoid conflicting with built-in comparers.
/// </summary>
public static class FileSizeComparer
{
    /// <summary>
    /// Gets the file size of an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The file size in bytes, or 0 if unknown.</returns>
    public static long GetFileSize(BaseItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Try to get size from media sources
        var mediaSource = item.GetMediaSources(false)?.FirstOrDefault();
        if (mediaSource?.Size != null && mediaSource.Size > 0)
        {
            return mediaSource.Size.Value;
        }

        // Fallback: try the item's own Size property
        if (item.Size.HasValue && item.Size.Value > 0)
        {
            return item.Size.Value;
        }

        return 0;
    }
}
