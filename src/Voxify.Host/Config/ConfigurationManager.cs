using System.Text.Json;

namespace Voxify.Config;

/// <summary>
/// Менеджер конфигурации — загрузка и сохранение настроек в JSON-файл.
/// </summary>
public class ConfigurationManager
{
    private const string ConfigFileName = "appsettings.json";
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppSettings Settings { get; private set; }

    public ConfigurationManager()
    {
        // Путь к конфигу рядом с exe-файлом
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
    /// Загружает настройки из файла или создаёт новые по умолчанию.
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
            Console.WriteLine($"Ошибка чтения конфигурации: {ex.Message}. Используются настройки по умолчанию.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка доступа к конфигурации: {ex.Message}. Используются настройки по умолчанию.");
        }
        
        // Возвращаем настройки по умолчанию
        return new AppSettings();
    }
    
    /// <summary>
    /// Сохраняет текущие настройки в файл.
    /// </summary>
    public void Save()
    {
        try
        {
            // Создаём директорию если не существует
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
            Console.WriteLine($"Ошибка сохранения конфигурации: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Обновляет настройки и сохраняет их.
    /// </summary>
    public void UpdateSettings(Action<AppSettings> updateAction)
    {
        updateAction(Settings);
        Save();
    }
}
