using GaymController.Wooting;
using Xunit;

namespace GaymController.Wooting.Tests {
    public class RawHidProviderTests {
        [Theory]
        [InlineData(0x31E3, 0x1100, true)]
        [InlineData(0x31E3, 0x1200, true)]
        [InlineData(0x31E3, 0x9999, false)]
        [InlineData(0xFFFF, 0x1100, false)]
        public void IsSupportedDeviceFiltersByVidPid(int vid, int pid, bool expected) {
            Assert.Equal(expected, RawHidProvider.IsSupportedDevice(vid, pid));
        }
    }
}
