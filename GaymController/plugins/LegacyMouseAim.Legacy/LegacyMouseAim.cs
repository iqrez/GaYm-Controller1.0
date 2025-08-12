namespace LegacyMouseAim.Legacy;

public interface ILegacyMouseAim
{
    (double x, double y) Translate(double deltaX, double deltaY);
}

public sealed class LegacyMouseAim : ILegacyMouseAim
{
    public (double x, double y) Translate(double deltaX, double deltaY)
    {
        // TODO: replace with ported legacy algorithm
        return (deltaX, deltaY);
    }
}
