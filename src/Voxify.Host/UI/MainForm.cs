using System.Drawing;
using Voxify.Config;
using Voxify.Core;

namespace Voxify.UI;

/// <summary>
/// Главная форма приложения Voxify.
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
        // Инициализация компонентов
        _contextMenu = new ContextMenuStrip();
        _notifyIcon = new NotifyIcon();
        
        // Загружаем конфигурацию
        _configManager = new ConfigurationManager();

        // Создаём компоненты ядра
        _voskEngine = new VoskEngine();
        _audioRecorder = new AudioRecorder();
        _speechRecognizer = new SpeechRecognizerService(_voskEngine, _audioRecorder);
        _hotkeyManager = new HotkeyManager();
        _textInputInjector = new TextInputInjector(_configManager.Settings.TextInput);

        // Настраиваем UI
        InitializeUI();
        
        // Подписываемся на события
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
        
        // Инициализируем приложение
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
        
        // Создаём иконку (используем стандартную)
        _notifyIcon.Icon = SystemIcons.Application;
        _notifyIcon.Text = "Voxify — Голосовой ввод";
        _notifyIcon.Visible = true;

        // Создаём контекстное меню
        var showItem = new ToolStripMenuItem("Показать", null, OnShowClick);
        var settingsItem = new ToolStripMenuItem("Настройки", null, OnSettingsClick);
        var separator = new ToolStripSeparator();
        var exitItem = new ToolStripMenuItem("Выход", null, OnExitClick);

        _contextMenu.Items.AddRange([showItem, settingsItem, separator, exitItem]);
        _notifyIcon.ContextMenuStrip = _contextMenu;

        // Обработка двойного клика
        _notifyIcon.DoubleClick += (s, e) => ShowForm();
    }

    private async void InitializeAsync()
    {
        // Инициализируем модель Vosk если путь указан
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
                    $"Модель загружена. Язык: {_configManager.Settings.Language}",
                    ToolTipIcon.Info
                );
            }
            catch (Exception ex)
            {
                _notifyIcon.ShowBalloonTip(
                    5000,
                    "Voxify — Ошибка",
                    $"Не удалось загрузить модель: {ex.Message}",
                    ToolTipIcon.Error
                );
            }
        }
        else
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                "Voxify — Внимание",
                "Путь к модели не указан. Настройте приложение через контекстное меню.",
                ToolTipIcon.Warning
            );
        }

        // Регистрируем горячую клавишу
        try
        {
            _hotkeyManager.RegisterHotkey(_configManager.Settings.Hotkey);
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                "Voxify — Ошибка",
                $"Не удалось зарегистрировать хоткей: {ex.Message}",
                ToolTipIcon.Error
            );
        }
    }

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        // Показываем уведомление о начале записи
        _notifyIcon.ShowBalloonTip(
            1000,
            "Voxify",
            "Запись речи...",
            ToolTipIcon.Info
        );

        try
        {
            // Распознаём речь (максимум 10 секунд)
            var text = await _speechRecognizer.RecognizeFromMicrophoneAsync(10);

            if (!string.IsNullOrEmpty(text))
            {
                // Вставляем текст
                _textInputInjector.TypeText(text);
                
                _notifyIcon.ShowBalloonTip(
                    2000,
                    "Voxify",
                    $"Распознано: {text}",
                    ToolTipIcon.Info
                );
            }
            else
            {
                _notifyIcon.ShowBalloonTip(
                    2000,
                    "Voxify",
                    "Речь не распознана",
                    ToolTipIcon.Info
                );
            }
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                "Voxify — Ошибка",
                $"Ошибка распознавания: {ex.Message}",
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
        // TODO: Открыть окно настроек
        MessageBox.Show(
            $"Настройки Voxify\n\nПуть к модели: {_configManager.Settings.ModelPath}\nЯзык: {_configManager.Settings.Language}\nГорячая клавиша: {_configManager.Settings.Hotkey}",
            "Voxify — Настройки",
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
        // Скрываем форму вместо закрытия (если не выходим через меню)
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
