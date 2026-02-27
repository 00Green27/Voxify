namespace Voxify.Core;

/// <summary>
/// Recording process state.
/// </summary>
public enum RecordingState
{
    /// <summary>
    /// Idle (recording not active).
    /// </summary>
    Idle,

    /// <summary>
    /// Recording in progress.
    /// </summary>
    Recording,

    /// <summary>
    /// Processing recognized text.
    /// </summary>
    Processing
}
