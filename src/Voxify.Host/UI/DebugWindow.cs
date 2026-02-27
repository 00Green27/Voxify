using System.Drawing;
using Voxify.Core;

namespace Voxify.UI;

/// <summary>
/// Debug window that displays real-time logs and application state.
/// </summary>
public class DebugWindow : Form
{
    private readonly DebugService _debugService;
    private readonly ListBox _logListBox;
    private readonly Label _statusLabel;
    private readonly Panel _statePanel;
    private readonly System.Windows.Forms.Timer _updateTimer;

    public DebugWindow(DebugService debugService)
    {
        _debugService = debugService;

        // Configure form
        Text = "Voxify Debug Console";
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Consolas", 9F, FontStyle.Regular);

        // Create controls
        _logListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9F, FontStyle.Regular),
            HorizontalScrollbar = true
        };

        _statePanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 120,
            Padding = new Padding(10)
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            BackColor = Color.FromArgb(240, 240, 240),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0)
        };

        // Create state labels
        var stateLabels = CreateStateLabels();
        _statePanel.Controls.AddRange(stateLabels);
        _statePanel.Controls.Add(_statusLabel);

        // Add controls to form
        Controls.Add(_logListBox);
        Controls.Add(_statePanel);

        // Subscribe to debug service events
        _debugService.LogAdded += OnLogAdded;
        _debugService.StateChanged += OnStateChanged;

        // Load existing logs
        LoadExistingLogs();

        // Set up update timer for real-time state updates
        _updateTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _updateTimer.Tick += (s, e) => UpdateStateDisplay();
        _updateTimer.Start();

        // Handle form closing
        FormClosing += DebugWindow_FormClosing;
    }

    private Control[] CreateStateLabels()
    {
        var labels = new List<Control>();
        var startY = 10;
        var labelHeight = 20;
        var gap = 5;

        // Recording State
        var recordingLabel = CreateStateLabel("Recording State:", "Idle", 10, startY);
        labels.Add(recordingLabel);

        // Audio Level
        var audioLabel = CreateStateLabel("Audio Level:", "0.00", 10, startY + labelHeight + gap);
        labels.Add(audioLabel);

        // VAD State
        var vadLabel = CreateStateLabel("VAD State:", "Inactive", 10, startY + 2 * (labelHeight + gap));
        labels.Add(vadLabel);

        // Provider
        var providerLabel = CreateStateLabel("Provider:", "N/A", 10, startY + 3 * (labelHeight + gap));
        labels.Add(providerLabel);

        // Store references for updates
        Tag = new StateLabels
        {
            RecordingState = recordingLabel,
            AudioLevel = audioLabel,
            VadState = vadLabel,
            Provider = providerLabel
        };

        return labels.ToArray();
    }

    private Label CreateStateLabel(string text, string initialValue, int x, int y)
    {
        return new Label
        {
            Text = $"{text} {initialValue}",
            Location = new Point(x, y),
            Size = new Size(380, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            Tag = text
        };
    }

    private class StateLabels
    {
        public Label RecordingState { get; set; } = null!;
        public Label AudioLevel { get; set; } = null!;
        public Label VadState { get; set; } = null!;
        public Label Provider { get; set; } = null!;
    }

    private void LoadExistingLogs()
    {
        foreach (var log in _debugService.Logs)
        {
            _logListBox.Items.Add(log.ToString());
        }
        _logListBox.TopIndex = _logListBox.Items.Count - 1;
    }

    private void OnLogAdded(object? sender, LogEntry e)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnLogAdded(sender, e));
            return;
        }

        _logListBox.Items.Add(e.ToString());
        _logListBox.TopIndex = _logListBox.Items.Count - 1;
    }

    private void OnStateChanged(object? sender, DebugState e)
    {
        // State is updated via timer
    }

    private void UpdateStateDisplay()
    {
        if (Tag is not StateLabels labels)
            return;

        var state = _debugService.State;

        // Update Recording State
        var recordingColor = state.IsRecording ? Color.Green : Color.Gray;
        if (state.RecordingState == RecordingState.Processing)
            recordingColor = Color.Orange;

        labels.RecordingState.Text = $"Recording State: {state.RecordingState}";
        labels.RecordingState.ForeColor = recordingColor;

        // Update Audio Level
        labels.AudioLevel.Text = $"Audio Level: {state.AudioLevel:F2}";
        labels.AudioLevel.ForeColor = state.AudioLevel > 0.1f ? Color.Blue : Color.Gray;

        // Update VAD State
        var vadText = state.IsVadActive ? $"Active ({state.VadConfidence:F2})" : "Inactive";
        labels.VadState.Text = $"VAD State: {vadText}";
        labels.VadState.ForeColor = state.IsVadActive ? Color.Green : Color.Gray;

        // Update Provider
        labels.Provider.Text = $"Provider: {state.SpeechProvider}";

        // Update status label
        _statusLabel.Text = $"[{DateTime.Now:HH:mm:ss}] Recording: {state.IsRecording} | VAD: {(state.IsVadActive ? "ON" : "OFF")} | Level: {state.AudioLevel:F2}";
    }

    private void DebugWindow_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // Hide instead of close
        e.Cancel = true;
        Hide();
    }

    /// <summary>
    /// Shows the debug window (brings to front if already open).
    /// </summary>
    public new void Show()
    {
        if (Visible)
        {
            BringToFront();
            Activate();
        }
        else
        {
            base.Show();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _updateTimer?.Dispose();
            _debugService.LogAdded -= OnLogAdded;
            _debugService.StateChanged -= OnStateChanged;
        }
        base.Dispose(disposing);
    }
}
