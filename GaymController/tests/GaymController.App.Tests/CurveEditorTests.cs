using GaymController.Shared;
using Xunit;

namespace GaymController.App.Tests {
    public class CurveEditorTests {
        [Fact]
        public void ExpoIdentityProducesLinearLut(){
            var builder = new CurveLutBuilder { Mode = CurveMode.Expo, Expo = 0.0, Gain = 1.0 };
            var lut = builder.ExportLut();
            Assert.Equal(256, lut.Length);
            for(int i=0;i<256;i++) Assert.Equal((byte)i, lut[i]);
        }
    }
}
