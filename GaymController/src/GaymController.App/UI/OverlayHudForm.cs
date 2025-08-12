using System.Drawing;
using System.Windows.Forms;
using GaymController.Shared;

namespace GaymController.App.UI {
    /// <summary>
    /// Transparent top-most window displaying live metrics.
    /// </summary>
    public sealed class OverlayHudForm : Form {
        private readonly Label _label;
        private readonly OverlayHudModel _model = new();

        public OverlayHudForm() {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            ForeColor = Color.Lime;
            Opacity = 0.7;
            _label = new Label { AutoSize = true };
            Controls.Add(_label);
        }

        public void UpdateMetrics(double rateHz, double latencyMs, bool rumbleActive) {
            _model.Update(rateHz, latencyMs, rumbleActive);
            _label.Text = _model.Format();
        }
    }
}
