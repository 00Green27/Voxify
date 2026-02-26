namespace Voxify.Core;

/// <summary>
/// Реализация распознавания речи через Vosk.
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
    /// Инициализирует распознаватель с указанной моделью.
    /// </summary>
    public async Task InitializeAsync(string modelPath, string language)
    {
        await _voskEngine.InitializeAsync(modelPath, language);
        _isInitialized = true;
    }

    /// <summary>
    /// Записывает аудио с микрофона и распознаёт речь.
    /// </summary>
    public async Task<string?> RecognizeAsync(byte[]? audioData = null, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Распознаватель не инициализирован");
        }

        try
        {
            // Записываем аудио
            byte[] audioBytes;
            if (audioData != null)
            {
                audioBytes = audioData;
            }
            else
            {
                _audioRecorder.StartRecording();
                
                // Ждём несколько секунд для записи
                await Task.Delay(3000, cancellationToken);
                
                audioBytes = await _audioRecorder.StopRecordingAsync();
            }

            if (audioBytes.Length == 0)
            {
                return null;
            }

            // Распознаём через Vosk
            using var recognizer = _voskEngine.CreateRecognizer();
            
            // Принимаем аудио-данные чанками по 4KB
            int chunkSize = 4096;
            for (int i = 0; i < audioBytes.Length; i += chunkSize)
            {
                int remaining = audioBytes.Length - i;
                int size = Math.Min(chunkSize, remaining);
                byte[] chunk = new byte[size];
                Array.Copy(audioBytes, i, chunk, 0, size);
                
                if (recognizer.AcceptWaveform(chunk, size))
                {
                    // Получаем полный результат
                    var result = recognizer.Result();
                    if (!string.IsNullOrEmpty(result))
                    {
                        return ExtractTextFromResult(result);
                    }
                }
            }

            // Финальный результат (частичный)
            var finalResult = recognizer.FinalResult();
            return ExtractTextFromResult(finalResult);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка распознавания: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Извлекает текст из JSON-результата Vosk.
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
    /// Записывает и распознаёт речь с микрофона.
    /// </summary>
    public async Task<string?> RecognizeFromMicrophoneAsync(int maxDurationSeconds = 10, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Распознаватель не инициализирован");
        }

        try
        {
            _audioRecorder.StartRecording();
            
            // Ждём указанное время или пока не будет отменено
            await Task.Delay(maxDurationSeconds * 1000, cancellationToken);
            
            var audioBytes = await _audioRecorder.StopRecordingAsync();
            
            return await RecognizeAsync(audioBytes, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Отмена - нормальное завершение
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
