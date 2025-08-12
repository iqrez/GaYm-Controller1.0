using Xunit;

namespace GaymController.App.Tests {
    public class CalibrationServiceTests {
        [Fact]
        public void CalibrateReturnsDefaults(){
            var svc = new CalibrationService();
            var data = svc.Calibrate();
            Assert.Equal(0f, data.OffsetX);
            Assert.Equal(0f, data.OffsetY);
        }
    }
}
