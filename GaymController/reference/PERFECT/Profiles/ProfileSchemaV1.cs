using System.Collections.Generic;

namespace WootMouseRemap
{
    public sealed class ProfileSchemaV1
    {
        public string Version { get; set; } = "1";
        public string Name { get; set; } = "default";
        // New: persisted current application mode (optional for backwards compatibility)
        public AppMode? CurrentAppMode { get; set; } = null; // null => treat as KbmToPad

        public bool WasdToLeftStick { get; set; } = true;

        // Hotkeys (virtual-key codes)
        public int OverlayToggleVk { get; set; } = 0xDC; // '\\'
        public uint OverlayToggleMods { get; set; } = 0; // MOD_* flags (Ctrl/Shift/Alt)
        public int ModeToggleVk     { get; set; } = 0x77; // F8
        public int SuppressToggleVk { get; set; } = 0x78; // F9
        // New optional hotkeys (0 = disabled)
        public int LockToggleVk { get; set; } = 0; // toggle overlay lock position
        public int CompactToggleVk { get; set; } = 0; // toggle compact mode

        // Overlay UI prefs
        public bool StartHidden { get; set; } = false;
        public int OverlayLeft { get; set; } = -1;   // persisted window X position
        public int OverlayTop  { get; set; } = -1;   // persisted window Y position
        // New advanced overlay prefs (backwards compatible)
        public bool OverlayLockPosition { get; set; } = false;
        public bool OverlaySnapEdges { get; set; } = true;
        public int OverlaySnapThreshold { get; set; } = 12;
        public bool OverlayAlwaysOnTop { get; set; } = true;
        public string OverlayTheme { get; set; } = "Dark"; // Dark | Light | Purple
        public bool OverlayCompact { get; set; } = false;      // new: compact mode (hide right panel)
        public int OverlayAccentArgb { get; set; } = 0;        // new: custom accent color (0 = unset)
        public List<int> OverlayRecentAccents { get; set; } = new(); // last picked accent ARGBs (max 8 maintained in code)
        public List<PerMonitorBounds> OverlayMonitorBounds { get; set; } = new(); // per-monitor stored bounds

        // Controller prefs
        public string PreferredInputBackend { get; set; } = "XInput"; // XInput or DirectInput
        public int PreferredXInputIndex { get; set; } = 0;
        public string PreferredDirectInputInstance { get; set; } = "";
        public bool XInputPassthrough { get; set; } = false; // new: persist passthrough toggle

        // Curve preview preferences
        public bool CurveShowGrid { get; set; } = true;
        public bool CurveShowLivePoint { get; set; } = true;

        public Dictionary<int, Xbox360Control> KeyMap { get; set; } = new()
        {
                        { 0x20, Xbox360Control.A }, // Space
            { 0x51, Xbox360Control.Y }, // Q
            { 0x45, Xbox360Control.X }, // E
        };

        public Dictionary<MouseInput, Xbox360Control> MouseMap { get; set; } = new()
        {
            { MouseInput.Left,  Xbox360Control.RightTrigger },
            { MouseInput.Right, Xbox360Control.LeftTrigger  },
            { MouseInput.Middle, Xbox360Control.RightStick  },
            { MouseInput.XButton1, Xbox360Control.LeftBumper },
            { MouseInput.XButton2, Xbox360Control.RightBumper },
            { MouseInput.ScrollUp, Xbox360Control.Y },
            { MouseInput.ScrollDown, Xbox360Control.B }
        };

        public Dictionary<Xbox360Control, RapidFireConfig> RapidFire { get; set; } = new(); // optional per-button

        public CurveSettings Curves { get; set; } = new();

        public sealed class CurveSettings
        {
            public float Sensitivity { get; set; } = 0.35f;
            public float Expo { get; set; } = 0.6f;
            public float AntiDeadzone { get; set; } = 0.05f;
            public float EmaAlpha { get; set; } = 0.35f;
            public float VelocityGain { get; set; } = 0.0f;
            public float JitterFloor { get; set; } = 0.0f;
            public float ScaleX { get; set; } = 1.0f;
            public float ScaleY { get; set; } = 1.0f;
        }
        public sealed class PerMonitorBounds { public string MonitorId { get; set; } = ""; public int X { get; set; } public int Y { get; set; } public int W { get; set; } public int H { get; set; } }
    }

    public sealed class RapidFireConfig { public double RateHz { get; set; } = 9.0; public int Burst { get; set; } = 0; }
}