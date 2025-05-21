using System;
using System.Diagnostics;

using Geom_Util.Interfaces;

using Godot;

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

// purpose here is to have an immutable 3d vector type, Unity's is mutable...
[DebuggerDisplay("({X}, {Y}, {Z})")]
public class ImVec3 : IEquatable<ImVec3>, IBounded
{
    public ImVec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;

        Godot_Util.Util.Assert(!float.IsNaN(X), "NaN!");
        Godot_Util.Util.Assert(!float.IsNaN(Y), "NaN!");
        Godot_Util.Util.Assert(!float.IsNaN(Z), "NaN!");
    }

    public ImVec3(Vector3 p)
    {
        X = p.X;
        Y = p.Y;
        Z = p.Z;

        Godot_Util.Util.Assert(!float.IsNaN(X), "NaN!");
        Godot_Util.Util.Assert(!float.IsNaN(Y), "NaN!");
        Godot_Util.Util.Assert(!float.IsNaN(Z), "NaN!");
    }

    public ImVec3()
    {
        X = Y = Z = 0;
    }

    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    public static ImVec3 operator +(ImVec3 lhs, ImVec3 rhs)
    {
        return new ImVec3(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
    }

    public static ImVec3 operator -(ImVec3 lhs, ImVec3 rhs)
    {
        return new ImVec3(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
    }

    public static ImVec3 operator -(ImVec3 lhs)
    {
        return new ImVec3(-lhs.X, -lhs.Y, -lhs.Z);
    }

    public static ImVec3 operator *(ImVec3 lhs, float rhs)
    {
        return new ImVec3(lhs.X * rhs, lhs.Y * rhs, lhs.Z * rhs);
    }

    public static ImVec3 operator /(ImVec3 lhs, float rhs)
    {
        return new ImVec3(lhs.X / rhs, lhs.Y / rhs, lhs.Z / rhs);
    }

    public bool IsBefore(ImVec3 other)
    {
        if (X < other.X)
        {
            return true;
        }
        else if (X > other.X)
        {
            return false;
        }

        if (Y < other.Y)
        {
            return true;
        }
        else if (Y > other.Y)
        {
            return false;
        }

        if (Z < other.Z)
        {
            return true;
        }
        else if (Z > other.Z)
        {
            return false;
        }

        // the two points are the same, so "IsBefore" is false, but we really do not expect to get asked this...
        return false;
    }

    public ImVec3Int ToImVec3Int()
    {
        return new ImVec3Int(Mathf.FloorToInt(X), Mathf.FloorToInt(Y), Mathf.FloorToInt(Z));
    }

    public ImVec3 Max(ImVec3 other)
    {
        return new ImVec3(Mathf.Max(X, other.X), Mathf.Max(Y, other.Y), Mathf.Max(Z, other.Z));
    }

    internal ImVec3 Min(ImVec3 other)
    {
        return new ImVec3(Mathf.Min(X, other.X), Mathf.Min(Y, other.Y), Mathf.Min(Z, other.Z));
    }

    public Godot.Vector3 ToVector3()
    {
        return new Godot.Vector3(X, Y, Z);
    }

    public ImVec3 Cross(ImVec3 rhs)
    {
        return new ImVec3(
            Y * rhs.Z - Z * rhs.Y,
            Z * rhs.X - X * rhs.Z,
            X * rhs.Y - Y * rhs.X);
    }

    public float Dot(ImVec3 rhs)
    {
        return X * rhs.X + Y * rhs.Y + Z * rhs.Z;
    }

    public ImVec3 Normalised()
    {
        return this / Length();
    }

    public float Length()
    {
        return Mathf.Sqrt(Length2());
    }

    public float Length2()
    {
        return Dot(this);
    }

    #region IEquatable
    public bool Equals(ImVec3 other)
    {
        return X == other.X
            && Y == other.Y
            && Z == other.Z;
    }
    #endregion

    public override int GetHashCode()
    {
        return X.GetHashCode() + Y.GetHashCode() * 3 + Z.GetHashCode() * 7;
    }

    #region IBounded
    public Immutable.ImBounds GetBounds()
    {
        return new ImBounds(this, this);
    }
    #endregion
}
