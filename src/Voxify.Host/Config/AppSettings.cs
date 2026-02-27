namespace Voxify.Config;

/// <summary>
/// Hotkey model with modifiers.
/// </summary>
public class HotkeyConfig
{
    /// <summary>
    /// Modifiers (Ctrl, Alt, Shift, Win).
    /// </summary>
    public List<string> Modifiers { get; set; } = ["Control"];

    /// <summary>
    /// Main key (e.g., F12).
    /// </summary>
    public string Key { get; set; } = "F12";

    /// <summary>
    /// Parses modifier string to Windows API code.
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
    /// Gets combined modifier code.
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
    /// Returns human-readable hotkey representation.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>(Modifiers.Select(m => m.Capitalize()));
        parts.Add(Key.ToUpper());
        return string.Join(" + ", parts);
    }
}

/// <summary>
/// Voice Activity Detection (VAD) settings.
/// </summary>
public class VoiceActivityDetectionConfig
{
    /// <summary>
    /// Включить ли VAD на базе ML.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Тип VAD: "simple" (порог громкости) или "silero" (ML модель).
    /// </summary>
    public string Mode { get; set; } = "simple";

    /// <summary>
    /// Volume threshold for activation (0.0 - 1.0) - для режима "simple".
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Тип VAD: "simple" (порог громкости) или "silero" (ML модель).
    /// </summary>
    public string Mode { get; set; } = "simple";

    /// <summary>
    /// Volume threshold for activation (0.0 - 1.0) - для режима "simple".
    /// </summary>
    public float SilenceThreshold { get; set; } = 0.05f;

    /// <summary>
    /// Minimum speech duration in milliseconds.
    /// </summary>
    public int MinSpeechDurationMs { get; set; } = 500;

    /// <summary>
    /// Minimum silence duration before stopping recording (ms) - для режима "silero".
    /// </summary>
    public int MinSilenceDurationMs { get; set; } = 500;

    /// <summary>
    /// Порог уверенности для детекции речи (0.0 - 1.0) - для режима "silero".
    /// </summary>
    public float SpeechThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Путь к модели Silero VAD (ONNX файл) - для режима "silero".
    /// </summary>
    public string SileroModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Путь к конфигурации Silero VAD (YAML файл) - для режима "silero".
    /// </summary>
    public string SileroConfigPath { get; set; } = string.Empty;
}

/// <summary>
/// Text input settings.
/// </summary>
public class TextInputConfig
{
    /// <summary>
    /// Delay between key presses in milliseconds.
    /// </summary>
    public int TypeDelayMs { get; set; } = 10;

    /// <summary>
    /// Paste via clipboard instead of keyboard emulation.
    /// </summary>
    public bool PasteAsClipboard { get; set; } = false;
}

/// <summary>
/// Main application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Path to Vosk model folder.
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Model language (ru-RU, en-US).
    /// </summary>
    public string Language { get; set; } = "ru-RU";

    /// <summary>
    /// Hotkey settings.
    /// </summary>
    public HotkeyConfig Hotkey { get; set; } = new();

    /// <summary>
    /// Voice Activity Detection settings.
    /// </summary>
    public VoiceActivityDetectionConfig VoiceActivityDetection { get; set; } = new();

    /// <summary>
    /// Text input settings.
    /// </summary>
    public TextInputConfig TextInput { get; set; } = new();
}

/// <summary>
/// Extension methods for strings.
/// </summary>
public static class StringExtensions
{
    public static string Capitalize(this string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
