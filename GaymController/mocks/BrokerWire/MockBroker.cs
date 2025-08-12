using System;
using System.Buffers.Binary;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using GaymController.Shared.Contracts;

namespace GaymController.Mocks.BrokerWire {
    public sealed class MockBroker {
        readonly string _pipe;
        public MockBroker(string pipeName){ _pipe=pipeName; }
        public async Task RunAsync(CancellationToken ct){
            using var server=new NamedPipeServerStream(_pipe, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
            await HandleClientAsync(server, ct).ConfigureAwait(false);
        }
        static async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken ct){
            bool running=true;
            while(running && pipe.IsConnected){
                using var frame=await WireIO.ReadFrameAsync(pipe, ct).ConfigureAwait(false);
                switch(frame.Type){
                    case 1:
                        ReplyHello(pipe);
                        break;
                    case 10:
                        uint slot=BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload);
                        ReplyOpen(pipe, slot);
                        break;
                    case 20:
                        ReplyAck(pipe,20);
                        break;
                    case 30:
                        ReplyAck(pipe,30);
                        running=false;
                        break;
                    default:
                        ReplyError(pipe,frame.Type);
                        running=false;
                        break;
                }
            }
        }
        static void ReplyHello(PipeStream pipe){ Span<byte> resp=stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(resp,0); WireIO.WriteFrame(pipe,2,resp); }
        static void ReplyOpen(PipeStream pipe,uint slot){ Span<byte> resp=stackalloc byte[8]; ulong handle=slot+1; BinaryPrimitives.WriteUInt64LittleEndian(resp,handle); WireIO.WriteFrame(pipe,11,resp); }
        static void ReplyAck(PipeStream pipe,uint echo){ Span<byte> resp=stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(resp,echo); WireIO.WriteFrame(pipe,21,resp); }
        static void ReplyError(PipeStream pipe,ushort type){ Span<byte> resp=stackalloc byte[8]; BinaryPrimitives.WriteUInt32LittleEndian(resp,1); BinaryPrimitives.WriteUInt32LittleEndian(resp[4..],type); WireIO.WriteFrame(pipe,255,resp); }
    }
}
