using System;
using System.Collections.Generic;

namespace Geom_Util;

public static class Orthogonal
{
    public static IEnumerable<Dir> BasicAxes = [ Dir.X, Dir.Y, Dir.Z ];

    // PlusX is different from MinusX for the case of IBounds where ExpandBy(PlusX, 1) increases Max.X, but ExpandBy(MinusX, 1) decreases Min.X
    // (e.g. expanding left or expanding right) where there is no distinction (such as ImVec3,MoveBy) then X (== PlusX; == Right) must be used
    //
    // Up and Down are defined, because those are universal, Left/Right/Forwards/Backwards are not, because those are usually relative and hence ambiguous
    [Flags]
    public enum Dir
    {
        PlusX =  0x01,          X = PlusX,
        PlusY =  0x02,          Y = PlusY,      Up = PlusY,
        PlusZ =  0x04,          Z = PlusZ,
        MinusX = 0x08,
        MinusY = 0x10,                          Down = MinusY,
        MinusZ = 0x20,

        AllMaxMoves = PlusX | PlusY | PlusZ,
        AllMinMoves = MinusX | MinusY | MinusZ,
    }

    public static Dir Reverse(Dir dir)
    {
        int ret = 0;

        if ((dir & Dir.PlusX) != 0)
        {
            ret |= (int)Dir.MinusX;
        }

        if ((dir & Dir.MinusX) != 0)
        {
            ret |= (int)Dir.PlusX;
        }

        if ((dir & Dir.PlusY) != 0)
        {
            ret |= (int)Dir.MinusY;
        }

        if ((dir & Dir.MinusY) != 0)
        {
            ret |= (int)Dir.PlusY;
        }

        if ((dir & Dir.PlusZ) != 0)
        {
            ret |= (int)Dir.MinusZ;
        }

        if ((dir & Dir.MinusZ) != 0)
        {
            ret |= (int)Dir.PlusZ;
        }

        return (Dir)ret;
    }
}