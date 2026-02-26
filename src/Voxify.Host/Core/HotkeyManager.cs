using System.Runtime.InteropServices;
using Voxify.Config;

namespace Voxify.Core;

/// <summary>
/// Менеджер глобальных горячих клавиш через Windows API.
/// </summary>
public class HotkeyManager : IDisposable
{
    // Модификаторы клавиш (Windows API)
    public const int MOD_ALT = 0x1;
    public const int MOD_CONTROL = 0x2;
    public const int MOD_SHIFT = 0x4;
    public const int MOD_WIN = 0x8;

    // Сообщение WM_HOTKEY
    public const int WM_HOTKEY = 0x312;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern short GlobalAddAtom(string lpString);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern short GlobalDeleteAtom(short nAtom);

    private readonly IntPtr _handle;
    private readonly HotkeyMessageFilter _messageFilter;
    private short _hotkeyId;
    private HotkeyConfig? _config;
    private bool _disposed;

    /// <summary>
    /// Событие при нажатии горячей клавиши.
    /// </summary>
    public event EventHandler? HotkeyPressed;

    public HotkeyManager()
    {
        // Используем handle процесса для регистрации хоткеев
        _handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        
        // Добавляем фильтр сообщений для обработки WM_HOTKEY
        _messageFilter = new HotkeyMessageFilter(this);
        System.Windows.Forms.Application.AddMessageFilter(_messageFilter);
        
        _hotkeyId = 0;
    }

    /// <summary>
    /// Регистрирует горячую клавишу.
    /// </summary>
    public void RegisterHotkey(HotkeyConfig config)
    {
        UnregisterHotkey();

        _config = config;
        
        // Получаем уникальный ID через GlobalAddAtom
        string atomName = Guid.NewGuid().ToString();
        _hotkeyId = GlobalAddAtom(atomName);

        if (_hotkeyId == 0)
        {
            throw new InvalidOperationException($"Не удалось получить уникальный ID для хоткея. Ошибка: {Marshal.GetLastWin32Error()}");
        }

        // Регистрируем горячую клавишу
        int modifiers = config.GetCombinedModifiers();
        uint vkCode = ParseKeyCode(config.Key);

        if (!RegisterHotKey(_handle, _hotkeyId, (uint)modifiers, vkCode))
        {
            int error = Marshal.GetLastWin32Error();
            GlobalDeleteAtom(_hotkeyId);
            _hotkeyId = 0;
            throw new InvalidOperationException($"Не удалось зарегистрировать горячую клавишу {config}. Ошибка Windows: {error}");
        }
    }

    /// <summary>
    /// Отменяет регистрацию горячей клавиши.
    /// </summary>
    public void UnregisterHotkey()
    {
        if (_hotkeyId != 0)
        {
            UnregisterHotKey(_handle, _hotkeyId);
            GlobalDeleteAtom(_hotkeyId);
            _hotkeyId = 0;
        }
    }

    /// <summary>
    /// Парсит код клавиши из строки.
    /// </summary>
    private static uint ParseKeyCode(string keyCode) => keyCode.ToUpper() switch
    {
        "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
        "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
        "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
        "A" => 0x41, "B" => 0x42, "C" => 0x43, "D" => 0x44,
        "E" => 0x45, "F" => 0x46, "G" => 0x47, "H" => 0x48,
        "I" => 0x49, "J" => 0x4A, "K" => 0x4B, "L" => 0x4C,
        "M" => 0x4D, "N" => 0x4E, "O" => 0x4F, "P" => 0x50,
        "Q" => 0x51, "R" => 0x52, "S" => 0x53, "T" => 0x54,
        "U" => 0x55, "V" => 0x56, "W" => 0x57, "X" => 0x58,
        "Y" => 0x59, "Z" => 0x5A,
        "D0" => 0x30, "D1" => 0x31, "D2" => 0x32, "D3" => 0x33,
        "D4" => 0x34, "D5" => 0x35, "D6" => 0x36, "D7" => 0x37,
        "D8" => 0x38, "D9" => 0x39,
        _ => 0x7B // F12 по умолчанию
    };

    /// <summary>
    /// Обрабатывает сообщение WM_HOTKEY.
    /// </summary>
    internal void OnHotKeyMessage()
    {
        HotkeyPressed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            UnregisterHotkey();
            System.Windows.Forms.Application.RemoveMessageFilter(_messageFilter);
            _disposed = true;
        }
    }

    /// <summary>
    /// Фильтр сообщений для обработки WM_HOTKEY.
    /// </summary>
    private class HotkeyMessageFilter : System.Windows.Forms.IMessageFilter
    {
        private readonly HotkeyManager _parent;

        public HotkeyMessageFilter(HotkeyManager parent)
        {
            _parent = parent;
        }

        public bool PreFilterMessage(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                _parent.OnHotKeyMessage();
                return true;
            }
            return false;
        }
    }
}
