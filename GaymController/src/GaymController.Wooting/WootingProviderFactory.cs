using System;

namespace GaymController.Wooting {
    /// <summary>
    /// Factory that returns the best available <see cref="IWootingProvider"/>.
    /// Prefers the optional SDK wrapper when present, otherwise falls back to
    /// the raw HID implementation.
    /// </summary>
    public static class WootingProviderFactory {
        public static IWootingProvider Create(){
            return SdkProvider.IsAvailable
                ? new SdkProvider()
                : new RawHidProvider();
        }
    }
}
