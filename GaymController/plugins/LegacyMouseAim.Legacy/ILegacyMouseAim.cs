namespace LegacyMouseAim.Legacy;

/// <summary>
/// Contract for the legacy mouse aim translator used by the compatibility
/// harness.  The interface mirrors the options exposed by the original
/// implementation so that the math can be exercised without pulling in the
/// entire legacy project.
/// </summary>
public interface ILegacyMouseAim
{
    float Sensitivity { get; set; }
    float Expo { get; set; }
    float AntiDeadzone { get; set; }
    float MaxSpeed { get; set; }
    float EmaAlpha { get; set; }
    float VelocityGain { get; set; }
    float JitterFloor { get; set; }
    float ScaleX { get; set; }
    float ScaleY { get; set; }

    /// <summary>Translate raw mouse delta values into stick units.</summary>
    (short X, short Y) ToStick(float dx, float dy);

    /// <summary>Reset internal smoothing state.</summary>
    void ResetSmoothing();
}
