using System.Windows.Forms;
using Voxify.Config;
using WindowsInput;
using WindowsInput.Native;

namespace Voxify.Core;

/// <summary>
/// Компонент для эмуляции ввода текста с клавиатуры.
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
    /// Вводит текст в активное окно.
    /// </summary>
    public void TypeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // Предобработка текста — удаляем спецсимволы Vosk
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
    /// Предобрабатывает текст — удаляет спецсимволы и нормализует.
    /// </summary>
    private static string PreprocessText(string text)
    {
        // Удаляем возможные маркеры Vosk и лишние пробелы
        return text
            .Replace("{", "")
            .Replace("}", "")
            .Replace("<", "")
            .Replace(">", "")
            .Trim();
    }

    /// <summary>
    /// Вводит текст через эмуляцию клавиатуры.
    /// </summary>
    private void TypeViaKeyboard(string text)
    {
        foreach (char c in text)
        {
            // Симулируем нажатие клавиши для символа
            _inputSimulator.Keyboard.TextEntry(c);
            
            if (_config.TypeDelayMs > 0)
            {
                Thread.Sleep(_config.TypeDelayMs);
            }
        }
    }

    /// <summary>
    /// Вводит текст через буфер обмена (быстрее для длинного текста).
    /// </summary>
    private void TypeViaClipboard(string text)
    {
        try
        {
            // Сохраняем текущее содержимое буфера
            var originalText = Clipboard.GetText();

            // Устанавливаем новый текст
            Clipboard.SetText(text);

            // Эмулируем Ctrl+V
            _inputSimulator.Keyboard.ModifiedKeyStroke(
                VirtualKeyCode.CONTROL,
                VirtualKeyCode.VK_V
            );

            // Восстанавливаем оригинальный буфер
            if (!string.IsNullOrEmpty(originalText))
            {
                Thread.Sleep(100);
                Clipboard.SetText(originalText);
            }
        }
        catch
        {
            // Fallback на клавиатуру если буфер не работает
            TypeViaKeyboard(text);
        }
    }

    /// <summary>
    /// Эмулирует нажатие Enter для отправки сообщения.
    /// </summary>
    public void PressEnter()
    {
        _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
    }

    /// <summary>
    /// Эмулирует нажатие Ctrl+V (вставить).
    /// </summary>
    public void Paste()
    {
        _inputSimulator.Keyboard.ModifiedKeyStroke(
            VirtualKeyCode.CONTROL,
            VirtualKeyCode.VK_V
        );
    }
}
