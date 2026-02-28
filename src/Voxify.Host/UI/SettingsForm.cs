using System.Drawing;
using System.Windows.Forms;
using Voxify.Config;
using Voxify.Core;

namespace Voxify.UI;

/// <summary>
/// Settings dialog for configuring Voxify application.
/// </summary>
public class SettingsForm : Form
{
    private readonly ConfigurationManager _configManager;
    private readonly DebugService _debugService;
    private readonly ModelDownloader _modelDownloader;

    // Controls - Hotkey section
    private GroupBox _hotkeyModeGroupBox = null!;
    private GroupBox _hotkeyCombinationGroupBox = null!;
    private ToggleSwitch _pushToTalkSwitch = null!;
    private Label _pushToTalkLabel = null!;
    private HotkeyInputTextBox _hotkeyInputTextBox = null!;

    // Controls - Model section
    private GroupBox _modelGroupBox = null!;
    private ComboBox _providerComboBox = null!;
    private ComboBox _modelComboBox = null!;
    private TextBox _modelPathTextBox = null!;
    private Button _browseButton = null!;
    private Button _downloadButton = null!;
    private ProgressBar _downloadProgressBar = null!;
    private Label _downloadStatusLabel = null!;
    private Panel _downloadPanel = null!;

    // Controls - Notifications section
    private GroupBox _notificationsGroupBox = null!;
    private CheckBox _showNotificationsCheckBox = null!;

    // Buttons
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private Button _showLogsButton = null!;

    private HotkeyConfig? _modifiedHotkeyConfig;
    private bool _isDownloading;

    public SettingsForm(ConfigurationManager configManager, DebugService debugService)
    {
        _configManager = configManager;
        _debugService = debugService;

        // Get models directory (relative to executable)
        var modelsDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "models");
        if (!Directory.Exists(modelsDir))
        {
            modelsDir = Path.Combine(AppContext.BaseDirectory, "models");
        }
        _modelDownloader = new ModelDownloader(modelsDir);

        // Configure form
        Text = "Voxify — Настройки";
        Size = new Size(550, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        // Create controls
        _hotkeyModeGroupBox = CreateHotkeyModeGroupBox();
        _hotkeyCombinationGroupBox = CreateHotkeyCombinationGroupBox();
        _modelGroupBox = CreateModelGroupBox();
        _notificationsGroupBox = CreateNotificationsGroupBox();
        _showLogsButton = CreateShowLogsButton();
        _saveButton = CreateSaveButton();
        _cancelButton = CreateCancelButton();

        // Add controls to form
        Controls.Add(_hotkeyModeGroupBox);
        Controls.Add(_hotkeyCombinationGroupBox);
        Controls.Add(_modelGroupBox);
        Controls.Add(_notificationsGroupBox);
        Controls.Add(_showLogsButton);
        Controls.Add(_saveButton);
        Controls.Add(_cancelButton);

        // Load current settings
        LoadSettings();

        // Subscribe to download events
        _modelDownloader.ProgressChanged += OnDownloadProgressChanged;
        _modelDownloader.DownloadCompleted += OnDownloadCompleted;

        // Handle form closing
        FormClosing += SettingsForm_FormClosing;
    }

    private GroupBox CreateHotkeyModeGroupBox()
    {
        var groupBox = new GroupBox
        {
            Text = "Режим работы",
            Location = new Point(10, 10),
            Size = new Size(510, 80),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Push-to-Talk toggle (off = Toggle mode, on = Push-to-Talk mode)
        _pushToTalkLabel = new Label
        {
            Text = "Push-to-Talk",
            Location = new Point(10, 30),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9F, FontStyle.Regular)
        };

        _pushToTalkSwitch = new ToggleSwitch
        {
            Location = new Point(115, 27),
            Checked = false
        };
        _pushToTalkSwitch.CheckedChanged += OnPushToTalkModeChanged;

        var pushToTalkHint = new Label
        {
            Text = "(удерживать для записи)",
            Location = new Point(175, 30),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        var toggleHint = new Label
        {
            Text = "Выключено: Toggle (нажать для старта/стопа)",
            Location = new Point(10, 55),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 7F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange([
            _pushToTalkLabel, _pushToTalkSwitch, pushToTalkHint, toggleHint
        ]);

        return groupBox;
    }

    private GroupBox CreateHotkeyCombinationGroupBox()
    {
        var groupBox = new GroupBox
        {
            Text = "Комбинация клавиш",
            Location = new Point(10, 100),
            Size = new Size(510, 90),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _hotkeyInputTextBox = new HotkeyInputTextBox
        {
            Location = new Point(10, 25),
            Size = new Size(480, 23),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        _hotkeyInputTextBox.HotkeyChanged += OnHotkeyChanged;

        var hotkeyHint = new Label
        {
            Text = "Нажмите комбинацию в поле выше (например, Ctrl + Z)",
            Location = new Point(10, 58),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 7F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange([_hotkeyInputTextBox, hotkeyHint]);

        return groupBox;
    }

    private GroupBox CreateModelGroupBox()
    {
        var groupBox = new GroupBox
        {
            Text = "Модель распознавания",
            Location = new Point(10, 200),
            Size = new Size(510, 210),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Provider selection
        var providerLabel = new Label
        {
            Text = "Провайдер:",
            Location = new Point(10, 25),
            AutoSize = true
        };

        _providerComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(10, 45),
            Width = 200
        };
        _providerComboBox.Items.AddRange(["Vosk", "Whisper"]);
        _providerComboBox.SelectedIndexChanged += OnProviderChanged;

        // Model selection
        var modelLabel = new Label
        {
            Text = "Модель:",
            Location = new Point(230, 25),
            AutoSize = true
        };

        _modelComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(230, 45),
            Width = 260
        };
        _modelComboBox.SelectedIndexChanged += OnModelSelected;

        // Model path
        var pathLabel = new Label
        {
            Text = "Путь к модели:",
            Location = new Point(10, 75),
            AutoSize = true
        };

        _modelPathTextBox = new TextBox
        {
            Location = new Point(10, 95),
            Width = 420,
            ReadOnly = true
        };

        _browseButton = new Button
        {
            Text = "Обзор...",
            Location = new Point(440, 93),
            AutoSize = true
        };
        _browseButton.Click += OnBrowseClick;

        // Download button
        _downloadButton = new Button
        {
            Text = "Скачать модель ▼",
            Location = new Point(10, 130),
            AutoSize = true
        };
        _downloadButton.Click += OnDownloadButtonClick;

        // Download progress panel
        _downloadPanel = new Panel
        {
            Location = new Point(10, 160),
            Size = new Size(480, 50),
            Visible = false
        };

        _downloadProgressBar = new ProgressBar
        {
            Location = new Point(0, 0),
            Size = new Size(480, 23),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _downloadStatusLabel = new Label
        {
            Location = new Point(0, 28),
            Size = new Size(480, 20),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 8F, FontStyle.Regular)
        };

        _downloadPanel.Controls.AddRange([_downloadProgressBar, _downloadStatusLabel]);

        groupBox.Controls.AddRange([
            providerLabel, _providerComboBox,
            modelLabel, _modelComboBox,
            pathLabel, _modelPathTextBox, _browseButton,
            _downloadButton, _downloadPanel
        ]);

        return groupBox;
    }

    private GroupBox CreateNotificationsGroupBox()
    {
        var groupBox = new GroupBox
        {
            Text = "Уведомления",
            Location = new Point(10, 420),
            Size = new Size(510, 80),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _showNotificationsCheckBox = new CheckBox
        {
            Text = "Показывать уведомления (кроме критичных ошибок)",
            Location = new Point(10, 25),
            AutoSize = true,
            Checked = true
        };

        var hintLabel = new Label
        {
            Text = "Критичные ошибки (Error) показываются всегда",
            Location = new Point(25, 50),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 8F, FontStyle.Italic),
            ForeColor = Color.Gray
        };

        groupBox.Controls.AddRange([_showNotificationsCheckBox, hintLabel]);

        return groupBox;
    }

    private Button CreateShowLogsButton()
    {
        return new Button
        {
            Text = "Показать логи",
            Location = new Point(10, 520),
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
    }

    private Button CreateSaveButton()
    {
        return new Button
        {
            Text = "Сохранить",
            Location = new Point(300, 520),
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
    }

    private Button CreateCancelButton()
    {
        return new Button
        {
            Text = "Отмена",
            Location = new Point(420, 520),
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            DialogResult = DialogResult.Cancel
        };
    }

    private void LoadSettings()
    {
        var settings = _configManager.Settings;

        // Load hotkey mode (Push-to-Talk toggle: on = PushToTalk, off = Toggle)
        _pushToTalkSwitch.Checked = settings.Hotkey.Mode.ToLower() == "pushtotalk";

        // Load hotkey combination
        _hotkeyInputTextBox.SetHotkeyConfig(settings.Hotkey);
        _modifiedHotkeyConfig = new HotkeyConfig
        {
            Mode = settings.Hotkey.Mode,
            Modifiers = settings.Hotkey.Modifiers,
            Key = settings.Hotkey.Key
        };

        // Load notification settings
        _showNotificationsCheckBox.Checked = settings.Ui.ShowNotifications;

        // Load model settings
        _providerComboBox.SelectedItem = settings.SpeechRecognition.Provider;
        _modelPathTextBox.Text = settings.SpeechRecognition.ModelPath;

        // Populate model combo box based on provider
        PopulateModelComboBox(settings.SpeechRecognition.Provider);

        // Select current model if available
        if (!string.IsNullOrEmpty(settings.SpeechRecognition.ModelPath))
        {
            var modelName = Path.GetFileName(settings.SpeechRecognition.ModelPath);
            for (int i = 0; i < _modelComboBox.Items.Count; i++)
            {
                if (_modelComboBox.Items[i] is ModelInfo info &&
                    (info.Id == modelName || info.Name.Contains(modelName)))
                {
                    _modelComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void PopulateModelComboBox(string provider)
    {
        _modelComboBox.Items.Clear();

        if (provider.ToLower() == "vosk")
        {
            // Add downloadable Vosk models
            foreach (var model in ModelDownloader.AvailableModels.Where(m => m.Provider == "Vosk"))
            {
                _modelComboBox.Items.Add(model);
            }

            // Add installed Vosk models
            var installedModels = _modelDownloader.GetInstalledModels()
                .Where(m => m.Provider == "Vosk" && !ModelDownloader.AvailableModels.Any(am => am.Id == m.Id));
            foreach (var model in installedModels)
            {
                _modelComboBox.Items.Add(model);
            }
        }
        else if (provider.ToLower() == "whisper")
        {
            // Add Whisper models (placeholder - would need actual Whisper model list)
            _modelComboBox.Items.Add(new ModelInfo
            {
                Id = "whisper-tiny",
                Name = "Whisper Tiny",
                Provider = "Whisper",
                Language = "Multi",
                Description = "Малая модель Whisper (39 МБ)"
            });
            _modelComboBox.Items.Add(new ModelInfo
            {
                Id = "whisper-base",
                Name = "Whisper Base",
                Provider = "Whisper",
                Language = "Multi",
                Description = "Базовая модель Whisper (74 МБ)"
            });
        }

        if (_modelComboBox.Items.Count > 0)
        {
            _modelComboBox.SelectedIndex = 0;
        }
    }

    private void OnHotkeyChanged(object? sender, HotkeyConfig e)
    {
        _modifiedHotkeyConfig = e;
    }

    private void OnPushToTalkModeChanged(object? sender, EventArgs e)
    {
        // Single toggle: checked = PushToTalk, unchecked = Toggle
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        var provider = _providerComboBox.SelectedItem?.ToString() ?? "Vosk";
        PopulateModelComboBox(provider);
    }

    private void OnModelSelected(object? sender, EventArgs e)
    {
        if (_modelComboBox.SelectedItem is ModelInfo model)
        {
            _modelPathTextBox.Text = Path.Combine(
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "models")),
                model.Id
            );
        }
    }

    private void OnBrowseClick(object? sender, EventArgs e)
    {
        using var folderDialog = new FolderBrowserDialog
        {
            Description = "Выберите папку с моделью",
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrEmpty(_modelPathTextBox.Text) && Directory.Exists(_modelPathTextBox.Text))
        {
            folderDialog.SelectedPath = _modelPathTextBox.Text;
        }

        if (folderDialog.ShowDialog() == DialogResult.OK)
        {
            _modelPathTextBox.Text = folderDialog.SelectedPath;
        }
    }

    private async void OnDownloadButtonClick(object? sender, EventArgs e)
    {
        if (_isDownloading || _modelComboBox.SelectedItem is not ModelInfo model)
            return;

        if (model.Provider != "Vosk")
        {
            MessageBox.Show(
                "Скачивание доступно только для моделей Vosk.\n\nДля Whisper модели используйте официальный сайт.",
                "Voxify",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _isDownloading = true;
        _downloadButton.Enabled = false;
        _downloadPanel.Visible = true;
        _downloadProgressBar.Value = 0;
        _downloadStatusLabel.Text = "Подготовка к загрузке...";

        try
        {
            await _modelDownloader.DownloadModelAsync(model);
        }
        catch (Exception ex)
        {
            _downloadStatusLabel.Text = $"Ошибка: {ex.Message}";
            _downloadStatusLabel.ForeColor = Color.Red;
            _isDownloading = false;
            _downloadButton.Enabled = true;
        }
    }

    private void OnDownloadProgressChanged(object? sender, DownloadProgressEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnDownloadProgressChanged(sender, e));
            return;
        }

        _downloadProgressBar.Value = e.ProgressPercentage;
        _downloadStatusLabel.Text = e.StatusMessage;
    }

    private void OnDownloadCompleted(object? sender, DownloadCompleteEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnDownloadCompleted(sender, e));
            return;
        }

        _isDownloading = false;
        _downloadButton.Enabled = true;

        if (e.Success)
        {
            _downloadStatusLabel.Text = "Модель успешно загружена!";
            _downloadStatusLabel.ForeColor = Color.Green;
            _modelPathTextBox.Text = e.ModelPath;

            // Refresh model list
            var provider = _providerComboBox.SelectedItem?.ToString() ?? "Vosk";
            PopulateModelComboBox(provider);

            MessageBox.Show(
                $"Модель успешно загружена и установлена!\n\nПуть: {e.ModelPath}",
                "Voxify",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            _downloadStatusLabel.Text = $"Ошибка: {e.ErrorMessage}";
            _downloadStatusLabel.ForeColor = Color.Red;

            MessageBox.Show(
                $"Ошибка при загрузке модели: {e.ErrorMessage}",
                "Voxify — Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void SaveSettings()
    {
        try
        {
            _configManager.UpdateSettings(settings =>
            {
                // Update hotkey mode
                settings.Hotkey.Mode = _pushToTalkSwitch.Checked ? "PushToTalk" : "Toggle";

                // Update hotkey combination
                if (_modifiedHotkeyConfig != null)
                {
                    settings.Hotkey.Modifiers = _modifiedHotkeyConfig.Modifiers;
                    settings.Hotkey.Key = _modifiedHotkeyConfig.Key;
                }

                // Update notification settings
                settings.Ui.ShowNotifications = _showNotificationsCheckBox.Checked;

                // Update model settings
                settings.SpeechRecognition.Provider = _providerComboBox.SelectedItem?.ToString() ?? "Vosk";
                settings.SpeechRecognition.ModelPath = _modelPathTextBox.Text;

                // Set language based on model
                if (_modelComboBox.SelectedItem is ModelInfo model)
                {
                    settings.SpeechRecognition.Language = model.Language;
                }
            });

            _debugService.Log("Settings", "Settings saved successfully", LogLevel.Info);

            MessageBox.Show(
                "Настройки сохранены!",
                "Voxify",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            _debugService.Log("Settings", $"Failed to save settings: {ex.Message}", LogLevel.Error);

            MessageBox.Show(
                $"Ошибка при сохранении настроек: {ex.Message}",
                "Voxify — Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _modelDownloader.ProgressChanged -= OnDownloadProgressChanged;
        _modelDownloader.DownloadCompleted -= OnDownloadCompleted;
    }

    /// <summary>
    /// Shows the settings dialog and handles button clicks.
    /// </summary>
    public new DialogResult ShowDialog()
    {
        // Wire up button click events
        _saveButton.Click += (s, e) => SaveSettings();
        _showLogsButton.Click += (s, e) => ShowLogs();

        return base.ShowDialog();
    }

    private void ShowLogs()
    {
        var debugWindow = new DebugWindow(_debugService);
        debugWindow.Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _modelDownloader?.Dispose();
        }
        base.Dispose(disposing);
    }
}
