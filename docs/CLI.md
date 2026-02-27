# Voxify CLI — Command Line Guide

## Overview

Voxify supports command-line control. You can send commands to a running instance from any terminal.

## Quick Start

```bash
# Start the application
Voxify.exe

# In another terminal — control the application
voxify --toggle
voxify --status
voxify --debug
```

## Available Commands

### `--toggle` / `--toggle-transcription`

Starts or stops voice recording.

```bash
voxify --toggle
```

**Equivalent:** Pressing the hotkey (default `Ctrl+F12` in Push-to-Talk mode).

---

### `--cancel`

Cancels the current recording operation.

```bash
voxify --cancel
```

**Usage:** If recording is stuck or you want to interrupt the process.

---

### `--status`

Returns the current application status.

```bash
voxify --status
```

**Possible values:**
- `Idle` — waiting
- `Recording` — recording audio
- `Processing` — speech recognition

---

### `--debug`

Enables/disables debug mode and opens the debug window.

```bash
voxify --debug
```

**What the debug window shows:**
- Real-time microphone audio level
- VAD state (speech detector)
- Recognized text (raw and processed)
- Recording status
- Event logs

---

### `--start-hidden`

Starts the application in background mode (without showing UI).

```bash
voxify --start-hidden
```

**Usage:** For auto-start with Windows.

---

## Usage Examples

### Automation with Scripts

**PowerShell — scheduled recording:**
```powershell
# Recording at 9:00 AM
Start-ScheduledTask -TaskName "VoxifyRecord"
```

**Bash (Git Bash on Windows):**
```bash
# Cycle: record → pause → record
for i in {1..5}; do
    voxify --toggle
    sleep 30
    voxify --toggle
    sleep 5
done
```

### Integration with Other Applications

**AutoHotkey script:**
```autohotkey
; Hotkey F1 for recording via Voxify
F1::
    Run, voxify --toggle
return
```

---

## Technical Details

### IPC (Inter-Process Communication)

Voxify uses **Named Pipes** for communication between CLI and the main application.

- **Pipe name:** `VoxifyIpcPipe`
- **Protocol:** JSON
- **Command format:**
  ```json
  {
    "Type": "toggle",
    "Parameters": {}
  }
  ```

### Return Values

CLI returns exit code:
- `0` — success
- `1` — error (application not running, invalid command)

---

## Troubleshooting

### "CLI cannot connect to the application"

**Cause:** The main Voxify application is not running.

**Solution:**
```bash
# Start the application
Voxify.exe

# Then try again
voxify --status
```

### "Access denied"

**Cause:** Access rights conflict (e.g., application running as administrator, CLI as regular user).

**Solution:** Run CLI as the same user or with administrator rights.

---

## See Also

- [DEBUG.md](DEBUG.md) — Debug Mode
- [README.md](../README.md) — Main Guide
