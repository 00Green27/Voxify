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

    private RecordingState _recordingState;

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

        // Initialize state
        _recordingState = RecordingState.Idle;

        // Configure UI
        InitializeUI();

        // Subscribe to events
        SubscribeToEvents();

        // Initialize the application
        InitializeAsync();
    }

    private void SubscribeToEvents()
    {
        // Toggle mode events
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        // PushToTalk mode events
        _hotkeyManager.PushToTalkKeyDown += OnPushToTalkKeyDown;
        _hotkeyManager.PushToTalkKeyUp += OnPushToTalkKeyUp;
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

    /// <summary>
    /// Updates tray icon based on recording state.
    /// </summary>
    private void UpdateTrayIconForRecording(bool isRecording)
    {
        if (isRecording)
        {
            // Green icon for recording state
            _notifyIcon.Icon = SystemIcons.Application;
            _notifyIcon.Text = $"Voxify — Recording... (Mode: {_hotkeyManager.Mode})";
        }
        else
        {
            // Default icon for idle state
            _notifyIcon.Icon = SystemIcons.Application;
            _notifyIcon.Text = "Voxify — Voice Input";
        }
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
        // Toggle mode: start/stop recording
        if (_recordingState == RecordingState.Idle)
        {
            await StartRecordingAsync();
        }
        else if (_recordingState == RecordingState.Recording)
        {
            await StopRecordingAndRecognizeAsync();
        }
    }

    private async void OnPushToTalkKeyDown(object? sender, EventArgs e)
    {
        // PushToTalk: start recording on key down
        if (_recordingState == RecordingState.Idle)
        {
            await StartRecordingAsync();
        }
    }

    private async void OnPushToTalkKeyUp(object? sender, EventArgs e)
    {
        // PushToTalk: stop recording on key up
        if (_recordingState == RecordingState.Recording)
        {
            await StopRecordingAndRecognizeAsync();
        }
    }

    private async Task StartRecordingAsync()
    {
        _recordingState = RecordingState.Recording;
        UpdateTrayIconForRecording(true);

        _notifyIcon.ShowBalloonTip(
            1000,
            "Voxify",
            "Recording speech...",
            ToolTipIcon.Info
        );
    }

    private async Task StopRecordingAndRecognizeAsync()
    {
        if (_recordingState != RecordingState.Recording)
        {
            return;
        }

        _recordingState = RecordingState.Processing;
        UpdateTrayIconForRecording(false);

        try
        {
            // Stop recording
            var audioBytes = await _audioRecorder.StopRecordingAsync();

            if (audioBytes.Length > 0)
            {
                // Recognize
                var text = await _speechRecognizer.RecognizeAsync(audioBytes);

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
            else
            {
                _notifyIcon.ShowBalloonTip(
                    2000,
                    "Voxify",
                    "No audio recorded",
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
        finally
        {
            _recordingState = RecordingState.Idle;
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
