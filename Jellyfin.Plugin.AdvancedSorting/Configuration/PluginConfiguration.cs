using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AdvancedSorting.Configuration;

/// <summary>
/// Plugin configuration for Advanced Sorting.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        EnableImdbTopSorting = true;
        ImdbTopListRefreshIntervalHours = 24;
    }

    /// <summary>
    /// Gets or sets a value indicating whether IMDb Top 250 sorting is enabled.
    /// </summary>
    public bool EnableImdbTopSorting { get; set; }

    /// <summary>
    /// Gets or sets the refresh interval (in hours) for the IMDb Top 250 list.
    /// </summary>
    public int ImdbTopListRefreshIntervalHours { get; set; }
}
