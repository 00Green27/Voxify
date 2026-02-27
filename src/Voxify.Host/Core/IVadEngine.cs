namespace Voxify.Core;

/// <summary>
/// Интерфейс для детектора голосовой активности (VAD).
/// </summary>
public interface IVadEngine : IDisposable
{
    /// <summary>
    /// Инициализирует VAD модель.
    /// </summary>
    /// <param name="modelPath">Путь к ONNX модели.</param>
    /// <param name="configPath">Путь к YAML конфигурации.</param>
    Task InitializeAsync(string modelPath, string configPath);

    /// <summary>
    /// Обрабатывает аудио сэмпл и возвращает результат детекции речи.
    /// </summary>
    /// <param name="samples">Массив аудио сэмплов (float, нормализованный -1.0 до 1.0).</param>
    /// <returns>Результат детекции речи.</returns>
    VadResult DetectSpeech(float[] samples);

    /// <summary>
    /// Сбрасывает состояние VAD для новой сессии.
    /// </summary>
    void Reset();

    /// <summary>
    /// Проверяет, инициализирована ли модель.
    /// </summary>
    bool IsInitialized { get; }
}

/// <summary>
/// Результат детекции речи.
/// </summary>
public class VadResult
{
    /// <summary>
    /// Обнаружена ли речь.
    /// </summary>
    public bool IsSpeech { get; init; }

    /// <summary>
    /// Уверенность детекции (0.0 - 1.0).
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Начало речи в миллисекундах (относительно начала потока).
    /// </summary>
    public long SpeechStartMs { get; init; }

    /// <summary>
    /// Конец речи в миллисекундах (относительно начала потока).
    /// </summary>
    public long SpeechEndMs { get; init; }

    /// <summary>
    /// Пустой результат (нет речи).
    /// </summary>
    public static VadResult Empty => new()
    {
        IsSpeech = false,
        Confidence = 0f,
        SpeechStartMs = 0,
        SpeechEndMs = 0
    };
}
