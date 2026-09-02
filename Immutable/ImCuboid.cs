using Geom_Util.Immutable;
using Geom_Util.Immutable.Interfaces;

public class ImCuboid : IBounded
{
    public ImVec3 Min => Bounds.Min;
    public ImVec3 Max => Bounds.Max;
    public ImBounds Bounds{ get; private set; }

    public ImCuboid(ImBounds bounds)
    {
        Bounds = bounds;
    }

    public ImBounds GetBounds()
    {
        return Bounds;
    }
}