using System;

namespace Geom_Util;

public struct MeshOptions
{
    public Func<Face, bool> Faces_filter = null;

    public bool Edges_IncludeSharp = true;
    public bool Edges_IncludeSmooth = true;
    public bool Edges_DetermineSmoothnessFromAngle = false; //< if set, call edges "sharp" or "smooth" based on their tagging
                                                            //< otherwise determine that based on the adjoining faces angle
                                                            //< and SplitangleDegrees
    public float Edges_Offset = 0f;                         // push edges out along the vert normals by this much
                                                            // lets us pull the mesh out of the surface, to remove Z-fighting
                                                            // (clever material Z-offset would be better, but I did not find that yet)

    public Func<Edge, bool> Edges_Filter;                   //< if set, ignore above flags and include exactly the set of edges
                                                            //< for which this is true

    public bool Normals_IncludeVert = false;
    public bool Normals_IncludeSplitVert = false;
    public bool Normals_IncludeEdge = false;               //< nothing uses these, but could be of interest
    public bool Normals_IncludeFace = false;

    public float? SplitAngleDegrees;                        //< if set, overrides splitting along edges tagged sharp
                                                            //< and instead splits along those with > this angle across them

    public float DrawNormalsLength = 0.5f;                  //< reasonable if your input data is around unit sized

    public MeshOptions()
    {
    }
}