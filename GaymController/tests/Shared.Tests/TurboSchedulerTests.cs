using Xunit;
using GaymController.Shared.Mapping;

namespace GaymController.Shared.Tests {
    public class TurboSchedulerTests {
        [Fact]
        public void TurboNode_Toggles_WithSchedulerTicks() {
            var turbo = new TurboNode("t") { RateHz = 10.0, Duty = 0.5 };
            turbo.OnEvent(new InputEvent("btn", 1.0, 0));
            var sched = new Scheduler();
            Assert.True(sched.Add(turbo));
            // After 40ms -> still within duty (on)
            sched.Tick(40.0);
            Assert.True(turbo.Output());
            // Advance another 20ms -> beyond duty -> off
            sched.Tick(20.0);
            Assert.False(turbo.Output());
            // Advance 40ms -> new cycle, back on
            sched.Tick(40.0);
            Assert.True(turbo.Output());
        }
    }
}
