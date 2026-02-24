using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AdvancedSorting.Api;

/// <summary>
/// API controller for advanced sorting operations.
/// Provides endpoints to sort library items by video bitrate, file size,
/// community rating, and IMDb Top 250 ranking.
/// </summary>
[ApiController]
[Authorize]
[Route("AdvancedSorting")]
[Produces(MediaTypeNames.Application.Json)]
public class AdvancedSortingController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly ImdbTopListManager _imdbTopListManager;
    private readonly ILogger<AdvancedSortingController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedSortingController"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="imdbTopListManager">The IMDb top list manager.</param>
    /// <param name="logger">The logger.</param>
    public AdvancedSortingController(
        ILibraryManager libraryManager,
        ImdbTopListManager imdbTopListManager,
        ILogger<AdvancedSortingController> logger)
    {
        _libraryManager = libraryManager;
        _imdbTopListManager = imdbTopListManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets movies sorted by video bitrate.
    /// </summary>
    /// <param name="ascending">Sort ascending (lowest first) if true, descending (highest first) if false. Default: false.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <returns>A list of items sorted by video bitrate.</returns>
    [HttpGet("ByBitrate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<SortedItemResult>> GetByBitrate(
        [FromQuery] bool ascending = false,
        [FromQuery] int limit = 100)
    {
        var items = GetMovieItems();

        var sorted = items
            .Select(item =>
            {
                var mediaSource = item.GetMediaSources(false)?.FirstOrDefault();
                var videoStream = mediaSource?.MediaStreams?
                    .FirstOrDefault(s => s.Type == MediaBrowser.Model.Entities.MediaStreamType.Video);
                var bitrate = videoStream?.BitRate ?? mediaSource?.Bitrate ?? 0;

                return new SortedItemResult
                {
                    Id = item.Id,
                    Name = item.Name,
                    Year = item.ProductionYear,
                    SortValue = bitrate,
                    SortDisplayValue = FormatBitrate(bitrate)
                };
            });

        sorted = ascending
            ? sorted.OrderBy(x => x.SortValue)
            : sorted.OrderByDescending(x => x.SortValue);

        return Ok(sorted.Take(limit));
    }

    /// <summary>
    /// Gets movies sorted by file size.
    /// </summary>
    /// <param name="ascending">Sort ascending if true. Default: false (largest first).</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <returns>A list of items sorted by file size.</returns>
    [HttpGet("ByFileSize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<SortedItemResult>> GetByFileSize(
        [FromQuery] bool ascending = false,
        [FromQuery] int limit = 100)
    {
        var items = GetMovieItems();

        var sorted = items
            .Select(item =>
            {
                var mediaSource = item.GetMediaSources(false)?.FirstOrDefault();
                long size = mediaSource?.Size ?? item.Size ?? 0;

                return new SortedItemResult
                {
                    Id = item.Id,
                    Name = item.Name,
                    Year = item.ProductionYear,
                    SortValue = size,
                    SortDisplayValue = FormatFileSize(size)
                };
            });

        sorted = ascending
            ? sorted.OrderBy(x => x.SortValue)
            : sorted.OrderByDescending(x => x.SortValue);

        return Ok(sorted.Take(limit));
    }

    /// <summary>
    /// Gets movies sorted by community rating.
    /// </summary>
    /// <param name="ascending">Sort ascending if true. Default: false (highest rated first).</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <returns>A list of items sorted by community rating.</returns>
    [HttpGet("ByCommunityRating")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<SortedItemResult>> GetByCommunityRating(
        [FromQuery] bool ascending = false,
        [FromQuery] int limit = 100)
    {
        var items = GetMovieItems();

        var sorted = items
            .Select(item => new SortedItemResult
            {
                Id = item.Id,
                Name = item.Name,
                Year = item.ProductionYear,
                SortValue = (long)((item.CommunityRating ?? 0f) * 10),
                SortDisplayValue = (item.CommunityRating ?? 0f).ToString("F1")
            });

        sorted = ascending
            ? sorted.OrderBy(x => x.SortValue)
            : sorted.OrderByDescending(x => x.SortValue);

        return Ok(sorted.Take(limit));
    }

    /// <summary>
    /// Gets movies sorted by their IMDb Top 250 rank.
    /// Only returns movies that are in the IMDb Top list.
    /// </summary>
    /// <param name="includeUnranked">Include movies not on the IMDb list (sorted to the end).</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <returns>A list of items sorted by IMDb Top rank.</returns>
    [HttpGet("ByImdbTopRank")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<SortedItemResult>> GetByImdbTopRank(
        [FromQuery] bool includeUnranked = false,
        [FromQuery] int limit = 250)
    {
        var items = GetMovieItems();

        var sorted = items
            .Select(item =>
            {
                string? imdbId = null;
                item.ProviderIds?.TryGetValue("Imdb", out imdbId);

                int? rank = null;
                if (!string.IsNullOrEmpty(imdbId))
                {
                    rank = _imdbTopListManager.GetRank(imdbId);
                }

                return new
                {
                    Item = item,
                    ImdbId = imdbId,
                    Rank = rank
                };
            })
            .Where(x => includeUnranked || x.Rank.HasValue)
            .OrderBy(x => x.Rank ?? int.MaxValue)
            .Take(limit)
            .Select(x => new SortedItemResult
            {
                Id = x.Item.Id,
                Name = x.Item.Name,
                Year = x.Item.ProductionYear,
                SortValue = x.Rank ?? 0,
                SortDisplayValue = x.Rank.HasValue ? $"#{x.Rank}" : "Not ranked",
                ImdbId = x.ImdbId
            });

        return Ok(sorted);
    }

    /// <summary>
    /// Gets the current IMDb Top list status.
    /// </summary>
    /// <returns>Status information about the IMDb Top list.</returns>
    [HttpGet("ImdbTopList/Status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ImdbTopListStatus> GetImdbTopListStatus()
    {
        return Ok(new ImdbTopListStatus
        {
            EntryCount = _imdbTopListManager.Count,
            LastUpdated = _imdbTopListManager.LastUpdated
        });
    }

    /// <summary>
    /// Updates the IMDb Top list with custom rankings.
    /// </summary>
    /// <param name="rankings">Dictionary mapping IMDb IDs to rank positions.</param>
    /// <returns>Status of the update.</returns>
    [HttpPost("ImdbTopList/Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ImdbTopListStatus> UpdateImdbTopList(
        [FromBody] Dictionary<string, int> rankings)
    {
        if (rankings == null || rankings.Count == 0)
        {
            return BadRequest("Rankings dictionary cannot be empty");
        }

        _imdbTopListManager.UpdateList(rankings);

        return Ok(new ImdbTopListStatus
        {
            EntryCount = _imdbTopListManager.Count,
            LastUpdated = _imdbTopListManager.LastUpdated
        });
    }

    /// <summary>
    /// Resets the IMDb Top list to the default hardcoded values.
    /// </summary>
    /// <returns>Status of the reset.</returns>
    [HttpPost("ImdbTopList/Reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ImdbTopListStatus> ResetImdbTopList()
    {
        _imdbTopListManager.LoadDefaultList();

        return Ok(new ImdbTopListStatus
        {
            EntryCount = _imdbTopListManager.Count,
            LastUpdated = _imdbTopListManager.LastUpdated
        });
    }

    private List<BaseItem> GetMovieItems()
    {
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            IsVirtualItem = false,
            Recursive = true,
        };

        return _libraryManager.GetItemList(query)?.ToList() ?? new List<BaseItem>();
    }

    private static string FormatBitrate(int bitrate)
    {
        if (bitrate >= 1_000_000)
        {
            return $"{bitrate / 1_000_000.0:F1} Mbps";
        }

        if (bitrate >= 1_000)
        {
            return $"{bitrate / 1_000.0:F0} Kbps";
        }

        return $"{bitrate} bps";
    }

    private static string FormatFileSize(long size)
    {
        if (size >= 1_073_741_824L)
        {
            return $"{size / 1_073_741_824.0:F2} GB";
        }

        if (size >= 1_048_576L)
        {
            return $"{size / 1_048_576.0:F1} MB";
        }

        if (size >= 1024L)
        {
            return $"{size / 1024.0:F0} KB";
        }

        return $"{size} bytes";
    }
}

/// <summary>
/// Represents a sorted item result.
/// </summary>
public class SortedItemResult
{
    /// <summary>
    /// Gets or sets the item ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the production year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the raw sort value.
    /// </summary>
    public long SortValue { get; set; }

    /// <summary>
    /// Gets or sets the formatted display value for the sort criterion.
    /// </summary>
    public string? SortDisplayValue { get; set; }

    /// <summary>
    /// Gets or sets the IMDb ID (when applicable).
    /// </summary>
    public string? ImdbId { get; set; }
}

/// <summary>
/// Status of the IMDb Top list.
/// </summary>
public class ImdbTopListStatus
{
    /// <summary>
    /// Gets or sets the number of entries.
    /// </summary>
    public int EntryCount { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
