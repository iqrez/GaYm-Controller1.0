using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GaymController.Shared.Contracts;

namespace GaymController.Mocks;

/// <summary>
/// Minimal broker emulator for integration tests.
/// </summary>
public sealed class MockBroker : IAsyncDisposable
{
    private readonly NamedPipeServerStream _server;
    private readonly CancellationTokenSource _cts = new();
    public GamepadState LastState;

    public MockBroker(string pipeName)
    {
        _server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
    }

    public async Task StartAsync()
    {
        await _server.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);
        _ = Task.Run(() => RunAsync(_cts.Token));
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var header = new byte[Wire.HeaderBytes];
        while (!ct.IsCancellationRequested)
        {
            await _server.ReadExactlyAsync(header, ct).ConfigureAwait(false);
            uint len = BinaryPrimitives.ReadUInt32LittleEndian(header);
            ushort type = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(4));
            var payload = new byte[len];
            await _server.ReadExactlyAsync(payload, ct).ConfigureAwait(false);

            switch (type)
            {
                case 1: // HELLO
                {
                    var res = new byte[Wire.HeaderBytes + 4];
                    Wire.WriteHeader(res, 4, 2); // HELLO_OK
                    BinaryPrimitives.WriteUInt32LittleEndian(res.AsSpan(Wire.HeaderBytes), 1);
                    await _server.WriteAsync(res, ct).ConfigureAwait(false);
                    break;
                }
                case 10: // OPEN_CONTROLLER
                {
                    var res = new byte[Wire.HeaderBytes + 8];
                    Wire.WriteHeader(res, 8, 11); // OPEN_OK
                    BinaryPrimitives.WriteUInt64LittleEndian(res.AsSpan(Wire.HeaderBytes), 1);
                    await _server.WriteAsync(res, ct).ConfigureAwait(false);
                    break;
                }
                case 20: // SET_STATE
                {
                    LastState = MemoryMarshal.Read<GamepadState>(payload.AsSpan(8));
                    var ack = new byte[Wire.HeaderBytes + 4];
                    Wire.WriteHeader(ack, 4, 21); // ACK
                    BinaryPrimitives.WriteUInt32LittleEndian(ack.AsSpan(Wire.HeaderBytes), 20);
                    await _server.WriteAsync(ack, ct).ConfigureAwait(false);
                    break;
                }
                default:
                    return;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _server.DisposeAsync();
        _cts.Dispose();
    }
}
