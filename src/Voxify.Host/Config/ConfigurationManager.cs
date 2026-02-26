using System.Text.Json;

namespace Voxify.Config;

/// <summary>
/// Configuration manager — loading and saving settings to JSON file.
/// </summary>
public class ConfigurationManager
{
    private const string ConfigFileName = "appsettings.json";
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppSettings Settings { get; private set; }

    public ConfigurationManager()
    {
        // Path to config next to exe file
        var baseDir = AppContext.BaseDirectory;
        _configPath = Path.Combine(baseDir, ConfigFileName);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        Settings = LoadOrCreateDefault();
    }
    
    /// <summary>
    /// Loads settings from file or creates new default ones.
    /// </summary>
    private AppSettings LoadOrCreateDefault()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error reading configuration: {ex.Message}. Using default settings.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error accessing configuration: {ex.Message}. Using default settings.");
        }

        // Return default settings
        return new AppSettings();
    }
    
    /// <summary>
    /// Saves current settings to file.
    /// </summary>
    public void Save()
    {
        try
        {
            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(_configPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(Settings, _jsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error saving configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates settings and saves them.
    /// </summary>
    public void UpdateSettings(Action<AppSettings> updateAction)
    {
        updateAction(Settings);
        Save();
    }
}
