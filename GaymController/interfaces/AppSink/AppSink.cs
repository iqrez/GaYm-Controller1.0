using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GaymController.Shared.Contracts;

namespace GaymController.Interfaces;

/// <summary>
/// Minimal client used by the app to send controller state to the broker.
/// </summary>
public sealed class AppSink : IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly byte[] _stateBuf;
    private ulong _handle;

    private const int StatePayloadBytes = 8 + 16; // handle + GamepadState

    public AppSink(Stream stream)
    {
        _stream = stream;
        _stateBuf = Wire.Rent(Wire.HeaderBytes + StatePayloadBytes);
        Wire.WriteHeader(_stateBuf, StatePayloadBytes, 20); // SET_STATE frame
    }

    public async Task HandshakeAsync(CancellationToken ct = default)
    {
        byte[] buf = Wire.Rent(Wire.HeaderBytes + 4);
        try
        {
            Wire.WriteHeader(buf, 4, 1); // HELLO
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes), 1);
            await _stream.WriteAsync(buf.AsMemory(0, Wire.HeaderBytes + 4), ct).ConfigureAwait(false);

            await _stream.ReadExactlyAsync(buf.AsMemory(0, Wire.HeaderBytes + 4), ct).ConfigureAwait(false);
        }
        finally { Wire.Return(buf); }
    }

    public async Task OpenAsync(uint slot, CancellationToken ct = default)
    {
        byte[] buf = Wire.Rent(Wire.HeaderBytes + 4);
        try
        {
            Wire.WriteHeader(buf, 4, 10); // OPEN_CONTROLLER
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes), slot);
            await _stream.WriteAsync(buf.AsMemory(0, Wire.HeaderBytes + 4), ct).ConfigureAwait(false);

            await _stream.ReadExactlyAsync(buf.AsMemory(0, Wire.HeaderBytes + 8), ct).ConfigureAwait(false);
            _handle = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(Wire.HeaderBytes));
        }
        finally { Wire.Return(buf); }
    }

    public async ValueTask SetStateAsync(GamepadState state, CancellationToken ct = default)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(_stateBuf.AsSpan(Wire.HeaderBytes), _handle);
        MemoryMarshal.Write(_stateBuf.AsSpan(Wire.HeaderBytes + 8), ref state);
        await _stream.WriteAsync(_stateBuf.AsMemory(0, Wire.HeaderBytes + StatePayloadBytes), ct).ConfigureAwait(false);
        await _stream.ReadExactlyAsync(_stateBuf.AsMemory(0, Wire.HeaderBytes + 4), ct).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        Wire.Return(_stateBuf);
        _stream.Dispose();
        return ValueTask.CompletedTask;
    }
}
