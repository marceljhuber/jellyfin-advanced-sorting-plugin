using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AdvancedSorting;

/// <summary>
/// Manages the cached IMDb Top 250 list.
/// Stores IMDb IDs with their rank positions and persists them to disk.
/// </summary>
public class ImdbTopListManager
{
    private readonly ILogger<ImdbTopListManager> _logger;
    private readonly string _dataFilePath;
    private ConcurrentDictionary<string, int> _imdbRanks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ImdbTopListManager"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">The logger.</param>
    public ImdbTopListManager(IApplicationPaths applicationPaths, ILogger<ImdbTopListManager> logger)
    {
        _logger = logger;
        _dataFilePath = Path.Combine(applicationPaths.PluginConfigurationsPath, "AdvancedSorting_ImdbTop250.json");
        Instance = this;
        LoadFromDisk();
    }

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ImdbTopListManager? Instance { get; private set; }

    /// <summary>
    /// Gets the last time the list was updated.
    /// </summary>
    public DateTime LastUpdated { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// Gets the number of entries in the list.
    /// </summary>
    public int Count => _imdbRanks.Count;

    /// <summary>
    /// Gets the rank for a given IMDb ID.
    /// </summary>
    /// <param name="imdbId">The IMDb ID (e.g., "tt0111161").</param>
    /// <returns>The rank (1-250) or null if not on the list.</returns>
    public int? GetRank(string imdbId)
    {
        if (_imdbRanks.TryGetValue(imdbId, out var rank))
        {
            return rank;
        }

        return null;
    }

    /// <summary>
    /// Gets all rankings as a dictionary.
    /// </summary>
    /// <returns>A copy of the current rankings.</returns>
    public Dictionary<string, int> GetAllRanks()
    {
        return new Dictionary<string, int>(_imdbRanks, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Updates the entire list with new rankings.
    /// </summary>
    /// <param name="rankings">Dictionary mapping IMDb IDs to ranks.</param>
    public void UpdateList(Dictionary<string, int> rankings)
    {
        _imdbRanks = new ConcurrentDictionary<string, int>(rankings, StringComparer.OrdinalIgnoreCase);
        LastUpdated = DateTime.UtcNow;
        SaveToDisk();
        _logger.LogInformation("IMDb Top list updated with {Count} entries", rankings.Count);
    }

    /// <summary>
    /// Loads the default IMDb Top 250 list (hardcoded well-known entries).
    /// This provides a starting point without requiring an external API.
    /// </summary>
    public void LoadDefaultList()
    {
        // A curated subset of the IMDb Top 250 (by IMDb ID and rank)
        // Users can update this via the API or scheduled task
        var defaults = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // Top 25 all-time classics
            { "tt0111161", 1 },   // The Shawshank Redemption
            { "tt0068646", 2 },   // The Godfather
            { "tt0468569", 3 },   // The Dark Knight
            { "tt0071562", 4 },   // The Godfather Part II
            { "tt0050083", 5 },   // 12 Angry Men
            { "tt0108052", 6 },   // Schindler's List
            { "tt0167260", 7 },   // The Lord of the Rings: The Return of the King
            { "tt0110912", 8 },   // Pulp Fiction
            { "tt0120737", 9 },   // The Lord of the Rings: The Fellowship of the Ring
            { "tt0137523", 10 },  // Fight Club
            { "tt0109830", 11 },  // Forrest Gump
            { "tt1375666", 12 },  // Inception
            { "tt0167261", 13 },  // The Lord of the Rings: The Two Towers
            { "tt0080684", 14 },  // Star Wars: Episode V - The Empire Strikes Back
            { "tt0133093", 15 },  // The Matrix
            { "tt0099685", 16 },  // Goodfellas
            { "tt0073486", 17 },  // One Flew Over the Cuckoo's Nest
            { "tt0114369", 18 },  // Se7en
            { "tt0038650", 19 },  // It's a Wonderful Life
            { "tt0076759", 20 },  // Star Wars: Episode IV - A New Hope
            { "tt0102926", 21 },  // The Silence of the Lambs
            { "tt0317248", 22 },  // City of God
            { "tt0120815", 23 },  // Saving Private Ryan
            { "tt0118799", 24 },  // Life Is Beautiful
            { "tt0245429", 25 },  // Spirited Away
            // 26-50
            { "tt0816692", 26 },  // Interstellar
            { "tt6751668", 27 },  // Parasite
            { "tt0120689", 28 },  // The Green Mile
            { "tt0361748", 29 },  // Inglourious Basterds
            { "tt0114814", 30 },  // The Usual Suspects
            { "tt0110413", 31 },  // Léon: The Professional
            { "tt0056058", 32 },  // Harakiri
            { "tt0103064", 33 },  // Terminator 2: Judgment Day
            { "tt0253474", 34 },  // The Pianist
            { "tt0047478", 35 },  // Seven Samurai
            { "tt0088763", 36 },  // Back to the Future
            { "tt0209144", 37 },  // Memento
            { "tt0172495", 38 },  // Gladiator
            { "tt0482571", 39 },  // The Prestige
            { "tt2582802", 40 },  // Whiplash
            { "tt0407887", 41 },  // The Departed
            { "tt0078788", 42 },  // Apocalypse Now
            { "tt0078748", 43 },  // Alien
            { "tt0095327", 44 },  // Grave of the Fireflies
            { "tt0082971", 45 },  // Raiders of the Lost Ark
            { "tt0032553", 46 },  // The Great Dictator
            { "tt1853728", 47 },  // Django Unchained
            { "tt0095765", 48 },  // Cinema Paradiso
            { "tt0405094", 49 },  // The Lives of Others
            { "tt0050825", 50 },  // Paths of Glory
            // 51-100 (selected entries)
            { "tt0043014", 51 },  // Sunset Boulevard
            { "tt4154756", 52 },  // Avengers: Infinity War
            { "tt4633694", 53 },  // Spider-Man: Into the Spider-Verse
            { "tt0057012", 54 },  // Dr. Strangelove
            { "tt0081505", 55 },  // The Shining
            { "tt4154796", 56 },  // Avengers: Endgame
            { "tt0064116", 57 },  // Once Upon a Time in the West
            { "tt0051201", 58 },  // Witness for the Prosecution
            { "tt0090605", 59 },  // Aliens
            { "tt0119698", 60 },  // Princess Mononoke
            { "tt0087843", 61 },  // Amadeus
            { "tt0361862", 62 },  // Spirited Away (City of God alternate)
            { "tt0364569", 63 },  // Oldboy
            { "tt0057565", 64 },  // Good, the Bad and the Ugly
            { "tt0052357", 65 },  // Vertigo
            { "tt0180093", 66 },  // Requiem for a Dream
            { "tt0338013", 67 },  // Eternal Sunshine of the Spotless Mind
            { "tt0910970", 68 },  // WALL·E
            { "tt0053125", 69 },  // North by Northwest
            { "tt1345836", 70 },  // The Dark Knight Rises
            { "tt0045152", 71 },  // Singin' in the Rain
            { "tt0211915", 72 },  // Amélie
            { "tt0062622", 73 },  // 2001: A Space Odyssey
            { "tt0086879", 74 },  // Once Upon a Time in America
            { "tt0066921", 75 },  // A Clockwork Orange
            { "tt0105236", 76 },  // Reservoir Dogs
            { "tt0086190", 77 },  // Star Wars: Episode VI - Return of the Jedi
            { "tt0075314", 78 },  // Taxi Driver
            { "tt0036775", 79 },  // Double Indemnity
            { "tt0044741", 80 },  // Ikiru
            { "tt0047396", 81 },  // Rear Window
            { "tt0112573", 82 },  // Braveheart
            { "tt0435761", 83 },  // Toy Story 3
            { "tt2106476", 84 },  // The Hunt
            { "tt0056592", 85 },  // To Kill a Mockingbird
            { "tt1187043", 86 },  // 3 Idiots
            { "tt0040522", 87 },  // Bicycle Thieves
            { "tt0338564", 88 },  // Incendies
            { "tt0021749", 89 },  // City Lights
            { "tt0053604", 90 },  // The Apartment
            { "tt0986264", 91 },  // Like Stars on Earth
            { "tt0093058", 92 },  // Full Metal Jacket
            { "tt0022100", 93 },  // M
            { "tt0082096", 94 },  // Das Boot
            { "tt1049413", 95 },  // Up
            { "tt1255953", 96 },  // A Separation
            { "tt0169547", 97 },  // American Beauty
            { "tt0119488", 98 },  // L.A. Confidential
            { "tt0070735", 99 },  // The Sting
            { "tt0071315", 100 }, // Chinatown
        };

        UpdateList(defaults);
        _logger.LogInformation("Loaded default IMDb Top 250 list with {Count} entries", defaults.Count);
    }

    private void SaveToDisk()
    {
        try
        {
            var data = new ImdbTopListData
            {
                LastUpdated = LastUpdated,
                Rankings = _imdbRanks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save IMDb Top list to disk");
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                _logger.LogInformation("No saved IMDb Top list found, loading defaults");
                LoadDefaultList();
                return;
            }

            var json = File.ReadAllText(_dataFilePath);
            var data = JsonSerializer.Deserialize<ImdbTopListData>(json);

            if (data?.Rankings != null && data.Rankings.Count > 0)
            {
                _imdbRanks = new ConcurrentDictionary<string, int>(data.Rankings, StringComparer.OrdinalIgnoreCase);
                LastUpdated = data.LastUpdated;
                _logger.LogInformation("Loaded IMDb Top list with {Count} entries from disk", _imdbRanks.Count);
            }
            else
            {
                LoadDefaultList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load IMDb Top list from disk, loading defaults");
            LoadDefaultList();
        }
    }

    /// <summary>
    /// Data model for persisting the IMDb list to JSON.
    /// </summary>
    private sealed class ImdbTopListData
    {
        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the rank mappings (IMDb ID -> rank).
        /// </summary>
        public Dictionary<string, int> Rankings { get; set; } = new();
    }
}
