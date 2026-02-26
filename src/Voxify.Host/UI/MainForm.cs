using System.Drawing;
using Voxify.Config;
using Voxify.Core;

namespace Voxify.UI;

/// <summary>
/// Main form of the Voxify application.
/// </summary>
public class MainForm : Form
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ConfigurationManager _configManager;
    private readonly HotkeyManager _hotkeyManager;
    private readonly SpeechRecognizerService _speechRecognizer;
    private readonly TextInputInjector _textInputInjector;
    private readonly VoskEngine _voskEngine;
    private readonly AudioRecorder _audioRecorder;

    public MainForm()
    {
        // Initialize components
        _contextMenu = new ContextMenuStrip();
        _notifyIcon = new NotifyIcon();

        // Load configuration
        _configManager = new ConfigurationManager();

        // Create core components
        _voskEngine = new VoskEngine();
        _audioRecorder = new AudioRecorder();
        _speechRecognizer = new SpeechRecognizerService(_voskEngine, _audioRecorder);
        _hotkeyManager = new HotkeyManager();
        _textInputInjector = new TextInputInjector(_configManager.Settings.TextInput);

        // Configure UI
        InitializeUI();

        // Subscribe to events
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        // Initialize the application
        InitializeAsync();
    }

    private void InitializeUI()
    {
        // Скрываем форму из taskbar
        this.ShowInTaskbar = false;
        this.WindowState = FormWindowState.Minimized;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MinimizeBox = false;
        this.MaximizeBox = false;
        
        // Create icon (use standard one)
        _notifyIcon.Icon = SystemIcons.Application;
        _notifyIcon.Text = "Voxify — Voice Input";
        _notifyIcon.Visible = true;

        // Create context menu
        var showItem = new ToolStripMenuItem("Show", null, OnShowClick);
        var settingsItem = new ToolStripMenuItem("Settings", null, OnSettingsClick);
        var separator = new ToolStripSeparator();
        var exitItem = new ToolStripMenuItem("Exit", null, OnExitClick);

        _contextMenu.Items.AddRange([showItem, settingsItem, separator, exitItem]);
        _notifyIcon.ContextMenuStrip = _contextMenu;

        // Handle double click
        _notifyIcon.DoubleClick += (s, e) => ShowForm();
    }

    private async void InitializeAsync()
    {
        // Initialize Vosk model if path is specified
        if (!string.IsNullOrEmpty(_configManager.Settings.ModelPath))
        {
            try
            {
                await _speechRecognizer.InitializeAsync(
                    _configManager.Settings.ModelPath,
                    _configManager.Settings.Language
                );

                _notifyIcon.ShowBalloonTip(
                    2000,
                    "Voxify",
                    $"Model loaded. Language: {_configManager.Settings.Language}",
                    ToolTipIcon.Info
                );
            }
            catch (Exception ex)
            {
                _notifyIcon.ShowBalloonTip(
                    5000,
                    "Voxify — Error",
                    $"Failed to load model: {ex.Message}",
                    ToolTipIcon.Error
                );
            }
        }
        else
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                "Voxify — Warning",
                "Model path is not specified. Configure the application via context menu.",
                ToolTipIcon.Warning
            );
        }

        // Register hotkey
        try
        {
            _hotkeyManager.RegisterHotkey(_configManager.Settings.Hotkey);
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                "Voxify — Error",
                $"Failed to register hotkey: {ex.Message}",
                ToolTipIcon.Error
            );
        }
    }

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        // Show notification about recording start
        _notifyIcon.ShowBalloonTip(
            1000,
            "Voxify",
            "Recording speech...",
            ToolTipIcon.Info
        );

        try
        {
            // Recognize speech (max 10 seconds)
            var text = await _speechRecognizer.RecognizeFromMicrophoneAsync(10);

            if (!string.IsNullOrEmpty(text))
            {
                // Insert text
                _textInputInjector.TypeText(text);

                _notifyIcon.ShowBalloonTip(
                    2000,
                    "Voxify",
                    $"Recognized: {text}",
                    ToolTipIcon.Info
                );
            }
            else
            {
                _notifyIcon.ShowBalloonTip(
                    2000,
                    "Voxify",
                    "Speech not recognized",
                    ToolTipIcon.Info
                );
            }
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                "Voxify — Error",
                $"Recognition error: {ex.Message}",
                ToolTipIcon.Error
            );
        }
    }

    private void OnShowClick(object? sender, EventArgs e)
    {
        ShowForm();
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        // TODO: Open settings window
        MessageBox.Show(
            $"Voxify Settings\n\nModel Path: {_configManager.Settings.ModelPath}\nLanguage: {_configManager.Settings.Language}\nHotkey: {_configManager.Settings.Hotkey}",
            "Voxify — Settings",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        _hotkeyManager.Dispose();
        _speechRecognizer.Dispose();
        Application.Exit();
    }

    private void ShowForm()
    {
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;

        ShowInTaskbar = true;
        Activate();

        // Скрываем обратно
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Hide form instead of closing (if not exiting via menu)
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
        }
        else
        {
            _notifyIcon.Dispose();
            base.OnFormClosing(e);
        }
    }
}
