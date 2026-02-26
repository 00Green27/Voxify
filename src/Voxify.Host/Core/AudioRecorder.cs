using NAudio.Wave;

namespace Voxify.Core;

/// <summary>
/// Component for recording audio from microphone.
/// </summary>
public class AudioRecorder : IDisposable
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _audioStream;
    private bool _isRecording;
    private bool _disposed;

    /// <summary>
    /// Event fired when receiving data from microphone.
    /// </summary>
    public event EventHandler<WaveBuffer>? DataAvailable;

    /// <summary>
    /// Starts recording from microphone.
    /// </summary>
    public void StartRecording()
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Recording is already in progress");
        }

        _audioStream = new MemoryStream();
        _waveIn = new WaveInEvent
        {
            // Format: 16kHz, 16-bit, mono (required by Vosk)
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 200
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _waveIn.StartRecording();
        _isRecording = true;
    }

    /// <summary>
    /// Stops recording and returns audio data.
    /// </summary>
    public async Task<byte[]> StopRecordingAsync()
    {
        if (!_isRecording)
        {
            return Array.Empty<byte>();
        }

        _waveIn?.StopRecording();

        // Wait for processing to complete
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
        if (!_disposed)
        {
            _waveIn?.Dispose();
            _audioStream?.Dispose();
            _disposed = true;
        }
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
