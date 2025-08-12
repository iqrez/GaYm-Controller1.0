using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace WootMouseRemap
{
    public static class LowLevelHooks
    {
        public static event Action<int, bool>? KeyEvent;           // vk, down
        public static event Action<MouseInput, bool>? MouseButton;  // button, down
        public static event Action<int, int>? MouseMove;            // dx, dy
        public static event Action? PanicTriggered;

        public static bool Suppress { get; set; } = false;

        private static IntPtr _hkKb = IntPtr.Zero;
        private static IntPtr _hkMs = IntPtr.Zero;
        private static Point _lastPt;

        public static void Install()
        {
            if (_hkKb != IntPtr.Zero) return;
            _lastPt = Cursor.Position;
            _kbProc = KbProc;
            _msProc = MsProc;
            _hkKb = SetWindowsHookEx(13 /*WH_KEYBOARD_LL*/, _kbProc, GetModuleHandle(IntPtr.Zero), 0);
            _hkMs = SetWindowsHookEx(14 /*WH_MOUSE_LL*/, _msProc, GetModuleHandle(IntPtr.Zero), 0);
            if (_hkKb == IntPtr.Zero || _hkMs == IntPtr.Zero) throw new InvalidOperationException("SetWindowsHookEx failed.");
        }

        public static void Uninstall()
        {
            if (_hkKb != IntPtr.Zero) { UnhookWindowsHookEx(_hkKb); _hkKb = IntPtr.Zero; }
            if (_hkMs != IntPtr.Zero) { UnhookWindowsHookEx(_hkMs); _hkMs = IntPtr.Zero; }
        }

        private static IntPtr KbProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = (int)wParam;
                var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                bool down = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
                bool up = msg == WM_KEYUP || msg == WM_SYSKEYUP;

                // Panic hotkey: Ctrl+Alt+Pause -> disable suppression
                if (down && kb.vkCode == (int)Keys.Pause && IsCtrlAltDown())
                {
                    Suppress = false;
                    PanicTriggered?.Invoke();
                    Logger.Warn("PANIC triggered: suppression disabled");
                }

                if (down || up) KeyEvent?.Invoke(kb.vkCode, down);

                if (Suppress) return (IntPtr)1;
            }
            return CallNextHookEx(_hkKb, nCode, wParam, lParam);
        }

        private static IntPtr MsProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = (int)wParam;
                var ms = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int dx = ms.pt.x - _lastPt.X;
                int dy = ms.pt.y - _lastPt.Y;
                _lastPt = new Point(ms.pt.x, ms.pt.y);

                switch (msg)
                {
                    case WM_LBUTTONDOWN: MouseButton?.Invoke(MouseInput.Left, true); break;
                    case WM_LBUTTONUP:   MouseButton?.Invoke(MouseInput.Left, false); break;
                    case WM_RBUTTONDOWN: MouseButton?.Invoke(MouseInput.Right, true); break;
                    case WM_RBUTTONUP:   MouseButton?.Invoke(MouseInput.Right, false); break;
                    case WM_MBUTTONDOWN: MouseButton?.Invoke(MouseInput.Middle, true); break;
                    case WM_MBUTTONUP:   MouseButton?.Invoke(MouseInput.Middle, false); break;
                    case WM_XBUTTONDOWN:
                        if (((ms.mouseData >> 16) & 0xffff) == 1) MouseButton?.Invoke(MouseInput.XButton1, true); else MouseButton?.Invoke(MouseInput.XButton2, true);
                        break;
                    case WM_XBUTTONUP:
                        if (((ms.mouseData >> 16) & 0xffff) == 1) MouseButton?.Invoke(MouseInput.XButton1, false); else MouseButton?.Invoke(MouseInput.XButton2, false);
                        break;
                    case WM_MOUSEWHEEL:
                        // wheel handled by RawInput; skip
                        break;
                    case WM_MOUSEMOVE:
                        if (dx != 0 || dy != 0) MouseMove?.Invoke(dx, dy);
                        break;
                }

                if (Suppress) return (IntPtr)1;
            }
            return CallNextHookEx(_hkMs, nCode, wParam, lParam);
        }

        private static bool IsCtrlAltDown()
        {
            return (GetKeyState((int)Keys.LControlKey) < 0 || GetKeyState((int)Keys.RControlKey) < 0)
                && (GetKeyState((int)Keys.LMenu) < 0 || GetKeyState((int)Keys.RMenu) < 0);
        }

        private static LowLevelProc? _kbProc, _msProc;

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT { public int vkCode, scanCode, flags, time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData, flags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x, y; }

        private const int WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101, WM_SYSKEYDOWN = 0x0104, WM_SYSKEYUP = 0x0105;
        private const int WM_MOUSEMOVE = 0x0200, WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202, WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205,
                          WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208, WM_XBUTTONDOWN = 0x020B, WM_XBUTTONUP = 0x020C, WM_MOUSEWHEEL = 0x020A;

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr GetModuleHandle(IntPtr lpModuleName);
        [DllImport("user32.dll")] private static extern short GetKeyState(int nVirtKey);
    }
}
