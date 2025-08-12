using Xunit;
using GaymController.Wooting;

namespace GaymController.Wooting.Tests {
    public class WootingProviderFactoryTests {
        [Fact]
        public void FallsBackToRawHidWhenSdkMissing(){
            using var provider = WootingProviderFactory.Create();
            Assert.IsType<RawHidProvider>(provider);
        }
    }
}
