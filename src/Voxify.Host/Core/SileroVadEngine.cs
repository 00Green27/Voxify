using ManySpeech.SileroVad;
using ManySpeech.SileroVad.Model;

namespace Voxify.Core;

/// <summary>
/// Реализация VAD двигателя на базе Silero VAD (ONNX).
/// </summary>
public class SileroVadEngine : IVadEngine
{
    private OfflineVad? _vad;
    private OfflineStream? _stream;
    private bool _disposed;
    private readonly object _lock = new();

    /// <summary>
    /// Проверяет, инициализирована ли модель.
    /// </summary>
    public bool IsInitialized => _vad != null;

    /// <summary>
    /// Инициализирует VAD модель.
    /// </summary>
    /// <param name="modelPath">Путь к ONNX модели.</param>
    /// <param name="configPath">Путь к YAML конфигурации.</param>
    public Task InitializeAsync(string modelPath, string configPath)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"VAD модель не найдена: {modelPath}");
        }

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"VAD конфигурация не найдена: {configPath}");
        }

        lock (_lock)
        {
            // threshold: 0.5 - баланс между чувствительностью и ложными срабатываниями
            // isDebug: false - отключаем отладочный вывод
            _vad = new OfflineVad(modelPath, configFilePath: configPath, threshold: 0.5F, isDebug: false);
            _stream = _vad.CreateOfflineStream();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Обрабатывает аудио сэмпл и возвращает результат детекции речи.
    /// </summary>
    /// <param name="samples">Массив аудио сэмплов (float, нормализованный -1.0 до 1.0).</param>
    /// <returns>Результат детекции речи.</returns>
    public VadResult DetectSpeech(float[] samples)
    {
        if (_vad == null || _stream == null)
        {
            throw new InvalidOperationException("VAD модель не инициализирована. Вызовите InitializeAsync().");
        }

        if (samples.Length == 0)
        {
            return VadResult.Empty;
        }

        lock (_lock)
        {
            try
            {
                // Добавляем сэмплы в поток
                _stream.AddSamples(samples);

                // Получаем результаты
                var results = _vad.GetResults(new List<OfflineStream> { _stream });

                if (results.Count == 0 || results[0].Segments.Count == 0)
                {
                    return VadResult.Empty;
                }

                var result = results[0];
                var lastSegment = result.Segments.LastOrDefault();

                if (lastSegment == null)
                {
                    return VadResult.Empty;
                }

                // Конвертируем из сэмплов в миллисекунды (при 16kHz)
                const int sampleRate = 16000;
                long startMs = lastSegment.Start * 1000 / sampleRate;
                long endMs = lastSegment.End * 1000 / sampleRate;

                // Вычисляем уверенность на основе количества сегментов
                float confidence = Math.Min(1.0f, result.Segments.Count / 10f);

                return new VadResult
                {
                    IsSpeech = true,
                    Confidence = confidence,
                    SpeechStartMs = startMs,
                    SpeechEndMs = endMs
                };
            }
            catch (Exception ex)
            {
                // Логгируем ошибку и возвращаем пустой результат
                Console.WriteLine($"[SileroVadEngine] Ошибка детекции: {ex.Message}");
                return VadResult.Empty;
            }
        }
    }

    /// <summary>
    /// Сбрасывает состояние VAD для новой сессии.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            if (_vad != null && _stream != null)
            {
                // Создаём новый поток для сброса состояния
                _stream = _vad.CreateOfflineStream();
            }
        }
    }

    /// <summary>
    /// Освобождает ресурсы.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                _stream?.Dispose();
                _vad?.Dispose();
                _disposed = true;
            }
        }

        GC.SuppressFinalize(this);
    }
}
