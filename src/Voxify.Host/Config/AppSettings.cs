namespace Voxify.Config;

/// <summary>
/// Модель горячей клавиши с модификаторами.
/// </summary>
public class HotkeyConfig
{
    /// <summary>
    /// Модификаторы (Ctrl, Alt, Shift, Win).
    /// </summary>
    public List<string> Modifiers { get; set; } = ["Control"];
    
    /// <summary>
    /// Основная клавиша (например, F12).
    /// </summary>
    public string Key { get; set; } = "F12";
    
    /// <summary>
    /// Парсит строку модификатора в Windows API код.
    /// </summary>
    public static int GetModifierCode(string modifier) => modifier.ToLower() switch
    {
        "control" => 0x2,    // MOD_CONTROL
        "alt" => 0x1,        // MOD_ALT
        "shift" => 0x4,      // MOD_SHIFT
        "win" => 0x8,        // MOD_WIN
        _ => 0
    };
    
    /// <summary>
    /// Получает комбинированный код модификаторов.
    /// </summary>
    public int GetCombinedModifiers()
    {
        int result = 0;
        foreach (var modifier in Modifiers)
        {
            result |= GetModifierCode(modifier);
        }
        return result;
    }
    
    /// <summary>
    /// Возвращает человекочитаемое представление хоткея.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>(Modifiers.Select(m => m.Capitalize()));
        parts.Add(Key.ToUpper());
        return string.Join(" + ", parts);
    }
}

/// <summary>
/// Настройки детекции голосовой активности (VAD).
/// </summary>
public class VoiceActivityDetectionConfig
{
    /// <summary>
    /// Порог громкости для срабатывания (0.0 - 1.0).
    /// </summary>
    public float SilenceThreshold { get; set; } = 0.05f;
    
    /// <summary>
    /// Минимальная длительность речи в миллисекундах.
    /// </summary>
    public int MinSpeechDurationMs { get; set; } = 500;
}

/// <summary>
/// Настройки ввода текста.
/// </summary>
public class TextInputConfig
{
    /// <summary>
    /// Задержка между нажатиями клавиш в миллисекундах.
    /// </summary>
    public int TypeDelayMs { get; set; } = 10;
    
    /// <summary>
    /// Вставлять через буфер обмена вместо эмуляции клавиатуры.
    /// </summary>
    public bool PasteAsClipboard { get; set; } = false;
}

/// <summary>
/// Основные настройки приложения.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Путь к папке с моделью Vosk.
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Язык модели (ru-RU, en-US).
    /// </summary>
    public string Language { get; set; } = "ru-RU";
    
    /// <summary>
    /// Настройки горячей клавиши.
    /// </summary>
    public HotkeyConfig Hotkey { get; set; } = new();
    
    /// <summary>
    /// Настройки детекции голосовой активности.
    /// </summary>
    public VoiceActivityDetectionConfig VoiceActivityDetection { get; set; } = new();
    
    /// <summary>
    /// Настройки ввода текста.
    /// </summary>
    public TextInputConfig TextInput { get; set; } = new();
}

/// <summary>
/// Extension methods для работы со строками.
/// </summary>
public static class StringExtensions
{
    public static string Capitalize(this string s) => 
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
