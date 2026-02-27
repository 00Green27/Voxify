# Changelog

All notable changes to Voxify.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)

---

## [0.2.0] — 2026-02-27

### Added

#### Phase 5: Debug Mode
- **Debug Window** with real-time logs (`Ctrl+Shift+D`)
- **File Logging** (`%APPDATA%\Voxify\logs\voxify.log`)
- **State Monitoring**:
  - Microphone audio level
  - VAD state (speech/silence)
  - Recognized text (raw and processed)
  - Recording status (Idle/Recording/Processing)
- **CLI Command** `--debug` for enabling debug mode

#### Phase 6: System Integration
- **Single Instance** — only one instance of the application
- **Auto-Start** via Windows Registry
- **System Events Handling**:
  - `SessionEnding` — graceful shutdown on Windows exit
  - `DisplaySettingsChanged` — adapt to display changes
- **Argument Forwarding** between instances via WM_COPYDATA

### Changed

#### Improved Error Handling
- Microphone availability check before recording
- Graceful cleanup on recording errors
- Informative error messages in notifications

#### Configuration
- Added `Debug` section in `appsettings.json`
- Added `SystemIntegration` section in `appsettings.json`
- Environment variable support in paths (e.g., `%APPDATA%`)

### Documentation

- **docs/CLI.md** — command-line interface guide
- **docs/MODELS.md** — speech model comparison
- **docs/DEBUG.md** — debugging guide
- **README.md** — updated with new features

### Technical Changes

- New `DebugService` class for centralized logging
- New `SingleInstanceManager` class for instance management
- New `SystemIntegration` class for system integration
- Updated `Program.cs` for single instance check
- Updated `MainForm.cs` with system event handlers

---

## [0.1.5] — 2026-02-27

### Added

#### Phase 4: Multiple Models Support
- **Whisper** — neural network recognition support
- **RecognizerFactory** — factory for creating recognizers
- **Provider Selection** in settings (`Vosk` or `Whisper`)
- **Whisper Model Configuration** (tiny, base, small, medium, large)

### Changed

- Updated `ISpeechRecognizer` interface for Whisper support
- Added `WhisperRecognizer.cs`
- Configuration unified in `SpeechRecognition` section

---

## [0.1.4] — 2026-02-27

### Added

#### Phase 3: CLI Control
- **Voxify.Cli** — separate CLI project
- **IPC Server** on Named Pipes for communication
- **CLI Commands**:
  - `--toggle` / `--toggle-transcription` — toggle recording
  - `--cancel` — cancel current operation
  - `--status` — application status
  - `--debug` — debug mode

### Changed

- Updated `IpcServer.cs` with asynchronous command handling
- Added `IpcClient.cs` for CLI

---

## [0.1.3] — 2026-02-27

### Added

#### Phase 2: Push-to-Talk Mode
- **Push-to-Talk Mode** — record while holding key
- **Toggle Mode** — classic start/stop on press
- **Mode Configuration** in `appsettings.json` (`Mode: "PushToTalk" | "Toggle"`)
- **HotkeyManager** with both modes support
- **RecordingState** enum (Idle, Recording, Processing)

### Changed

- Updated `HotkeyManager.cs` with keyboard hook for Push-to-Talk
- Updated `HotkeyConfig.cs` with `Mode` field
- Integration in `MainForm.cs` with `PushToTalkKeyDown` / `PushToTalkKeyUp` handlers

---

## [0.1.2] — 2026-02-27

### Added

#### Phase 1: ML-based VAD (Silero-like)
- **IVadEngine** — interface for VAD engine
- **SileroVadEngine** — implementation via ONNX/Silero
- **VoiceActivityDetection** section in settings
- **AudioRecorder** with VAD support
- **VAD Modes**: `simple` (volume threshold) and `silero` (ML model)

### Changed

- Updated `AudioRecorder.cs` with VAD handling
- Added `SpeechDetected` / `SpeechEnded` events
- VAD settings configuration in `appsettings.json`

---

## [0.1.1] — 2026-02-26

### Added

- **Modular Architecture** (ADR-0001)
- **VoskEngine** — speech recognition via Vosk
- **HotkeyManager** — global hotkeys
- **AudioRecorder** — microphone audio capture
- **TextInputInjector** — text input to applications
- **IpcServer** — IPC for future CLI
- **appsettings.json** — application configuration

### Changed

- Updated `MainForm.cs` with full component integration
- Updated `Program.cs` as entry point

---

## [0.1.0] — 2026-02-26

### Added

- **First MVP Release**
- Basic speech recognition via Vosk
- Hotkey-based recording
- Text input to applications
- System tray icon

---

## Links

- [Project Repository](https://github.com/yourusername/voxify)
- [Development Plan](docs/plans/2026-02-26-voxify-mvp.md)
- [Handy-like Features Plan](docs/plans/2026-02-27-handy-features.md)
