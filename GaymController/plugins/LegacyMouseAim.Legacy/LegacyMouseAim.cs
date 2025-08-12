using System;

namespace LegacyMouseAim.Legacy {
    public sealed class LegacyMouseAim : ILegacyMouseAim {
        public float Sensitivity { get; set; } = 0.35f;
        public float Expo { get; set; } = 0.6f;
        public float AntiDeadzone { get; set; } = 0.05f;
        public float MaxSpeed { get; set; } = 1.0f;
        public float EmaAlpha { get; set; } = 0.35f;
        public float VelocityGain { get; set; } = 0.0f;
        public float JitterFloor { get; set; } = 0.0f;
        public float ScaleX { get; set; } = 1.0f;
        public float ScaleY { get; set; } = 1.0f;
        float _emaX, _emaY, _lastR;
        public (short X, short Y) ToStick(float dx, float dy){
            float scale = Sensitivity / 50f;
            float x = dx * scale * ScaleX;
            float y = dy * scale * ScaleY;
            float r = MathF.Sqrt(x*x + y*y);
            if(r < JitterFloor){ x=0f; y=0f; r=0f; }
            if(r > 0f){
                float ux = x / r; float uy = y / r;
                float v = MathF.Abs(r - _lastR);
                float velGain = 1f + VelocityGain * Math.Clamp(v,0f,1.5f);
                r *= velGain; _lastR = r;
                float maxR = MaxSpeed > 0f ? MaxSpeed : 1f;
                float rn = Math.Clamp(r / maxR, 0f, 1f);
                float expo = Expo; if(expo > 0f) rn = MathF.Pow(rn, 1f - expo);
                float adz = AntiDeadzone; rn = rn<=0f?0f:(adz + (1f-adz)*rn);
                float targetR = rn * maxR;
                x = ux * targetR; y = uy * targetR;
            } else _lastR = 0f;
            float alpha = EmaAlpha;
            if(alpha > 0f){
                _emaX += alpha * (x - _emaX);
                _emaY += alpha * (y - _emaY);
                x = _emaX; y = _emaY;
            }
            float rr = MathF.Sqrt(x*x + y*y);
            float m = MaxSpeed > 0f ? MaxSpeed : 1f;
            if(rr > m){ float s = m / rr; x *= s; y *= s; }
            short sx = (short)Math.Clamp(x * 32767f, -32768f, 32767f);
            short sy = (short)Math.Clamp(-y * 32767f, -32768f, 32767f);
            return (sx, sy);
        }
        public void Reset(){ _emaX=0f; _emaY=0f; _lastR=0f; }
    }
}
