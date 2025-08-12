using System;
using System.Buffers.Binary;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GaymController.Broker;
using GaymController.Shared.Contracts;
using Xunit;

public class BrokerSessionManagerTests {
    [Fact]
    public async Task HelloOpenSetStateFlow(){
        string name = "gc_test_" + Guid.NewGuid().ToString("N");
        using var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var wait = server.WaitForConnectionAsync();
        using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync();
        await wait;
        var session = new SessionManager(server);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var runTask = session.RunAsync(cts.Token);
        var buf = new byte[Wire.HeaderBytes + 8];
        // HELLO
        Wire.WriteHeader(buf,4,1);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes),1);
        await client.WriteAsync(buf.AsMemory(0,Wire.HeaderBytes+4),cts.Token);
        await client.FlushAsync(cts.Token);
        await ReadExactAsync(client, buf.AsMemory(0,Wire.HeaderBytes+4), cts.Token);
        Assert.Equal((ushort)2, BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(4)));
        // OPEN_CONTROLLER
        Wire.WriteHeader(buf,4,10);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes),0);
        await client.WriteAsync(buf.AsMemory(0,Wire.HeaderBytes+4),cts.Token);
        await client.FlushAsync(cts.Token);
        await ReadExactAsync(client, buf.AsMemory(0,Wire.HeaderBytes+8), cts.Token);
        Assert.Equal((ushort)11, BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(4)));
        ulong handle = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(Wire.HeaderBytes));
        // SET_STATE
        var state = GamepadState.Neutral;
        int sz = Marshal.SizeOf<GamepadState>();
        var buf2 = new byte[Wire.HeaderBytes + 8 + sz];
        Wire.WriteHeader(buf2,(uint)(8+sz),20);
        BinaryPrimitives.WriteUInt64LittleEndian(buf2.AsSpan(Wire.HeaderBytes),handle);
        MemoryMarshal.Write(buf2.AsSpan(Wire.HeaderBytes+8), ref state);
        await client.WriteAsync(buf2, cts.Token);
        await client.FlushAsync(cts.Token);
        await ReadExactAsync(client, buf.AsMemory(0,Wire.HeaderBytes+4), cts.Token);
        Assert.Equal((ushort)21, BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(4)));
        Assert.Equal(20u, BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes)));
        client.Dispose();
        await runTask;
    }
    private static async Task ReadExactAsync(PipeStream pipe, Memory<byte> mem, CancellationToken ct){
        int offset=0;
        while(offset<mem.Length){
            int n = await pipe.ReadAsync(mem.Slice(offset), ct);
            if(n==0) throw new InvalidOperationException("eof");
            offset += n;
        }
    }
}
