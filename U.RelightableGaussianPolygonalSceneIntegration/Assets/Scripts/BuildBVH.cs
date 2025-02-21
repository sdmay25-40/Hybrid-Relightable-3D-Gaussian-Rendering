using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

public static class BuildBVH 
{   

    private static readonly float COST_TRAV = 1;
    private static readonly float COST_ITRSCT = 2;
    private static readonly float NUM_DIVISIONS = 3;

    /// <summary>
    /// Determine the triangle position, AAAB size, and cost of a split in an AABB 
    /// </summary>
    private static float DetermineSplit(Vector3 pos, int axis, Vector3[] verts, int[] triangles, int[] includedTriangleIdxs, 
    MeshFilter mesh, ref List<int> leftBoxTris, ref List<int> rightBoxTris, ref AABB left, ref AABB right){
        left.min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        left.max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        right.min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        right.max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach(int tri in includedTriangleIdxs){
            // Get triangle info 
            Vector3 vert0 = verts[triangles[tri]];
            Vector3 vert1 = verts[triangles[tri + 1]];
            Vector3 vert2 = verts[triangles[tri + 2]];

            Vector3 centroid = Utils.CalcTriangleCentroid(vert0, vert1, vert2);

            bool onLeft = false;

            // Split on the x Axis
            if(axis == 0){
                if(centroid.x < pos.x){
                    onLeft = true;
                }
            }
            // Split on the y Axis
            else if(axis == 1){
                if(centroid.y < pos.y){
                    onLeft = true;
                }
            }
            // Split on the the z Axis
            else{
                if(centroid.z < pos.z){
                    onLeft = true;
                }
            }
            

            // Add triangle to proper side
            if(onLeft){
                leftBoxTris.Add(tri);
                left.min = Vector3.Min(left.min, vert0);
                left.min = Vector3.Min(left.min, vert1);
                left.min = Vector3.Min(left.min, vert2);

                left.max = Vector3.Max(left.max, vert0);
                left.max = Vector3.Max(left.max, vert1);
                left.max = Vector3.Max(left.max, vert2);    

            }
            else{          
                rightBoxTris.Add(tri);
                right.min = Vector3.Min(right.min, vert0);
                right.min = Vector3.Min(right.min, vert1);
                right.min = Vector3.Min(right.min, vert2);

                right.max = Vector3.Max(right.max, vert0);
                right.max = Vector3.Max(right.max, vert1);
                right.max = Vector3.Max(right.max, vert2); 
            } 
        }

        // Caculate the cost of this division 
        float cost = COST_TRAV + 
            (Utils.CalcAABBArea(left) * (COST_ITRSCT * leftBoxTris.Count)) +
            (Utils.CalcAABBArea(right) * (COST_ITRSCT * rightBoxTris.Count));
            
        return cost;
    }

    private static void DivideAABB(Vector3[] triangleCentroids, MeshFilter mesh, int[] triangleIdxs, 
        int divisionNum, ref AABB root, ref List<AABB> aabbList, ref List<Triangle> triangles){

        // Evaluate all possible splits (All three axis for every centroid)
        float lowestCost = float.MaxValue;
        List<int> bestLeftTris = new List<int>();
        List<int> bestRightTris = new List<int>();
        AABB bestLeft = new AABB();
        AABB bestRight = new AABB();

    
        for(int i = 0; i < 3; i++){
            foreach(int triIdx in triangleIdxs){
                Vector3 cent = triangleCentroids[triIdx / 3];
                List<int> leftTris = new List<int>();
                List<int> rightTris = new List<int>();
                AABB left = new AABB();
                AABB right = new AABB();

                // Determine the triangle position, AABB size, and cost of this split
                float divCost = DetermineSplit(cent, i, mesh.sharedMesh.vertices, mesh.sharedMesh.triangles, 
                    triangleIdxs, mesh, ref leftTris, ref rightTris, ref left, ref right);

                // If this split is the lowest cost update values to match for it 
                if(divCost < lowestCost){
                    lowestCost = divCost;
                    bestLeftTris = leftTris;
                    bestRightTris = rightTris;
                    bestLeft = left;
                    bestRight = right;
                }
            }
        }

        // Setup leaf nodes or subdivide further
        if(divisionNum == NUM_DIVISIONS){
            // Add left triangles to list (if there are any)
            bestLeft.triangleCount = (uint) bestLeftTris.Count;
            bestLeft.triangleStartIndex = (uint) triangles.Count;
            for(int i = 0; i < bestLeftTris.Count; i++){
                int vert0Idx = mesh.sharedMesh.triangles[bestLeftTris[i]];
                int vert1Idx = mesh.sharedMesh.triangles[bestLeftTris[i] + 1];
                int vert2Idx = mesh.sharedMesh.triangles[bestLeftTris[i] + 2];

                Triangle tri = new Triangle();
                tri.position0 = Utils.Vec3ToVec4(mesh.sharedMesh.vertices[vert0Idx]);
                tri.position1 = Utils.Vec3ToVec4(mesh.sharedMesh.vertices[vert1Idx]);
                tri.position2 = Utils.Vec3ToVec4(mesh.sharedMesh.vertices[vert2Idx]);

                triangles.Add(tri);
            }

            // Add right triangles to list
            bestRight.triangleCount = (uint) bestRightTris.Count;
            bestRight.triangleStartIndex = (uint) triangles.Count;
            for(int i = 0; i < bestRightTris.Count; i++){
                int vert0Idx = mesh.sharedMesh.triangles[bestRightTris[i]];
                int vert1Idx = mesh.sharedMesh.triangles[bestRightTris[i] + 1];
                int vert2Idx = mesh.sharedMesh.triangles[bestRightTris[i] + 2];

                Triangle tri = new Triangle();
                tri.position0 = Utils.Vec3ToVec4(mesh.sharedMesh.vertices[vert0Idx]);
                tri.position1 = Utils.Vec3ToVec4(mesh.sharedMesh.vertices[vert1Idx]);
                tri.position2 = Utils.Vec3ToVec4(mesh.sharedMesh.vertices[vert2Idx]);
                
                triangles.Add(tri);


            }

            // Setup root
            root.leftChildIndex = (uint) aabbList.Count;
            aabbList.Add(bestLeft);
            root.rightChildIndex = (uint) aabbList.Count;
            aabbList.Add(bestRight);
        }
        else{
            // Subdivide left and right (or make them leaf nodes if they have no triangles)
            bestLeft.triangleCount = uint.MaxValue;
            DivideAABB(triangleCentroids, mesh, bestLeftTris.ToArray(), divisionNum + 1,
                ref bestLeft, ref aabbList, ref triangles);
            bestRight.triangleCount = uint.MaxValue;
            DivideAABB(triangleCentroids, mesh, bestRightTris.ToArray(), divisionNum + 1,
                ref bestRight, ref aabbList, ref triangles);
            
            // Setup root
            root.leftChildIndex = (uint) aabbList.Count;
            aabbList.Add(bestLeft);
            root.rightChildIndex = (uint) aabbList.Count;
            aabbList.Add(bestRight);
        }
    }


    public static uint BuildBVHForMesh(MeshFilter mesh, ref List<AABB> aabbList, ref List<Triangle> triangles){
        // Get all centroids of triangles in this mesh and set min and max
        int[] tris = new int[mesh.sharedMesh.triangles.Length / 3];
        Vector3[] triangleCentroids = new Vector3[mesh.sharedMesh.triangles.Length / 3];

        AABB root = new AABB();
        root.min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        root.max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int i =0; i < mesh.sharedMesh.triangles.Length; i+=3){
            int vert0Idx = mesh.sharedMesh.triangles[i];
            int vert1Idx = mesh.sharedMesh.triangles[i + 1];
            int vert2Idx = mesh.sharedMesh.triangles[i + 2];

            Vector3 vert0 = mesh.sharedMesh.vertices[vert0Idx];
            Vector3 vert1 = mesh.sharedMesh.vertices[vert1Idx];
            Vector3 vert2 = mesh.sharedMesh.vertices[vert2Idx];

            root.min = Vector3.Min(root.min, vert0);
            root.min = Vector3.Min(root.min, vert1);
            root.min = Vector3.Min(root.min, vert2);
            root.max = Vector3.Max(root.max, vert0);
            root.max = Vector3.Max(root.max, vert1);
            root.max = Vector3.Max(root.max, vert2);

            triangleCentroids[i / 3] =  Utils.CalcTriangleCentroid(vert0, vert1, vert2);
            tris[i / 3] = i; 
        }

        // Divide mesh
        root.triangleCount = uint.MaxValue;  
        DivideAABB(triangleCentroids, mesh, tris, 1, ref root, ref aabbList, ref triangles); 
        aabbList.Add(root);

        return (uint) (aabbList.Count - 1);

    }
}
