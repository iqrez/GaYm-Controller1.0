using System;
namespace GaymController.Shared.Mapping {
    public enum PortType { Scalar, Vector2, Bool, Trigger, ButtonBits, GamepadState }
    public interface INode { string Id { get; } void OnEvent(InputEvent e); void OnTick(double dtMs); }
    public readonly struct InputEvent {
        public readonly string Source; public readonly double Value; public readonly long TimestampUs;
        public InputEvent(string source, double value, long ts){ Source=source; Value=value; TimestampUs=ts; }
    }
}
