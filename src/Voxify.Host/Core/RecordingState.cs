namespace Voxify.Core;

/// <summary>
/// Состояние процесса записи.
/// </summary>
public enum RecordingState
{
    /// <summary>
    /// Бездействие (запись не активна).
    /// </summary>
    Idle,

    /// <summary>
    /// Идёт запись.
    /// </summary>
    Recording,

    /// <summary>
    /// Обработка распознанного текста.
    /// </summary>
    Processing
}
