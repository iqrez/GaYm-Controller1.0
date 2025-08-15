using System;
using System.IO;
using Xunit;

namespace GaymController.App.Tests {
    public class UserSettingsTests {
        [Fact]
        public void SaveAndLoadRoundtrip(){
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var path = Path.Combine(dir, "user_settings.json");
            if(File.Exists(path)) File.Delete(path);
            var s = UserSettings.Load();
            s.IsFirstRun=false;
            s.IsCalibrated=true;
            s.Calibration=new CalibrationData{OffsetX=1, OffsetY=2};
            s.Save();
            var loaded = UserSettings.Load();
            Assert.False(loaded.IsFirstRun);
            Assert.True(loaded.IsCalibrated);
            Assert.Equal(1f, loaded.Calibration?.OffsetX);
            Assert.Equal(2f, loaded.Calibration?.OffsetY);
        }
    }
}
