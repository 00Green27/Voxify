using NAudio.Wave;

namespace Voxify.Core;

/// <summary>
/// Компонент для записи аудио с микрофона.
/// </summary>
public class AudioRecorder : IDisposable
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _audioStream;
    private bool _isRecording;
    private bool _disposed;

    /// <summary>
    /// Событие при получении данных с микрофона.
    /// </summary>
    public event EventHandler<WaveBuffer>? DataAvailable;

    /// <summary>
    /// Начинает запись с микрофона.
    /// </summary>
    public void StartRecording()
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Запись уже идёт");
        }

        _audioStream = new MemoryStream();
        _waveIn = new WaveInEvent
        {
            // Формат: 16kHz, 16-bit, mono (требуется Vosk)
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 200
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _waveIn.StartRecording();
        _isRecording = true;
    }

    /// <summary>
    /// Останавливает запись и возвращает аудио-данные.
    /// </summary>
    public async Task<byte[]> StopRecordingAsync()
    {
        if (!_isRecording)
        {
            return Array.Empty<byte>();
        }

        _waveIn?.StopRecording();
        
        // Ждём завершения обработки
        await Task.Delay(100);

        _isRecording = false;

        if (_audioStream != null)
        {
            return _audioStream.ToArray();
        }

        return Array.Empty<byte>();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_audioStream != null && e.Buffer != null)
        {
            _audioStream.Write(e.Buffer, 0, e.BytesRecorded);
            DataAvailable?.Invoke(this, new WaveBuffer(e.Buffer, 0, e.BytesRecorded));
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _isRecording = false;
    }

    /// <summary>
    /// Проверяет доступность микрофонов в системе.
    /// </summary>
    public static bool IsMicrophoneAvailable()
    {
        return WaveIn.DeviceCount > 0;
    }

    /// <summary>
    /// Возвращает список доступных устройств записи.
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
        if (!_disposed)
        {
            _waveIn?.Dispose();
            _audioStream?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Буфер аудио-данных.
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
