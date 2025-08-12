using System;
using System.Runtime.InteropServices;

namespace WootMouseRemap
{
    /// <summary>
    /// Centralizes global hotkey registration and dispatch. Consumers subscribe to events instead of
    /// handling WM_HOTKEY directly in the UI class. Re-register when profile changes.
    /// </summary>
    public sealed class HotkeyManager : IDisposable
    {
        // Hotkey IDs kept private to avoid leaking implementation details
        private const int HK_OVERLAY_TOGGLE = 100;
        private const int HK_MODE_TOGGLE = 101;
        private const int HK_SUPPRESS_TOGGLE = 102;
        private const int HK_LOCK_TOGGLE = 103;
        private const int HK_COMPACT_TOGGLE = 107;

        private readonly Func<IntPtr> _hwndProvider;
        private bool _registered;

        public HotkeyManager(Func<IntPtr> hwndProvider)
        {
            _hwndProvider = hwndProvider ?? throw new ArgumentNullException(nameof(hwndProvider));
        }

        #region Win32
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion

        public event Action? OverlayToggle;
        public event Action? ModeNext;
        public event Action? SuppressToggle;
        public event Action? LockToggle;
        public event Action? CompactToggle;

        public void RegisterAll(ProfileSchemaV1? profile)
        {
            if (profile == null) return;
            UnregisterAll();
            try
            {
                var h = _hwndProvider();
                if (h == IntPtr.Zero) return;
                // Overlay toggle (optional)
                if (profile.OverlayToggleVk != 0)
                    RegisterHotKey(h, HK_OVERLAY_TOGGLE, profile.OverlayToggleMods, profile.OverlayToggleVk);
                // Mode toggle (default F8 if unset)
                int modeVk = profile.ModeToggleVk > 0 ? profile.ModeToggleVk : 0x77; // VK_F8
                RegisterHotKey(h, HK_MODE_TOGGLE, 0, modeVk);
                // Suppression toggle (default F9 if unset)
                int supVk = profile.SuppressToggleVk > 0 ? profile.SuppressToggleVk : 0x78; // VK_F9
                RegisterHotKey(h, HK_SUPPRESS_TOGGLE, 0, supVk);
                if (profile.LockToggleVk > 0) RegisterHotKey(h, HK_LOCK_TOGGLE, 0, profile.LockToggleVk);
                if (profile.CompactToggleVk > 0) RegisterHotKey(h, HK_COMPACT_TOGGLE, 0, profile.CompactToggleVk);
                _registered = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to register hotkeys", ex);
            }
        }

        public void ProcessHotkey(int id)
        {
            try
            {
                switch (id)
                {
                    case HK_OVERLAY_TOGGLE: OverlayToggle?.Invoke(); break;
                    case HK_MODE_TOGGLE: ModeNext?.Invoke(); break;
                    case HK_SUPPRESS_TOGGLE: SuppressToggle?.Invoke(); break;
                    case HK_LOCK_TOGGLE: LockToggle?.Invoke(); break;
                    case HK_COMPACT_TOGGLE: CompactToggle?.Invoke(); break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Hotkey dispatch failed", ex);
            }
        }

        public void UnregisterAll()
        {
            if (!_registered) return;
            try
            {
                var h = _hwndProvider();
                if (h == IntPtr.Zero) return;
                UnregisterHotKey(h, HK_OVERLAY_TOGGLE);
                UnregisterHotKey(h, HK_MODE_TOGGLE);
                UnregisterHotKey(h, HK_SUPPRESS_TOGGLE);
                UnregisterHotKey(h, HK_LOCK_TOGGLE);
                UnregisterHotKey(h, HK_COMPACT_TOGGLE);
            }
            catch { }
            _registered = false;
        }

        public void Dispose()
        {
            UnregisterAll();
        }
    }
}
