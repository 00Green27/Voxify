namespace Voxify.Core;

/// <summary>
/// Factory for creating speech recognizers.
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
    /// Creates a speech recognizer based on configuration.
    /// </summary>
    /// <param name="provider">Speech provider.</param>
    /// <returns>Speech recognizer.</returns>
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
    /// Creates a speech recognizer based on string provider name.
    /// </summary>
    /// <param name="providerName">Provider name: "Vosk" or "Whisper".</param>
    /// <returns>Speech recognizer.</returns>
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
    /// Creates Vosk recognizer.
    /// </summary>
    private ISpeechRecognizer CreateVoskRecognizer()
    {
        return new SpeechRecognizerService(_voskEngine, _audioRecorder);
    }

    /// <summary>
    /// Creates Whisper recognizer.
    /// </summary>
    private ISpeechRecognizer CreateWhisperRecognizer()
    {
        return new WhisperRecognizer(_audioRecorder);
    }
}
