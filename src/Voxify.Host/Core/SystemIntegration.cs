using Microsoft.Win32;

namespace Voxify.Core;

/// <summary>
/// System integration features: auto-start, session events, display events.
/// </summary>
public class SystemIntegration : IDisposable
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Voxify";
    private bool _disposed;

    /// <summary>
    /// Event fired when Windows session is ending (shutdown/logoff).
    /// </summary>
    public event EventHandler<SessionEndingEventArgs>? SessionEnding;

    /// <summary>
    /// Event fired when display settings change.
    /// </summary>
    public event EventHandler? DisplaySettingsChanged;

    /// <summary>
    /// Gets whether auto-start is currently enabled.
    /// </summary>
    public bool IsAutoStartEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
                if (key == null)
                    return false;

                var value = key.GetValue(AppName);
                return value != null && value.ToString() == GetApplicationPath();
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Enables or disables auto-start on Windows login.
    /// </summary>
    /// <param name="enabled">True to enable auto-start, false to disable.</param>
    public void SetAutoStart(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                throw new InvalidOperationException("Cannot access Windows Registry Run key");
            }

            if (enabled)
            {
                var appPath = GetApplicationPath();
                key.SetValue(AppName, appPath, RegistryValueKind.String);
            }
            else
            {
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set auto-start: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Subscribes to system events.
    /// </summary>
    public void SubscribeToSystemEvents()
    {
        SystemEvents.SessionEnding += OnSessionEnding;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    /// <summary>
    /// Unsubscribes from system events.
    /// </summary>
    public void UnsubscribeFromSystemEvents()
    {
        SystemEvents.SessionEnding -= OnSessionEnding;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }

    private void OnSessionEnding(object? sender, SessionEndingEventArgs e)
    {
        SessionEnding?.Invoke(this, e);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        DisplaySettingsChanged?.Invoke(this, e);
    }

    private static string GetApplicationPath()
    {
        return $"\"{Environment.ProcessPath ?? Application.ExecutablePath}\"";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            UnsubscribeFromSystemEvents();
            _disposed = true;
        }
    }
}
