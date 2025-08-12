using GaymController.Shared;
using Xunit;

namespace Shared.Tests {
    public class OverlayHudModelTests {
        [Fact]
        public void FormatReflectsUpdatedMetrics() {
            var model = new OverlayHudModel();
            model.Update(120.5, 5.25, true);
            var text = model.Format();
            Assert.Contains("Rate: 120.5 Hz", text);
            Assert.Contains("Latency: 5.2 ms", text);
            Assert.Contains("Rumble: On", text);
        }
    }
}
