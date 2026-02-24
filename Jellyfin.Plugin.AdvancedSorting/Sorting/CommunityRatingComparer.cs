using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;

namespace Jellyfin.Plugin.AdvancedSorting.Sorting;

/// <summary>
/// Comparer that sorts items by community rating.
/// Items without a rating are sorted to the bottom.
/// </summary>
public class CommunityRatingComparer : IBaseItemComparer
{
    /// <inheritdoc />
    public ItemSortBy Type => ItemSortBy.CommunityRating;

    /// <inheritdoc />
    public int Compare(BaseItem? x, BaseItem? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        return GetRating(x).CompareTo(GetRating(y));
    }

    private static float GetRating(BaseItem item)
    {
        return item.CommunityRating ?? 0f;
    }
}
