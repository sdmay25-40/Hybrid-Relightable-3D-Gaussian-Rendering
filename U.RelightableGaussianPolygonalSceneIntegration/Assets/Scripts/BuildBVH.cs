using System.Collections.Generic;
using UnityEngine;

public static class BuildBVH 
{   
    private const float COST_TRAV = 1;
    private const float COST_ITRSCT = 2;
    private const float NUM_DIVISIONS = 8;

    /// <summary>
    /// Calculate the total area of an Axis Aligned Bounding Box
    /// </summary>
    private static float calcAABBArea(AABB calcAreaOf)
    {
        return (calcAreaOf.max.x - calcAreaOf.min.x) * (calcAreaOf.max.y - calcAreaOf.min.y) * (calcAreaOf.max.z - calcAreaOf.min.z);
    }

    /// <summary>
    /// Calculate the centroid of a triangle from its 3 vertices
    /// </summary>
    private static Vector3 calcTriangleCentroid(Vector3 vert1, Vector3 vert2, Vector3 vert3)
    {
        float xCent = (vert1.x + vert2.x + vert3.x) / 3.0f;
        float yCent = (vert1.y + vert2.y + vert3.y) / 3.0f;
        float zCent = (vert1.z + vert2.z + vert3.z) / 3.0f;
        return  new Vector3(xCent, yCent, zCent);
    }
    
    /// <summary>
    /// Create a new Triangle struct corresponding to a triangle within a mesh
    /// and add it to the List for triangles.
    /// </summary>
    /// <param name="triangles">The List of all Triangle structs in this scene </param>
    /// <param name="vertices">The List contaning all Vertex structs for the scene </param>
    /// <param name="meshVertexStartIndex">The index of the first vertex for this mesh
    /// within the vertices List</param>
    /// <param name="triangleVertexStartIndex">The index of the first vertex for this triangle 
    /// within the mesh's triangles array</param>    
    /// <param name="meshTriangleArray">The triangle's array for the mesh containign the triangle </param>
    private static void addTriangleStruct(ref List<Triangle> triangles, int meshVertexStartIndex, int triangleVertexStartIndex, int[] meshTriangleArray)
    {
        // make struct and add it to list
        Triangle t = new Triangle(){
            v0 = (uint) (meshTriangleArray[triangleVertexStartIndex] + meshVertexStartIndex),
            v1 = (uint) (meshTriangleArray[triangleVertexStartIndex + 1] + meshVertexStartIndex),
            v2 = (uint) (meshTriangleArray[triangleVertexStartIndex + 2] + meshVertexStartIndex)
        };
        triangles.Add(t);
    }

    /// <source>https://medium.com/@bromanz/how-to-create-awesome-accelerators-the-surface-area-heuristic-e14b5dec6160</source>
    /// <source>https://pbr-book.org/3ed-2018/Primitives_and_Intersection_Acceleration/Bounding_Volume_Hierarchies</source>
    /// <summary>
    /// Determine the triangle position, AAAB size, and cost of a split in an AABB 
    /// </summary> 
    private static float DetermineSplit(Vector3 pos, int axis, int[] triangles, int[] includedTriangleIdxs, ref List<int> leftBoxTris, ref List<int> rightBoxTris, ref AABB left, ref AABB right, List<Vertex> vertices, int vertexStartIndex)
    {
        left.min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        left.max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        right.min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        right.max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach(int tri in includedTriangleIdxs){
            // Get triangle info 
            Vector3 vert0 = vertices[triangles[tri] + vertexStartIndex].position;
            Vector3 vert1 = vertices[triangles[tri + 1] + vertexStartIndex].position;
            Vector3 vert2 = vertices[triangles[tri + 2] + vertexStartIndex].position;

            Vector3 centroid = calcTriangleCentroid(vert0, vert1, vert2);

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
            (calcAABBArea(left) * (COST_ITRSCT * leftBoxTris.Count)) +
            (calcAABBArea(right) * (COST_ITRSCT * rightBoxTris.Count));
            
        return cost;
    }

    /// <source> https://pbr-book.org/3ed-2018/Primitives_and_Intersection_Acceleration/Bounding_Volume_Hierarchies </source>
    /// *The Source was used as a guide to develop but the implthis not a one to 
    /// one implementation.
    private static void DivideAABB(Vector3[] triangleCentroids, int[] meshTriangles, int[] triangleIdxs, int divisionNum, ref AABB root, ref List<AABB> aabbList, ref List<Triangle> triangles, List<Vertex> vertices, int vertexStartIndex){
        // evaluate all possible splits (all three axis for every centroid)
        float lowestCost = float.MaxValue;
        List<int> bestLeftTris = new List<int>();
        List<int> bestRightTris = new List<int>();
        AABB bestLeft = new AABB();
        AABB bestRight = new AABB();
        bestLeft.primitiveType = PrimType.Triangle;
        bestRight.primitiveType = PrimType.Triangle;
    
        for(int i = 0; i < 3; i++)
        {
            foreach(int triIdx in triangleIdxs)
            {
                Vector3 cent = triangleCentroids[triIdx / 3];
                List<int> leftTris = new List<int>();
                List<int> rightTris = new List<int>();
                AABB left = new AABB();
                AABB right = new AABB();
                left.primitiveType = PrimType.Triangle;
                right.primitiveType = PrimType.Triangle;

                // determine the triangle position, AABB size, and cost of this split
                float divCost = DetermineSplit(cent, i, meshTriangles, triangleIdxs, ref leftTris, ref rightTris, ref left, ref right, vertices, vertexStartIndex);

                // if this split is the lowest cost update values to match for it 
                if (divCost < lowestCost)
                {
                    lowestCost = divCost;
                    bestLeftTris = leftTris;
                    bestRightTris = rightTris;
                    bestLeft = left;
                    bestRight = right;
                }
            }
        }

        // setup leaf nodes or subdivide further
        if (divisionNum == NUM_DIVISIONS)
        {
            // add left triangles to list (if there are any)
            bestLeft.primitiveCount = (uint) bestLeftTris.Count;
            bestLeft.primitiveStartIndex = (uint) triangles.Count;
            for(int i = 0; i < bestLeftTris.Count; i++)
            {
                addTriangleStruct(ref triangles, vertexStartIndex, bestLeftTris[i], meshTriangles);
            }

            // add right triangles to list
            bestRight.primitiveCount = (uint) bestRightTris.Count;
            bestRight.primitiveStartIndex = (uint) triangles.Count;
            for(int i = 0; i < bestRightTris.Count; i++)
            {
                addTriangleStruct(ref triangles, vertexStartIndex, bestRightTris[i], meshTriangles);
            }

            // setup root
            root.leftChildIndex = (uint) aabbList.Count;
            aabbList.Add(bestLeft);
            root.rightChildIndex = (uint) aabbList.Count;
            aabbList.Add(bestRight);
        }
        else
        {
            // subdivide left and right (or make them leaf nodes if they have only one triangle)
            if(bestLeftTris.Count == 1)
            {
                bestLeft.primitiveCount = 1;
                bestLeft.primitiveStartIndex = (uint) triangles.Count;
                addTriangleStruct(ref triangles, vertexStartIndex, bestLeftTris[0], meshTriangles);
            }
            else
            {
                bestLeft.primitiveCount = uint.MaxValue;
                DivideAABB(triangleCentroids, meshTriangles, bestLeftTris.ToArray(), divisionNum + 1, ref bestLeft, ref aabbList, ref triangles, vertices, vertexStartIndex);
            }

            if (bestRightTris.Count == 1)
            {
                bestRight.primitiveCount = 1;
                bestRight.primitiveStartIndex = (uint) triangles.Count;
                addTriangleStruct(ref triangles, vertexStartIndex, bestRightTris[0], meshTriangles);
            }
            else
            {
                bestRight.primitiveCount = uint.MaxValue;
                DivideAABB(triangleCentroids, meshTriangles, bestRightTris.ToArray(), divisionNum + 1, ref bestRight, ref aabbList, ref triangles, vertices, vertexStartIndex);
            }

            // setup root
            root.leftChildIndex = (uint) aabbList.Count;
            aabbList.Add(bestLeft);
            root.rightChildIndex = (uint) aabbList.Count;
            aabbList.Add(bestRight);
        }
    }

    public static uint BuildBVHForMesh(int[] meshTriangles, ref List<AABB> aabbList, ref List<Triangle> triangles, ref List<Vertex> vertices, uint vertexStartIndex)
    {
        // get all centroids of triangles in this mesh and set min and max
        int[] tris = new int[meshTriangles.Length / 3];
        Vector3[] triangleCentroids = new Vector3[meshTriangles.Length / 3];
        
        AABB root = new AABB();
        root.primitiveType = PrimType.Triangle;
        root.min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        root.max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int i =0; i < meshTriangles.Length; i+=3)
        {
            int vert0Idx = meshTriangles[i];
            int vert1Idx = meshTriangles[i + 1];
            int vert2Idx = meshTriangles[i + 2];

            Vector3 vert0 = vertices[vert0Idx + (int) vertexStartIndex].position;
            Vector3 vert1 = vertices[vert1Idx + (int) vertexStartIndex].position;
            Vector3 vert2 = vertices[vert2Idx + (int) vertexStartIndex].position;

            root.min = Vector3.Min(root.min, vert0);
            root.min = Vector3.Min(root.min, vert1);
            root.min = Vector3.Min(root.min, vert2);
            root.max = Vector3.Max(root.max, vert0);
            root.max = Vector3.Max(root.max, vert1);
            root.max = Vector3.Max(root.max, vert2);

            triangleCentroids[i / 3] = calcTriangleCentroid(vert0, vert1, vert2);
            tris[i / 3] = i; 
        }

        // divide mesh
        root.primitiveCount = uint.MaxValue;  
        DivideAABB(triangleCentroids, meshTriangles, tris, 1, ref root, ref aabbList, ref triangles, vertices, (int) vertexStartIndex); 
        aabbList.Add(root);

        return (uint) (aabbList.Count - 1);
    }
}