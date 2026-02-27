# Voxify — Debug Guide

## Overview

Voxify includes built-in debugging tools for diagnosing issues with speech recognition, audio, and system integration.

## Debug Window

### Opening

**Hotkey:** `Ctrl + Shift + D`

**Via CLI:**
```bash
voxify --debug
```

### What the Debug Window Shows

```
┌─────────────────────────────────────────────────────────┐
│ Voxify Debug Console                          [─][□][X] │
├─────────────────────────────────────────────────────────┤
│ Recording State: Recording                              │
│ Audio Level: 0.45                                       │
│ VAD State: Active (0.87)                                │
│ Provider: Vosk                                          │
├─────────────────────────────────────────────────────────┤
│ [12:34:56.123] Debug: Debug mode enabled                │
│ [12:34:57.456] MainForm: Recording started              │
│ [12:34:58.789] AudioRecorder: Speech detected           │
│ [12:35:01.012] Recognition: Raw text: "hello"          │
│ [12:35:02.345] Recognition: Processed: "Hello!"         │
│ [12:35:02.567] MainForm: Text injected successfully     │
└─────────────────────────────────────────────────────────┘
```

### State Fields

| Field | Description |
|-------|-------------|
| **Recording State** | Current state: `Idle`, `Recording`, `Processing` |
| **Audio Level** | Microphone volume level (0.0 - 1.0) |
| **VAD State** | Speech detector state: `Active`/`Inactive` + confidence |
| **Provider** | Current recognition provider: `Vosk` or `Whisper` |

### Log Levels

| Level | Description | Color |
|-------|-------------|-------|
| `Info` | General information | White |
| `Debug` | Debug details | Gray |
| `Warning` | Warnings | Yellow |
| `Error` | Errors | Red |

---

## File Logging

### Enabling

In `appsettings.json`:
```json
{
  "Debug": {
    "Enabled": true,
    "LogToFile": true,
    "LogPath": "%APPDATA%\\Voxify\\logs\\voxify.log",
    "MaxLogLines": 1000
  }
}
```

### Log Location

**Default:**
```
%APPDATA%\Voxify\logs\voxify.log
```

**Example path:**
```
C:\Users\<User>\AppData\Roaming\Voxify\logs\voxify.log
```

### Log Format

```
[HH:mm:ss.fff] Category: Message
```

**Example:**
```
[12:34:56.123] Debug: Debug mode enabled
[12:34:57.456] AudioRecorder: Speech detected at 12:34:57.456
[12:34:58.789] Recognition: Raw text: "hello how are you"
```

---

## Troubleshooting

### Problem: Microphone Not Working

**Symptoms:**
- In debug window `Audio Level: 0.00`
- Error: `No microphone devices available`

**Check:**
1. Open debug window (`Ctrl+Shift+D`)
2. Check `Audio Level` — should change when talking
3. Check logs for errors

**Solution:**
1. Check microphone settings in Windows
2. Make sure microphone is connected
3. Check microphone access permissions

---

### Problem: VAD Not Detecting Speech

**Symptoms:**
- `VAD State: Inactive` even when talking
- Recording doesn't start automatically

**Check:**
1. Open debug window
2. Check `Audio Level` — should be > 0.05
3. Check `VAD State`

**Solution:**
1. Increase sensitivity in `appsettings.json`:
   ```json
   {
     "VoiceActivityDetection": {
       "SilenceThreshold": 0.03
     }
   }
   ```
2. Check microphone level in Windows
3. Speak louder or closer to the microphone

---

### Problem: Recognition Not Working

**Symptoms:**
- Recording works, but text is not inserted
- Error: `Speech recognizer is not initialized`

**Check:**
1. Check logs for model loading errors
2. Check `Provider` in debug window

**Solution:**
1. Make sure model path is correct:
   ```json
   {
     "SpeechRecognition": {
       "ModelPath": "C:\\Voxify\\Models\\vosk-model-small-ru-0.22"
     }
   }
   ```
2. Check that the model exists
3. Restart the application

---

### Problem: Second Instance Not Closing

**Symptoms:**
- Two Voxify windows open
- Conflicts during recording

**Check:**
1. Check `SingleInstance` setting:
   ```json
   {
     "SystemIntegration": {
       "SingleInstance": true
     }
   }
   ```

**Solution:**
1. Close all Voxify instances
2. Check Task Manager for `Voxify.exe`
3. Start the application again

---

## Performance

### Measuring Latency

Debug window shows logs with millisecond precision. To measure latency:

1. Open debug window
2. Press recording hotkey
3. Find in logs:
   - `Recording started` — recording start
   - `Speech detected` — speech detection
   - `Audio recorded: X bytes` — recording end
   - `Raw text: "..."` — recognition complete
   - `Text injected successfully` — text inserted

**Latency formula:**
```
Latency = (Text injected) - (Recording started)
```

**Target value:** < 500 ms

---

### Performance Optimization

**If latency > 500 ms:**

1. **Reduce model size:**
   - Whisper tiny instead of small
   - Vosk small instead of large

2. **Disable VAD if not needed:**
   ```json
   {
     "VoiceActivityDetection": {
       "Enabled": false
     }
   }
   ```

3. **Close other applications:**
   - Browsers
   - Games
   - Background processes

---

## Frequently Asked Questions

### Q: How to save logs for bug report?

**A:** Logs are automatically saved to `%APPDATA%\Voxify\logs\voxify.log`. Attach this file to the GitHub issue.

### Q: Can I increase the number of log lines?

**A:** Yes, change `MaxLogLines` in `appsettings.json`:
```json
{
  "Debug": {
    "MaxLogLines": 5000
  }
}
```

### Q: Debug window slows down the application

**A:** Reduce update frequency in `DebugWindow.cs` (parameter `Interval = 100` ms).

---

## See Also

- [CLI.md](CLI.md) — Command-line control
- [MODELS.md](MODELS.md) — Speech models comparison
- [README.md](../README.md) — Main guide
