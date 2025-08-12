using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GaymController.Shared.Contracts;

namespace GaymController.Mocks.BrokerWire {
    public readonly struct WireFrame : IDisposable {
        public ushort Type { get; }
        public byte[] Buffer { get; }
        public int PayloadLength { get; }
        public WireFrame(ushort type, byte[] buffer, int payloadLength){ Type=type; Buffer=buffer; PayloadLength=payloadLength; }
        public ReadOnlySpan<byte> Payload => Buffer.AsSpan(0, PayloadLength);
        public void Dispose()=>Wire.Return(Buffer);
    }
    public static class WireIO {
        static async Task ReadExactAsync(Stream s, Memory<byte> buf, CancellationToken ct){
            int off=0; while(off<buf.Length){ int r=await s.ReadAsync(buf.Slice(off), ct).ConfigureAwait(false); if(r==0) throw new EndOfStreamException(); off+=r; }
        }
        public static async Task<WireFrame> ReadFrameAsync(Stream s, CancellationToken ct){
            byte[] header=new byte[Wire.HeaderBytes];
            await ReadExactAsync(s, header, ct).ConfigureAwait(false);
            uint len=BinaryPrimitives.ReadUInt32LittleEndian(header);
            ushort type=BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(4));
            int payloadLen=(int)len-Wire.HeaderBytes;
            var buf=Wire.Rent(payloadLen);
            await ReadExactAsync(s, buf.AsMemory(0,payloadLen), ct).ConfigureAwait(false);
            return new WireFrame(type, buf, payloadLen);
        }
        public static void WriteFrame(Stream s, ushort type, ReadOnlySpan<byte> payload){
            int len=Wire.HeaderBytes+payload.Length;
            var buf=Wire.Rent(len);
            Wire.WriteHeader(buf, (uint)len, type);
            payload.CopyTo(buf.AsSpan(Wire.HeaderBytes));
            s.Write(buf,0,len);
            s.Flush();
            Wire.Return(buf);
        }
    }
}
