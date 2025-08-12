using System.Runtime.InteropServices;
namespace GaymController.Shared.Contracts {
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct GamepadState {
        public ushort LX, LY, RX, RY;
        public ushort LT, RT;
        public uint Buttons;
        public static GamepadState Neutral => new() { LX=32767, LY=32767, RX=32767, RY=32767, LT=0, RT=0, Buttons=0 };
    }
}
