using System.Collections.Generic;
using GaymController.Shared.Mapping;
using GaymController.Interfaces.Mapping;

namespace GaymController.Mocks.Mapping {
    /// <summary>
    /// Simple in-memory mapping graph kernel used for testing. Hot paths avoid
    /// allocations by reusing collections.
    /// </summary>
    public sealed class MappingGraphKernel : IMappingGraphKernel {
        private readonly Dictionary<string, INode> _nodes = new();
        private readonly Dictionary<string, List<INode>> _edges = new();

        public void AddNode(INode node) {
            _nodes[node.Id] = node;
        }

        public void Connect(string srcId, string dstId) {
            if (!_nodes.ContainsKey(srcId) || !_nodes.ContainsKey(dstId)) return;
            if (!_edges.TryGetValue(srcId, out var list)) {
                list = new List<INode>();
                _edges[srcId] = list;
            }
            if (!list.Contains(_nodes[dstId])) {
                list.Add(_nodes[dstId]);
            }
        }

        public void Dispatch(string nodeId, InputEvent e) {
            if (!_nodes.TryGetValue(nodeId, out var node)) return;
            node.OnEvent(e);
            if (_edges.TryGetValue(nodeId, out var list)) {
                foreach (var n in list) n.OnEvent(e);
            }
        }

        public void Tick(double dtMs) {
            foreach (var n in _nodes.Values) n.OnTick(dtMs);
        }
    }
}
