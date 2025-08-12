using System;
using GaymController.Shared.Mapping;

namespace GaymController.Wooting {
    public interface IWootingProvider : IDisposable {
        event EventHandler<InputEvent>? OnKeyAnalog;
        void Start(); void Stop();
    }
}
