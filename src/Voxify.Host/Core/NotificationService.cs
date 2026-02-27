namespace Voxify.Core;

/// <summary>
/// Notification level for filtering.
/// </summary>
public enum NotificationLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Service for managing balloon tip notifications.
/// </summary>
public class NotificationService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Config.UiConfig _uiConfig;
    private readonly DebugService _debugService;
    private bool _disposed;

    public NotificationService(NotifyIcon notifyIcon, Config.UiConfig uiConfig, DebugService debugService)
    {
        _notifyIcon = notifyIcon;
        _uiConfig = uiConfig;
        _debugService = debugService;
    }

    /// <summary>
    /// Shows a notification with the specified level.
    /// Critical errors (Error, Warning) are always shown.
    /// Info notifications respect the ShowNotifications setting.
    /// </summary>
    public void Show(string title, string message, NotificationLevel level = NotificationLevel.Info)
    {
        // Always show errors and warnings
        // Only show info if notifications are enabled
        if (level == NotificationLevel.Info && !_uiConfig.ShowNotifications)
        {
            _debugService.Log("Notification", $"Suppressed info notification: {message}", LogLevel.Debug);
            return;
        }

        ShowBalloonTip(title, message, level);
    }

    /// <summary>
    /// Shows an info notification.
    /// </summary>
    public void ShowInfo(string title, string message)
    {
        Show(title, message, NotificationLevel.Info);
    }

    /// <summary>
    /// Shows a warning notification (always shown).
    /// </summary>
    public void ShowWarning(string title, string message)
    {
        Show(title, message, NotificationLevel.Warning);
    }

    /// <summary>
    /// Shows an error notification (always shown).
    /// </summary>
    public void ShowError(string title, string message)
    {
        Show(title, message, NotificationLevel.Error);
    }

    /// <summary>
    /// Shows a balloon tip with the specified parameters.
    /// </summary>
    private void ShowBalloonTip(string title, string message, NotificationLevel level)
    {
        try
        {
            var toolTipIcon = level switch
            {
                NotificationLevel.Info => ToolTipIcon.Info,
                NotificationLevel.Warning => ToolTipIcon.Warning,
                NotificationLevel.Error => ToolTipIcon.Error,
                _ => ToolTipIcon.Info
            };

            // Timeout: 2 seconds for info, 5 seconds for warnings/errors
            var timeout = level == NotificationLevel.Info ? 2000 : 5000;

            _notifyIcon.ShowBalloonTip(timeout, title, message, toolTipIcon);

            _debugService.Log("Notification", $"[{level}] {title}: {message}", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            _debugService.Log("Notification", $"Failed to show notification: {ex.Message}", LogLevel.Error);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
