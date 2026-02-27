namespace Voxify.Core;

/// <summary>
/// Interface for Voice Activity Detection (VAD) engine.
/// </summary>
public interface IVadEngine : IDisposable
{
    /// <summary>
    /// Initializes the VAD model.
    /// </summary>
    /// <param name="modelPath">Path to ONNX model.</param>
    /// <param name="configPath">Path to YAML config.</param>
    Task InitializeAsync(string modelPath, string configPath);

    /// <summary>
    /// Processes audio sample and returns speech detection result.
    /// </summary>
    /// <param name="samples">Audio samples array (float, normalized -1.0 to 1.0).</param>
    /// <returns>Speech detection result.</returns>
    VadResult DetectSpeech(float[] samples);

    /// <summary>
    /// Resets VAD state for a new session.
    /// </summary>
    void Reset();

    /// <summary>
    /// Checks if the model is initialized.
    /// </summary>
    bool IsInitialized { get; }
}

/// <summary>
/// Speech detection result.
/// </summary>
public class VadResult
{
    /// <summary>
    /// Whether speech is detected.
    /// </summary>
    public bool IsSpeech { get; init; }

    /// <summary>
    /// Detection confidence (0.0 - 1.0).
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Speech start in milliseconds (relative to stream start).
    /// </summary>
    public long SpeechStartMs { get; init; }

    /// <summary>
    /// Speech end in milliseconds (relative to stream start).
    /// </summary>
    public long SpeechEndMs { get; init; }

    /// <summary>
    /// Empty result (no speech).
    /// </summary>
    public static VadResult Empty => new()
    {
        IsSpeech = false,
        Confidence = 0f,
        SpeechStartMs = 0,
        SpeechEndMs = 0
    };
}
