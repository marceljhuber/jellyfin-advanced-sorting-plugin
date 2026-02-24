using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.AdvancedSorting.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.AdvancedSorting;

/// <summary>
/// The Advanced Sorting plugin for Jellyfin.
/// Provides additional sort orders: Video Bitrate, File Size, Community Rating, and IMDb Top 250 rank.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Advanced Sorting";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("b3f0e574-a91c-4f24-8b6a-7c1d9e2f5a80");

    /// <inheritdoc />
    public override string Description => "Adds advanced sorting options: Video Bitrate, File Size, Community Rating, and IMDb Top 250 ranking.";

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Configuration.configPage.html",
                    GetType().Namespace)
            }
        ];
    }
}
