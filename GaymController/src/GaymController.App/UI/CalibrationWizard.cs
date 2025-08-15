using System.Windows.Forms;

namespace GaymController.App.UI {
    public class CalibrationWizard : Form {
        private readonly CalibrationService _service;
        public CalibrationData? Calibration { get; private set; }
        public CalibrationWizard(CalibrationService service){
            _service = service;
            Text = "First-Run Calibration";
            Width = 400; Height = 200;
            var btn = new Button { Text = "Calibrate", Dock = DockStyle.Fill };
            btn.Click += (s,e) => {
                Calibration = _service.Calibrate();
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(btn);
        }
    }
}
