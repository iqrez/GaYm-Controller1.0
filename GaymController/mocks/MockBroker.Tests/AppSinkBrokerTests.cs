using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using GaymController.Interfaces;
using GaymController.Mocks;
using GaymController.Shared.Contracts;
using Xunit;

public class AppSinkBrokerTests
{
    [Fact]
    public async Task State_Flow_Reaches_MockBroker()
    {
        string pipe = "gc_test_" + Guid.NewGuid().ToString("N");
        await using var broker = new MockBroker(pipe);
        var serverTask = broker.StartAsync();

        using var client = new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(2000);
        await serverTask; // ensure server loop is running

        await using var sink = new AppSink(client);
        await sink.HandshakeAsync();
        await sink.OpenAsync(0);

        var state = new GamepadState { LX=1, LY=2, RX=3, RY=4, LT=5, RT=6, Buttons=7 };
        await sink.SetStateAsync(state);

        Assert.Equal(state, broker.LastState);
    }
}
