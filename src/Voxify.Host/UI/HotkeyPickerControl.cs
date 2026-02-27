using System.Drawing;
using System.Windows.Forms;
using Voxify.Config;
using Voxify.Core;

namespace Voxify.UI;

/// <summary>
/// UserControl for selecting and previewing a hotkey combination.
/// </summary>
public class HotkeyPickerControl : UserControl
{
    private readonly CheckBox _ctrlCheckBox;
    private readonly CheckBox _altCheckBox;
    private readonly CheckBox _shiftCheckBox;
    private readonly CheckBox _winCheckBox;
    private readonly ComboBox _keyComboBox;
    private readonly Label _previewLabel;
    private readonly Button _testButton;

    private HotkeyConfig? _currentConfig;
    private bool _isTesting;

    public event EventHandler<HotkeyConfig>? HotkeyChanged;

    public HotkeyPickerControl()
    {
        // Configure control
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(10);

        // Create modifier checkboxes
        _ctrlCheckBox = new CheckBox
        {
            Text = "Ctrl",
            AutoSize = true,
            Location = new Point(0, 0)
        };
        _ctrlCheckBox.CheckedChanged += OnModifierChanged;

        _altCheckBox = new CheckBox
        {
            Text = "Alt",
            AutoSize = true,
            Location = new Point(60, 0)
        };
        _altCheckBox.CheckedChanged += OnModifierChanged;

        _shiftCheckBox = new CheckBox
        {
            Text = "Shift",
            AutoSize = true,
            Location = new Point(120, 0)
        };
        _shiftCheckBox.CheckedChanged += OnModifierChanged;

        _winCheckBox = new CheckBox
        {
            Text = "Win",
            AutoSize = true,
            Location = new Point(190, 0)
        };
        _winCheckBox.CheckedChanged += OnModifierChanged;

        // Create key dropdown
        _keyComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 80,
            Location = new Point(0, 30)
        };
        PopulateKeyComboBox();
        _keyComboBox.SelectedIndexChanged += OnKeyChanged;

        // Create preview label
        _previewLabel = new Label
        {
            AutoSize = true,
            Location = new Point(100, 33),
            Font = new Font(Font.FontFamily, 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215)
        };

        // Create test button
        _testButton = new Button
        {
            Text = "Проверить",
            AutoSize = true,
            Location = new Point(250, 28),
            Enabled = false
        };
        _testButton.Click += OnTestClick;

        // Add controls
        Controls.AddRange([
            _ctrlCheckBox, _altCheckBox, _shiftCheckBox, _winCheckBox,
            _keyComboBox, _previewLabel, _testButton
        ]);

        // Set minimum size
        MinimumSize = new Size(400, 70);
    }

    private void PopulateKeyComboBox()
    {
        var keys = new List<string>();

        // Function keys
        for (int i = 1; i <= 12; i++)
        {
            keys.Add($"F{i}");
        }

        // Letters
        for (char c = 'A'; c <= 'Z'; c++)
        {
            keys.Add(c.ToString());
        }

        // Numbers
        for (int i = 0; i <= 9; i++)
        {
            keys.Add($"D{i}");
        }

        _keyComboBox.Items.AddRange(keys.ToArray());
        _keyComboBox.SelectedIndex = 0; // F1 by default
    }

    private void OnModifierChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
        ValidateAndNotify();
    }

    private void OnKeyChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
        ValidateAndNotify();
    }

    private void UpdatePreview()
    {
        var parts = new List<string>();

        if (_ctrlCheckBox.Checked) parts.Add("Ctrl");
        if (_altCheckBox.Checked) parts.Add("Alt");
        if (_shiftCheckBox.Checked) parts.Add("Shift");
        if (_winCheckBox.Checked) parts.Add("Win");

        var key = _keyComboBox.SelectedItem?.ToString() ?? "F12";
        parts.Add(key);

        _previewLabel.Text = string.Join(" + ", parts);
    }

    private void ValidateAndNotify()
    {
        // At least one modifier is required
        bool hasModifier = _ctrlCheckBox.Checked || _altCheckBox.Checked || 
                          _shiftCheckBox.Checked || _winCheckBox.Checked;

        _testButton.Enabled = hasModifier;

        if (hasModifier)
        {
            _currentConfig = new HotkeyConfig
            {
                Modifiers = GetSelectedModifiers(),
                Key = _keyComboBox.SelectedItem?.ToString() ?? "F12"
            };

            HotkeyChanged?.Invoke(this, _currentConfig);
        }
        else
        {
            _currentConfig = null;
        }
    }

    private List<string> GetSelectedModifiers()
    {
        var modifiers = new List<string>();
        if (_ctrlCheckBox.Checked) modifiers.Add("Control");
        if (_altCheckBox.Checked) modifiers.Add("Alt");
        if (_shiftCheckBox.Checked) modifiers.Add("Shift");
        if (_winCheckBox.Checked) modifiers.Add("Win");
        return modifiers;
    }

    private async void OnTestClick(object? sender, EventArgs e)
    {
        if (_currentConfig == null || _isTesting)
            return;

        _isTesting = true;
        _testButton.Enabled = false;
        _testButton.Text = "Нажмите хоткей...";
        _testButton.BackColor = Color.LightGreen;

        // In a real implementation, we would register the hotkey temporarily
        // For now, just show a message
        await Task.Delay(2000);

        _isTesting = false;
        _testButton.Enabled = true;
        _testButton.Text = "Проверить";
        _testButton.BackColor = SystemColors.Control;
    }

    /// <summary>
    /// Sets the current hotkey configuration.
    /// </summary>
    public void SetHotkeyConfig(HotkeyConfig config)
    {
        _currentConfig = config;

        // Set modifiers
        _ctrlCheckBox.Checked = config.Modifiers.Contains("Control", StringComparer.OrdinalIgnoreCase);
        _altCheckBox.Checked = config.Modifiers.Contains("Alt", StringComparer.OrdinalIgnoreCase);
        _shiftCheckBox.Checked = config.Modifiers.Contains("Shift", StringComparer.OrdinalIgnoreCase);
        _winCheckBox.Checked = config.Modifiers.Contains("Win", StringComparer.OrdinalIgnoreCase);

        // Set key
        if (config.Key != null)
        {
            _keyComboBox.SelectedItem = config.Key;
        }

        UpdatePreview();
    }

    /// <summary>
    /// Gets the current hotkey configuration.
    /// </summary>
    public HotkeyConfig GetHotkeyConfig()
    {
        return new HotkeyConfig
        {
            Mode = "Toggle", // Mode is set separately
            Modifiers = GetSelectedModifiers(),
            Key = _keyComboBox.SelectedItem?.ToString() ?? "F12"
        };
    }
}
