using System;
using System.Drawing;
using System.Windows.Forms;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace WootMouseRemap
{
    public sealed class InputVisualizerControl : Control
    {
        public short LX { get; set; }
        public short LY { get; set; }
        public short RX { get; set; }
        public short RY { get; set; }
        public byte LT { get; set; }
        public byte RT { get; set; }
        public bool BtnA, BtnB, BtnX, BtnY, LB, RB, Back, Start, L3, R3, DUp, DDown, DLeft, DRight;

        // New: theme accent color for highlights (buttons pressed, stick dot, bars)
        public Color AccentColor { get; set; } = Color.DodgerBlue;

        public InputVisualizerControl()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var bg = this.BackColor.IsEmpty ? Color.Black : this.BackColor;
            var fg = this.ForeColor.IsEmpty ? Color.White : this.ForeColor;
            var panel = Adjust(bg, IsDark(bg) ? +20 : -15);
            var outline = fg;
            var accent = this.AccentColor.IsEmpty ? Color.DodgerBlue : this.AccentColor;

            g.Clear(bg);

            // Draw sticks (left and right)
            DrawStick(g, new Rectangle(20, 20, 200, 200), LX, LY, "L", panel, outline, accent, fg);
            DrawStick(g, new Rectangle(240, 20, 200, 200), RX, RY, "R", panel, outline, accent, fg);

            // Triggers bars
            DrawBar(g, new Rectangle(20, 240, 200, 20), LT / 255f, "LT", panel, outline, accent, fg);
            DrawBar(g, new Rectangle(240, 240, 200, 20), RT / 255f, "RT", panel, outline, accent, fg);

            // Buttons grid
            int x = 480, y = 20, w = 60, h = 30, p = 8;
            DrawBtn(g, x, y, w, h, "A", BtnA, panel, outline, accent, fg);
            DrawBtn(g, x + w + p, y, w, h, "B", BtnB, panel, outline, accent, fg);
            DrawBtn(g, x + (w + p) * 2, y, w, h, "X", BtnX, panel, outline, accent, fg);
            DrawBtn(g, x + (w + p) * 3, y, w, h, "Y", BtnY, panel, outline, accent, fg);

            y += h + p;
            DrawBtn(g, x, y, w, h, "LB", LB, panel, outline, accent, fg);
            DrawBtn(g, x + w + p, y, w, h, "RB", RB, panel, outline, accent, fg);
            DrawBtn(g, x + (w + p) * 2, y, w, h, "Back", Back, panel, outline, accent, fg);
            DrawBtn(g, x + (w + p) * 3, y, w, h, "Start", Start, panel, outline, accent, fg);

            y += h + p;
            DrawBtn(g, x, y, w, h, "L3", L3, panel, outline, accent, fg);
            DrawBtn(g, x + w + p, y, w, h, "R3", R3, panel, outline, accent, fg);
            DrawBtn(g, x + (w + p) * 2, y, w, h, "D-Up", DUp, panel, outline, accent, fg);
            DrawBtn(g, x + (w + p) * 3, y, w, h, "D-Down", DDown, panel, outline, accent, fg);

            y += h + p;
            DrawBtn(g, x, y, w, h, "D-Left", DLeft, panel, outline, accent, fg);
            DrawBtn(g, x + w + p, y, w, h, "D-Right", DRight, panel, outline, accent, fg);
        }

        private void DrawStick(System.Drawing.Graphics g, Rectangle rect, short sx, short sy, string label, Color panel, Color outline, Color accent, Color fg)
        {
            using var panelBr = new SolidBrush(panel);
            using var outlinePen = new Pen(outline);
            using var textBr = new SolidBrush(fg);
            using var centerPen = new Pen(outline);
            using var dotBr = new SolidBrush(accent);

            g.FillRectangle(panelBr, rect);
            g.DrawString(label, this.Font, textBr, rect.Left + 4, rect.Top + 4);

            // center
            int cx = rect.Left + rect.Width / 2;
            int cy = rect.Top + rect.Height / 2;
            g.DrawEllipse(centerPen, cx - 2, cy - 2, 4, 4);

            // point
            float nx = sx / 32767f;
            float ny = sy / 32767f;
            int px = cx + (int)(nx * (rect.Width / 2 - 10));
            int py = cy - (int)(ny * (rect.Height / 2 - 10));
            g.FillEllipse(dotBr, px - 6, py - 6, 12, 12);
        }

        private void DrawBar(System.Drawing.Graphics g, Rectangle rect, float v, string label, Color panel, Color outline, Color accent, Color fg)
        {
            using var panelBr = new SolidBrush(panel);
            using var barBr = new SolidBrush(accent);
            using var outlinePen = new Pen(outline);
            using var textBr = new SolidBrush(fg);

            g.FillRectangle(panelBr, rect);
            int w = (int)(rect.Width * Math.Max(0, Math.Min(1, v)));
            g.FillRectangle(barBr, rect.Left, rect.Top, w, rect.Height);
            g.DrawRectangle(outlinePen, rect);
            g.DrawString(label, this.Font, textBr, rect.Right + 6, rect.Top - 2);
        }

        private void DrawBtn(System.Drawing.Graphics g, int x, int y, int w, int h, string label, bool down, Color panel, Color outline, Color accent, Color fg)
        {
            var rect = new Rectangle(x, y, w, h);
            using var fill = new SolidBrush(down ? accent : panel);
            using var outlinePen = new Pen(outline);
            using var textBr = new SolidBrush(fg);
            g.FillRectangle(fill, rect);
            g.DrawRectangle(outlinePen, rect);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(label, this.Font, textBr, rect, sf);
        }

        private static bool IsDark(Color c)
        {
            // Perceived luminance
            double l = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            return l < 0.5;
        }

        private static Color Adjust(Color c, int delta)
        {
            int r = Math.Max(0, Math.Min(255, c.R + delta));
            int g = Math.Max(0, Math.Min(255, c.G + delta));
            int b = Math.Max(0, Math.Min(255, c.B + delta));
            return Color.FromArgb(r, g, b);
        }
    }
}
