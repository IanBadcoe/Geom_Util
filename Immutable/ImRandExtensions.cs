using Godot_Util;

namespace Geom_Util.Immutable;

public static class ClRandImExtensions
{
        // a ImVec3 between (0, 0, 0) and (1, 1, 1)
        public static ImVec3 ImVec3(this ClRand rand)
        {
            return new ImVec3(rand.Float(), rand.Float(), rand.Float());
        }

        public static ImVec3 ImVec3(this ClRand rand, ImVec3 range)
        {
            return new ImVec3(rand.Float() * range.X, rand.Float() * range.Y, rand.Float() * range.Z);
        }

        public static ImVec3 ImVec3(this ClRand rand, ImVec3 min, ImVec3 max)
        {
            return ImVec3(rand, max - min) + min;
        }

        public static ImVec3Int ImVec3Int(this ClRand rand, ImVec3Int range)
        {
            return new ImVec3Int(rand.IntRange(0, range.X), rand.IntRange(0, range.Y), rand.IntRange(0, range.Z));
        }

        public static ImVec3Int ImVec3Int(this ClRand rand, ImVec3Int min, ImVec3Int max)
        {
            return ImVec3Int(rand, max - min) + min;
        }

}