using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.IO.Pipes;

namespace GaymController.Shared.Contracts {
    public static class Wire {
        public const int HeaderBytes = 8; // u32 len, u16 type, u16 flags
        public static byte[] Rent(int size) => ArrayPool<byte>.Shared.Rent(size);
        public static void Return(byte[] buf) => ArrayPool<byte>.Shared.Return(buf);
        public static void WriteHeader(Span<byte> dst, uint len, ushort type, ushort flags=0) {
            BinaryPrimitives.WriteUInt32LittleEndian(dst, len);
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(4), type);
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(6), flags);
        }
    }
}
