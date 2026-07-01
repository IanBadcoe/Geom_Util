using Geom_Util.Immutable;
using Geom_Util.Interfaces;
using Godot.Bridge;

public class ImCuboid : IBounded
{
    public ImVec3 Min { get; init; }
    public ImVec3 Max { get; init; }

    public ImCuboid(ImVec3 min, ImVec3 max)
    {
        Min = min;
        Max = max;
    }

    public ImBounds GetBounds()
    {
        return new ImBounds(Min, Max);
    }
}