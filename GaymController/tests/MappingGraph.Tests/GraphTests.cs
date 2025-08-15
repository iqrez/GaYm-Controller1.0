using System;
using GaymController.Shared.Mapping;
using Xunit;

namespace MappingGraph.Tests;

public class GraphTests
{
    private sealed class TestNode : INode
    {
        public string Id { get; }
        public int EventCount { get; private set; }
        public int TickCount { get; private set; }
        public TestNode(string id) => Id = id;
        public void OnEvent(InputEvent e) { EventCount++; }
        public void OnTick(double dtMs) { TickCount++; }
    }

    [Fact]
    public void EventPropagatesThroughConnections()
    {
        var a = new TestNode("a");
        var b = new TestNode("b");
        var c = new TestNode("c");
        var g = new GraphBuilder()
            .AddNode(a)
            .AddNode(b)
            .AddNode(c)
            .Connect("a", "b")
            .Connect("b", "c")
            .Build();
        g.OnEvent("a", new InputEvent("src", 1.0, 0));
        Assert.Equal(1, a.EventCount);
        Assert.Equal(1, b.EventCount);
        Assert.Equal(1, c.EventCount);
    }

    [Fact]
    public void TickVisitsAllNodes()
    {
        var a = new TestNode("a");
        var b = new TestNode("b");
        var g = new GraphBuilder()
            .AddNode(a)
            .AddNode(b)
            .Build();
        g.Tick(16.0);
        Assert.Equal(1, a.TickCount);
        Assert.Equal(1, b.TickCount);
    }
}
