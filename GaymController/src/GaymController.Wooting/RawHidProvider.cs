using System;
using GaymController.Shared.Mapping;

namespace GaymController.Wooting {
    // Stub - implement overlapped HID reads + mapping file application
    public sealed class RawHidProvider : IWootingProvider {
        public event EventHandler<InputEvent>? OnKeyAnalog;
        public void Start(){ /* TODO */ } public void Stop(){ /* TODO */ }
        public void Dispose(){ Stop(); }
    }
}
