# Voxify

Voice text input for Windows using Vosk — offline speech recognition with input to any application.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Status](https://img.shields.io/badge/status-MVP-orange)

## Features

- 🔒 **Offline Recognition** — works without internet via Vosk
- ⌨️ **Push-to-Talk** — recording by hotkey press (configurable)
- 🎤 **Microphone Support** — audio capture via NAudio
- 📝 **Input to Any Application** — text insertion via keyboard emulation
- 🛠️ **Minimalistic UI** — system tray icon
- 🌐 **Multilingual** — Russian and English support
- 🧠 **Smart VAD** — Silero ML-based Voice Activity Detection (optional)
- 🔇 **Silence Filtering** — auto-detect speech start/end, skip silence

## Requirements

- **OS**: Windows 10/11
- **.NET**: .NET 10.0 SDK
- **Microphone**: Any recording device
- **Vosk Model**: ~50 MB per language (small model)

## Quick Start

### 1. Download Vosk Model

**Recommended for start** (small models ~50 MB):

| Language | Model | Link |
|----------|-------|------|
| 🇷🇺 Russian | vosk-model-small-ru-0.22 | [Download](https://alphacephei.com/vosk/models/vosk-model-small-ru-0.22.zip) |
| 🇬🇧 English | vosk-model-small-en-us-0.15 | [Download](https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip) |

**Instructions**:
1. Download the model ZIP archive
2. Extract to a folder, e.g.: `C:\Voxify\Models\vosk-model-small-ru-0.22`
3. See more: [models/README.md](models/README.md)

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
# copy .\publish\* "C:\Program Files\Voxify\"
```

## Usage

1. **Start**: Launch the application — icon will appear in system tray
2. **Configure**: On first launch, specify model path in settings
3. **Record**: Press hotkey (default `Ctrl+F12`) → speak → release
4. **Result**: Recognized text will be automatically inserted into active field

### Hotkeys (default)

| Action | Keys |
|--------|------|
| Start/stop recording | `Ctrl + F12` |

## Configuration

Settings file: `%APPDATA%\Voxify\appsettings.json`

```json
{
  "ModelPath": "C:\\Voxify\\Models\\vosk-model-small-ru-0.22",
  "Language": "ru-RU",
  "Hotkey": {
    "Modifiers": ["Control"],
    "Key": "F12"
  },
  "VoiceActivityDetection": {
    "SilenceThreshold": 0.05,
    "MinSpeechDurationMs": 500
  },
  "TextInput": {
    "TypeDelayMs": 10,
    "PasteAsClipboard": false
  }
}
```

### Settings Description

| Parameter | Description | Default |
|-----------|-------------|---------|
| `ModelPath` | Path to Vosk model folder | `""` |
| `Language` | Model language (ru-RU, en-US) | `"ru-RU"` |
| `Hotkey.Modifiers` | Modifiers (Control, Alt, Shift, Win) | `["Control"]` |
| `Hotkey.Key` | Key (F1-F12, A-Z, 0-9) | `"F12"` |
| `Hotkey.Mode` | Hotkey mode: `Toggle` or `PushToTalk` | `"Toggle"` |
| `VoiceActivityDetection.Enabled` | Enable VAD (Voice Activity Detection) | `false` |
| `VoiceActivityDetection.Mode` | VAD mode: `simple` or `silero` | `"simple"` |
| `VoiceActivityDetection.SilenceThreshold` | Volume threshold (0.0-1.0) | `0.05` |
| `VoiceActivityDetection.MinSpeechDurationMs` | Min speech duration before recording starts (ms) | `500` |
| `VoiceActivityDetection.MinSilenceDurationMs` | Min silence duration before recording stops (ms) | `500` |
| `VoiceActivityDetection.SileroModelPath` | Path to Silero VAD ONNX model | `models\vad\silero_vad.onnx` |
| `VoiceActivityDetection.SileroConfigPath` | Path to Silero VAD YAML config | `models\vad\vad.yaml` |
| `TextInput.TypeDelayMs` | Delay between characters (ms) | `10` |
| `TextInput.PasteAsClipboard` | Paste via clipboard | `false` |

## Architecture

The project uses **modular architecture** with separation into:

- **Core** — speech recognition, hotkeys, text input (no UI dependencies)
- **UI** — system tray, context menu (WinForms NotifyIcon)
- **Config** — settings management (JSON)

See [ADR-0001](docs/decisions/ADR-0001-modular-architecture.md) for details.

## Technologies

| Component     | Technology           | Version |
| ------------- | -------------------- | ------- |
| Framework     | .NET                 | 10.0    |
| Language      | C#                   | 13      |
| Recognition   | Vosk                 | 0.3.38  |
| Audio         | NAudio               | 2.2.1   |
| Text Input    | InputSimulator       | 1.0.4   |
| UI            | System.Windows.Forms | built-in |
| Configuration | System.Text.Json     | built-in |

## Known Limitations

- ⚠️ **Windows Only** — uses WinForms and Windows API
- ⚠️ **Doesn't Work in Games** — anti-cheats may block keyboard emulation
- ⚠️ **Recognition Accuracy** — depends on model (small ~77-90%, large ~94-95%)
- ⚠️ **InputSimulator Warning** — package built for .NET Framework, but works correctly

## Development Plan

See [Implementation Plan](docs/plans/2026-02-26-voxify-mvp.md) for details.

### Future Improvements

- [ ] Settings GUI window
- [ ] Support for multiple languages simultaneously
- [ ] Recognized text history
- [ ] Macros for frequent phrases
- [ ] Auto-correction of common errors
- [ ] Status indication (icon color)

## License

MIT License — see [LICENSE](LICENSE) file.

## Links

- [Vosk Documentation](https://alphacephei.com/vosk/)
- [Vosk Models](https://alphacephei.com/vosk/models)
- [NAudio GitHub](https://github.com/NAudio/NAudio)
- [InputSimulator GitHub](https://github.com/michaelnoonan/inputsimulator)

## Contributing

Issues and pull requests are welcome!

---

**Voxify** — voice input for everyone. 🎤
