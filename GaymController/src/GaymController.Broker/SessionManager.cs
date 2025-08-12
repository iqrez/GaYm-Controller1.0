using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using GaymController.Shared.Contracts;

namespace GaymController.Broker {
    public sealed class SessionManager {
        private readonly NamedPipeServerStream _pipe;
        private readonly Dictionary<ulong, uint> _handles = new();
        private ulong _nextHandle = 1;
        public SessionManager(NamedPipeServerStream pipe){ _pipe=pipe; }
        public static Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken ct) => new SessionManager(pipe).RunAsync(ct);
        public async Task RunAsync(CancellationToken ct){
            var buf = Wire.Rent(Wire.HeaderBytes + 65536);
            try{
                if(!await ReadFrameAsync(buf, ct)) return;
                var type = BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(4));
                if(type!=1){ await SendErrorAsync(buf,1,0,ct); return; }
                uint proto = BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes));
                if(proto!=1){ await SendErrorAsync(buf,1,0,ct); return; }
                Wire.WriteHeader(buf,4,2);
                BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes),0);
                await _pipe.WriteAsync(buf.AsMemory(0,Wire.HeaderBytes+4),ct);
                while(_pipe.IsConnected && !ct.IsCancellationRequested){
                    if(!await ReadFrameAsync(buf, ct)) break;
                    await ProcessAsync(buf, ct);
                }
            } finally { Wire.Return(buf); }
        }
        private async Task ProcessAsync(byte[] buf, CancellationToken ct){
            uint len = BinaryPrimitives.ReadUInt32LittleEndian(buf);
            ushort type = BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(4));
            switch(type){
                case 10: {
                    uint slot = BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes));
                    ulong handle = _nextHandle++;
                    _handles[handle]=slot;
                    Wire.WriteHeader(buf,8,11);
                    BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(Wire.HeaderBytes),handle);
                    await _pipe.WriteAsync(buf.AsMemory(0,Wire.HeaderBytes+8),ct);
                    break;
                }
                case 20: {
                    ulong h = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(Wire.HeaderBytes));
                    if(!_handles.ContainsKey(h)){ await SendErrorAsync(buf,2,20,ct); break; }
                    Wire.WriteHeader(buf,4,21);
                    BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes),20);
                    await _pipe.WriteAsync(buf.AsMemory(0,Wire.HeaderBytes+4),ct);
                    break;
                }
                case 30: {
                    ulong h = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(Wire.HeaderBytes));
                    _handles.Remove(h);
                    Wire.WriteHeader(buf,4,21);
                    BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes),30);
                    await _pipe.WriteAsync(buf.AsMemory(0,Wire.HeaderBytes+4),ct);
                    break;
                }
                default:
                    await SendErrorAsync(buf,3,type,ct);
                    break;
            }
        }
        private async Task SendErrorAsync(byte[] buf, uint code, ushort detail, CancellationToken ct){
            Wire.WriteHeader(buf,8,255);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes),code);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(Wire.HeaderBytes+4),detail);
            await _pipe.WriteAsync(buf.AsMemory(0,Wire.HeaderBytes+8),ct);
        }
        private async Task<bool> ReadFrameAsync(byte[] buf, CancellationToken ct){
            if(!await ReadExactAsync(buf.AsMemory(0,Wire.HeaderBytes), ct)) return false;
            uint len = BinaryPrimitives.ReadUInt32LittleEndian(buf);
            if(len>65536) return false;
            return await ReadExactAsync(buf.AsMemory(Wire.HeaderBytes,(int)len), ct);
        }
        private async Task<bool> ReadExactAsync(Memory<byte> mem, CancellationToken ct){
            int off=0;
            while(off<mem.Length){
                int n=await _pipe.ReadAsync(mem.Slice(off), ct);
                if(n==0) return false;
                off+=n;
            }
            return true;
        }
    }
}
