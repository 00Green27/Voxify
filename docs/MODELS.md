# Voxify — Speech Recognition Models Comparison

## Overview

Voxify supports two speech recognition providers:
- **Vosk** — offline recognition, high speed
- **Whisper** — neural network recognition, high accuracy

## Quick Comparison

| Provider | Model | Size | Accuracy | Speed | GPU | Languages |
|----------|-------|------|----------|-------|-----|-----------|
| **Vosk** | small-ru | 45 MB | ~85% | ⚡ Fast | ❌ | RU |
| **Vosk** | small-en | 50 MB | ~85% | ⚡ Fast | ❌ | EN |
| **Whisper** | tiny | 39 MB | ~80% | 🐌 Medium | ✅ | Multi |
| **Whisper** | base | 74 MB | ~85% | 🐌 Medium | ✅ | Multi |
| **Whisper** | small | 244 MB | ~90% | 🐢 Slow | ✅ | Multi |
| **Whisper** | medium | 769 MB | ~95% | 🐢🐢 Very Slow | ✅ | Multi |
| **Whisper** | large-v3 | 3094 MB | ~98% | 🐢🐢🐢 Extreme | ✅ | Multi |

---

## Vosk

### Advantages
- ✅ **Fast recognition** — minimal latency
- ✅ **Fully offline** — no internet required
- ✅ **Small size** — models 40-50 MB
- ✅ **Low CPU usage** — suitable for weak PCs
- ✅ **Simple setup** — model in a folder

### Disadvantages
- ❌ **One language at a time** — separate model needed
- ❌ **Lower accuracy** — especially with noise
- ❌ **No context understanding** — word-by-word recognition

### Recommended Models

| Language | Model | Link |
|----------|-------|------|
| Russian | `vosk-model-small-ru-0.22` | [Download](https://alphacephei.com/vosk/models) |
| English | `vosk-model-small-en-us-0.15` | [Download](https://alphacephei.com/vosk/models) |

### Configuration

```json
{
  "SpeechRecognition": {
    "Provider": "Vosk",
    "ModelPath": "C:\\Voxify\\Models\\vosk-model-small-ru-0.22",
    "Language": "ru-RU"
  }
}
```

---

## Whisper

### Advantages
- ✅ **High accuracy** — better noise handling
- ✅ **Multilingual** — one model for many languages
- ✅ **Context understanding** — considers phrase context
- ✅ **GPU support** — acceleration on graphics card
- ✅ **Punctuation** — automatically adds punctuation

### Disadvantages
- ❌ **Slower** — especially on CPU
- ❌ **Larger size** — models from 39 MB to 3 GB
- ❌ **High CPU/GPU usage** — may load the system

### Recommended Models

| Scenario | Model | Reason |
|----------|-------|--------|
| **Weak PC** | `tiny` | Minimal size, acceptable accuracy |
| **Balance** | `base` | Good size/accuracy ratio |
| **Accuracy** | `small` | High accuracy with reasonable size |
| **Maximum** | `medium` | Best accuracy for important tasks |

### Configuration

```json
{
  "SpeechRecognition": {
    "Provider": "Whisper",
    "ModelPath": "C:\\Voxify\\Models\\ggml-tiny.bin",
    "Language": "ru"
  }
}
```

---

## Model Selection: Recommendations

### For Text Dictation (Russian)

**Recommendation:** Vosk small-ru

**Reason:**
- Fast recognition without delays
- Good accuracy for Russian
- Doesn't load the system

### For Multilingual Environment

**Recommendation:** Whisper base or small

**Reason:**
- One model for all languages
- Automatic language detection
- Better accuracy for mixed speech

### For Weak Hardware

**Recommendation:** Whisper tiny or Vosk small

**Reason:**
- Minimal resource consumption
- Acceptable accuracy for basic tasks

### For Maximum Accuracy

**Recommendation:** Whisper medium

**Reason:**
- Highest recognition accuracy
- Best noise and accent handling
- Automatic punctuation

---

## Model Installation

### Vosk

1. Download the model from [official site](https://alphacephei.com/vosk/models)
2. Extract to a folder (e.g., `C:\Voxify\Models\vosk-model-small-ru-0.22`)
3. Specify the path in `appsettings.json`:
   ```json
   "ModelPath": "C:\\Voxify\\Models\\vosk-model-small-ru-0.22"
   ```

### Whisper

1. Install the model via `whisper.net` or download manually
2. Place the model file in a folder (e.g., `C:\Voxify\Models\`)
3. Specify the path and model type:
   ```json
   {
     "Provider": "Whisper",
     "ModelPath": "C:\\Voxify\\Models\\ggml-tiny.bin",
     "WhisperModel": "tiny"
   }
   ```

---

## Performance

### Latency (on Intel i5-8400, without GPU)

| Model | Latency (sec) | CPU Usage |
|-------|---------------|-----------|
| Vosk small-ru | ~0.3 sec | 15% |
| Whisper tiny | ~1.5 sec | 40% |
| Whisper base | ~3 sec | 60% |
| Whisper small | ~8 sec | 80% |
| Whisper medium | ~20 sec | 100% |

### Latency (on Intel i5-8400 + GTX 1660)

| Model | Latency (sec) | GPU Usage |
|-------|---------------|-----------|
| Whisper tiny | ~0.5 sec | 30% |
| Whisper base | ~1 sec | 40% |
| Whisper small | ~2 sec | 60% |
| Whisper medium | ~5 sec | 80% |

---

## Frequently Asked Questions

### Q: Which model is better for Russian?

**A:** Vosk small-ru for speed, Whisper small for accuracy.

### Q: Can I switch models on the fly?

**A:** No, you need to restart the application after changing `appsettings.json`.

### Q: How much space is needed for all models?

**A:** 
- Vosk (RU + EN): ~100 MB
- Whisper (all models): ~4.5 GB

### Q: Does Whisper work without internet?

**A:** Yes, all models work fully offline after download.

---

## See Also

- [CLI.md](CLI.md) — Command-line control
- [DEBUG.md](DEBUG.md) — Troubleshooting
- [README.md](../README.md) — Main guide
