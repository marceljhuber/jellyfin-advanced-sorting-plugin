using System;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AdvancedSorting.Sorting;

/// <summary>
/// Utility class for comparing items by their IMDb Top 250 rank.
/// Used internally by the API controller; not registered as an IBaseItemComparer
/// to avoid conflicting with built-in comparers.
/// </summary>
public static class ImdbTopRankComparer
{
    /// <summary>
    /// Gets the IMDb Top 250 rank for an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The rank (1-250), or <see cref="int.MaxValue"/> if not ranked.</returns>
    public static int GetImdbRank(BaseItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.ProviderIds != null &&
            item.ProviderIds.TryGetValue("Imdb", out var imdbId) &&
            !string.IsNullOrEmpty(imdbId))
        {
            var rank = ImdbTopListManager.Instance?.GetRank(imdbId);
            if (rank.HasValue)
            {
                return rank.Value;
            }
        }

        return int.MaxValue;
    }
}
