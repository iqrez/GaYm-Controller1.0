namespace LegacyMouseAim.Legacy
{
    /// <summary>
    /// Simple interface to translate mouse deltas into stick coordinates
    /// using the legacy curve processor.
    /// </summary>
    public interface ILegacyMouseAim
    {
        (short X, short Y) Translate(int dx, int dy);
        void Reset();
    }

    /// <summary>
    /// Implementation of ILegacyMouseAim that mirrors the math from the
    /// original project. Only the translation logic is preserved.
    /// </summary>
    public sealed class LegacyMouseAimTranslator : ILegacyMouseAim
    {
        private readonly CurveProcessor _curve = new();

        public (short X, short Y) Translate(int dx, int dy)
        {
            return _curve.ToStick(dx, dy);
        }

        public void Reset() => _curve.ResetSmoothing();
    }
}
