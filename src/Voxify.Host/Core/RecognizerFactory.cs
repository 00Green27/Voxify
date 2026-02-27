namespace Voxify.Core;

/// <summary>
/// Фабрика для создания распознавателей речи.
/// </summary>
public class RecognizerFactory
{
    private readonly AudioRecorder _audioRecorder;
    private readonly VoskEngine _voskEngine;

    public RecognizerFactory(AudioRecorder audioRecorder, VoskEngine voskEngine)
    {
        _audioRecorder = audioRecorder;
        _voskEngine = voskEngine;
    }

    /// <summary>
    /// Создаёт распознаватель речи на основе конфигурации.
    /// </summary>
    /// <param name="provider">Провайдер распознавания.</param>
    /// <returns>Распознаватель речи.</returns>
    public ISpeechRecognizer CreateRecognizer(SpeechProvider provider)
    {
        return provider switch
        {
            SpeechProvider.Vosk => CreateVoskRecognizer(),
            SpeechProvider.Whisper => CreateWhisperRecognizer(),
            _ => throw new ArgumentException($"Unknown speech provider: {provider}", nameof(provider))
        };
    }

    /// <summary>
    /// Создаёт распознаватель речи на основе строкового имени провайдера.
    /// </summary>
    /// <param name="providerName">Имя провайдера: "Vosk" или "Whisper".</param>
    /// <returns>Распознаватель речи.</returns>
    public ISpeechRecognizer CreateRecognizer(string providerName)
    {
        var provider = providerName.ToLower() switch
        {
            "vosk" => SpeechProvider.Vosk,
            "whisper" => SpeechProvider.Whisper,
            _ => throw new ArgumentException($"Unknown speech provider: {providerName}", nameof(providerName))
        };

        return CreateRecognizer(provider);
    }

    /// <summary>
    /// Создаёт Vosk распознаватель.
    /// </summary>
    private ISpeechRecognizer CreateVoskRecognizer()
    {
        return new SpeechRecognizerService(_voskEngine, _audioRecorder);
    }

    /// <summary>
    /// Создаёт Whisper распознаватель.
    /// </summary>
    private ISpeechRecognizer CreateWhisperRecognizer()
    {
        return new WhisperRecognizer(_audioRecorder);
    }
}
