using System;
using GaymController.Shared.Mapping;
using Xunit;

namespace Mapping.Tests {
    public class AntiRecoilNodeTests {
        [Fact]
        public void DisabledNodeProducesNoOutput() {
            var node = new AntiRecoilNode("ar") { VerticalComp = 0.2 };
            node.OnEvent(new InputEvent("Fire", 1.0, 0));
            node.OnTick(16.0);
            Assert.Equal(0.0, node.Output());
        }

        [Fact]
        public void EnabledNodeDecaysOverTime() {
            var node = new AntiRecoilNode("ar") { Enabled = true, VerticalComp = 0.2, DecayMs = 100.0 };
            node.OnEvent(new InputEvent("Fire", 1.0, 0));
            Assert.Equal(-0.2, node.Output(), 5);
            node.OnTick(50.0);
            var expected = -0.2 * Math.Exp(-50.0 / 100.0);
            Assert.InRange(node.Output(), expected - 1e-6, expected + 1e-6);
            node.OnEvent(new InputEvent("Fire", 0.0, 0));
            node.OnTick(10.0);
            Assert.Equal(0.0, node.Output(), 5);
        }
    }
}
