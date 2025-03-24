using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public enum MaterialType : uint
{
    Diffuse = 0,
    Emissive = 1,
    Textured = 2
}

// when updating, ensure structs in 'Shaders/utils.cginc' are updated to match
// ensure structs satisfy 16-byte alignment; padding is only necessary for arrays
public struct AABB
{
    public Vector3 min;
    public Vector3 max;
    public uint leftChildIndex;
    public uint rightChildIndex;
    public uint triangleCount; // if not a leaf node, set to uint.MaxValue
    public uint triangleStartIndex;
    private Vector2 padding;
}

public struct CameraData
{
    public Vector4 position;
    public Vector4 quaternion;
}

public struct GameObjectData
{
    public Matrix4x4 normalMatrix;
    public Matrix4x4 worldToObject;
    public uint aabbRootIndex;
    public uint materialIndex;
    private Vector2 padding;
}

public struct MaterialData
{
    public MaterialType type;
    public Vector4 albedo;
    public uint albedoTextureIndex;
    private Vector2 padding;
}

public struct PathHitRecord
{
    public float t;
    public uint materialType;
    public Vector4 albedo;
    public Vector3 normal;
    private Vector3 padding;
}

public struct PathPayload
{
    public Vector4 direction;
    public Vector3 origin;
    public uint bounce;
    public Vector4 throughput;
}

public struct Triangle
{
    public uint v0;
    public uint v1;
    public uint v2;
    private uint padding;
}

public struct Vertex
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 albedoUV;
}

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

    public static string AABBToString(AABB bb){
        return "{Max: " + bb.max + ", Min: " + bb.min + " Count:" + bb.triangleCount + ", Left Child Index"
            + bb.leftChildIndex + ", Right Child Index: " + bb.rightChildIndex;
    }

    public static string TriangleToString(Triangle tri, List<Vertex> vertices){
        Vector3 vert0 = vertices[(int) tri.v0].position;
        Vector3 vert1 = vertices[(int) tri.v1].position;
        Vector3 vert2 = vertices[(int) tri.v2].position;

        return "{Point0: " + vert0.x + "," + vert0.y + "," + vert0.z  + " " +
        "Point1: " + vert1.x + "," + vert1.y + "," + vert1.z  + " " +
        "Point2: " + vert2.x + "," + vert2.y + "," + vert2.z  + "}";
    }

    public static void WriteBVHToFile(List<AABB> toWrite, int rootIndex){
        string bvhStr = "";

        List<AABB> currLevel = new List<AABB>();
        List<AABB> lastLevel = new List<AABB>();
        lastLevel.Add(toWrite[rootIndex]);
        bvhStr += AABBToString(toWrite[rootIndex]) + "\n";

        while(lastLevel[0].triangleCount == uint.MaxValue){
            // For every AABB processed last iteration
            foreach(AABB bb in lastLevel){
                // Process children
                bvhStr += AABBToString(toWrite[(int) bb.leftChildIndex]) + " ";
                currLevel.Add(toWrite[(int) bb.leftChildIndex]);

                bvhStr += AABBToString(toWrite[(int) bb.rightChildIndex]) + " ";
                currLevel.Add(toWrite[(int) bb.rightChildIndex]);
            }
            lastLevel = currLevel;
            currLevel = new List<AABB>();
            bvhStr += "\n";
        }

        File.WriteAllText("bvhDebug.txt", bvhStr);
    }

    /// <summary>
    /// Returns
    /// true: If the two triangles are equal
    /// false: If the two triangles aren't equal
    /// </summary>
    public static bool TriangleEquals(Triangle t1, Triangle t2){
        List<uint> t1Verts = new List<uint>{t1.v0, t1.v1, t1.v2};
        List<uint> t2Verts = new List<uint>{t2.v0, t2.v1, t2. v2};
        // Check all of the t1 verts against the t2Verts
        foreach(uint v in t1Verts){
            bool vFound = false;
            for(int i = 0; i < t2Verts.Count; i++){
                if(v == t2Verts[i]){
                    vFound = true;
                    t2Verts.RemoveAt(i);
                    break;
                }
            }

            // If this vertex hasn't been found return false
            if(!vFound){
                return false;
            }
        }

        // Return true because all vertices have been found 
        return true;
    }

    // Check if a given AABB is inside the AABB defined by the given min and max values
    public static bool AABBWithinMinMax(AABB aabb, Vector3 min, Vector3 max){
        if(aabb.min.x >= min.x && aabb.min.y >= min.y && aabb.min.z >= min.z
        && aabb.max.x <= max.x && aabb.max.y <= max.y && aabb.max.z <= max.z){
            return true;
        }

        return false;
    }



    // Check if a given point is within a minimum or maximum
    public static bool PointWithinMinMax(Vector4 point, Vector3 min, Vector3 max){
        if(point.x >= min.x && point.y >= min.y && point.z >= min.z 
        && point.x <= max.x && point.y <= max.y && point.z <= max.z){
            return true;
        }

        return false;
    }

    // Check is a triangle is located between a minimum and maximum
    public static bool TriangleWithinMinMax(Triangle tri, Vector3 min, Vector3 max, MeshFilter meshTriWithin,
        List<Vertex> vertices){
        Vector3 vert0 = vertices[(int) tri.v0].position;
        Vector3 vert1 = vertices[(int) tri.v1].position;
        Vector3 vert2 = vertices[(int) tri.v2].position;

        if(PointWithinMinMax(meshTriWithin.transform.TransformPoint(vert0), min, max) 
        && PointWithinMinMax(meshTriWithin.transform.TransformPoint(vert1), min, max) 
        && PointWithinMinMax(meshTriWithin.transform.TransformPoint(vert2), min, max)){
            return true;
        }

        return false;
    }
}
