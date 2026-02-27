using ManySpeech.SileroVad;
using ManySpeech.SileroVad.Model;

namespace Voxify.Core;

/// <summary>
/// VAD engine implementation based on Silero VAD (ONNX).
/// </summary>
public class SileroVadEngine : IVadEngine
{
    private OfflineVad? _vad;
    private OfflineStream? _stream;
    private bool _disposed;
    private readonly object _lock = new();

    /// <summary>
    /// Checks if the model is initialized.
    /// </summary>
    public bool IsInitialized => _vad != null;

    /// <summary>
    /// Initializes the VAD model.
    /// </summary>
    /// <param name="modelPath">Path to ONNX model.</param>
    /// <param name="configPath">Path to YAML config.</param>
    public Task InitializeAsync(string modelPath, string configPath)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"VAD model not found: {modelPath}");
        }

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"VAD config not found: {configPath}");
        }

        lock (_lock)
        {
            // threshold: 0.5 - balance between sensitivity and false positives
            // isDebug: false - disable debug output
            _vad = new OfflineVad(modelPath, configFilePath: configPath, threshold: 0.5F, isDebug: false);
            _stream = _vad.CreateOfflineStream();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes audio sample and returns speech detection result.
    /// </summary>
    /// <param name="samples">Audio samples array (float, normalized -1.0 to 1.0).</param>
    /// <returns>Speech detection result.</returns>
    public VadResult DetectSpeech(float[] samples)
    {
        if (_vad == null || _stream == null)
        {
            throw new InvalidOperationException("VAD model not initialized. Call InitializeAsync().");
        }

        if (samples.Length == 0)
        {
            return VadResult.Empty;
        }

        lock (_lock)
        {
            try
            {
                // Add samples to stream
                _stream.AddSamples(samples);

                // Get results
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

                // Convert from samples to milliseconds (at 16kHz)
                const int sampleRate = 16000;
                long startMs = lastSegment.Start * 1000 / sampleRate;
                long endMs = lastSegment.End * 1000 / sampleRate;

                // Calculate confidence based on segment count
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
                // Log error and return empty result
                Console.WriteLine($"[SileroVadEngine] Detection error: {ex.Message}");
                return VadResult.Empty;
            }
        }
    }

    /// <summary>
    /// Resets VAD state for a new session.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            if (_vad != null && _stream != null)
            {
                // Create new stream to reset state
                _stream = _vad.CreateOfflineStream();
            }
        }
    }

    /// <summary>
    /// Releases resources.
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
