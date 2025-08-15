using System.Threading;
using System.Diagnostics;
namespace GaymController.Shared.Mapping {
    /// <summary>
    /// Simple allocation-free scheduler that ticks registered nodes.
    /// Intended for macro/turbo style updates where a steady tick is required.
    /// </summary>
    public sealed class Scheduler {
        private readonly INode?[] _nodes;
        private int _count;
        /// <param name="capacity">Maximum number of nodes to track.</param>
        public Scheduler(int capacity = 64){
            _nodes = new INode?[capacity];
            _count = 0;
        }
        /// <summary>Registers a node for ticking. Returns false if capacity exceeded.</summary>
        public bool Add(INode node){
            if(_count >= _nodes.Length) return false;
            _nodes[_count++] = node;
            return true;
        }
        /// <summary>
        /// Ticks all nodes with the provided delta time in milliseconds.
        /// </summary>
        public void Tick(double dtMs){
            var n = _count;
            var arr = _nodes;
            for(int i=0;i<n;i++) arr[i]!.OnTick(dtMs);
        }
    }
}
