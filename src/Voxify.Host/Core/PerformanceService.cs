using Voxify.Core;

namespace Voxify.Core;

/// <summary>
/// Performance metrics for measuring latency.
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Time when hotkey was pressed (recording start requested).
    /// </summary>
    public DateTime HotkeyPressedTime { get; set; }

    /// <summary>
    /// Time when recording actually started.
    /// </summary>
    public DateTime RecordingStartTime { get; set; }

    /// <summary>
    /// Time when speech was detected.
    /// </summary>
    public DateTime SpeechDetectedTime { get; set; }

    /// <summary>
    /// Time when recording stopped.
    /// </summary>
    public DateTime RecordingStopTime { get; set; }

    /// <summary>
    /// Time when recognition completed.
    /// </summary>
    public DateTime RecognitionCompleteTime { get; set; }

    /// <summary>
    /// Time when text was injected.
    /// </summary>
    public DateTime TextInjectedTime { get; set; }

    /// <summary>
    /// Total latency from hotkey press to text injection (ms).
    /// </summary>
    public double TotalLatencyMs => (TextInjectedTime - HotkeyPressedTime).TotalMilliseconds;

    /// <summary>
    /// Recording latency from hotkey press to recording start (ms).
    /// </summary>
    public double RecordingLatencyMs => (RecordingStartTime - HotkeyPressedTime).TotalMilliseconds;

    /// <summary>
    /// Recognition latency from recording stop to recognition complete (ms).
    /// </summary>
    public double RecognitionLatencyMs => (RecognitionCompleteTime - RecordingStopTime).TotalMilliseconds;

    /// <summary>
    /// Injection latency from recognition complete to text injected (ms).
    /// </summary>
    public double InjectionLatencyMs => (TextInjectedTime - RecognitionCompleteTime).TotalMilliseconds;

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    public void Reset()
    {
        HotkeyPressedTime = DateTime.MinValue;
        RecordingStartTime = DateTime.MinValue;
        SpeechDetectedTime = DateTime.MinValue;
        RecordingStopTime = DateTime.MinValue;
        RecognitionCompleteTime = DateTime.MinValue;
        TextInjectedTime = DateTime.MinValue;
    }

    /// <summary>
    /// Returns metrics summary for logging.
    /// </summary>
    public override string ToString()
    {
        return $"Total: {TotalLatencyMs:F0}ms | Recording: {RecordingLatencyMs:F0}ms | Recognition: {RecognitionLatencyMs:F0}ms | Injection: {InjectionLatencyMs:F0}ms";
    }
}

/// <summary>
/// Performance monitoring service.
/// </summary>
public class PerformanceService
{
    private readonly DebugService? _debugService;
    private readonly PerformanceMetrics _metrics = new();

    /// <summary>
    /// Event fired when performance metrics are available.
    /// </summary>
    public event EventHandler<PerformanceMetrics>? MetricsAvailable;

    public PerformanceService(DebugService? debugService = null)
    {
        _debugService = debugService;
    }

    /// <summary>
    /// Records hotkey press time.
    /// </summary>
    public void OnHotkeyPressed()
    {
        _metrics.Reset();
        _metrics.HotkeyPressedTime = DateTime.Now;
        _debugService?.Log("Performance", "Hotkey pressed - starting timing", LogLevel.Debug);
    }

    /// <summary>
    /// Records recording start time.
    /// </summary>
    public void OnRecordingStarted()
    {
        _metrics.RecordingStartTime = DateTime.Now;
        var latency = _metrics.RecordingLatencyMs;
        _debugService?.Log("Performance", $"Recording started (latency: {latency:F0}ms)", LogLevel.Debug);
    }

    /// <summary>
    /// Records speech detection time.
    /// </summary>
    public void OnSpeechDetected()
    {
        _metrics.SpeechDetectedTime = DateTime.Now;
        var delay = (_metrics.SpeechDetectedTime - _metrics.RecordingStartTime).TotalMilliseconds;
        _debugService?.Log("Performance", $"Speech detected (delay: {delay:F0}ms)", LogLevel.Debug);
    }

    /// <summary>
    /// Records recording stop time.
    /// </summary>
    public void OnRecordingStopped()
    {
        _metrics.RecordingStopTime = DateTime.Now;
        var duration = (_metrics.RecordingStopTime - _metrics.RecordingStartTime).TotalMilliseconds;
        _debugService?.Log("Performance", $"Recording stopped (duration: {duration:F0}ms)", LogLevel.Debug);
    }

    /// <summary>
    /// Records recognition complete time.
    /// </summary>
    public void OnRecognitionComplete()
    {
        _metrics.RecognitionCompleteTime = DateTime.Now;
        var latency = _metrics.RecognitionLatencyMs;
        _debugService?.Log("Performance", $"Recognition complete (latency: {latency:F0}ms)", LogLevel.Debug);
    }

    /// <summary>
    /// Records text injection time and publishes metrics.
    /// </summary>
    public void OnTextInjected()
    {
        _metrics.TextInjectedTime = DateTime.Now;
        
        var injectionLatency = _metrics.InjectionLatencyMs;
        var totalLatency = _metrics.TotalLatencyMs;
        
        _debugService?.Log("Performance", $"Text injected (injection: {injectionLatency:F0}ms, total: {totalLatency:F0}ms)", LogLevel.Info);
        _debugService?.Log("Performance", _metrics.ToString(), LogLevel.Info);

        MetricsAvailable?.Invoke(this, _metrics);
    }

    /// <summary>
    /// Gets current metrics snapshot.
    /// </summary>
    public PerformanceMetrics GetMetrics()
    {
        return new PerformanceMetrics
        {
            HotkeyPressedTime = _metrics.HotkeyPressedTime,
            RecordingStartTime = _metrics.RecordingStartTime,
            SpeechDetectedTime = _metrics.SpeechDetectedTime,
            RecordingStopTime = _metrics.RecordingStopTime,
            RecognitionCompleteTime = _metrics.RecognitionCompleteTime,
            TextInjectedTime = _metrics.TextInjectedTime
        };
    }
}
