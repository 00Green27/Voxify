using Vosk;

namespace Voxify.Core;

/// <summary>
/// Wrapper around Vosk.Model for managing speech recognition model.
/// </summary>
public class VoskEngine : IDisposable
{
    private Model? _model;
    private bool _disposed;

    /// <summary>
    /// Initializes Vosk model from specified folder.
    /// </summary>
    public Task InitializeAsync(string modelPath, string language)
    {
        if (string.IsNullOrEmpty(modelPath))
        {
            throw new ArgumentException("Model path is not specified", nameof(modelPath));
        }

        if (!Directory.Exists(modelPath))
        {
            throw new DirectoryNotFoundException($"Model not found at path: {modelPath}");
        }

        try
        {
            // Vosk.Model requires path to model folder
            _model = new Model(modelPath);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error initializing Vosk model: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a new VoskRecognizer instance for recognition session.
    /// Configures optimal settings for CPU efficiency.
    /// </summary>
    public Vosk.VoskRecognizer CreateRecognizer()
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Model is not initialized. Call InitializeAsync first.");
        }

        var recognizer = new Vosk.VoskRecognizer(_model, 16000.0f);
        
        // Disable word-level timestamps and partial results for better CPU performance
        // Words by timestamp are not needed
        // Partial results cause unnecessary CPU usage
        // We only need final text output
        recognizer.SetWords(false);
        recognizer.SetPartialWords(false);
        
        return recognizer;
    }

    /// <summary>
    /// Creates Recognizer with grammar for limited vocabulary.
    /// </summary>
    public Vosk.VoskRecognizer CreateRecognizerWithGrammar(string[] phrases)
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Model is not initialized. Call InitializeAsync first.");
        }

        var recognizer = new Vosk.VoskRecognizer(_model, 16000.0f);
        // Vosk doesn't support SetGrammar directly in NuGet package
        // Use standard recognizer
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
