namespace Voxify.Core;

/// <summary>
/// Интерфейс для компонента распознавания речи.
/// </summary>
public interface ISpeechRecognizer
{
    /// <summary>
    /// Инициализирует распознаватель с указанной моделью.
    /// </summary>
    Task InitializeAsync(string modelPath, string language);
    
    /// <summary>
    /// Распознаёт речь из аудио-данных.
    /// </summary>
    Task<string?> RecognizeAsync(byte[] audioData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Освобождает ресурсы.
    /// </summary>
    void Dispose();
}
