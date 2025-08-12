using System;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace GaymController.Broker {
    public sealed class GcService : ServiceBase {
        private CancellationTokenSource? _cts;
        protected override void OnStart(string[] args){ _cts=new(); _=RunAsync(_cts.Token); }
        protected override void OnStop(){ _cts?.Cancel(); }
        private async Task RunAsync(CancellationToken ct){
            while(!ct.IsCancellationRequested){
                using var server=new NamedPipeServerStream("GaymBroker", PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
                _=HandleClientAsync(server, ct);
            }
        }
        private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken ct){
            // TODO: wire read loop, HELLO, OPEN, SET_STATE -> driver IOCTL
            await Task.CompletedTask;
        }
    }
}
