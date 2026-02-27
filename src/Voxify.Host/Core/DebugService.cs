using Voxify.Config;

namespace Voxify.Core;

/// <summary>
/// Represents a single log entry in the debug service.
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public LogLevel Level { get; init; }

    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss.fff}] {Category}: {Message}";
    }
}

/// <summary>
/// Log level for debug entries.
/// </summary>
public enum LogLevel
{
    Info,
    Warning,
    Error,
    Debug
}

/// <summary>
/// Debug state of the application.
/// </summary>
public class DebugState
{
    public bool IsRecording { get; set; }
    public float AudioLevel { get; set; }
    public bool IsVadActive { get; set; }
    public float VadConfidence { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string ProcessedText { get; set; } = string.Empty;
    public RecordingState RecordingState { get; set; } = RecordingState.Idle;
    public string SpeechProvider { get; set; } = string.Empty;
}

/// <summary>
/// Central debug service for logging and state tracking.
/// </summary>
public class DebugService : IDisposable
{
    private readonly object _lock = new();
    private readonly List<LogEntry> _logs = new();
    private readonly DebugConfig _config;
    private StreamWriter? _logWriter;
    private bool _disposed;

    /// <summary>
    /// Current debug state.
    /// </summary>
    public DebugState State { get; } = new();

    /// <summary>
    /// Event fired when a new log entry is added.
    /// </summary>
    public event EventHandler<LogEntry>? LogAdded;

    /// <summary>
    /// Event fired when debug state changes.
    /// </summary>
    public event EventHandler<DebugState>? StateChanged;

    /// <summary>
    /// Gets the list of log entries (read-only).
    /// </summary>
    public IReadOnlyList<LogEntry> Logs
    {
        get
        {
            lock (_lock)
            {
                return _logs.AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets whether debug mode is enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    public DebugService(DebugConfig config)
    {
        _config = config;
        IsEnabled = config.Enabled;

        if (config.LogToFile)
        {
            InitializeLogFile(config.LogPath);
        }
    }

    /// <summary>
    /// Enables or disables debug mode.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        Log("Debug", $"Debug mode {(enabled ? "enabled" : "disabled")}", LogLevel.Info);
    }

    /// <summary>
    /// Logs a message with the specified category and level.
    /// </summary>
    public void Log(string category, string message, LogLevel level = LogLevel.Info)
    {
        if (!IsEnabled)
            return;

        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Category = category,
            Message = message,
            Level = level
        };

        lock (_lock)
        {
            _logs.Add(entry);

            // Trim old logs if exceeding max
            if (_logs.Count > _config.MaxLogLines)
            {
                _logs.RemoveAt(0);
            }
        }

        // Write to file if enabled
        _logWriter?.WriteLine(entry.ToString());
        _logWriter?.Flush();

        // Fire event
        LogAdded?.Invoke(this, entry);
    }

    /// <summary>
    /// Updates the audio level in debug state.
    /// </summary>
    public void UpdateAudioLevel(float level)
    {
        if (!IsEnabled)
            return;

        State.AudioLevel = level;
        NotifyStateChanged();
    }

    /// <summary>
    /// Updates the recording state.
    /// </summary>
    public void UpdateRecordingState(RecordingState state, bool isRecording)
    {
        if (!IsEnabled)
            return;

        State.RecordingState = state;
        State.IsRecording = isRecording;
        Log("State", $"Recording state: {state}, IsRecording: {isRecording}", LogLevel.Debug);
        NotifyStateChanged();
    }

    /// <summary>
    /// Updates VAD state.
    /// </summary>
    public void UpdateVadState(bool isActive, float confidence = 0f)
    {
        if (!IsEnabled)
            return;

        State.IsVadActive = isActive;
        State.VadConfidence = confidence;

        if (isActive)
        {
            Log("VAD", $"Speech detected (confidence: {confidence:F2})", LogLevel.Debug);
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the raw recognized text.
    /// </summary>
    public void SetRawText(string text)
    {
        if (!IsEnabled)
            return;

        State.RawText = text;
        Log("Recognition", $"Raw text: \"{text}\"", LogLevel.Debug);
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the processed text.
    /// </summary>
    public void SetProcessedText(string text)
    {
        if (!IsEnabled)
            return;

        State.ProcessedText = text;
        Log("Recognition", $"Processed text: \"{text}\"", LogLevel.Debug);
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the speech provider name.
    /// </summary>
    public void SetSpeechProvider(string provider)
    {
        if (!IsEnabled)
            return;

        State.SpeechProvider = provider;
        Log("Config", $"Speech provider: {provider}", LogLevel.Info);
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, State);
    }

    private void InitializeLogFile(string logPath)
    {
        try
        {
            // Expand environment variables
            var expandedPath = Environment.ExpandEnvironmentVariables(logPath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(expandedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _logWriter = new StreamWriter(expandedPath, append: true)
            {
                AutoFlush = true
            };

            Log("Debug", $"Log file initialized: {expandedPath}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DebugService] Failed to initialize log file: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logWriter?.Dispose();
            _disposed = true;
        }
    }
}
