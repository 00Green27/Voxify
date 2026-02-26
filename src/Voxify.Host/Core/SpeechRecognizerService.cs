namespace Voxify.Core;

/// <summary>
/// Speech recognition implementation via Vosk.
/// </summary>
public class SpeechRecognizerService : ISpeechRecognizer
{
    private readonly VoskEngine _voskEngine;
    private readonly AudioRecorder _audioRecorder;
    private bool _isInitialized;
    private bool _disposed;

    public SpeechRecognizerService(VoskEngine voskEngine, AudioRecorder audioRecorder)
    {
        _voskEngine = voskEngine;
        _audioRecorder = audioRecorder;
    }

    /// <summary>
    /// Initializes recognizer with specified model.
    /// </summary>
    public async Task InitializeAsync(string modelPath, string language)
    {
        await _voskEngine.InitializeAsync(modelPath, language);
        _isInitialized = true;
    }

    /// <summary>
    /// Records audio from microphone and recognizes speech.
    /// </summary>
    public async Task<string?> RecognizeAsync(byte[]? audioData = null, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Recognizer is not initialized");
        }

        try
        {
            // Record audio
            byte[] audioBytes;
            if (audioData != null)
            {
                audioBytes = audioData;
            }
            else
            {
                _audioRecorder.StartRecording();

                // Wait a few seconds for recording
                await Task.Delay(3000, cancellationToken);

                audioBytes = await _audioRecorder.StopRecordingAsync();
            }

            if (audioBytes.Length == 0)
            {
                return null;
            }

            // Recognize via Vosk
            using var recognizer = _voskEngine.CreateRecognizer();

            // Accept audio data in 4KB chunks
            int chunkSize = 4096;
            for (int i = 0; i < audioBytes.Length; i += chunkSize)
            {
                int remaining = audioBytes.Length - i;
                int size = Math.Min(chunkSize, remaining);
                byte[] chunk = new byte[size];
                Array.Copy(audioBytes, i, chunk, 0, size);

                if (recognizer.AcceptWaveform(chunk, size))
                {
                    // Get full result
                    var result = recognizer.Result();
                    if (!string.IsNullOrEmpty(result))
                    {
                        return ExtractTextFromResult(result);
                    }
                }
            }

            // Final result (partial)
            var finalResult = recognizer.FinalResult();
            return ExtractTextFromResult(finalResult);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Recognition error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extracts text from Vosk JSON result.
    /// </summary>
    private static string? ExtractTextFromResult(string jsonResult)
    {
        if (string.IsNullOrEmpty(jsonResult))
        {
            return null;
        }

        // Vosk возвращает JSON вида: {"text": "распознанный текст"}
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(jsonResult);
            if (doc.RootElement.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString();
            }
        }
        catch
        {
            // Если не удалось распарсить JSON, возвращаем как есть
            return jsonResult;
        }

        return null;
    }

    /// <summary>
    /// Records and recognizes speech from microphone.
    /// </summary>
    public async Task<string?> RecognizeFromMicrophoneAsync(int maxDurationSeconds = 10, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Recognizer is not initialized");
        }

        try
        {
            _audioRecorder.StartRecording();

            // Wait specified time or until cancelled
            await Task.Delay(maxDurationSeconds * 1000, cancellationToken);

            var audioBytes = await _audioRecorder.StopRecordingAsync();

            return await RecognizeAsync(audioBytes, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation - normal completion
            await _audioRecorder.StopRecordingAsync();
            return null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _voskEngine.Dispose();
            _audioRecorder.Dispose();
            _disposed = true;
        }
    }
}
