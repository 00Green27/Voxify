using System.Windows.Forms;
using Voxify.Config;
using WindowsInput;
using WindowsInput.Native;

namespace Voxify.Core;

/// <summary>
/// Component for emulating keyboard text input.
/// </summary>
public class TextInputInjector
{
    private readonly InputSimulator _inputSimulator;
    private readonly TextInputConfig _config;

    public TextInputInjector(TextInputConfig config)
    {
        _inputSimulator = new InputSimulator();
        _config = config;
    }

    /// <summary>
    /// Types text into the active window.
    /// </summary>
    public void TypeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // Preprocess text — remove Vosk special characters
        string processedText = PreprocessText(text);

        if (_config.PasteAsClipboard)
        {
            TypeViaClipboard(processedText);
        }
        else
        {
            TypeViaKeyboard(processedText);
        }
    }

    /// <summary>
    /// Preprocesses text — removes special characters and normalizes.
    /// </summary>
    private static string PreprocessText(string text)
    {
        // Remove possible Vosk markers and extra spaces
        return text
            .Replace("{", "")
            .Replace("}", "")
            .Replace("<", "")
            .Replace(">", "")
            .Trim();
    }

    /// <summary>
    /// Types text via keyboard emulation.
    /// </summary>
    private void TypeViaKeyboard(string text)
    {
        foreach (char c in text)
        {
            // Simulate key press for character
            _inputSimulator.Keyboard.TextEntry(c);

            if (_config.TypeDelayMs > 0)
            {
                Thread.Sleep(_config.TypeDelayMs);
            }
        }
    }

    /// <summary>
    /// Types text via clipboard (faster for long text).
    /// </summary>
    private void TypeViaClipboard(string text)
    {
        try
        {
            // Save current clipboard content
            var originalText = Clipboard.GetText();

            // Set new text
            Clipboard.SetText(text);

            // Emulate Ctrl+V
            _inputSimulator.Keyboard.ModifiedKeyStroke(
                VirtualKeyCode.CONTROL,
                VirtualKeyCode.VK_V
            );

            // Restore original clipboard
            if (!string.IsNullOrEmpty(originalText))
            {
                Thread.Sleep(100);
                Clipboard.SetText(originalText);
            }
        }
        catch
        {
            // Fallback to keyboard if clipboard doesn't work
            TypeViaKeyboard(text);
        }
    }

    /// <summary>
    /// Emulates Enter key press to send message.
    /// </summary>
    public void PressEnter()
    {
        _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
    }

    /// <summary>
    /// Emulates Ctrl+V (paste).
    /// </summary>
    public void Paste()
    {
        _inputSimulator.Keyboard.ModifiedKeyStroke(
            VirtualKeyCode.CONTROL,
            VirtualKeyCode.VK_V
        );
    }
}
