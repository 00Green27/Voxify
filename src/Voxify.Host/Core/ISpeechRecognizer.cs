namespace Voxify.Core;

/// <summary>
/// Провайдер распознавания речи.
/// </summary>
public enum SpeechProvider
{
    /// <summary>
    /// Vosk — офлайн распознавание через модель Vosk.
    /// </summary>
    Vosk,

    /// <summary>
    /// Whisper — офлайн распознавание через модель Whisper.
    /// </summary>
    Whisper
}

/// <summary>
/// Интерфейс для компонента распознавания речи.
/// </summary>
public interface ISpeechRecognizer : IDisposable
{
    /// <summary>
    /// Gets the speech provider type.
    /// </summary>
    SpeechProvider Provider { get; }

    /// <summary>
    /// Инициализирует распознаватель с указанной моделью.
    /// </summary>
    Task InitializeAsync(string modelPath, string language);

    /// <summary>
    /// Распознаёт речь из аудио-данных.
    /// </summary>
    Task<string?> RecognizeAsync(byte[] audioData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, инициализирован ли распознаватель.
    /// </summary>
    bool IsInitialized { get; }
}
