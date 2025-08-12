using System;

namespace GaymController.Shared {
    /// <summary>
    /// Simple model backing the on-screen overlay HUD.
    /// Tracks update rate, latency and rumble state.
    /// </summary>
    public sealed class OverlayHudModel {
        public double RateHz { get; private set; }
        public double LatencyMs { get; private set; }
        public bool RumbleActive { get; private set; }

        /// <summary>Update the metrics shown by the HUD.</summary>
        public void Update(double rateHz, double latencyMs, bool rumbleActive) {
            RateHz = rateHz;
            LatencyMs = latencyMs;
            RumbleActive = rumbleActive;
        }

        /// <summary>Returns a multi-line formatted string for display.</summary>
        public string Format() =>
            $"Rate: {RateHz:F1} Hz\nLatency: {LatencyMs:F1} ms\nRumble: {(RumbleActive ? "On" : "Off")}";
    }
}
