using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;

namespace Voxify.Core;

/// <summary>
/// Represents a downloadable speech recognition model.
/// </summary>
public class ModelInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty; // Vosk or Whisper
    public string Language { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public string ExpectedChecksum { get; init; } = string.Empty;
    public long ExpectedSizeBytes { get; init; }
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Returns human-readable model name for ComboBox display.
    /// </summary>
    public override string ToString() => Name;
}

/// <summary>
/// Model downloader with progress reporting and checksum validation.
/// </summary>
public class ModelDownloader : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _modelsDirectory;
    private bool _disposed;

    // Predefined models list
    // Note: Checksums are currently disabled. To add them:
    // 1. Download the model ZIP file
    // 2. Run: sha256sum vosk-model-small-ru-0.22.zip
    // 3. Add the hash to ExpectedChecksum field
    public static readonly List<ModelInfo> AvailableModels = new()
    {
        new ModelInfo
        {
            Id = "vosk-small-ru-0.22",
            Name = "Vosk Small Russian",
            Provider = "Vosk",
            Language = "ru-RU",
            DownloadUrl = "https://alphacephei.com/vosk/models/vosk-model-small-ru-0.22.zip",
            ExpectedChecksum = string.Empty, // TODO: Add SHA256 checksum when available
            ExpectedSizeBytes = 47_185_920, // ~45 MB actual size
            Description = "Малая русская модель Vosk (~45 МБ)"
        },
        new ModelInfo
        {
            Id = "vosk-small-en-us-0.15",
            Name = "Vosk Small English",
            Provider = "Vosk",
            Language = "en-US",
            DownloadUrl = "https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip",
            ExpectedChecksum = string.Empty, // TODO: Add SHA256 checksum when available
            ExpectedSizeBytes = 41_943_040, // ~40 MB actual size
            Description = "Малая английская модель Vosk (~40 МБ)"
        }
    };

    public event EventHandler<DownloadProgressEventArgs>? ProgressChanged;
    public event EventHandler<DownloadCompleteEventArgs>? DownloadCompleted;

    public ModelDownloader(string modelsDirectory)
    {
        _modelsDirectory = modelsDirectory;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10) // Long timeout for large models
        };
    }

    /// <summary>
    /// Downloads and installs a model.
    /// </summary>
    public async Task<string> DownloadModelAsync(ModelInfo model, CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure models directory exists
            Directory.CreateDirectory(_modelsDirectory);

            // Create temp file for download
            var tempFilePath = Path.Combine(_modelsDirectory, $"{model.Id}_temp.zip");
            var extractPath = Path.Combine(_modelsDirectory, model.Id);

            // Delete temp file if exists
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            // Delete existing model directory if exists
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, recursive: true);
            }

            // Download with progress
            await DownloadFileWithProgressAsync(model.DownloadUrl, tempFilePath, model.ExpectedSizeBytes, cancellationToken);

            // Validate checksum if provided
            if (!string.IsNullOrEmpty(model.ExpectedChecksum))
            {
                ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                {
                    ProgressPercentage = 100,
                    StatusMessage = "Проверка контрольной суммы..."
                });

                var isValid = await ValidateChecksumAsync(tempFilePath, model.ExpectedChecksum);
                if (!isValid)
                {
                    File.Delete(tempFilePath);
                    throw new InvalidOperationException($"Checksum validation failed for model {model.Name}");
                }
            }

            // Extract ZIP
            ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
            {
                ProgressPercentage = 100,
                StatusMessage = "Распаковка модели..."
            });

            ZipFile.ExtractToDirectory(tempFilePath, extractPath);

            // Delete temp file
            File.Delete(tempFilePath);

            // Notify completion
            DownloadCompleted?.Invoke(this, new DownloadCompleteEventArgs
            {
                ModelId = model.Id,
                ModelPath = extractPath,
                Success = true
            });

            return extractPath;
        }
        catch (Exception ex)
        {
            DownloadCompleted?.Invoke(this, new DownloadCompleteEventArgs
            {
                ModelId = model.Id,
                ModelPath = string.Empty,
                Success = false,
                ErrorMessage = ex.Message
            });

            throw;
        }
    }

    /// <summary>
    /// Downloads a file with progress reporting.
    /// </summary>
    private async Task DownloadFileWithProgressAsync(string url, string destinationPath, long expectedSize, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? expectedSize;
        var downloadedBytes = 0L;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        var lastProgressUpdate = DateTime.MinValue;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (bytesRead == 0)
                break;

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            downloadedBytes += bytesRead;

            // Throttle progress updates to once per 100ms
            var now = DateTime.Now;
            if ((now - lastProgressUpdate).TotalMilliseconds >= 100 || downloadedBytes == totalBytes)
            {
                var progressPercentage = totalBytes > 0 ? (int)(downloadedBytes * 100 / totalBytes) : 0;
                var mbDownloaded = downloadedBytes / (1024.0 * 1024.0);
                var mbTotal = totalBytes / (1024.0 * 1024.0);

                ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                {
                    ProgressPercentage = progressPercentage,
                    DownloadedBytes = downloadedBytes,
                    TotalBytes = totalBytes,
                    StatusMessage = $"Загрузка: {mbDownloaded:F1} / {mbTotal:F1} МБ"
                });

                lastProgressUpdate = now;
            }
        }
    }

    /// <summary>
    /// Validates file checksum using SHA256.
    /// </summary>
    private static async Task<bool> ValidateChecksumAsync(string filePath, string expectedHash)
    {
        await using var fileStream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(fileStream);
        var actualHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        return actualHash == expectedHash.ToLowerInvariant();
    }

    /// <summary>
    /// Gets installed models from the models directory.
    /// </summary>
    public List<ModelInfo> GetInstalledModels()
    {
        var installed = new List<ModelInfo>();

        if (!Directory.Exists(_modelsDirectory))
            return installed;

        // Check for Vosk models (directories with specific structure)
        foreach (var dir in Directory.GetDirectories(_modelsDirectory))
        {
            var dirName = Path.GetFileName(dir);
            
            // Check if it's a Vosk model directory
            if (File.Exists(Path.Combine(dir, "am", "final.mdl")) || 
                File.Exists(Path.Combine(dir, "conf", "model.conf")))
            {
                var modelInfo = AvailableModels.FirstOrDefault(m => m.Id == dirName);
                if (modelInfo != null)
                {
                    installed.Add(modelInfo);
                }
                else
                {
                    // Unknown Vosk model
                    installed.Add(new ModelInfo
                    {
                        Id = dirName,
                        Name = dirName,
                        Provider = "Vosk",
                        Language = "Unknown",
                        Description = $"Локальная модель: {dirName}"
                    });
                }
            }
        }

        // Check for Whisper models (.bin files)
        foreach (var file in Directory.GetFiles(_modelsDirectory, "*.bin"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            installed.Add(new ModelInfo
            {
                Id = fileName,
                Name = fileName,
                Provider = "Whisper",
                Language = "Multi",
                Description = $"Модель Whisper: {fileName}"
            });
        }

        return installed;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Event args for download progress updates.
/// </summary>
public class DownloadProgressEventArgs : EventArgs
{
    public int ProgressPercentage { get; init; }
    public long DownloadedBytes { get; init; }
    public long TotalBytes { get; init; }
    public string StatusMessage { get; init; } = string.Empty;
}

/// <summary>
/// Event args for download completion.
/// </summary>
public class DownloadCompleteEventArgs : EventArgs
{
    public string ModelId { get; init; } = string.Empty;
    public string ModelPath { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
