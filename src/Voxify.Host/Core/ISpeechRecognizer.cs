namespace Voxify.Core;

/// <summary>
/// Speech recognition provider.
/// </summary>
public enum SpeechProvider
{
    /// <summary>
    /// Vosk — offline recognition via Vosk model.
    /// </summary>
    Vosk,

    /// <summary>
    /// Whisper — offline recognition via Whisper model.
    /// </summary>
    Whisper
}

/// <summary>
/// Interface for speech recognition component.
/// </summary>
public interface ISpeechRecognizer : IDisposable
{
    /// <summary>
    /// Gets the speech provider type.
    /// </summary>
    SpeechProvider Provider { get; }

    /// <summary>
    /// Initializes the recognizer with the specified model.
    /// </summary>
    Task InitializeAsync(string modelPath, string language);

    /// <summary>
    /// Recognizes speech from audio data.
    /// </summary>
    Task<string?> RecognizeAsync(byte[] audioData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the recognizer is initialized.
    /// </summary>
    bool IsInitialized { get; }
}
