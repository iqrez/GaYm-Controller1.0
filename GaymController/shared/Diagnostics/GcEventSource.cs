using System.Diagnostics.Tracing;

namespace GaymController.Diagnostics {
    /// <summary>Simple ETW provider for GaymController diagnostics.</summary>
    [EventSource(Name="GaymController")]
    public sealed class GcEventSource : EventSource {
        public static readonly GcEventSource Log = new();
        [Event(1, Level = EventLevel.Informational)]
        public void Info(string message) => WriteEvent(1, message);
        [Event(2, Level = EventLevel.Error)]
        public void Error(string message) => WriteEvent(2, message);
    }
}
