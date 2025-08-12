# WootMouseRemap (fix9)

Changes:
- Single-instance via mutex.
- Better global exception handling with logging + crash dialog.
- ViGEm status log hooks.
- RawInput explicit Register() during startup.
- Added `app.manifest` (PerMonitorV2 DPI, asInvoker).
- Switched to `Microsoft.NET.Sdk` + bumped ViGEm client to `1.21.256` to silence warnings.

## Build
```powershell
dotnet restore
dotnet build -c Release
# Optional single-file publish
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false
```


## fix18m_patched3 -> patched4 additions
- Mode toggle is also bound to **Middle Mouse** by default (and F8).
- **OS Suppression** now auto-syncs with mode: ON when outputting to controller, OFF in mouse/keyboard mode. Panic: Ctrl+Alt+Pause.
- **No-motion watchdog**: when you lift the mouse (no deltas for ~18ms), right stick is hard-zeroed and smoothing is reset to prevent drift.
- **Diagonal smoothing**: mapper uses radial processing + vector EMA to eliminate XY stutter at diagonals.
