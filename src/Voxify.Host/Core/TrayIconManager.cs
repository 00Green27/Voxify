using System.Drawing;
using Microsoft.Win32;
using Voxify.Config;

namespace Voxify.Core;

/// <summary>
/// Recording state for tray icon.
/// </summary>
public enum TrayIconState
{
    Idle,
    Recording,
    Processing
}

/// <summary>
/// Theme for tray icons.
/// </summary>
public enum IconTheme
{
    Light,
    Dark
}

/// <summary>
/// Manages tray icons based on application state and system theme.
/// </summary>
public class TrayIconManager : IDisposable
{
    private readonly Dictionary<(TrayIconState, IconTheme), Icon> _iconCache;
    private readonly NotifyIcon _notifyIcon;
    private readonly DebugService _debugService;
    private bool _disposed;
    private TrayIconState _currentState;
    private IconTheme _currentTheme;

    /// <summary>
    /// Event fired when icon changes.
    /// </summary>
    public event EventHandler<Icon>? IconChanged;

    public TrayIconManager(NotifyIcon notifyIcon, DebugService debugService)
    {
        _notifyIcon = notifyIcon;
        _debugService = debugService;
        _iconCache = new Dictionary<(TrayIconState, IconTheme), Icon>();
        _currentState = TrayIconState.Idle;
        _currentTheme = GetSystemTheme();

        LoadIcons();
        UpdateIcon();

        // Subscribe to system theme changes
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    /// <summary>
    /// Loads all icons from embedded resources.
    /// </summary>
    private void LoadIcons()
    {
        var assembly = typeof(TrayIconManager).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        // Icon resource names pattern:
        // Voxify.resources.mic_idle_dark.png
        // Voxify.resources.mic_recording_dark.png
        // Voxify.resources.mic_transcribing_dark.png
        // Voxify.resources.mic_idle_light.png
        // Voxify.resources.mic_recording_light.png
        // Voxify.resources.mic_transcribing_light.png

        var iconMap = new Dictionary<string, string>
        {
            { "mic_idle_dark", "idle_dark" },
            { "mic_recording_dark", "recording_dark" },
            { "mic_transcribing_dark", "processing_dark" },
            { "mic_idle_light", "idle_light" },
            { "mic_recording_light", "recording_light" },
            { "mic_transcribing_light", "processing_light" }
        };

        foreach (var resourceName in resourceNames)
        {
            // Find matching icon
            foreach (var kvp in iconMap)
            {
                if (resourceName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        var icon = CreateIconFromPng(stream, 32);
                        var key = ParseIconKey(kvp.Value);
                        _iconCache[key] = icon;
                        _debugService.Log("TrayIcon", $"Loaded icon: {resourceName}", LogLevel.Debug);
                    }
                    break;
                }
            }
        }

        _debugService.Log("TrayIcon", $"Loaded {_iconCache.Count} icons", LogLevel.Info);
    }

    /// <summary>
    /// Parses icon key to state and theme.
    /// </summary>
    private static (TrayIconState, IconTheme) ParseIconKey(string key)
    {
        var parts = key.Split('_');
        if (parts.Length != 2)
            return (TrayIconState.Idle, IconTheme.Dark);

        var state = parts[0] switch
        {
            "idle" => TrayIconState.Idle,
            "recording" => TrayIconState.Recording,
            "processing" => TrayIconState.Processing,
            _ => TrayIconState.Idle
        };

        var theme = parts[1] switch
        {
            "dark" => IconTheme.Dark,
            "light" => IconTheme.Light,
            _ => IconTheme.Dark
        };

        return (state, theme);
    }

    /// <summary>
    /// Creates an Icon from a PNG stream.
    /// </summary>
    private static Icon CreateIconFromPng(Stream pngStream, int size)
    {
        using var bitmap = new Bitmap(pngStream);
        using var resized = new Bitmap(bitmap, new Size(size, size));
        var iconPtr = resized.GetHicon();
        return Icon.FromHandle(iconPtr);
    }

    /// <summary>
    /// Updates the tray icon based on current state and theme.
    /// </summary>
    public void UpdateIcon()
    {
        var key = (_currentState, _currentTheme);

        if (_iconCache.TryGetValue(key, out var icon))
        {
            _notifyIcon.Icon = icon;
            IconChanged?.Invoke(this, icon);
        }
        else
        {
            // Fallback to default icon
            _notifyIcon.Icon = SystemIcons.Application;
            _debugService.Log("TrayIcon", $"Icon not found for {key}, using fallback", LogLevel.Warning);
        }
    }

    /// <summary>
    /// Sets the recording state and updates the icon.
    /// </summary>
    public void SetState(TrayIconState state)
    {
        _currentState = state;
        UpdateIcon();
        _debugService.Log("TrayIcon", $"State changed to {state}", LogLevel.Debug);
    }

    /// <summary>
    /// Updates the tooltip text.
    /// </summary>
    public void SetTooltip(string text)
    {
        _notifyIcon.Text = text;
    }

    /// <summary>
    /// Gets the current system theme.
    /// </summary>
    private static IconTheme GetSystemTheme()
    {
        try
        {
            // Check Windows 10/11 theme setting
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            
            if (key != null)
            {
                var appsUseLightTheme = key.GetValue("AppsUseLightTheme");
                if (appsUseLightTheme is int value && value == 0)
                {
                    return IconTheme.Dark;
                }
                return IconTheme.Light;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TrayIconManager] Failed to detect theme: {ex.Message}");
        }

        // Default to light theme
        return IconTheme.Light;
    }

    /// <summary>
    /// Handles system theme changes.
    /// </summary>
    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            var newTheme = GetSystemTheme();
            if (newTheme != _currentTheme)
            {
                _currentTheme = newTheme;
                UpdateIcon();
                _debugService.Log("TrayIcon", $"Theme changed to {_currentTheme}", LogLevel.Info);
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

            // Dispose all cached icons
            foreach (var icon in _iconCache.Values)
            {
                icon?.Dispose();
            }
            _iconCache.Clear();

            _disposed = true;
        }
    }
}
