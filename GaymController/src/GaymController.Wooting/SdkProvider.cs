using System;
using System.Runtime.InteropServices;
using GaymController.Shared.Mapping;

namespace GaymController.Wooting {
    /// <summary>
    /// Provider that uses the optional Wooting analog SDK when the native
    /// library is available on the system. The implementation only verifies
    /// library presence; if the SDK cannot be loaded the caller should fall
    /// back to <see cref="RawHidProvider"/>.
    /// </summary>
    public sealed class SdkProvider : IWootingProvider {
        private IntPtr _handle;
        public static bool IsAvailable {
            get {
                return NativeLibrary.TryLoad("wooting-analog-sdk", out _)
                    || NativeLibrary.TryLoad("WootingAnalogWrapper", out _)
                    || NativeLibrary.TryLoad("wooting_analog", out _);
            }
        }

        public SdkProvider(){
            if (!NativeLibrary.TryLoad("wooting-analog-sdk", out _handle)
                && !NativeLibrary.TryLoad("WootingAnalogWrapper", out _handle)
                && !NativeLibrary.TryLoad("wooting_analog", out _handle))
                throw new DllNotFoundException("Wooting analog SDK not found");
        }

        public event EventHandler<InputEvent>? OnKeyAnalog;
        public void Start(){ /* SDK polling would start here */ }
        public void Stop(){ /* SDK polling would stop here */ }

        public void Dispose(){
            Stop();
            if (_handle != IntPtr.Zero){
                NativeLibrary.Free(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }
}
