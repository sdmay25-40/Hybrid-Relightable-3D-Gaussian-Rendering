using System;
using System.Collections.Generic;
using System.IO;
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

    public static string AABBToString(AABB bb){
        return "{Max: " + bb.max + ", Min: " + bb.min + " Count:" + bb.triangleCount + ", Left Child Index"
            + bb.leftChildIndex + ", Right Child Index: " + bb.rightChildIndex;
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
}