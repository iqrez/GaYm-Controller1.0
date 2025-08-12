using System;
using System.Threading;
using System.Threading.Tasks;
using GaymController.Mocks.BrokerWire;
using GaymController.Shared.Contracts;
using Xunit;

public class BrokerWireTests {
    [Fact]
    public async Task Handshake_Open_SetState_Close_Works(){
        string pipe=$"gc_{Guid.NewGuid()}";
        var broker=new MockBroker(pipe);
        var cts=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var serverTask=broker.RunAsync(cts.Token);
        var app=new MockApp(pipe);
        await app.ConnectAsync(cts.Token);
        await app.HelloAsync(cts.Token);
        var handle=await app.OpenAsync(0,cts.Token);
        await app.SetStateAsync(handle, GamepadState.Neutral, cts.Token);
        await app.CloseAsync(handle, cts.Token);
        await app.DisposeAsync();
        cts.Cancel();
        await serverTask;
    }
}
