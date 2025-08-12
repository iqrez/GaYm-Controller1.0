using System;
using System.Runtime.InteropServices;
using System.Threading;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System.Diagnostics;

namespace WootMouseRemap
{
    public sealed class XInputPassthrough : IDisposable
    {
        private readonly Xbox360ControllerWrapper _pad;
        private Thread? _thread;
        private CancellationTokenSource? _cts;

        public bool Enabled { get; private set; }
        public int PlayerIndex { get; private set; } = 0; // 0..3
        public bool IsConnected { get; private set; }
        public int PacketRateHz { get; private set; }

        private int _packetsThisSec;
        private Stopwatch? _rateSw;

        public XInputPassthrough(Xbox360ControllerWrapper pad) => _pad = pad;

        public void SetPlayerIndex(int index) => PlayerIndex = Math.Clamp(index, 0, 3);

        public void Start()
        {
            if (Enabled) return;
            _cts = new CancellationTokenSource();
            _thread = new Thread(() => Run(_cts.Token)) { IsBackground = true, Name = "XInputPassthrough" };
            Enabled = true;
            _thread.Start();
        }

        public void Stop()
        {
            Enabled = false;
            try { _cts?.Cancel(); } catch { }
            try { _thread?.Join(200); } catch { }
            _cts?.Dispose(); _cts = null; _thread = null;
            IsConnected = false;
            PacketRateHz = 0;
            _packetsThisSec = 0;
            _rateSw = null;
            try { _pad.ResetAll(); } catch { }
        }

        private void Run(CancellationToken ct)
        {
            XINPUT_STATE state;
            int lastPacket = -1;
            _packetsThisSec = 0;
            _rateSw = Stopwatch.StartNew();
            PacketRateHz = 0;
            IsConnected = false;

            while (!ct.IsCancellationRequested)
            {
                // Read current index dynamically so UI changes take effect immediately
                uint idx = (uint)PlayerIndex;

                if (XInputGetState(idx, out state) != 0)
                {
                    // Not connected
                    IsConnected = false;
                    lastPacket = -1;
                    Thread.Sleep(200);
                    TickRate();
                    continue;
                }

                IsConnected = true;

                if (state.dwPacketNumber != lastPacket)
                {
                    lastPacket = (int)state.dwPacketNumber;
                    _packetsThisSec++;
                    var gp = state.Gamepad;

                    // Buttons
                    SetBtn(gp.wButtons, 0x1000, Xbox360Button.A); // A
                    SetBtn(gp.wButtons, 0x2000, Xbox360Button.B); // B
                    SetBtn(gp.wButtons, 0x4000, Xbox360Button.X); // X
                    SetBtn(gp.wButtons, 0x8000, Xbox360Button.Y); // Y

                    SetBtn(gp.wButtons, 0x0100, Xbox360Button.LeftShoulder);
                    SetBtn(gp.wButtons, 0x0200, Xbox360Button.RightShoulder);
                    // Correct Start/Back mapping to match XInput (0x0010 Start, 0x0020 Back)
                    SetBtn(gp.wButtons, 0x0010, Xbox360Button.Start);
                    SetBtn(gp.wButtons, 0x0020, Xbox360Button.Back);
                    SetBtn(gp.wButtons, 0x0040, Xbox360Button.LeftThumb);
                    SetBtn(gp.wButtons, 0x0080, Xbox360Button.RightThumb);

                    // D-Pad
                    _pad.SetDpad(
                        (gp.wButtons & 0x0001) != 0,
                        (gp.wButtons & 0x0002) != 0,
                        (gp.wButtons & 0x0004) != 0,
                        (gp.wButtons & 0x0008) != 0
                    );

                    // Triggers
                    _pad.SetTrigger(false, gp.bLeftTrigger);
                    _pad.SetTrigger(true,  gp.bRightTrigger);

                    // Sticks
                    _pad.SetLeftStick(gp.sThumbLX, gp.sThumbLY);
                    _pad.SetRightStick(gp.sThumbRX, gp.sThumbRY);
                }

                // Ensure the ViGEm report is submitted at a steady rate
                _pad.Submit();
                Thread.Sleep(4); // ~250Hz
                TickRate();
            }
        }

        private void TickRate()
        {
            var sw = _rateSw;
            if (sw != null && sw.IsRunning && sw.ElapsedMilliseconds >= 1000)
            {
                PacketRateHz = _packetsThisSec;
                _packetsThisSec = 0;
                sw.Restart();
            }
        }

        private void SetBtn(ushort buttons, int mask, Xbox360Button btn)
        {
            _pad.SetButton(btn, (buttons & mask) != 0);
        }

        public void Dispose() => Stop();

        // ==== XInput structs ====
        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        // Prefer xinput1_4, fallback to xinput1_3 (older systems)
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern int XInputGetState14(uint dwUserIndex, out XINPUT_STATE pState);

        [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")]
        private static extern int XInputGetState13(uint dwUserIndex, out XINPUT_STATE pState);

        private static int XInputGetState(uint index, out XINPUT_STATE state)
        {
            try { return XInputGetState14(index, out state); }
            catch
            {
                try { return XInputGetState13(index, out state); }
                catch { state = default; return -1; }
            }
        }
    }
}
