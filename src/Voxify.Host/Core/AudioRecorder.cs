using NAudio.Wave;

namespace Voxify.Core;

/// <summary>
/// Режим работы рекордера.
/// </summary>
public enum RecordingMode
{
    /// <summary>
    /// Запись без VAD (всегда записывает).
    /// </summary>
    Always,

    /// <summary>
    /// Запись запускается при детекции речи (простой порог громкости).
    /// </summary>
    SimpleVad,

    /// <summary>
    /// Запись запускается при детекции речи (ML модель Silero).
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
    /// Проверяет, идёт ли запись.
    /// </summary>
    public bool IsRecording => _isRecording;

    /// <summary>
    /// Проверяет, обнаружена ли речь (для VAD режимов).
    /// </summary>
    public bool IsSpeechDetected => _isSpeechDetected;

    /// <summary>
    /// Создаёт новый экземпляр AudioRecorder с VAD поддержкой.
    /// </summary>
    /// <param name="recordingMode">Режим записи.</param>
    /// <param name="silenceThreshold">Порог тишины для SimpleVAD (0.0-1.0).</param>
    /// <param name="minSpeechDurationMs">Минимальная длительность речи (мс).</param>
    /// <param name="minSilenceDurationMs">Минимальная длительность тишины для остановки (мс).</param>
    /// <param name="vadEngine">VAD двигатель для SileroVad режима.</param>
    /// <param name="debugService">Сервис отладки для логирования.</param>
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
            throw new ArgumentException("VadEngine должен быть предоставлен для режима SileroVad");
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

                // Событие о начале записи
                RecordingStarted?.Invoke(this, EventArgs.Empty);

                // Инициализируем VAD если нужно
                if (_recordingMode == RecordingMode.SileroVad && _vadEngine != null && !_vadEngine.IsInitialized)
                {
                    // VAD должен быть инициализирован заранее
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
    /// Принудительно останавливает запись и возвращает аудио.
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

        // Всегда записываем в буфер
        _audioStream.Write(e.Buffer, 0, e.BytesRecorded);
        Console.WriteLine($"[AudioRecorder] DataAvailable: {e.BytesRecorded} bytes, total: {_audioStream.Length} bytes");
        DataAvailable?.Invoke(this, new WaveBuffer(e.Buffer, 0, e.BytesRecorded));

        // Обрабатываем VAD если нужно
        if (_recordingMode != RecordingMode.Always)
        {
            ProcessVad(e.Buffer, e.BytesRecorded);
        }
    }

    private void ProcessVad(byte[] buffer, int bytesRecorded)
    {
        // Конвертируем в float сэмплы (-1.0 до 1.0)
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
    /// Простой детектор речи по порогу громкости.
    /// </summary>
    private bool DetectSimpleVad(float[] samples)
    {
        if (samples.Length == 0)
        {
            return false;
        }

        // Вычисляем среднеквадратичную амплитуду (RMS)
        double sum = 0;
        foreach (var sample in samples)
        {
            sum += sample * sample;
        }
        double rms = Math.Sqrt(sum / samples.Length);

        // Обновляем debug state
        _debugService?.UpdateAudioLevel((float)rms);

        return rms > _silenceThreshold;
    }

    /// <summary>
    /// Детектор речи на базе Silero VAD.
    /// </summary>
    private bool DetectSileroVad(float[] samples)
    {
        if (_vadEngine == null || !_vadEngine.IsInitialized)
        {
            // Fallback на простой VAD если Silero не инициализирован
            return DetectSimpleVad(samples);
        }

        try
        {
            var result = _vadEngine.DetectSpeech(samples);
            
            // Обновляем debug state
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
    /// Обрабатывает состояние речи (начало/конец).
    /// </summary>
    private void HandleSpeechState(bool detected)
    {
        var now = DateTime.Now;

        if (!_isSpeechDetected && detected)
        {
            // Потенциальное начало речи
            if (_speechStartTime == DateTime.MinValue)
            {
                _speechStartTime = now;
            }

            // Проверяем, прошла ли минимальная длительность
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
                // Проверяем, прошла ли минимальная длительность тишины
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
    /// Конвертирует byte[] буфер в float[] сэмплы (-1.0 до 1.0).
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

        // Событие об остановке записи
        RecordingStopped?.Invoke(this, EventArgs.Empty);

        // Сбрасываем VAD
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
