using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WootMouseRemap.Controllers
{
    public static class ControllerManager
    {
        public static IEnumerable<(int Index, string Name)> EnumerateXInput()
        {
            for (int i = 0; i < 4; i++)
            {
                bool connected = ProbeXInputConnected(i);
                yield return (i, $"XInput P{i + 1} {(connected ? "[Connected]" : "[None]")}");
            }
        }

        private struct XINPUT_GAMEPAD { public ushort wButtons; public byte bLeftTrigger; public byte bRightTrigger; public short sThumbLX, sThumbLY, sThumbRX, sThumbRY; }
        private struct XINPUT_STATE { public uint dwPacketNumber; public XINPUT_GAMEPAD Gamepad; }
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")] private static extern int XInputGetState14(uint dwUserIndex, out XINPUT_STATE pState);
        [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")] private static extern int XInputGetState13(uint dwUserIndex, out XINPUT_STATE pState);
        private static bool ProbeXInputConnected(int index)
        {
            XINPUT_STATE s;
            try { return XInputGetState14((uint)index, out s) == 0; }
            catch { try { return XInputGetState13((uint)index, out s) == 0; } catch { return false; } }
        }

#if DIRECTINPUT
        public static IEnumerable<(Guid Instance, string Name)> EnumerateDirectInput()
        {
            var di = new SharpDX.DirectInput.DirectInput();
            foreach (var dev in di.GetDevices(SharpDX.DirectInput.DeviceClass.GameControl, SharpDX.DirectInput.DeviceEnumerationFlags.AttachedOnly))
                yield return (dev.InstanceGuid, $"{dev.ProductName} ({dev.InstanceGuid})");
        }
#else
        public static IEnumerable<(Guid Instance, string Name)> EnumerateDirectInput()
        {
            yield break;
        }
#endif
    }
}
