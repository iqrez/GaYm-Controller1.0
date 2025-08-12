using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace VPad.Broker;

/// <summary>
/// IHostedService wrapper for VPadBrokerServer to integrate with Generic Host.
/// </summary>
public sealed class BrokerHostedService : IHostedService, IDisposable
{
    private readonly VPadBrokerServer _server;
    private readonly CancellationTokenSource _cts = new();
    private Task? _runTask;

    public BrokerHostedService(VPadBrokerServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Run the blocking server on a background Task
        _runTask = Task.Run(() => _server.Run(_cts.Token), cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        if (_runTask is null)
            return;

        try
        {
            await Task.WhenAny(_runTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
