using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WootMouseRemap
{
    // Centralized ThemeManager to avoid duplicate definitions
    public sealed class ThemeManager
    {
        private readonly List<Control> _controls = new();

        public void RegisterControl(Control control)
        {
            if (control == null) return;
            _controls.Add(control);
        }

        public void ApplyTheme(bool darkTheme)
        {
            foreach (var control in _controls)
                ApplyThemeToControl(control, darkTheme);
        }

        private static void ApplyThemeToControl(Control control, bool darkTheme)
        {
            if (control == null) return;
            if (darkTheme)
            {
                control.BackColor = Color.FromArgb(45, 45, 48);
                control.ForeColor = Color.White;
            }
            else
            {
                control.BackColor = Color.FromArgb(246, 246, 246);
                control.ForeColor = Color.Black;
            }

            foreach (Control child in control.Controls)
                ApplyThemeToControl(child, darkTheme);
        }
    }
}
