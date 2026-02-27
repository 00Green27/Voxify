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
    private readonly TextInputInjector _textInputInjector;
    private readonly VoskEngine _voskEngine;
    private readonly AudioRecorder _audioRecorder;
    private readonly IpcServer _ipcServer;
    private readonly RecognizerFactory _recognizerFactory;

    private ISpeechRecognizer? _speechRecognizer;
    private RecordingState _recordingState;
    private TaskCompletionSource<bool>? _recordingCompletionSource;
    private bool _debugMode;

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
        _recognizerFactory = new RecognizerFactory(_audioRecorder, _voskEngine);
        _hotkeyManager = new HotkeyManager();
        _textInputInjector = new TextInputInjector(_configManager.Settings.TextInput);

        // Initialize state
        _recordingState = RecordingState.Idle;
        _debugMode = false;

        // Configure UI
        InitializeUI();

        // Initialize IPC server
        _ipcServer = new IpcServer();
        _ipcServer.CommandReceivedAsync += OnIpcCommandReceivedAsync;
        _ipcServer.Start();

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

        // Audio recorder events
        _audioRecorder.RecordingStarted += OnRecordingStarted;
        _audioRecorder.RecordingStopped += OnRecordingStopped;
    }

    private void OnRecordingStarted(object? sender, EventArgs e)
    {
        Console.WriteLine("[MainForm] Recording started");
    }

    private void OnRecordingStopped(object? sender, EventArgs e)
    {
        Console.WriteLine("[MainForm] Recording stopped");
        _recordingCompletionSource?.TrySetResult(true);
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
        // Create speech recognizer based on configured provider
        var speechConfig = _configManager.Settings.SpeechRecognition;
        
        // Fallback to legacy config if new config is empty
        if (string.IsNullOrEmpty(speechConfig.ModelPath) && !string.IsNullOrEmpty(_configManager.Settings.ModelPath))
        {
            speechConfig.ModelPath = _configManager.Settings.ModelPath;
            speechConfig.Language = _configManager.Settings.Language;
            speechConfig.Provider = "Vosk";
        }

        if (!string.IsNullOrEmpty(speechConfig.ModelPath))
        {
            try
            {
                // Create recognizer based on provider
                _speechRecognizer = _recognizerFactory.CreateRecognizer(speechConfig.Provider);
                
                await _speechRecognizer.InitializeAsync(
                    speechConfig.ModelPath,
                    speechConfig.Language
                );

                var providerName = _speechRecognizer.Provider == SpeechProvider.Vosk ? "Vosk" : "Whisper";
                _notifyIcon.ShowBalloonTip(
                    2000,
                    "Voxify",
                    $"{providerName} model loaded. Language: {speechConfig.Language}",
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
        _recordingCompletionSource = new TaskCompletionSource<bool>();
        UpdateTrayIconForRecording(true);

        // Start recording
        _audioRecorder.StartRecording();

        Console.WriteLine("[MainForm] StartRecording called");

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
            Console.WriteLine("[MainForm] Stopping recording...");

            // Stop recording
            var audioBytes = await _audioRecorder.StopRecordingAsync();

            // Wait for recording to fully stop
            if (_recordingCompletionSource != null)
            {
                await _recordingCompletionSource.Task.TimeoutAfter(TimeSpan.FromSeconds(2));
            }

            Console.WriteLine($"[MainForm] Audio recorded: {audioBytes.Length} bytes");

            if (audioBytes.Length > 0)
            {
                // Recognize
                if (_speechRecognizer != null && _speechRecognizer.IsInitialized)
                {
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
                        "Voxify — Error",
                        "Speech recognizer is not initialized",
                        ToolTipIcon.Error
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
            _recordingCompletionSource = null;
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

    /// <summary>
    /// Обработка команд от IPC клиента (CLI).
    /// </summary>
    private async Task<IpcResponse> OnIpcCommandReceivedAsync(IpcCommand command)
    {
        Console.WriteLine($"[MainForm] Получена IPC команда: {command.Type}");

        try
        {
            switch (command.Type.ToLower())
            {
                case "toggle":
                    await HandleToggleCommand();
                    return new IpcResponse { Success = true, Message = "Toggle command processed" };

                case "cancel":
                    await HandleCancelCommand();
                    return new IpcResponse { Success = true, Message = "Cancel command processed" };

                case "status":
                    return await HandleStatusCommand();

                case "debug":
                    return await HandleDebugCommand();

                default:
                    Console.WriteLine($"[MainForm] Неизвестная команда: {command.Type}");
                    return new IpcResponse { Success = false, Message = $"Unknown command: {command.Type}" };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainForm] Error processing command {command.Type}: {ex.Message}");
            return new IpcResponse { Success = false, Message = ex.Message };
        }
    }

    private async Task HandleToggleCommand()
    {
        if (_recordingState == RecordingState.Idle)
        {
            await StartRecordingAsync();
        }
        else if (_recordingState == RecordingState.Recording)
        {
            await StopRecordingAndRecognizeAsync();
        }
    }

    private async Task HandleCancelCommand()
    {
        if (_recordingState == RecordingState.Recording)
        {
            await _audioRecorder.ForceStopAsync();
            _recordingState = RecordingState.Idle;
            UpdateTrayIconForRecording(false);
            Console.WriteLine("[MainForm] Recording cancelled via CLI");
        }
    }

    private async Task<IpcResponse> HandleStatusCommand()
    {
        var status = _recordingState.ToString();
        Console.WriteLine($"[MainForm] Status: {status}");
        return new IpcResponse { Success = true, Message = "Status retrieved", Data = status };
    }

    private async Task<IpcResponse> HandleDebugCommand()
    {
        _debugMode = !_debugMode;
        var status = _debugMode ? "enabled" : "disabled";
        Console.WriteLine($"[MainForm] Debug mode: {status}");
        return new IpcResponse { Success = true, Message = "Debug mode toggled", Data = status };
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        _hotkeyManager.Dispose();
        _speechRecognizer?.Dispose();
        _ipcServer.Dispose();
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
