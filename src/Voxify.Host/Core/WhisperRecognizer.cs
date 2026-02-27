using Whisper.net;
using Whisper.net.Wave;

namespace Voxify.Core;

/// <summary>
/// Speech recognition implementation via Whisper.
/// </summary>
public class WhisperRecognizer : ISpeechRecognizer
{
    private readonly AudioRecorder _audioRecorder;
    private string _modelPath = string.Empty;
    private string _language = "ru";
    private bool _isInitialized;
    private bool _disposed;

    public SpeechProvider Provider => SpeechProvider.Whisper;
    public bool IsInitialized => _isInitialized;

    public WhisperRecognizer(AudioRecorder audioRecorder)
    {
        _audioRecorder = audioRecorder;
    }

    /// <summary>
    /// Initializes recognizer with specified Whisper model.
    /// </summary>
    /// <param name="modelPath">Path to Whisper model file (.bin)</param>
    /// <param name="language">Language code (e.g., "ru", "en")</param>
    public async Task InitializeAsync(string modelPath, string language)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Whisper model not found: {modelPath}");
        }

        _modelPath = modelPath;
        _language = language.ToLower().StartsWith("ru") ? "ru" : "en";
        _isInitialized = true;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Recognizes speech from audio data.
    /// </summary>
    public async Task<string?> RecognizeAsync(byte[] audioData, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Whisper recognizer is not initialized");
        }

        if (audioData.Length == 0)
        {
            return null;
        }

        try
        {
            // Convert audio to WAV format for Whisper
            // Audio should be 16kHz, 16-bit, mono (as recorded by AudioRecorder)
            using var memoryStream = new MemoryStream();
            
            // Write WAV header
            WriteWavHeader(memoryStream, audioData);
            memoryStream.Write(audioData, 0, audioData.Length);
            memoryStream.Position = 0;

            // Process with Whisper
            var result = await ProcessAudioAsync(memoryStream, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Whisper recognition error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Processes audio data with Whisper.
    /// </summary>
    private async Task<string?> ProcessAudioAsync(Stream audioStream, CancellationToken cancellationToken)
    {
        var segments = new List<string>();

        // Create Whisper factory and processor
        using var factory = WhisperFactory.FromPath(_modelPath);
        
        var builder = factory.CreateBuilder()
            .WithLanguage(_language);

        using var processor = builder.Build();

        // Whisper processes audio and returns segments
        await foreach (var segment in processor.ProcessAsync(audioStream, cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(segment.Text))
            {
                segments.Add(segment.Text.Trim());
            }
        }

        return segments.Count > 0 ? string.Join(" ", segments) : null;
    }

    /// <summary>
    /// Writes WAV header for 16kHz, 16-bit, mono audio.
    /// </summary>
    private static void WriteWavHeader(Stream stream, byte[] audioData)
    {
        const int sampleRate = 16000;
        const int bitsPerSample = 16;
        const int channels = 1;

        var header = new List<byte>();

        // RIFF header
        header.AddRange(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        header.AddRange(BitConverter.GetBytes(36 + audioData.Length)); // File size - 8
        header.AddRange(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt subchunk
        header.AddRange(System.Text.Encoding.ASCII.GetBytes("fmt "));
        header.AddRange(BitConverter.GetBytes(16)); // Subchunk1Size (16 for PCM)
        header.AddRange(BitConverter.GetBytes((short)1)); // AudioFormat (1 for PCM)
        header.AddRange(BitConverter.GetBytes((short)channels));
        header.AddRange(BitConverter.GetBytes(sampleRate));
        header.AddRange(BitConverter.GetBytes(sampleRate * channels * bitsPerSample / 8)); // ByteRate
        header.AddRange(BitConverter.GetBytes((short)(channels * bitsPerSample / 8))); // BlockAlign
        header.AddRange(BitConverter.GetBytes((short)bitsPerSample));

        // data subchunk
        header.AddRange(System.Text.Encoding.ASCII.GetBytes("data"));
        header.AddRange(BitConverter.GetBytes(audioData.Length));

        stream.Write(header.ToArray(), 0, header.Count);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _audioRecorder.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
