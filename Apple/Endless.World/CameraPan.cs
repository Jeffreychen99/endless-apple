namespace Endless.Apple.World;

/// <summary>Touch-camera offset in map tiles, independent of character movement.</summary>
public sealed class CameraPan
{
    public double X { get; private set; }
    public double Y { get; private set; }
    public void Drag(double renderedX, double renderedY)
    {
        if (!double.IsFinite(renderedX) || !double.IsFinite(renderedY)) return;
        // Inverse of WorldCamera's 64×32 diamond projection. The scene follows the finger.
        X = Math.Clamp(X-renderedX/64-renderedY/32,-32,32);
        Y = Math.Clamp(Y+renderedX/64-renderedY/32,-32,32);
    }
    public void Reset() { X=0; Y=0; }
}
