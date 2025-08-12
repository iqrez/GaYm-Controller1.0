using System;
using System.Buffers;
using System.Buffers.Binary;

namespace GaymController.Shared.Contracts {
    public enum MsgType : ushort {
        HELLO=1, HELLO_OK=2, OPEN_CONTROLLER=10, OPEN_OK=11,
        SET_STATE=20, ACK=21, CLOSE_CONTROLLER=30,
        RUMBLE_SUBSCRIBE=40, RUMBLE_EVENT=41, ERROR=255
    }
    public static class Wire {
        public const int HeaderBytes=8; // u32 len, u16 type, u16 flags
        public static byte[] Rent(int size)=>ArrayPool<byte>.Shared.Rent(size);
        public static void Return(byte[] buf)=>ArrayPool<byte>.Shared.Return(buf);
        public static void WriteHeader(Span<byte> dst,uint len,ushort type,ushort flags=0){
            BinaryPrimitives.WriteUInt32LittleEndian(dst,len);
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(4),type);
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(6),flags);
        }
        public static int PackHello(Span<byte> dst){
            const int len=HeaderBytes+4; WriteHeader(dst,len,(ushort)MsgType.HELLO);
            BinaryPrimitives.WriteUInt32LittleEndian(dst.Slice(HeaderBytes),1);
            return len;
        }
        public static int PackHelloOk(Span<byte> dst,uint caps){
            const int len=HeaderBytes+4; WriteHeader(dst,len,(ushort)MsgType.HELLO_OK);
            BinaryPrimitives.WriteUInt32LittleEndian(dst.Slice(HeaderBytes),caps);
            return len;
        }
        public static int PackOpenController(Span<byte> dst,uint slot){
            const int len=HeaderBytes+4; WriteHeader(dst,len,(ushort)MsgType.OPEN_CONTROLLER);
            BinaryPrimitives.WriteUInt32LittleEndian(dst.Slice(HeaderBytes),slot);
            return len;
        }
        public static int PackOpenOk(Span<byte> dst,ulong handle){
            const int len=HeaderBytes+8; WriteHeader(dst,len,(ushort)MsgType.OPEN_OK);
            BinaryPrimitives.WriteUInt64LittleEndian(dst.Slice(HeaderBytes),handle);
            return len;
        }
        public static int PackSetState(Span<byte> dst,ulong handle,GamepadState state){
            const int len=HeaderBytes+24; WriteHeader(dst,len,(ushort)MsgType.SET_STATE);
            var o=HeaderBytes;
            BinaryPrimitives.WriteUInt64LittleEndian(dst.Slice(o),handle); o+=8;
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(o),state.LX); o+=2;
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(o),state.LY); o+=2;
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(o),state.RX); o+=2;
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(o),state.RY); o+=2;
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(o),state.LT); o+=2;
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(o),state.RT); o+=2;
            BinaryPrimitives.WriteUInt32LittleEndian(dst.Slice(o),state.Buttons);
            return len;
        }
        public static int PackAck(Span<byte> dst,uint echoType){
            const int len=HeaderBytes+4; WriteHeader(dst,len,(ushort)MsgType.ACK);
            BinaryPrimitives.WriteUInt32LittleEndian(dst.Slice(HeaderBytes),echoType);
            return len;
        }
        public static int PackCloseController(Span<byte> dst,ulong handle){
            const int len=HeaderBytes+8; WriteHeader(dst,len,(ushort)MsgType.CLOSE_CONTROLLER);
            BinaryPrimitives.WriteUInt64LittleEndian(dst.Slice(HeaderBytes),handle);
            return len;
        }
        public static int PackRumbleSubscribe(Span<byte> dst,ulong handle){
            const int len=HeaderBytes+8; WriteHeader(dst,len,(ushort)MsgType.RUMBLE_SUBSCRIBE);
            BinaryPrimitives.WriteUInt64LittleEndian(dst.Slice(HeaderBytes),handle);
            return len;
        }
        public static int PackRumbleEvent(Span<byte> dst,ulong handle,ushort low,ushort high){
            const int len=HeaderBytes+12; WriteHeader(dst,len,(ushort)MsgType.RUMBLE_EVENT);
            var o=HeaderBytes;
            BinaryPrimitives.WriteUInt64LittleEndian(dst.Slice(o),handle); o+=8;
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(o),low); o+=2;
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(o),high);
            return len;
        }
        public static int PackError(Span<byte> dst,uint code,uint detail){
            const int len=HeaderBytes+8; WriteHeader(dst,len,(ushort)MsgType.ERROR);
            var o=HeaderBytes;
            BinaryPrimitives.WriteUInt32LittleEndian(dst.Slice(o),code); o+=4;
            BinaryPrimitives.WriteUInt32LittleEndian(dst.Slice(o),detail);
            return len;
        }
    }
}
