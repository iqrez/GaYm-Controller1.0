using System;
using System.Buffers.Binary;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GaymController.Shared.Contracts;

namespace GaymController.Mocks.BrokerWire {
    public sealed class MockApp : IAsyncDisposable {
        readonly NamedPipeClientStream _pipe;
        public MockApp(string pipe){ _pipe=new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous); }
        public Task ConnectAsync(CancellationToken ct)=>_pipe.ConnectAsync(ct);
        public async Task HelloAsync(CancellationToken ct){
            SendHello(_pipe);
            using var frame=await WireIO.ReadFrameAsync(_pipe,ct).ConfigureAwait(false);
            if(frame.Type!=2) throw new InvalidOperationException();
        }
        public async Task<ulong> OpenAsync(uint slot, CancellationToken ct){
            SendOpen(_pipe, slot);
            using var frame=await WireIO.ReadFrameAsync(_pipe,ct).ConfigureAwait(false);
            if(frame.Type!=11) throw new InvalidOperationException();
            ulong handle=BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload);
            return handle;
        }
        public async Task SetStateAsync(ulong handle, GamepadState state, CancellationToken ct){
            SendSetState(_pipe, handle, state);
            using var frame=await WireIO.ReadFrameAsync(_pipe,ct).ConfigureAwait(false);
            if(frame.Type!=21) throw new InvalidOperationException();
            uint echo=BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload);
            if(echo!=20) throw new InvalidOperationException();
        }
        public async Task CloseAsync(ulong handle, CancellationToken ct){
            SendClose(_pipe, handle);
            using var frame=await WireIO.ReadFrameAsync(_pipe,ct).ConfigureAwait(false);
            if(frame.Type!=21) throw new InvalidOperationException();
        }
        static void SendHello(PipeStream pipe){
            Span<byte> payload=stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(payload,1);
            WireIO.WriteFrame(pipe,1,payload);
        }
        static void SendOpen(PipeStream pipe, uint slot){
            Span<byte> payload=stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(payload,slot);
            WireIO.WriteFrame(pipe,10,payload);
        }
        static void SendSetState(PipeStream pipe, ulong handle, GamepadState state){
            Span<byte> payload=stackalloc byte[8+16];
            BinaryPrimitives.WriteUInt64LittleEndian(payload,handle);
            MemoryMarshal.Write(payload[8..], ref state);
            WireIO.WriteFrame(pipe,20,payload);
        }
        static void SendClose(PipeStream pipe, ulong handle){
            Span<byte> payload=stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(payload,handle);
            WireIO.WriteFrame(pipe,30,payload);
        }
        public ValueTask DisposeAsync(){ _pipe.Dispose(); return ValueTask.CompletedTask; }
    }
}
