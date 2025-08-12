using GaymController.Shared.Mapping;
using Xunit;

namespace GaymController.Shared.Tests {
    public class AutoSprintNodeTests {
        [Fact]
        public void DoesNotSprintWhenDisabled() {
            var node = new AutoSprintNode("n");
            node.OnEvent(new InputEvent("Move", 1.0, 0));
            node.OnTick(16);
            Assert.False(node.Output());
        }

        [Fact]
        public void TogglesAndFollowsMovement() {
            var node = new AutoSprintNode("n");
            // toggle on
            node.OnEvent(new InputEvent("Toggle", 1, 0));
            node.OnEvent(new InputEvent("Toggle", 0, 0));
            node.OnEvent(new InputEvent("Move", 0.8, 0));
            node.OnTick(16);
            Assert.True(node.Output());
            // stop moving
            node.OnEvent(new InputEvent("Move", 0.0, 0));
            node.OnTick(16);
            Assert.False(node.Output());
            // move again still toggled on
            node.OnEvent(new InputEvent("Move", 0.9, 0));
            node.OnTick(16);
            Assert.True(node.Output());
            // toggle off
            node.OnEvent(new InputEvent("Toggle", 1, 0));
            node.OnEvent(new InputEvent("Toggle", 0, 0));
            node.OnTick(16);
            Assert.False(node.Output());
        }
    }
}
