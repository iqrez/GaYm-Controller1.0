using System;
using System.Windows.Forms;

namespace GaymController.App.UI {
    public class MainForm : Form {
        public MainForm(){
            Text="Gaym Controller"; Width=980; Height=640;
            Load += MainForm_Load;
        }

        private void MainForm_Load(object? sender, EventArgs e){
            var settings = UserSettings.Load();
            if(settings.IsFirstRun || !settings.IsCalibrated){
                using var wiz = new CalibrationWizard(new CalibrationService());
                if(wiz.ShowDialog(this)==DialogResult.OK){
                    settings.IsFirstRun=false;
                    settings.IsCalibrated=true;
                    settings.Calibration=wiz.Calibration;
                    settings.Save();
                }
            }
        }
    }
}
