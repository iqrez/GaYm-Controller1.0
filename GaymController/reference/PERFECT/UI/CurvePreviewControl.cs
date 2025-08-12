using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WootMouseRemap
{
    // 1D response curve preview (input magnitude -> stick magnitude), theme-aware with live point and trail
    public sealed class CurvePreviewControl : Control
    {
        public CurveProcessor? Curve { get; private set; }
        public bool ShowGrid { get; set; } = true;
        public bool ShowLivePoint { get; set; } = true;
        public Color AccentColor { get; set; } = Color.DodgerBlue;
        public int InputRangePx { get; set; } = 120; // input magnitude range for horizontal axis

        private readonly Queue<(float inN, float outN)> _trail = new();
        private const int TrailMax = 120;

        private float _liveInN;
        private float _liveOutN;

        public CurvePreviewControl()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        public void SetCurve(CurveProcessor? curve)
        {
            Curve = curve;
            Invalidate();
        }

        public void UpdateLive(int dx, int dy, short sx, short sy)
        {
            // normalize input by InputRangePx (radial)
            float rin = MathF.Sqrt(dx * dx + dy * dy);
            float inN = InputRangePx > 0 ? Math.Clamp(rin / InputRangePx, 0f, 1f) : 0f;
            float rout = MathF.Sqrt((sx / 32767f) * (sx / 32767f) + (sy / 32767f) * (sy / 32767f));
            float outN = Math.Clamp(rout, 0f, 1f);
            _liveInN = inN; _liveOutN = outN;
            _trail.Enqueue((inN, outN));
            while (_trail.Count > TrailMax) _trail.Dequeue();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var bg = BackColor.IsEmpty ? Color.Black : BackColor;
            var fg = ForeColor.IsEmpty ? Color.White : ForeColor;
            var accent = AccentColor.IsEmpty ? Color.DodgerBlue : AccentColor;
            g.Clear(bg);

            var rect = ClientRectangle;
            if (rect.Width < 20 || rect.Height < 20) return;

            // Plot area with padding
            int padL = 36, padR = 10, padT = 10, padB = 26;
            var plot = Rectangle.FromLTRB(rect.Left + padL, rect.Top + padT, rect.Right - padR, rect.Bottom - padB);

            // Grid and axes
            using var axisPen = new Pen(Color.FromArgb(160, fg));
            using var gridPen = new Pen(Color.FromArgb(40, fg));
            using var labelBr = new SolidBrush(fg);
            using var font = new Font(Font, FontStyle.Regular);

            if (ShowGrid)
            {
                for (int i = 0; i <= 10; i++)
                {
                    int x = plot.Left + (int)(i / 10f * plot.Width);
                    g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                    int y = plot.Bottom - (int)(i / 10f * plot.Height);
                    g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                }
            }
            // axes
            g.DrawRectangle(axisPen, plot);
            g.DrawString("0", font, labelBr, plot.Left - 16, plot.Bottom - 12);
            g.DrawString("1", font, labelBr, plot.Left - 16, plot.Top - 2);
            g.DrawString("Input", font, labelBr, plot.Left + plot.Width / 2 - 18, plot.Bottom + 4);
            g.DrawString("Output", font, labelBr, plot.Left - 30, plot.Top - 16);

            // Curve path
            var pathPts = SampleCurve(plot);
            if (pathPts.Count > 1)
            {
                using var curvePen = new Pen(accent, 2f);
                g.DrawLines(curvePen, pathPts.ToArray());
            }

            // Trail and live point
            if (ShowLivePoint)
            {
                using var trailPen = new Pen(Color.FromArgb(120, accent), 1.5f);
                using var liveBr = new SolidBrush(accent);
                using var livePen = new Pen(accent, 2f);

                PointF? prev = null;
                foreach (var (inN, outN) in _trail)
                {
                    var p = ToPoint(plot, inN, outN);
                    if (prev.HasValue) g.DrawLine(trailPen, prev.Value, p);
                    prev = p;
                }
                var live = ToPoint(plot, _liveInN, _liveOutN);
                g.FillEllipse(liveBr, live.X - 3, live.Y - 3, 6, 6);
                g.DrawEllipse(livePen, live.X - 5, live.Y - 5, 10, 10);
            }
        }

        private List<PointF> SampleCurve(Rectangle plot)
        {
            var pts = new List<PointF>(256);
            if (Curve == null)
                return pts;

            // Use a temporary processor to avoid EMA interference during sampling
            var cp = new CurveProcessor
            {
                Sensitivity = Curve.Sensitivity,
                Expo = Curve.Expo,
                AntiDeadzone = Curve.AntiDeadzone,
                MaxSpeed = Curve.MaxSpeed,
                EmaAlpha = 0f, // disable smoothing for static response
                VelocityGain = Curve.VelocityGain,
                JitterFloor = 0f, // ignore for preview
                ScaleX = Curve.ScaleX,
                ScaleY = Curve.ScaleY
            };

            int N = 200;
            for (int i = 0; i <= N; i++)
            {
                float t = i / (float)N; // 0..1 input magnitude
                float dx = t * InputRangePx;
                var (sx, sy) = cp.ToStick(dx, 0);
                float outN = Math.Clamp(MathF.Sqrt((sx / 32767f) * (sx / 32767f) + (sy / 32767f) * (sy / 32767f)), 0f, 1f);
                pts.Add(ToPoint(plot, t, outN));
            }
            return pts;
        }

        private static PointF ToPoint(Rectangle plot, float inN, float outN)
        {
            float x = plot.Left + inN * plot.Width;
            float y = plot.Bottom - outN * plot.Height;
            return new PointF(x, y);
        }
    }
}
