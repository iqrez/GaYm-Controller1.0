using GaymController.Shared.Mapping;

namespace GaymController.Interfaces.Mapping {
    /// <summary>
    /// Describes the minimal kernel-side mapping graph engine. Nodes are
    /// addressed by string identifiers and receive input events and tick updates.
    /// The implementation should avoid per-event allocations on hot paths.
    /// </summary>
    public interface IMappingGraphKernel {
        /// <summary>Register a node with the graph.</summary>
        void AddNode(INode node);
        /// <summary>Connect one node's output to another's input.</summary>
        void Connect(string srcId, string dstId);
        /// <summary>Dispatch an input event into a node.</summary>
        void Dispatch(string nodeId, InputEvent e);
        /// <summary>Tick all nodes in the graph.</summary>
        void Tick(double dtMs);
    }
}
