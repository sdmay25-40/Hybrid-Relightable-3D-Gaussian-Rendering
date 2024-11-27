using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public static class Utils {
    /// <summary>
    ///  Calculate the total area of an Axis Aligned Bounding Box
    /// </summary>
    /// 
    public static float CalcAABBArea(AABB calcAreaOf){
        return (calcAreaOf.max.x - calcAreaOf.min.x) * (calcAreaOf.max.y - calcAreaOf.min.y) 
            * (calcAreaOf.max.z - calcAreaOf.min.z);
    }

    /// <summary>
    /// Calculate the centroid of a triangle from its 3 vertices
    /// </summary>
    public static Vector3 CalcTriangleCentroid(Vector3 vert1, Vector3 vert2, Vector3 vert3){
        float xCent = (vert1.x + vert2.x + vert3.x) / 3.0f;
        float yCent = (vert1.y + vert2.y + vert3.y) / 3.0f;
        float zCent = (vert1.z + vert2.z + vert3.z) / 3.0f;

        return  new Vector3(xCent, yCent, zCent);
    }

    /// <summary>
    /// Convert a Vector3 to a Vector4 with the same xyz components and a w component of 1
    /// </summary>
    public static Vector4 Vec3ToVec4(Vector3 toCon){
        return new Vector4(toCon.x, toCon.y, toCon.z, 1);
    }
}