using System;
using System.IO.Pipes;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace VPad.Broker;

public interface IVPadLogger : IDisposable
{
    void Information(string messageTemplate, params object?[] args);
    void Debug(string messageTemplate, params object?[] args);
    void Error(Exception ex, string messageTemplate);
    void Fatal(Exception ex, string messageTemplate);
}

public sealed class LoggerAdapter : IVPadLogger
{
    private readonly ILogger _logger;
    public LoggerAdapter(ILogger logger) => _logger = logger;
    public void Information(string m, params object?[] a) => _logger.LogInformation(m, a);
    public void Debug(string m, params object?[] a) => _logger.LogDebug(m, a);
    public void Error(Exception ex, string m) => _logger.LogError(ex, m);
    public void Fatal(Exception ex, string m) => _logger.LogCritical(ex, m);
    public void Dispose() { }
}

public sealed record VPadBrokerOptions(string PipeName)
{
    public int MaxInstances { get; init; } = 50;
}

internal static class Log
{
    public static void Information(string messageTemplate, params object?[] args) =>
        Console.WriteLine($"[INFO] {DateTime.Now:O} {messageTemplate}{(args.Length>0 ? " | " + string.Join(", ", args) : string.Empty)}");
    public static void Debug(string messageTemplate, params object?[] args) =>
        Console.WriteLine($"[DBG ] {DateTime.Now:O} {messageTemplate}{(args.Length>0 ? " | " + string.Join(", ", args) : string.Empty)}");
    public static void Error(Exception ex, string messageTemplate) =>
        Console.WriteLine($"[ERR ] {DateTime.Now:O} {messageTemplate} | {ex}");
    public static void Fatal(Exception ex, string messageTemplate) =>
        Console.WriteLine($"[FATL] {DateTime.Now:O} {messageTemplate} | {ex}");
}

public sealed class ConsoleVPadLogger : IVPadLogger
{
    public void Information(string messageTemplate, params object?[] args) => Log.Information(messageTemplate, args);
    public void Debug(string messageTemplate, params object?[] args) => Log.Debug(messageTemplate, args);
    public void Error(Exception ex, string messageTemplate) => Log.Error(ex, messageTemplate);
    public void Fatal(Exception ex, string messageTemplate) => Log.Fatal(ex, messageTemplate);
    public void Dispose() { /* no-op */ }
}

[SupportedOSPlatform("windows")]
internal static class Native
{
    public static readonly Guid GUID_DEVINTERFACE_VPADPAD = new Guid("E2A2D4A8-8BB3-41D8-BFC7-43B0B7D23B19");
    public static readonly Guid GUID_DEVINTERFACE_VPADBUS = new Guid("9289F3A7-6E3A-4A3B-917A-3F662C527C5D");

    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, string? Enumerator, IntPtr hwndParent, uint Flags);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, out int RequiredSize, IntPtr DeviceInfoData);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, out int RequiredSize, IntPtr DeviceInfoData);
    [DllImport("setupapi.dll", SetLastError=true)] static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    [StructLayout(LayoutKind.Sequential)] struct SP_DEVICE_INTERFACE_DATA { public int cbSize; public Guid InterfaceClassGuid; public int Flags; public IntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] struct SP_DEVICE_INTERFACE_DETAIL_DATA { public int cbSize; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)] public string DevicePath; }

    const uint DIGCF_PRESENT         = 0x00000002;
    const uint DIGCF_DEVICEINTERFACE = 0x00000010;

    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
        uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, IntPtr lpInBuffer, int nInBufferSize,
        IntPtr lpOutBuffer, int nOutBufferSize, out int lpBytesReturned, IntPtr lpOverlapped);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, ref VPAD_STATE inbuf, int nInBufferSize,
        IntPtr lpOutBuffer, int nOutBufferSize, out int lpBytesReturned, IntPtr lpOverlapped);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, ref VPAD_LEDS inbuf, int nInBufferSize,
        IntPtr lpOutBuffer, int nOutBufferSize, out int lpBytesReturned, IntPtr lpOverlapped);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, IntPtr inbuf, int inlen,
        ref VPAD_RUMBLE outbuf, int outlen, out int bytes, IntPtr ol);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, IntPtr inbuf, int inlen,
        ref VPAD_LEDS outbuf, int outlen, out int bytes, IntPtr ol);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool CloseHandle(IntPtr hObject);

    const uint GENERIC_READ  = 0x80000000;
    const uint GENERIC_WRITE = 0x40000000;
    const uint FILE_SHARE_READ  = 0x00000001;
    const uint FILE_SHARE_WRITE = 0x00000002;
    const uint OPEN_EXISTING = 3;

    const uint FILE_DEVICE_VPAD    = 0x9A00;
    const uint FILE_DEVICE_VPADBUS = 0x9A10;
    static uint CTL_CODE(uint dev, uint func, uint method, uint access) =>
        ((dev) << 16) | ((access) << 14) | ((func) << 2) | (method);

    static readonly uint IOCTL_VPAD_GET_VERSION = CTL_CODE(FILE_DEVICE_VPAD,    0x901, 0, 1);
    static readonly uint IOCTL_VPAD_SET_STATE   = CTL_CODE(FILE_DEVICE_VPAD,    0x902, 0, 2);
    static readonly uint IOCTL_VPAD_CREATE      = CTL_CODE(FILE_DEVICE_VPAD,    0x903, 0, 2);
    static readonly uint IOCTL_VPAD_DESTROY     = CTL_CODE(FILE_DEVICE_VPAD,    0x904, 0, 2);
    static readonly uint IOCTL_VPAD_GET_RUMBLE  = CTL_CODE(FILE_DEVICE_VPAD,    0x905, 0, 1);
    static readonly uint IOCTL_VPAD_SET_LEDS    = CTL_CODE(FILE_DEVICE_VPAD,    0x906, 0, 2);
    static readonly uint IOCTL_VPAD_GET_LEDS    = CTL_CODE(FILE_DEVICE_VPAD,    0x907, 0, 1);

    static readonly uint IOCTL_VPADBUS_GET_PADCOUNT = CTL_CODE(FILE_DEVICE_VPADBUS, 0xA01, 0, 1);
    static readonly uint IOCTL_VPADBUS_SET_PADCOUNT = CTL_CODE(FILE_DEVICE_VPADBUS, 0xA02, 0, 2);
    static readonly uint IOCTL_VPADBUS_RESCAN       = CTL_CODE(FILE_DEVICE_VPADBUS, 0xA03, 0, 2);

    [StructLayout(LayoutKind.Sequential, Pack=1)] public struct VPAD_STATE { public ushort Buttons; public byte LeftTrigger; public byte RightTrigger; public short LX, LY, RX, RY; }
    [StructLayout(LayoutKind.Sequential, Pack=1)] public struct VPAD_RUMBLE { public uint Sequence; public byte Left; public byte Right; }
    [StructLayout(LayoutKind.Sequential, Pack=1)] public struct VPAD_LEDS { public byte R; public byte G; public byte B; }
    static IntPtr OpenNthInterface(Guid guid, uint index)
    {
        var h = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (h == (IntPtr)(-1)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            var ifdata = new SP_DEVICE_INTERFACE_DATA { cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
            uint count = 0;
            for (uint i = 0; ; i++)
            {
                if (!SetupDiEnumDeviceInterfaces(h, IntPtr.Zero, ref guid, i, ref ifdata))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "No interface at index");
                if (count == index)
                {
                    if (!SetupDiGetDeviceInterfaceDetail(h, ref ifdata, IntPtr.Zero, 0, out int needed, IntPtr.Zero))
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err != 122) throw new System.ComponentModel.Win32Exception(err);
                    }
                    var detail = new SP_DEVICE_INTERFACE_DETAIL_DATA { cbSize = IntPtr.Size == 8 ? 8 : 6, DevicePath = new string('\0', 260) };
                    if (!SetupDiGetDeviceInterfaceDetail(h, ref ifdata, ref detail, Marshal.SizeOf<SP_DEVICE_INTERFACE_DETAIL_DATA>(), out _, IntPtr.Zero))
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    var path = detail.DevicePath.TrimEnd('\0');
                    var dev = CreateFile(path, GENERIC_READ|GENERIC_WRITE, FILE_SHARE_READ|FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                    if (dev != (IntPtr)(-1)) return dev;
                }
                count++;
            }
        }
        finally { SetupDiDestroyDeviceInfoList(h); }
    }
    static IntPtr OpenPadByIndex(uint index) => OpenNthInterface(GUID_DEVINTERFACE_VPADPAD, index);
    static IntPtr OpenBus() => OpenNthInterface(GUID_DEVINTERFACE_VPADBUS, 0);

    // === FAKE backend for CI or dev machines without drivers ===
    public static bool UseFake => Environment.GetEnvironmentVariable("VPAD_FAKE") == "1";

    public sealed class FakePadHandle : IDisposable
    {
        // Remove volatile from struct fields
        public VPAD_STATE State = default;
        public VPAD_LEDS Leds = default;
        public VPAD_RUMBLE Rumble = default;
        private uint _seq;
        // Implement missing methods for fake
        public void SetState(VPAD_STATE st)
        {
            State = st;
            // Echo triggers into rumble and advance sequence to simulate activity
            Rumble.Sequence = ++_seq;
            Rumble.Left = st.LeftTrigger;
            Rumble.Right = st.RightTrigger;
        }
        public VPAD_RUMBLE GetRumble() { return Rumble; }
        public void SetLeds(byte r, byte g, byte b) { Leds = new VPAD_LEDS { R = r, G = g, B = b }; }
        public VPAD_LEDS GetLeds() { return Leds; }
        public void Dispose() {}
    }

    public sealed class PadHandle : IDisposable
    {
        public IntPtr Dev;
        public FakePadHandle? Fake;
        public PadHandle(uint index)
        {
            if (UseFake)
            {
                Dev = IntPtr.Zero;
                Fake = new FakePadHandle();
                return;
            }
            Dev = OpenPadByIndex(index);
            if (!DeviceIoControl(Dev, IOCTL_VPAD_CREATE, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "IOCTL_VPAD_CREATE failed");
        }
        public void Dispose()
        {
            if (UseFake) return;
            if (Dev != (IntPtr)(-1) && Dev != IntPtr.Zero)
            {
                DeviceIoControl(Dev, IOCTL_VPAD_DESTROY, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
                CloseHandle(Dev);
            }
            Dev = (IntPtr)(-1);
        }
        public void SetState(VPAD_STATE st)
        {
            if (UseFake) { Fake!.SetState(st); return; }
            if (!DeviceIoControl(Dev, IOCTL_VPAD_SET_STATE, ref st, Marshal.SizeOf<VPAD_STATE>(), IntPtr.Zero, 0, out _, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "IOCTL_VPAD_SET_STATE failed");
        }
        public VPAD_RUMBLE GetRumble()
        {
            if (UseFake) { return Fake!.GetRumble(); }
            var r = new VPAD_RUMBLE();
            if (!DeviceIoControl(Dev, IOCTL_VPAD_GET_RUMBLE, IntPtr.Zero, 0, ref r, Marshal.SizeOf<VPAD_RUMBLE>(), out _, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "IOCTL_VPAD_GET_RUMBLE failed");
            return r;
        }
        public void SetLeds(byte r, byte g, byte b)
        {
            if (UseFake) { Fake!.SetLeds(r,g,b); return; }
            var leds = new VPAD_LEDS { R=r, G=g, B=b };
            if (!DeviceIoControl(Dev, IOCTL_VPAD_SET_LEDS, ref leds, Marshal.SizeOf<VPAD_LEDS>(), IntPtr.Zero, 0, out _, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "IOCTL_VPAD_SET_LEDS failed");
        }
        public VPAD_LEDS GetLeds()
        {
            if (UseFake) { return Fake!.GetLeds(); }
            var leds = new VPAD_LEDS();
            if (!DeviceIoControl(Dev, IOCTL_VPAD_GET_LEDS, IntPtr.Zero, 0, ref leds, Marshal.SizeOf<VPAD_LEDS>(), out _, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "IOCTL_VPAD_GET_LEDS failed");
            return leds;
        }
    }

    // Add stubs for missing Native methods
    public static uint GetPadCountFromBus() => 4; // TODO: implement actual logic
    public static void SetPadCountOnBus(uint n) { /* TODO: implement */ }
    public static void RescanBus() { /* TODO: implement */ }
}

internal enum BrokerCommand : byte
{
    Version=1, Count=2, Create=3, Destroy=4, SetState=5, GetRumble=6, SetLeds=7, GetLeds=8,
    PadCountGet=20, PadCountSet=21, Rescan=22
}

public sealed class VPadBrokerServer
{
    private readonly IVPadLogger _log;
    private readonly string _pipeName;
    private readonly int _maxInstances;
    private readonly ConcurrentDictionary<int, Native.PadHandle> _pads = new();

    public VPadBrokerServer(IVPadLogger log, string pipeName) : this(log, new VPadBrokerOptions(pipeName)) {}

    public VPadBrokerServer(IVPadLogger log, VPadBrokerOptions options)
    {
        _log = log;
        _pipeName = options.PipeName;
        _maxInstances = options.MaxInstances;
    }

    public void Run(CancellationToken? externalToken = null)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken ?? CancellationToken.None);
        var token = cts.Token;
        while (!token.IsCancellationRequested)
        {
            var server = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, _maxInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            server.WaitForConnection();
            ThreadPool.QueueUserWorkItem(_ => HandleClient(server));
        }
    }

    private void HandleClient(NamedPipeServerStream s)
    {
        using var br = new BinaryReader(s);
        using var bw = new BinaryWriter(s);
        try
        {
            while (true)
            {
                var cmd = (BrokerCommand)br.ReadByte();
                int index = br.ReadInt32();
                _log.Debug("Received command {Command} for index {Index}", cmd, index);
                switch (cmd)
                {
                    case BrokerCommand.Version:
                        bw.Write(0x00010003u); bw.Flush(); break;
                    case BrokerCommand.Count:
                        bw.Write((uint)Native.GetPadCountFromBus()); bw.Flush(); break;
                    case BrokerCommand.Create:
                        _pads.GetOrAdd(index, i => new Native.PadHandle((uint)i)); bw.Flush(); break;
                    case BrokerCommand.Destroy:
                        if (_pads.TryRemove(index, out var pad)) pad.Dispose(); bw.Flush(); break;
                    case BrokerCommand.SetState:
                    {
                        var st = new Native.VPAD_STATE { Buttons = br.ReadUInt16(), LeftTrigger = br.ReadByte(), RightTrigger = br.ReadByte(),
                            LX = br.ReadInt16(), LY = br.ReadInt16(), RX = br.ReadInt16(), RY = br.ReadInt16() };
                        if (!_pads.TryGetValue(index, out var pad2)) pad2 = _pads.GetOrAdd(index, i => new Native.PadHandle((uint)i));
                        pad2.SetState(st);
                        bw.Flush(); break;
                    }
                    case BrokerCommand.GetRumble:
                    {
                        if (!_pads.TryGetValue(index, out var pad3)) pad3 = _pads.GetOrAdd(index, i => new Native.PadHandle((uint)i));
                        var r = pad3.GetRumble();
                        bw.Write(r.Sequence); bw.Write(r.Left); bw.Write(r.Right); bw.Flush(); break;
                    }
                    case BrokerCommand.SetLeds:
                    {
                        byte r = br.ReadByte(), g = br.ReadByte(), b = br.ReadByte();
                        if (!_pads.TryGetValue(index, out var pad4)) pad4 = _pads.GetOrAdd(index, i => new Native.PadHandle((uint)i));
                        pad4.SetLeds(r,g,b); bw.Flush(); break;
                    }
                    case BrokerCommand.GetLeds:
                    {
                        if (!_pads.TryGetValue(index, out var pad5)) pad5 = _pads.GetOrAdd(index, i => new Native.PadHandle((uint)i));
                        var leds = pad5.GetLeds(); bw.Write(leds.R); bw.Write(leds.G); bw.Write(leds.B); bw.Flush(); break;
                    }
                    case BrokerCommand.PadCountGet:
                        bw.Write(Native.GetPadCountFromBus()); bw.Flush(); break;
                    case BrokerCommand.PadCountSet:
                    {
                        uint n = br.ReadUInt32(); Native.SetPadCountOnBus(n); bw.Flush(); break;
                    }
                    case BrokerCommand.Rescan:
                        Native.RescanBus(); bw.Flush(); break;
                    default: return;
                }
            }
        }
        catch (EndOfStreamException) { }
        catch (Exception ex) { _log.Error(ex, "Exception in HandleClient"); }
    }
}

internal static class Program
{
    private const string PipeName = "VPadBroker";
    public static void Main(string[] args)
    {
        using var logger = new ConsoleVPadLogger();
        logger.Information("VPadBroker starting. Listening on \\ \\\\pipe\\VPadBroker (fake={Fake})", Native.UseFake ? "1" : "0");
        try
        {
            var server = new VPadBrokerServer(logger, new VPadBrokerOptions(PipeName));
            server.Run();
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "VPadBroker terminated unexpectedly");
        }
    }
}
