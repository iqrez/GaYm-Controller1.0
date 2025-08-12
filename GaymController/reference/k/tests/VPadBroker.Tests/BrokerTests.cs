using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Xunit;

enum BrokerCommand : byte
{
    Version=1, Count=2, Create=3, Destroy=4, SetState=5, GetRumble=6, SetLeds=7, GetLeds=8,
    PadCountGet=20, PadCountSet=21, Rescan=22
}

public class BrokerTests : IDisposable
{
    private readonly Process _proc;

    private static string FindSolutionRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "InHouse-VirtualPad.sln"))) return dir;
            var parent = Directory.GetParent(dir);
            dir = parent?.FullName ?? string.Empty;
        }
        throw new InvalidOperationException("Solution root not found");
    }

    public BrokerTests()
    {
        _proc = new Process();
        string root = FindSolutionRoot();
        string dllPath = Path.Combine(root, "src", "service", "VPadBroker", "bin", "x64", "Debug", "net8.0-windows", "VPadBroker.dll");
        _proc.StartInfo.FileName = "dotnet";
        _proc.StartInfo.Arguments = '"' + dllPath + '"';
        _proc.StartInfo.UseShellExecute = false;
        _proc.StartInfo.CreateNoWindow = true;
        _proc.StartInfo.Environment["VPAD_FAKE"] = "1";
        _proc.Start();
        Thread.Sleep(300); // give it time to listen
    }

    public void Dispose()
    {
        try { if (!_proc.HasExited) _proc.Kill(true); } catch {}
        _proc.Dispose();
    }

    private static (BinaryReader br, BinaryWriter bw, NamedPipeClientStream pipe) Connect()
    {
        var pipe = new NamedPipeClientStream(".", "VPadBroker", PipeDirection.InOut);
        pipe.Connect(3000);
        return (new BinaryReader(pipe), new BinaryWriter(pipe), pipe);
    }

    [Fact]
    public void Version_Is_Expected()
    {
        var (br,bw,pipe) = Connect();
        using (br) using (bw) using (pipe)
        {
            bw.Write((byte)BrokerCommand.Version); bw.Write(0); bw.Flush();
            Assert.Equal((uint)0x00010003, br.ReadUInt32());
        }
    }

    [Fact]
    public void Count_And_Pad_Ops_Work_In_Fake_Mode()
    {
        var (br,bw,pipe) = Connect();
        using (br) using (bw) using (pipe)
        {
            bw.Write((byte)BrokerCommand.Count); bw.Write(0); bw.Flush();
            uint count = br.ReadUInt32();
            Assert.True(count >= 1);

            // Create pad 0 and set state
            bw.Write((byte)BrokerCommand.Create); bw.Write(0); bw.Flush();

            bw.Write((byte)BrokerCommand.SetState); bw.Write(0);
            bw.Write((ushort)3); bw.Write((byte)10); bw.Write((byte)20);
            bw.Write((short)100); bw.Write((short)-100); bw.Write((short)0); bw.Write((short)0);
            bw.Flush();

            // Rumble echoes triggers in fake mode
            bw.Write((byte)BrokerCommand.GetRumble); bw.Write(0); bw.Flush();
            uint seq = br.ReadUInt32(); byte l = br.ReadByte(); byte r = br.ReadByte();
            Assert.True(seq > 0); Assert.Equal((byte)10, l); Assert.Equal((byte)20, r);

            // LEDs set/get
            bw.Write((byte)BrokerCommand.SetLeds); bw.Write(0); bw.Write((byte)1); bw.Write((byte)2); bw.Write((byte)3); bw.Flush();
            bw.Write((byte)BrokerCommand.GetLeds); bw.Write(0); bw.Flush();
            Assert.Equal((byte)1, br.ReadByte()); Assert.Equal((byte)2, br.ReadByte()); Assert.Equal((byte)3, br.ReadByte());

            // Destroy
            bw.Write((byte)BrokerCommand.Destroy); bw.Write(0); bw.Flush();
        }
    }
}
