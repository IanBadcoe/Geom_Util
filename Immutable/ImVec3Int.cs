using System;
using System.Collections.Generic;
using System.Diagnostics;

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
public class ImVec3Int : IEquatable<ImVec3Int>
{
    public ImVec3Int(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public ImVec3Int(Vector3I p)
    {
        X = p.X;
        Y = p.Y;
        Z = p.Z;
    }

    public ImVec3Int()
    {
        X = Y = Z = 0;
    }

    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public IEnumerable<ImVec3Int> OrthoNeighbours
    {
        get
        {
            yield return new ImVec3Int(X + 1, Y, Z);
            yield return new ImVec3Int(X - 1, Y, Z);
            yield return new ImVec3Int(X, Y + 1, Z);
            yield return new ImVec3Int(X, Y - 1, Z);
            yield return new ImVec3Int(X, Y, Z + 1);
            yield return new ImVec3Int(X, Y, Z - 1);
        }
    }

    public IEnumerable<ImVec3Int> AllNeighbours
    {
        get
        {
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    for (int k = -1; k < 2; k++)
                    {
                        if (i != 0 || j != 0 || k != 0)
                        {
                            yield return new ImVec3Int(X + i, Y + j, Z + k);
                        }
                    }
                }
            }
        }
    }

    public static ImVec3Int operator +(ImVec3Int lhs, ImVec3Int rhs)
    {
        return new ImVec3Int(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
    }

    public static ImVec3Int operator -(ImVec3Int lhs, ImVec3Int rhs)
    {
        return new ImVec3Int(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
    }

    public static ImVec3Int operator *(ImVec3Int lhs, float rhs)
    {
        return new ImVec3Int((int)(lhs.X * rhs), (int)(lhs.Y * rhs), (int)(lhs.Z * rhs));
    }

    public float Size2()
    {
        return X * X + Y * Y + Z * Z;
    }

    public bool IsBefore(ImVec3Int other)
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
        return new ImVec3Int(X, Y, Z);
    }

    #region IEquatable
    public bool Equals(ImVec3Int other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }
    #endregion

    public override int GetHashCode()
    {
        return HashCode.Combine(X.GetHashCode(), Y.GetHashCode(), Z.GetHashCode());
    }

    internal ImVec3 ToImVec3()
    {
        return new ImVec3(X, Y, Z);
    }
}
