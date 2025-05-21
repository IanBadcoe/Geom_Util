using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Geom_Util.Immutable;

// C# equality is a total mess, having a policy here of:
// - never override operator ==/!=
// -- so that x != null always means pure reference comparison
// - never override Equals(object obj)
// -- so we get type-safety on x.Equals(y)
// - use T.Equals(T other) for _value_ compare
// -- so it errors for null
// -- which is the override for IEquatable<T> which at least some of the built-in containers use
// - and override GetHashCode when you override that, also for containers...

// immutable bounds object...
[DebuggerDisplay("({Min.X}, {Min.Y}, {Min.Z}) -> ({Max.X}, {Max.Y}, {Max.Z})")]
public class ImBounds : IEquatable<ImBounds>
{
    public ImVec3 Min { get; private set; }
    public ImVec3 Max { get; private set; }

    public ImBounds()
    {
        // we have exactly one representation of empty
        Min = new ImVec3(1, 0, 0);
        Max = new ImVec3(0, 0, 0);
    }

    public ImBounds(ImVec3 min, ImVec3 max)
    {
        if (min.X > max.X || min.Y > max.Y || min.Z > max.Z)
        {
            // we have exactly one representation of empty
            Min = new ImVec3(1, 0, 0);
            Max = new ImVec3(0, 0, 0);
        }

        Min = min;
        Max = max;
    }

    public ImBounds(ImVec3 v)
    {
        // make the zero-sized bounds for a point...
        Min = Max = v;
    }

    public ImBounds Encapsulating(ImVec3 v)
    {
        var point_bound = new ImBounds(v, v);
        if (IsEmpty)
        {
            return point_bound;
        }
        else
        {
            return this.UnionedWith(point_bound);
        }
    }

    public ImVec3 Size => Max - Min;
    public ImVec3 Centre => (Min + Max) / 2;

    public bool Contains(ImVec3 pnt)
    {
        return pnt.X >= Min.X && pnt.X <= Max.X
            && pnt.Y >= Min.Y && pnt.Y <= Max.Y
            && pnt.Z >= Min.Z && pnt.Z <= Max.Z;
    }

    public bool Contains(ImBounds bounds)
    {
        return Contains(bounds.Max) && Contains(bounds.Min);
    }

    public ImBounds ExpandedBy(float bound_extension)
    {
        var exp_vec = new ImVec3(bound_extension, bound_extension, bound_extension);

        return new ImBounds(Min - exp_vec, Max + exp_vec);
    }

    public IEnumerable<ImVec3> Corners
    {
        get
        {
            yield return new ImVec3(Min.X, Min.Y, Min.Z);
            yield return new ImVec3(Min.X, Min.Y, Max.Z);
            yield return new ImVec3(Min.X, Max.Y, Min.Z);
            yield return new ImVec3(Min.X, Max.Y, Max.Z);
            yield return new ImVec3(Max.X, Min.Y, Min.Z);
            yield return new ImVec3(Max.X, Min.Y, Max.Z);
            yield return new ImVec3(Max.X, Max.Y, Max.Z);
            yield return new ImVec3(Max.X, Max.Y, Min.Z);
        }
    }

    public float Volume => Size.X * Size.Y * Size.Z;

    public bool IsEmpty => Min.X == 1 && Max.X == 0;

    public ImBounds UnionedWith(ImBounds other)
    {
        if (IsEmpty)
        {
            return other;
        }

        if (other.IsEmpty)
        {
            return this;
        }

        return new ImBounds(Min.Min(other.Min), Max.Max(other.Max));
    }

    public bool Overlaps(ImBounds b)
    {
        return !ClearOf(b);
    }

    public bool ClearOf(ImBounds b)
    {
        return Min.X > b.Max.X
            || Min.Y > b.Max.Y
            || Min.Z > b.Max.Z
            || b.Min.X > Max.X
            || b.Min.Y > Max.Y
            || b.Min.Z > Max.Z;
    }

    #region IEquatable
    public bool Equals(ImBounds other)
    {
        return Min.Equals(other.Min) && Max.Equals(other.Max);
    }
    #endregion

    public override int GetHashCode()
    {
        return Min.GetHashCode() + Max.GetHashCode() * 3;
    }
}
