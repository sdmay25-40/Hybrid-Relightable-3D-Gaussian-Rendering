using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

public static class BuildAABB 
{   

    private static readonly float COST_TRAV = 1;
    private static readonly float COST_ITRSCT = 2;
    private static readonly float NUM_DIVISIONS = 8;

    /// <summary>
    /// Determine the triangle position, AAB size, and cost of a split in an AABB 
    /// </summary>
    private static float DetermineSplit(Vector3 pos, int axis, Vector3[] verts, int[] triangles, int[] includedTriangleIdxs, 
    ref List<int> leftBoxTris, ref List<int> rightBoxTris, ref AABB left, ref AABB right){
        left.min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        left.max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        right.min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        right.max = new Vector3(float.MinValue, float.MinValue, float.MinValue);



        foreach(int tri in includedTriangleIdxs){
            // Get triangle info 
            Vector3 vert1 = verts[triangles[tri]];
            Vector3 vert2 = verts[triangles[tri] + 1];
            Vector3 vert3 = verts[triangles[tri] + 2];
            Vector3 centroid = Utils.CalcTriangleCentroid(vert1, vert2, vert3);

            Boolean onLeft = false;

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
            

            // Add triangle to proper sode
            if(onLeft){
                leftBoxTris.Add(tri);
                left.min = Vector3.Min(left.min, vert1);
                left.min = Vector3.Min(left.min, vert2);
                left.min = Vector3.Min(left.min, vert3);

                left.max = Vector3.Max(left.max, vert1);
                left.max = Vector3.Max(left.max, vert2);
                left.max = Vector3.Max(left.max, vert3);    

            }
            else{
                rightBoxTris.Add(tri);
                right.min = Vector3.Min(right.min, vert1);
                right.min = Vector3.Min(right.min, vert2);
                right.min = Vector3.Min(right.min, vert3);

                right.max = Vector3.Max(right.max, vert1);
                right.max = Vector3.Max(right.max, vert2);
                right.max = Vector3.Max(right.max, vert3); 
            }
        }

        // Caculate the cost of this division 
        float cost = COST_TRAV + 
            (Utils.CalcAABBArea(left) * (COST_ITRSCT * leftBoxTris.Count)) +
            (Utils.CalcAABBArea(right) * (COST_ITRSCT * rightBoxTris.Count));
            
        return cost;
    }

    private static void DivideAABB(){
        
    }

    public static void BuildAABBForMesh(MeshFilter mesh, ref List<AABB> aabbList){
        // Get all centroids of triangles in this mesh
        int[] tris = new int[mesh.sharedMesh.triangles.Length / 3];
        Vector3[] triangleCentroids = new Vector3[mesh.sharedMesh.triangles.Length / 3];
        for (int i =0; i < mesh.sharedMesh.triangles.Length / 3; i+=3){
            int vert1Idx = mesh.sharedMesh.triangles[i];
            int vert2Idx = mesh.sharedMesh.triangles[i + 1];
            int vert3Idx = mesh.sharedMesh.triangles[i + 2];

            Vector3 vert1 = mesh.sharedMesh.vertices[vert1Idx];
            Vector3 vert2 = mesh.sharedMesh.vertices[vert2Idx];
            Vector3 vert3 = mesh.sharedMesh.vertices[vert3Idx];

            triangleCentroids[i / 3] =  Utils.CalcTriangleCentroid(vert1, vert2, vert3);
            tris[i / 3] = i; 
        }


        // Evaluate all possible splits (All three axis for every centroid)
        float lowestCost = float.MaxValue;
        List<int> bestLeftTris;
        List<int> bestRightTris;
        AABB bestLeft;
        AABB bestRight;

        for(int i = 0; i < 3; i++){
            foreach(Vector3 cent in triangleCentroids){
                List<int> leftTris = new List<int>();
                List<int> rightTris = new List<int>();
                AABB left = new AABB();
                AABB right = new AABB();

                // Determine the triangle position, AAB size, and cost of this split
                float divCost = DetermineSplit(cent, i, mesh.sharedMesh.vertices, mesh.sharedMesh.triangles, 
                    tris, ref leftTris, ref rightTris, ref left, ref right);

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




        
    }
}
