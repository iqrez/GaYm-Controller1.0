namespace LegacyMouseAim.Legacy {
    public interface ILegacyMouseAim {
        float Sensitivity { get; set; }
        float Expo { get; set; }
        float AntiDeadzone { get; set; }
        float MaxSpeed { get; set; }
        float EmaAlpha { get; set; }
        float VelocityGain { get; set; }
        float JitterFloor { get; set; }
        float ScaleX { get; set; }
        float ScaleY { get; set; }
        (short X, short Y) ToStick(float dx, float dy);
        void Reset();
    }
}
