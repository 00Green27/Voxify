using NAudio.Wave;

namespace Voxify.Core;

/// <summary>
/// Recorder operating mode.
/// </summary>
public enum RecordingMode
{
    /// <summary>
    /// Recording without VAD (always records).
    /// </summary>
    Always,

    /// <summary>
    /// Recording starts on speech detection (simple volume threshold).
    /// </summary>
    SimpleVad,

    /// <summary>
    /// Recording starts on speech detection (Silero ML model).
    /// </summary>
    SileroVad
}

/// <summary>
/// Component for recording audio from microphone with VAD support.
/// </summary>
public class AudioRecorder : IDisposable
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _audioStream;
    private bool _isRecording;
    private bool _isDisposed;
    private bool _isSpeechDetected;
    private DateTime _speechStartTime;
    private DateTime _lastSpeechTime;
    private readonly RecordingMode _recordingMode;
    private readonly float _silenceThreshold;
    private readonly int _minSpeechDurationMs;
    private readonly int _minSilenceDurationMs;
    private readonly IVadEngine? _vadEngine;
    private readonly object _lock = new();
    private readonly DebugService? _debugService;

    /// <summary>
    /// Event fired when recording is started.
    /// </summary>
    public event EventHandler? RecordingStarted;

    /// <summary>
    /// Event fired when receiving data from microphone.
    /// </summary>
    public event EventHandler<WaveBuffer>? DataAvailable;

    /// <summary>
    /// Event fired when recording is stopped.
    /// </summary>
    public event EventHandler? RecordingStopped;

    /// <summary>
    /// Event fired when speech is detected (VAD activation).
    /// </summary>
    public event EventHandler? SpeechDetected;

    /// <summary>
    /// Event fired when speech ends (VAD deactivation).
    /// </summary>
    public event EventHandler? SpeechEnded;

    /// <summary>
    /// Checks if recording is in progress.
    /// </summary>
    public bool IsRecording => _isRecording;

    /// <summary>
    /// Checks if speech is detected (for VAD modes).
    /// </summary>
    public bool IsSpeechDetected => _isSpeechDetected;

    /// <summary>
    /// Creates a new AudioRecorder instance with VAD support.
    /// </summary>
    /// <param name="recordingMode">Recording mode.</param>
    /// <param name="silenceThreshold">Silence threshold for SimpleVAD (0.0-1.0).</param>
    /// <param name="minSpeechDurationMs">Minimum speech duration in milliseconds.</param>
    /// <param name="minSilenceDurationMs">Minimum silence duration before stopping (ms).</param>
    /// <param name="vadEngine">VAD engine for SileroVad mode.</param>
    /// <param name="debugService">Debug service for logging.</param>
    public AudioRecorder(
        RecordingMode recordingMode = RecordingMode.Always,
        float silenceThreshold = 0.05f,
        int minSpeechDurationMs = 500,
        int minSilenceDurationMs = 500,
        IVadEngine? vadEngine = null,
        DebugService? debugService = null)
    {
        _recordingMode = recordingMode;
        _silenceThreshold = silenceThreshold;
        _minSpeechDurationMs = minSpeechDurationMs;
        _minSilenceDurationMs = minSilenceDurationMs;
        _vadEngine = vadEngine;
        _debugService = debugService;

        if (recordingMode == RecordingMode.SileroVad && vadEngine == null)
        {
            throw new ArgumentException("VadEngine must be provided for SileroVad mode");
        }
    }

    /// <summary>
    /// Starts recording from microphone.
    /// </summary>
    public void StartRecording()
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Recording is already in progress");
        }

        // Check if microphone is available
        if (!IsMicrophoneAvailable())
        {
            throw new InvalidOperationException("No microphone devices available. Please check your audio input settings.");
        }

        lock (_lock)
        {
            _audioStream = new MemoryStream();
            _waveIn = new WaveInEvent
            {
                // Format: 16kHz, 16-bit, mono (required by Vosk and Silero VAD)
                // Buffer: 30ms - optimal balance between stutter risk and latency
                WaveFormat = new WaveFormat(16000, 16, 1),
                BufferMilliseconds = 30
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            try
            {
                _waveIn.StartRecording();
                _isRecording = true;
                _isSpeechDetected = _recordingMode == RecordingMode.Always;
                _speechStartTime = DateTime.MinValue;
                _lastSpeechTime = DateTime.MinValue;

                // Recording started event
                RecordingStarted?.Invoke(this, EventArgs.Empty);

                // Initialize VAD if needed
                if (_recordingMode == RecordingMode.SileroVad && _vadEngine != null && !_vadEngine.IsInitialized)
                {
                    // VAD should be initialized beforehand
                    Console.WriteLine("[AudioRecorder] VAD engine should be initialized before recording");
                }
            }
            catch (Exception ex)
            {
                // Clean up on error
                _waveIn?.Dispose();
                _waveIn = null;
                _audioStream?.Dispose();
                _audioStream = null;
                throw new InvalidOperationException($"Failed to start recording: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Stops recording and returns audio data.
    /// </summary>
    public async Task<byte[]> StopRecordingAsync()
    {
        Console.WriteLine($"[AudioRecorder] StopRecordingAsync called, _isRecording={_isRecording}, _audioStream length={_audioStream?.Length ?? 0}");

        if (!_isRecording)
        {
            return Array.Empty<byte>();
        }

        lock (_lock)
        {
            _waveIn?.StopRecording();
        }

        // Wait for processing to complete
        await Task.Delay(100);

        _isRecording = false;

        if (_audioStream != null)
        {
            var data = _audioStream.ToArray();
            Console.WriteLine($"[AudioRecorder] StopRecordingAsync returning {data.Length} bytes");
            return data;
        }

        Console.WriteLine("[AudioRecorder] StopRecordingAsync: audioStream is null");
        return Array.Empty<byte>();
    }

    /// <summary>
    /// Force stops recording and returns audio.
    /// </summary>
    public async Task<byte[]> ForceStopAsync()
    {
        return await StopRecordingAsync();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_audioStream == null || e.Buffer == null)
        {
            Console.WriteLine("[AudioRecorder] DataAvailable: buffer or stream is null");
            return;
        }

        // Always write to buffer
        _audioStream.Write(e.Buffer, 0, e.BytesRecorded);
        Console.WriteLine($"[AudioRecorder] DataAvailable: {e.BytesRecorded} bytes, total: {_audioStream.Length} bytes");
        DataAvailable?.Invoke(this, new WaveBuffer(e.Buffer, 0, e.BytesRecorded));

        // Process VAD if needed
        if (_recordingMode != RecordingMode.Always)
        {
            ProcessVad(e.Buffer, e.BytesRecorded);
        }
    }

    private void ProcessVad(byte[] buffer, int bytesRecorded)
    {
        // Convert to float samples (-1.0 to 1.0)
        float[] samples = BufferToFloatSamples(buffer, bytesRecorded);

        bool speechDetected = _recordingMode switch
        {
            RecordingMode.SimpleVad => DetectSimpleVad(samples),
            RecordingMode.SileroVad => DetectSileroVad(samples),
            _ => true
        };

        HandleSpeechState(speechDetected);
    }

    /// <summary>
    /// Simple speech detector by volume threshold.
    /// </summary>
    private bool DetectSimpleVad(float[] samples)
    {
        if (samples.Length == 0)
        {
            return false;
        }

        // Calculate RMS (Root Mean Square) amplitude
        double sum = 0;
        foreach (var sample in samples)
        {
            sum += sample * sample;
        }
        double rms = Math.Sqrt(sum / samples.Length);

        // Update debug state
        _debugService?.UpdateAudioLevel((float)rms);

        return rms > _silenceThreshold;
    }

    /// <summary>
    /// Speech detector based on Silero VAD.
    /// </summary>
    private bool DetectSileroVad(float[] samples)
    {
        if (_vadEngine == null || !_vadEngine.IsInitialized)
        {
            // Fallback to simple VAD if Silero is not initialized
            return DetectSimpleVad(samples);
        }

        try
        {
            var result = _vadEngine.DetectSpeech(samples);
            
            // Update debug state
            _debugService?.UpdateVadState(result.IsSpeech, result.Confidence);
            
            return result.IsSpeech && result.Confidence >= _silenceThreshold;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AudioRecorder] Silero VAD error: {ex.Message}");
            return DetectSimpleVad(samples);
        }
    }

    /// <summary>
    /// Handles speech state (start/end).
    /// </summary>
    private void HandleSpeechState(bool detected)
    {
        var now = DateTime.Now;

        if (!_isSpeechDetected && detected)
        {
            // Potential speech start
            if (_speechStartTime == DateTime.MinValue)
            {
                _speechStartTime = now;
            }

            // Check if minimum duration passed
            if ((now - _speechStartTime).TotalMilliseconds >= _minSpeechDurationMs)
            {
                _isSpeechDetected = true;
                _lastSpeechTime = now;
                _debugService?.Log("AudioRecorder", "Speech started", LogLevel.Debug);
                SpeechDetected?.Invoke(this, EventArgs.Empty);
                Console.WriteLine($"[AudioRecorder] Speech detected at {now:HH:mm:ss.fff}");
            }
        }
        else if (_isSpeechDetected)
        {
            if (detected)
            {
                _lastSpeechTime = now;
            }
            else
            {
                // Check if minimum silence duration passed
                if ((now - _lastSpeechTime).TotalMilliseconds >= _minSilenceDurationMs)
                {
                    _isSpeechDetected = false;
                    _speechStartTime = DateTime.MinValue;
                    _debugService?.Log("AudioRecorder", "Speech ended", LogLevel.Debug);
                    SpeechEnded?.Invoke(this, EventArgs.Empty);
                    Console.WriteLine($"[AudioRecorder] Speech ended at {now:HH:mm:ss.fff}");
                }
            }
        }
    }

    /// <summary>
    /// Converts byte[] buffer to float[] samples (-1.0 to 1.0).
    /// </summary>
    private static float[] BufferToFloatSamples(byte[] buffer, int bytesRecorded)
    {
        int sampleCount = bytesRecorded / 2; // 16-bit = 2 bytes per sample
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            // Little-endian 16-bit PCM
            short sample = BitConverter.ToInt16(buffer, i * 2);
            samples[i] = sample / 32768f; // Normalize to -1.0..1.0
        }

        return samples;
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _isRecording = false;
        _isSpeechDetected = false;
        _speechStartTime = DateTime.MinValue;

        // Recording stopped event
        RecordingStopped?.Invoke(this, EventArgs.Empty);

        // Reset VAD
        if (_recordingMode == RecordingMode.SileroVad && _vadEngine != null)
        {
            _vadEngine.Reset();
        }
    }

    /// <summary>
    /// Checks microphone availability in the system.
    /// </summary>
    public static bool IsMicrophoneAvailable()
    {
        return WaveIn.DeviceCount > 0;
    }

    /// <summary>
    /// Returns list of available recording devices.
    /// </summary>
    public static string[] GetAvailableDevices()
    {
        var devices = new List<string>();
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            var caps = WaveIn.GetCapabilities(i);
            devices.Add(caps.ProductName);
        }
        return devices.ToArray();
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            lock (_lock)
            {
                _waveIn?.Dispose();
                _audioStream?.Dispose();
                _vadEngine?.Dispose();
                _isDisposed = true;
            }
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Audio data buffer.
/// </summary>
public class WaveBuffer
{
    public byte[] Data { get; }
    public int Length { get; }

    public WaveBuffer(byte[] data, int offset, int length)
    {
        Data = new byte[length];
        Array.Copy(data, offset, Data, 0, length);
        Length = length;
    }
}
