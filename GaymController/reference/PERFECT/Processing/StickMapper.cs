using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WootMouseRemap
{
    public sealed class StickMapper
    {
        private readonly CurveProcessor _curve = new();
        private readonly HashSet<Keys> _pressed = new();

        public CurveProcessor Curve => _curve;

        public (short X, short Y) MouseToRightStick(int dx, int dy) => _curve.ToStick(dx, dy);

        public (short X, short Y) WasdToLeftStick()
        {
            int x = 0, y = 0;
            if (_pressed.Contains(Keys.A)) x -= 1;
            if (_pressed.Contains(Keys.D)) x += 1;
            if (_pressed.Contains(Keys.W)) y -= 1;
            if (_pressed.Contains(Keys.S)) y += 1;

            float fx = x; float fy = y;
            float len = MathF.Sqrt(fx * fx + fy * fy);
            if (len > 1e-5) { fx /= len; fy /= len; }
            short sx = (short)(fx * 32767);
            short sy = (short)(-fy * 32767);
            return (sx, sy);
        }

        public void UpdateKey(int vk, bool down)
        {
            Keys k = (Keys)vk;
            if (down) _pressed.Add(k); else _pressed.Remove(k);
        }
    }
}
