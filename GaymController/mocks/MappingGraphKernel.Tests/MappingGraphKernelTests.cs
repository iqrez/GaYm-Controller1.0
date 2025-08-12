using GaymController.Mocks.Mapping;
using GaymController.Shared.Mapping;
using Xunit;

class DummyNode : INode {
    public string Id { get; }
    public int EventCount;
    public double LastValue;
    public double TickSum;
    public DummyNode(string id){ Id=id; }
    public void OnEvent(InputEvent e){ EventCount++; LastValue=e.Value; }
    public void OnTick(double dtMs){ TickSum+=dtMs; }
}

public class MappingGraphKernelTests {
    [Fact]
    public void DispatchHitsConnectedNodes() {
        var kernel = new MappingGraphKernel();
        var a = new DummyNode("A");
        var b = new DummyNode("B");
        kernel.AddNode(a); kernel.AddNode(b); kernel.Connect("A","B");
        kernel.Dispatch("A", new InputEvent("src", 1.0, 0));
        Assert.Equal(1, a.EventCount);
        Assert.Equal(1, b.EventCount);
    }

    [Fact]
    public void TickPropagatesToAllNodes(){
        var kernel = new MappingGraphKernel();
        var a = new DummyNode("A");
        var b = new DummyNode("B");
        kernel.AddNode(a); kernel.AddNode(b);
        kernel.Tick(5.0);
        Assert.Equal(5.0, a.TickSum);
        Assert.Equal(5.0, b.TickSum);
    }
}
