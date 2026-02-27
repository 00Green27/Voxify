# Voxify

Voice text input for Windows using Vosk/Whisper — offline speech recognition with input to any application.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Status](https://img.shields.io/badge/status-Production--Ready-green)

## Features

- 🔒 **Offline Recognition** — works without internet via Vosk or Whisper
- ⌨️ **Push-to-Talk** — recording by hotkey press (configurable)
- 🎤 **Microphone Support** — audio capture via NAudio
- 📝 **Input to Any Application** — text insertion via keyboard emulation
- 🛠️ **Minimalistic UI** — system tray icon
- 🌐 **Multilingual** — Russian and English support
- 🧠 **Smart VAD** — Silero ML-based Voice Activity Detection (optional)
- 🔇 **Silence Filtering** — auto-detect speech start/end, skip silence
- 💻 **CLI Control** — manage Voxify from command line
- 🐞 **Debug Mode** — real-time logs and state monitoring
- 🔐 **Single Instance** — only one instance runs at a time
- ⚡ **Auto-Start** — launch on Windows login (optional)

## Requirements

- **OS**: Windows 10/11
- **.NET**: .NET 10.0 SDK
- **Microphone**: Any recording device
- **Vosk Model**: ~50 MB per language (small model)
- **Whisper Model** (optional): 39 MB - 3 GB

## Quick Start

### 1. Download Model

**Vosk (recommended for start)**:

| Language | Model | Link |
|----------|-------|------|
| 🇷🇺 Russian | vosk-model-small-ru-0.22 | [Download](https://alphacephei.com/vosk/models/vosk-model-small-ru-0.22.zip) |
| 🇬🇧 English | vosk-model-small-en-us-0.15 | [Download](https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip) |

**Instructions**:
1. Download the model ZIP archive
2. Extract to a folder, e.g.: `C:\Voxify\Models\vosk-model-small-ru-0.22`

### 2. Build the Application

```bash
# Clone repository
git clone <repository-url>
cd Voxify

# Restore packages
dotnet restore

# Build
dotnet build -c Release

# Run
dotnet run --project src/Voxify.Host/Voxify.Host.csproj
```

### 3. Publish (optional)

```bash
# Create executable
dotnet publish -c Release -o ./publish

# Copy to installation folder
xcopy .\publish\* "C:\Program Files\Voxify\" /E /Y
```

## Usage

1. **Start**: Launch the application — icon will appear in system tray
2. **Configure**: Model path is configured in `appsettings.json`
3. **Record**: Press hotkey (default `Ctrl+F12` in Push-to-Talk mode) → speak → release
4. **Result**: Recognized text will be automatically inserted into active field

### CLI Control

Manage Voxify from command line:

```bash
# Get status
voxify --status

# Toggle recording
voxify --toggle

# Cancel current recording
voxify --cancel

# Toggle debug mode
voxify --debug

# Show help
voxify --help
```

See [CLI Documentation](docs/CLI.md) for details.

### Hotkeys (default)

| Action | Keys |
|--------|------|
| Push-to-Talk recording | `Ctrl + F12` (hold) |
| Open Debug Window | `Ctrl + Shift + D` |

## Configuration

Settings file: `%APPDATA%\Voxify\appsettings.json`

```json
{
  "SpeechRecognition": {
    "Provider": "Vosk",
    "ModelPath": "C:\\Voxify\\Models\\vosk-model-small-ru-0.22",
    "Language": "ru-RU",
    "WhisperModel": "tiny"
  },
  "Hotkey": {
    "Mode": "PushToTalk",
    "Modifiers": ["Control"],
    "Key": "F12"
  },
  "VoiceActivityDetection": {
    "Enabled": false,
    "Mode": "simple",
    "SilenceThreshold": 0.05,
    "MinSpeechDurationMs": 500,
    "MinSilenceDurationMs": 500
  },
  "TextInput": {
    "TypeDelayMs": 10,
    "PasteAsClipboard": false
  },
  "Debug": {
    "Enabled": false,
    "Hotkey": {
      "Modifiers": ["Control", "Shift"],
      "Key": "D"
    },
    "LogToFile": true,
    "LogPath": "%APPDATA%\\Voxify\\logs\\voxify.log",
    "MaxLogLines": 1000
  },
  "SystemIntegration": {
    "SingleInstance": true,
    "AutoStart": false,
    "StartHidden": false
  }
}
```

### Settings Description

| Section | Parameter | Description | Default |
|---------|-----------|-------------|---------|
| **SpeechRecognition** | `Provider` | Speech provider: `Vosk` or `Whisper` | `"Vosk"` |
| | `ModelPath` | Path to model folder (Vosk) or file (Whisper) | `""` |
| | `Language` | Model language (ru-RU, en-US, ru, en) | `"ru-RU"` |
| | `WhisperModel` | Whisper model: tiny, base, small, medium, large | `"tiny"` |
| **Hotkey** | `Mode` | Hotkey mode: `Toggle` or `PushToTalk` | `"Toggle"` |
| | `Modifiers` | Modifiers: Control, Alt, Shift, Win | `["Control"]` |
| | `Key` | Key: F1-F12, A-Z, 0-9 | `"F12"` |
| **VoiceActivityDetection** | `Enabled` | Enable VAD | `false` |
| | `Mode` | VAD mode: `simple` or `silero` | `"simple"` |
| | `SilenceThreshold` | Volume threshold (0.0-1.0) | `0.05` |
| | `MinSpeechDurationMs` | Min speech duration (ms) | `500` |
| | `MinSilenceDurationMs` | Min silence before stop (ms) | `500` |
| **TextInput** | `TypeDelayMs` | Delay between characters (ms) | `10` |
| | `PasteAsClipboard` | Paste via clipboard | `false` |
| **Debug** | `Enabled` | Enable debug mode | `false` |
| | `LogToFile` | Enable file logging | `true` |
| | `LogPath` | Path to log file | `%APPDATA%\Voxify\logs\voxify.log` |
| **SystemIntegration** | `SingleInstance` | Only one instance allowed | `true` |
| | `AutoStart` | Start on Windows login | `false` |
| | `StartHidden` | Start minimized | `false` |

## Models

Voxify supports two speech recognition providers:

| Provider | Model | Size | Accuracy | Speed | GPU | Best For |
|----------|-------|------|----------|-------|-----|----------|
| **Vosk** | small-ru | 45 MB | ~85% | ⚡ Fast | ❌ | Fast dictation |
| **Whisper** | tiny | 39 MB | ~80% | 🐌 Medium | ✅ | Low-end PCs |
| **Whisper** | base | 74 MB | ~85% | 🐌 Medium | ✅ | Balance |
| **Whisper** | small | 244 MB | ~90% | 🐢 Slow | ✅ | High accuracy |
| **Whisper** | medium | 769 MB | ~95% | 🐢🐢 Very Slow | ✅ | Professional |

See [Models Guide](docs/MODELS.md) for detailed comparison and download links.

### Quick Setup

**Vosk (recommended for start)**:
```json
{
  "SpeechRecognition": {
    "Provider": "Vosk",
    "ModelPath": "C:\\Voxify\\Models\\vosk-model-small-ru-0.22",
    "Language": "ru-RU"
  }
}
```

**Whisper (high accuracy)**:
```json
{
  "SpeechRecognition": {
    "Provider": "Whisper",
    "ModelPath": "C:\\Voxify\\Models\\ggml-whisper-base.bin",
    "Language": "ru"
  }
}
```

## Debug Mode

Press `Ctrl + Shift + D` to open the Debug Window:

- 📊 Real-time audio level monitoring
- 🎙️ VAD state visualization
- 📝 Raw and processed text display
- 📋 Event log with timestamps

Logs are saved to `%APPDATA%\Voxify\logs\voxify.log`

See [Debug Guide](docs/DEBUG.md) for troubleshooting.

## Architecture

The project uses **modular architecture** with separation into:

- **Core** — speech recognition, hotkeys, text input (no UI dependencies)
- **UI** — system tray, context menu (WinForms NotifyIcon)
- **Config** — settings management (JSON)
- **CLI** — command-line interface

See [ADR-0001](docs/decisions/ADR-0001-modular-architecture.md) for details.

## Technologies

| Component     | Technology           | Version |
| ------------- | -------------------- | ------- |
| Framework     | .NET                 | 10.0    |
| Language      | C#                   | 13      |
| Recognition   | Vosk / Whisper       | 0.3.38 / 1.7.3 |
| Audio         | NAudio               | 2.2.1   |
| VAD           | Silero / Simple      | 1.1.2   |
| Text Input    | InputSimulator       | 1.0.4   |
| UI            | System.Windows.Forms | built-in |
| Configuration | System.Text.Json     | built-in |
| IPC           | Named Pipes          | built-in |

## Known Limitations

- ⚠️ **Windows Only** — uses WinForms and Windows API
- ⚠️ **Doesn't Work in Games** — anti-cheats may block keyboard emulation
- ⚠️ **Recognition Accuracy** — depends on model (small ~77-90%, large ~94-95%)
- ⚠️ **InputSimulator Warning** — package built for .NET Framework, but works correctly

## Documentation

| Document | Description |
|----------|-------------|
| [CLI Guide](docs/CLI.md) | Command-line interface reference |
| [Models Guide](docs/MODELS.md) | Speech model comparison and setup |
| [Debug Guide](docs/DEBUG.md) | Troubleshooting and debugging |
| [Implementation Plan](docs/plans/2026-02-26-voxify-mvp.md) | Development roadmap |

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

## License

MIT License — see [LICENSE](LICENSE) file.

## Links

- [Vosk Documentation](https://alphacephei.com/vosk/)
- [Vosk Models](https://alphacephei.com/vosk/models)
- [Whisper.net](https://github.com/sandrohanea/whisper.net)
- [NAudio GitHub](https://github.com/NAudio/NAudio)
- [InputSimulator GitHub](https://github.com/michaelnoonan/inputsimulator)

## Contributing

Issues and pull requests are welcome!

---

**Voxify** — voice input for everyone. 🎤
