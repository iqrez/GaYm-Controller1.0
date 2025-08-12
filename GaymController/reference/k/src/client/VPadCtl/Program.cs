
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

enum BrokerCommand : byte
{
    Version=1, Count=2, Create=3, Destroy=4, SetState=5, GetRumble=6, SetLeds=7, GetLeds=8,
    PadCountGet=20, PadCountSet=21, Rescan=22
}

class Program
{
    static void SendHeader(BinaryWriter bw, BrokerCommand cmd, int index) { bw.Write((byte)cmd); bw.Write(index); }
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:\n VPadCtl version\n VPadCtl count\n VPadCtl create <index>\n VPadCtl destroy <index>\n VPadCtl set <index> <buttons> <lt> <rt> <lx> <ly> <rx> <ry>\n VPadCtl rumble <index>\n VPadCtl leds <index> <r> <g> <b>\n VPadCtl ledsget <index>\n VPadCtl padcount get\n VPadCtl padcount set <n>\n VPadCtl rescan\n VPadCtl demo <index> <seconds>");
            return 1;
        }
        using var client = new NamedPipeClientStream(".", "VPadBroker", PipeDirection.InOut);
        client.Connect(3000);
        using var br = new BinaryReader(client);
        using var bw = new BinaryWriter(client);

        switch (args[0].ToLowerInvariant())
        {
            case "version":
                SendHeader(bw, BrokerCommand.Version, 0); bw.Flush();
                Console.WriteLine($"Version: {br.ReadUInt32()}"); return 0;
            case "count":
                SendHeader(bw, BrokerCommand.Count, 0); bw.Flush();
                Console.WriteLine($"Pads: {br.ReadUInt32()}"); return 0;
            case "create":
                SendHeader(bw, BrokerCommand.Create, int.Parse(args[1])); bw.Flush(); Console.WriteLine("Created."); return 0;
            case "destroy":
                SendHeader(bw, BrokerCommand.Destroy, int.Parse(args[1])); bw.Flush(); Console.WriteLine("Destroyed."); return 0;
            case "set":
            {
                int i=1; int idx=int.Parse(args[i++]);
                ushort buttons=ushort.Parse(args[i++]); byte lt=byte.Parse(args[i++]); byte rt=byte.Parse(args[i++]);
                short lx=short.Parse(args[i++]); short ly=short.Parse(args[i++]); short rx=short.Parse(args[i++]); short ry=short.Parse(args[i++]);
                SendHeader(bw, BrokerCommand.SetState, idx);
                bw.Write(buttons); bw.Write(lt); bw.Write(rt); bw.Write(lx); bw.Write(ly); bw.Write(rx); bw.Write(ry);
                bw.Flush(); Console.WriteLine("State set."); return 0;
            }
            case "rumble":
                SendHeader(bw, BrokerCommand.GetRumble, int.Parse(args[1])); bw.Flush();
                Console.WriteLine($"Rumble seq={br.ReadUInt32()} L={br.ReadByte()} R={br.ReadByte()}"); return 0;
            case "leds":
            {
                int idx=int.Parse(args[1]); byte r=byte.Parse(args[2]); byte g=byte.Parse(args[3]); byte b=byte.Parse(args[4]);
                SendHeader(bw, BrokerCommand.SetLeds, idx); bw.Write(r); bw.Write(g); bw.Write(b); bw.Flush(); Console.WriteLine("LEDs set."); return 0;
            }
            case "ledsget":
            {
                int idx=int.Parse(args[1]);
                SendHeader(bw, BrokerCommand.GetLeds, idx); bw.Flush();
                Console.WriteLine($"LEDs R={br.ReadByte()} G={br.ReadByte()} B={br.ReadByte()}"); return 0;
            }
            case "padcount":
            {
                if (args[1].ToLowerInvariant()=="get")
                {
                    SendHeader(bw, BrokerCommand.PadCountGet, 0); bw.Flush();
                    Console.WriteLine($"PadCount: {br.ReadUInt32()}"); return 0;
                }
                if (args[1].ToLowerInvariant()=="set")
                {
                    uint n = uint.Parse(args[2]);
                    SendHeader(bw, BrokerCommand.PadCountSet, 0); bw.Write(n); bw.Flush(); Console.WriteLine("PadCount set."); return 0;
                }
                Console.WriteLine("Usage: VPadCtl padcount get|set <n>"); return 1;
            }
            case "rescan":
                SendHeader(bw, BrokerCommand.Rescan, 0); bw.Flush(); Console.WriteLine("Rescan requested."); return 0;
            case "demo":
            {
                int idx=int.Parse(args[1]); int seconds=int.Parse(args[2]);
                SendHeader(bw, BrokerCommand.Create, idx); bw.Flush();
                long end = Environment.TickCount64 + seconds*1000L; uint lastSeq = 0;
                while (Environment.TickCount64 < end)
                {
                    double t = (Environment.TickCount64 % 100000)/1000.0;
                    short lx = (short)(Math.Sin(t*2.0)*15000);
                    short ly = (short)(Math.Cos(t*2.0)*15000);
                    SendHeader(bw, BrokerCommand.SetState, idx);
                    bw.Write((ushort)((t%1.0)<0.5 ? 1:2)); bw.Write((byte)(Math.Abs(Math.Sin(t))*255)); bw.Write((byte)(Math.Abs(Math.Cos(t))*255));
                    bw.Write(lx); bw.Write(ly); bw.Write((short)0); bw.Write((short)0); bw.Flush();
                    SendHeader(bw, BrokerCommand.GetRumble, idx); bw.Flush();
                    uint seq = br.ReadUInt32(); byte l = br.ReadByte(); byte r = br.ReadByte();
                    if (seq != lastSeq) { lastSeq = seq; Console.WriteLine($"Rumble: L={l} R={r}"); }
                    Thread.Sleep(10);
                }
                SendHeader(bw, BrokerCommand.Destroy, idx); bw.Flush(); return 0;
            }
        }
        return 1;
    }
}
