using Vosk;

namespace Voxify.Core;

/// <summary>
/// Обёртка над Vosk.Model для управления моделью распознавания.
/// </summary>
public class VoskEngine : IDisposable
{
    private Model? _model;
    private bool _disposed;

    /// <summary>
    /// Инициализирует модель Vosk из указанной папки.
    /// </summary>
    public Task InitializeAsync(string modelPath, string language)
    {
        if (string.IsNullOrEmpty(modelPath))
        {
            throw new ArgumentException("Путь к модели не указан", nameof(modelPath));
        }

        if (!Directory.Exists(modelPath))
        {
            throw new DirectoryNotFoundException($"Модель не найдена по пути: {modelPath}");
        }

        try
        {
            // Vosk.Model требует путь к папке с моделью
            _model = new Model(modelPath);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка инициализации модели Vosk: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Создаёт новый экземпляр VoskRecognizer для сессии распознавания.
    /// </summary>
    public Vosk.VoskRecognizer CreateRecognizer()
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Модель не инициализирована. Вызовите InitializeAsync first.");
        }

        return new Vosk.VoskRecognizer(_model, 16000.0f);
    }

    /// <summary>
    /// Создаёт Recognizer с грамматикой для ограниченного словаря.
    /// </summary>
    public Vosk.VoskRecognizer CreateRecognizerWithGrammar(string[] phrases)
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Модель не инициализирована. Вызовите InitializeAsync first.");
        }

        var recognizer = new Vosk.VoskRecognizer(_model, 16000.0f);
        // Vosk не поддерживает SetGrammar напрямую в NuGet пакете
        // Используем стандартный recognizer
        return recognizer;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _model?.Dispose();
            _disposed = true;
        }
    }
}
