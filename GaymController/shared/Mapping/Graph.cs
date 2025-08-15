using System;
using System.Collections.Generic;

namespace GaymController.Shared.Mapping;

public sealed class Graph
{
    private readonly INode[] _nodes;
    private readonly int[] _edgeIndex;
    private readonly int[] _edges;
    private readonly Dictionary<string, int> _idToIndex;
    private readonly int[] _stack;

    internal Graph(INode[] nodes, int[] edgeIndex, int[] edges, Dictionary<string, int> idMap)
    {
        _nodes = nodes;
        _edgeIndex = edgeIndex;
        _edges = edges;
        _idToIndex = idMap;
        _stack = new int[Math.Max(nodes.Length, edges.Length)];
    }

    public void OnEvent(string id, InputEvent e)
    {
        if (!_idToIndex.TryGetValue(id, out var idx)) return;
        var stack = _stack; var sp = 0;
        stack[sp++] = idx;
        while (sp > 0)
        {
            var i = stack[--sp];
            _nodes[i].OnEvent(e);
            var start = _edgeIndex[i];
            var end = _edgeIndex[i + 1];
            for (int k = start; k < end; k++)
            {
                stack[sp++] = _edges[k];
            }
        }
    }

    public void Tick(double dtMs)
    {
        var nodes = _nodes;
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].OnTick(dtMs);
    }
}

public sealed class GraphBuilder
{
    private readonly List<INode> _nodes = new();
    private readonly Dictionary<string, int> _idToIndex = new(StringComparer.Ordinal);
    private readonly List<(int from, int to)> _edges = new();

    public GraphBuilder AddNode(INode node)
    {
        if (!_idToIndex.TryAdd(node.Id, _nodes.Count))
            throw new ArgumentException("duplicate node id");
        _nodes.Add(node);
        return this;
    }

    public GraphBuilder Connect(string fromId, string toId)
    {
        if (!_idToIndex.TryGetValue(fromId, out var f))
            throw new ArgumentException("unknown fromId");
        if (!_idToIndex.TryGetValue(toId, out var t))
            throw new ArgumentException("unknown toId");
        _edges.Add((f, t));
        return this;
    }

    public Graph Build()
    {
        var nodes = _nodes.ToArray();
        int n = nodes.Length;
        var edgeIndex = new int[n + 1];
        foreach (var e in _edges)
            edgeIndex[e.from + 1]++;
        for (int i = 0; i < n; i++)
            edgeIndex[i + 1] += edgeIndex[i];
        var adj = new int[_edges.Count];
        var temp = new int[n];
        foreach (var e in _edges)
        {
            var pos = edgeIndex[e.from] + temp[e.from]++;
            adj[pos] = e.to;
        }
        var idMap = new Dictionary<string, int>(_idToIndex, StringComparer.Ordinal);
        return new Graph(nodes, edgeIndex, adj, idMap);
    }
}
