using System.Drawing;
using System.Windows.Forms;
using Voxify.Config;

namespace Voxify.UI;

/// <summary>
/// TextBox for capturing hotkey combinations from keyboard input.
/// User presses keys (e.g., Ctrl+Z) and the combination is displayed.
/// </summary>
public class HotkeyInputTextBox : TextBox
{
    private HotkeyConfig? _currentConfig;
    private bool _isRecording;

    public event EventHandler<HotkeyConfig>? HotkeyChanged;

    public HotkeyInputTextBox()
    {
        // Configure text box
        ReadOnly = true;
        Text = "Нажмите комбинацию...";
        ForeColor = Color.Gray;
        TextAlign = HorizontalAlignment.Center;
        
        // Subscribe to key events
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        GotFocus += OnGotFocus;
        LostFocus += OnLostFocus;
    }

    private void OnGotFocus(object? sender, EventArgs e)
    {
        _isRecording = true;
        Text = "Нажмите комбинацию...";
        ForeColor = Color.Gray;
    }

    private void OnLostFocus(object? sender, EventArgs e)
    {
        _isRecording = false;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isRecording)
            return;

        e.SuppressKeyPress = true; // Prevent system key processing

        var modifiers = new List<string>();
        
        // Collect pressed modifiers
        if (Control.ModifierKeys.HasFlag(Keys.Control))
            modifiers.Add("Control");
        if (Control.ModifierKeys.HasFlag(Keys.Alt))
            modifiers.Add("Alt");
        if (Control.ModifierKeys.HasFlag(Keys.Shift))
            modifiers.Add("Shift");
        if (Control.ModifierKeys.HasFlag(Keys.LWin) || Control.ModifierKeys.HasFlag(Keys.RWin))
            modifiers.Add("Win");

        // Get the main key (non-modifier)
        var key = e.KeyCode;
        
        // Ignore modifier-only presses
        if (key == Keys.ControlKey || key == Keys.Alt || key == Keys.ShiftKey || 
            key == Keys.LWin || key == Keys.RWin)
        {
            return;
        }

        // Convert key to readable string
        string keyString = ConvertKeysToString(key);
        
        // Require at least one modifier
        if (modifiers.Count == 0)
        {
            Text = "Требуется модификатор (Ctrl/Alt/Shift/Win)";
            ForeColor = Color.Red;
            _currentConfig = null;
            return;
        }

        // Build display string
        var displayParts = new List<string>();
        if (modifiers.Contains("Control")) displayParts.Add("Ctrl");
        if (modifiers.Contains("Alt")) displayParts.Add("Alt");
        if (modifiers.Contains("Shift")) displayParts.Add("Shift");
        if (modifiers.Contains("Win")) displayParts.Add("Win");
        displayParts.Add(keyString);

        Text = string.Join(" + ", displayParts);
        ForeColor = Color.Black;

        // Create config
        _currentConfig = new HotkeyConfig
        {
            Modifiers = modifiers,
            Key = keyString
        };

        HotkeyChanged?.Invoke(this, _currentConfig);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        // Optional: Clear state on key release if needed
    }

    private static string ConvertKeysToString(Keys key)
    {
        // Handle special keys
        return key switch
        {
            >= Keys.F1 and <= Keys.F12 => key.ToString(),
            >= Keys.A and <= Keys.Z => key.ToString().ToUpper(),
            >= Keys.D0 and <= Keys.D9 => key.ToString()[1..], // Remove 'D' prefix
            >= Keys.NumPad0 and <= Keys.NumPad9 => key.ToString()[7..], // Remove 'NumPad' prefix
            Keys.Space => "Space",
            Keys.Tab => "Tab",
            Keys.Enter => "Enter",
            Keys.Escape => "Escape",
            Keys.Delete => "Delete",
            Keys.Back => "Back",
            Keys.Insert => "Insert",
            Keys.Home => "Home",
            Keys.End => "End",
            Keys.PageUp => "PageUp",
            Keys.PageDown => "PageDown",
            Keys.Left => "←",
            Keys.Right => "→",
            Keys.Up => "↑",
            Keys.Down => "↓",
            _ => key.ToString()
        };
    }

    /// <summary>
    /// Sets the current hotkey configuration.
    /// </summary>
    public void SetHotkeyConfig(HotkeyConfig config)
    {
        _currentConfig = config;

        var displayParts = new List<string>();
        if (config.Modifiers.Contains("Control", StringComparer.OrdinalIgnoreCase)) 
            displayParts.Add("Ctrl");
        if (config.Modifiers.Contains("Alt", StringComparer.OrdinalIgnoreCase)) 
            displayParts.Add("Alt");
        if (config.Modifiers.Contains("Shift", StringComparer.OrdinalIgnoreCase)) 
            displayParts.Add("Shift");
        if (config.Modifiers.Contains("Win", StringComparer.OrdinalIgnoreCase)) 
            displayParts.Add("Win");
        
        displayParts.Add(config.Key.ToUpper());

        Text = string.Join(" + ", displayParts);
        ForeColor = Color.Black;
    }

    /// <summary>
    /// Gets the current hotkey configuration.
    /// </summary>
    public HotkeyConfig? GetHotkeyConfig()
    {
        return _currentConfig;
    }
}
