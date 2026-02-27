namespace Voxify.Config;

/// <summary>
/// Hotkey model with modifiers.
/// </summary>
public class HotkeyConfig
{
    /// <summary>
    /// Hotkey mode: Toggle (press to start/stop) or PushToTalk (hold to record).
    /// </summary>
    public string Mode { get; set; } = "Toggle";

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
    /// Enable ML-based VAD.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// VAD mode: "simple" (volume threshold) or "silero" (ML model).
    /// </summary>
    public string Mode { get; set; } = "simple";

    /// <summary>
    /// Volume threshold for activation (0.0 - 1.0) - for "simple" mode.
    /// </summary>
    public float SilenceThreshold { get; set; } = 0.05f;

    /// <summary>
    /// Minimum speech duration in milliseconds.
    /// </summary>
    public int MinSpeechDurationMs { get; set; } = 500;

    /// <summary>
    /// Minimum silence duration before stopping recording (ms) - for "silero" mode.
    /// </summary>
    public int MinSilenceDurationMs { get; set; } = 500;

    /// <summary>
    /// Speech confidence threshold (0.0 - 1.0) - for "silero" mode.
    /// </summary>
    public float SpeechThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Path to Silero VAD model (ONNX file) - for "silero" mode.
    /// </summary>
    public string SileroModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to Silero VAD config (YAML file) - for "silero" mode.
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
/// Speech recognition settings.
/// </summary>
public class SpeechRecognitionConfig
{
    /// <summary>
    /// Speech provider: Vosk or Whisper.
    /// </summary>
    public string Provider { get; set; } = "Vosk";

    /// <summary>
    /// Path to speech recognition model folder/file.
    /// For Vosk: folder path (e.g., C:\Voxify\Models\vosk-model-small-ru-0.22)
    /// For Whisper: model file path (e.g., C:\Voxify\Models\ggml-tiny.bin)
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Model language (ru-RU, en-US, ru, en).
    /// </summary>
    public string Language { get; set; } = "ru-RU";

    /// <summary>
    /// Whisper model type (tiny, base, small, medium, large-v3).
    /// Used only when Provider is Whisper.
    /// </summary>
    public string WhisperModel { get; set; } = "tiny";
}

/// <summary>
/// Debug mode settings.
/// </summary>
public class DebugConfig
{
    /// <summary>
    /// Enable debug mode on startup.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Hotkey for opening debug window.
    /// </summary>
    public HotkeyConfig Hotkey { get; set; } = new()
    {
        Modifiers = ["Control", "Shift"],
        Key = "D"
    };

    /// <summary>
    /// Enable logging to file.
    /// </summary>
    public bool LogToFile { get; set; } = true;

    /// <summary>
    /// Path to log file (supports environment variables like %APPDATA%).
    /// </summary>
    public string LogPath { get; set; } = "%APPDATA%\\Voxify\\logs\\voxify.log";

    /// <summary>
    /// Maximum number of log lines to keep in memory (for debug window).
    /// </summary>
    public int MaxLogLines { get; set; } = 1000;
}

/// <summary>
/// System integration settings.
/// </summary>
public class SystemIntegrationConfig
{
    /// <summary>
    /// Enable single instance mode (only one instance can run at a time).
    /// </summary>
    public bool SingleInstance { get; set; } = true;

    /// <summary>
    /// Enable auto-start on Windows login.
    /// </summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>
    /// Start minimized (hidden) on startup.
    /// </summary>
    public bool StartHidden { get; set; } = false;
}

/// <summary>
/// UI and notification settings.
/// </summary>
public class UiConfig
{
    /// <summary>
    /// Show balloon tip notifications (Info level).
    /// Critical errors (Error, Warning) are always shown.
    /// </summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>
    /// Use dark theme for UI elements (false = light theme, null = auto-detect).
    /// </summary>
    public bool? UseDarkTheme { get; set; } = null;
}

/// <summary>
/// Main application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Path to Vosk model folder (legacy, for backward compatibility).
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Model language (ru-RU, en-US) (legacy).
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

    /// <summary>
    /// Speech recognition settings (new unified config).
    /// </summary>
    public SpeechRecognitionConfig SpeechRecognition { get; set; } = new();

    /// <summary>
    /// Debug mode settings.
    /// </summary>
    public DebugConfig Debug { get; set; } = new();

    /// <summary>
    /// System integration settings.
    /// </summary>
    public SystemIntegrationConfig SystemIntegration { get; set; } = new();

    /// <summary>
    /// UI and notification settings.
    /// </summary>
    public UiConfig Ui { get; set; } = new();
}

/// <summary>
/// Extension methods for strings.
/// </summary>
public static class StringExtensions
{
    public static string Capitalize(this string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
