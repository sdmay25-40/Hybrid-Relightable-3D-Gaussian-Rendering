using System.Collections.Generic;
// Needed to shorten use of big integer without introducing confusion between 
// Unity and System types
using N = System.Numerics; 
using UnityEngine;
using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

public static class BuildBVH 
{   
    private const float COST_TRAV = 1;
    private const float COST_ITRSCT = 2;
    private const float NUM_DIVISIONS = 8;

    /// <summary>
    /// Calculate the total area of an Axis Aligned Bounding Box
    /// </summary>
    private static float CalcAABBArea(AABB calcAreaOf)
    {
        return (calcAreaOf.max.x - calcAreaOf.min.x) * (calcAreaOf.max.y - calcAreaOf.min.y) * (calcAreaOf.max.z - calcAreaOf.min.z);
    }

    /// <summary>
    /// Calculate the centroid of a triangle from its 3 vertices
    /// </summary>
    private static Vector3 CalcTriangleCentroid(Vector3 vert1, Vector3 vert2, Vector3 vert3)
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
    private static void AddTriangleStruct(ref List<Triangle> triangles, int meshVertexStartIndex, int triangleVertexStartIndex, int[] meshTriangleArray)
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

            Vector3 centroid = CalcTriangleCentroid(vert0, vert1, vert2);

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
            (CalcAABBArea(left) * (COST_ITRSCT * leftBoxTris.Count)) +
            (CalcAABBArea(right) * (COST_ITRSCT * rightBoxTris.Count));
            
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
                AddTriangleStruct(ref triangles, vertexStartIndex, bestLeftTris[i], meshTriangles);
            }

            // add right triangles to list
            bestRight.primitiveCount = (uint) bestRightTris.Count;
            bestRight.primitiveStartIndex = (uint) triangles.Count;
            for(int i = 0; i < bestRightTris.Count; i++)
            {
                AddTriangleStruct(ref triangles, vertexStartIndex, bestRightTris[i], meshTriangles);
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
                AddTriangleStruct(ref triangles, vertexStartIndex, bestLeftTris[0], meshTriangles);
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
                AddTriangleStruct(ref triangles, vertexStartIndex, bestRightTris[0], meshTriangles);
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

            triangleCentroids[i / 3] = CalcTriangleCentroid(vert0, vert1, vert2);
            tris[i / 3] = i; 
        }

        // divide mesh
        root.primitiveCount = uint.MaxValue;  
        DivideAABB(triangleCentroids, meshTriangles, tris, 1, ref root, ref aabbList, ref triangles, vertices, (int) vertexStartIndex); 
        aabbList.Add(root);

        return (uint) (aabbList.Count - 1);
    }


    /// <summary>
    /// Expand the bits in a number to have gaps as part of morton code encoding.
    /// </summary>
    /// <source>https://www.forceflow.be/2013/10/07/morton-encodingdecoding-through-bit-interleaving-implementations//</source>
    private static N.BigInteger SplitBy3(int a){
        N.BigInteger x = a;
        x = (x | x << 32) & 0x1f00000000ffff; // shift left 32 bits, OR with self, and 00011111000000000000000000000000000000001111111111111111
        x = (x | x << 16) & 0x1f0000ff0000ff; // shift left 32 bits, OR with self, and 00011111000000000000000011111111000000000000000011111111
        x = (x | x << 8) & 0x100f00f00f00f00f; // shift left 32 bits, OR with self, and 0001000000001111000000001111000000001111000000001111000000000000
        x = (x | x << 4) & 0x10c30c30c30c30c3; // shift left 32 bits, OR with self, and 0001000011000011000011000011000011000011000011000011000100000000
        x = (x | x << 2) & 0x1249249249249249;

        return x;
    }

    /// <summary>
    /// Calculate the morton codes for all Gaussians 
    /// </summary>
    /// <source>https://www.forceflow.be/2013/10/07/morton-encodingdecoding-through-bit-interleaving-implementations/</source>
    private static MortonPayload[] CalcMortonCodes(Gaussian3D[] gaussians){
       MortonPayload[] mortonCodes = new MortonPayload[gaussians.Length];
        for(int i = 0; i < gaussians.Length; i++){
           Gaussian3D g = gaussians[i];
            N.BigInteger x = SplitBy3((int) g.pos.x);
            N.BigInteger y = SplitBy3((int) g.pos.y);
            N.BigInteger z = SplitBy3((int) g.pos.z);

            MortonPayload p;
            p.mortonCode = x | y << 1 | z << 2;
            p.gaussianIdx = i;

            mortonCodes[i] = p;
    
        }

        return mortonCodes;

    }

    /// <summary>
    /// Convert a Gaussian's covariance matrix stored as a 4x4 Unity matrix to 
    /// a 3x3 MathDotNet Matrix. 
    /// </summary>
    private static Matrix<float> ConvertCovMatrixToMathNetMatrix(Matrix4x4 toConvert){
        Matrix<float> m = Matrix<float>.Build.Dense(3, 3);
        for(int i = 0; i < 3; i++){
            float[] row = {toConvert.GetRow(i).x, toConvert.GetRow(i).y, toConvert.GetRow(i).z};
            m.SetRow(i, row);
        }

        return m;
    }


    private static AABB CreateAABBForGaussian(Gaussian3D gaussian, uint gaussianIdx){
        AABB aabb = new AABB();
        aabb.primitiveCount = 1u;
        aabb.primitiveStartIndex = gaussianIdx;
        aabb.primitiveType = PrimType.Gaussian;


        /* Algorithm: Find standard deviation for each dimension * it by 1 + % of data to capute.
        Then use that to find min/max
        */   
        Matrix<float> covAsMathNetMatrix = ConvertCovMatrixToMathNetMatrix(gaussian.cov);
        // There's an argument for writing this ourselves but it wouldn't be fun
        Evd<float> evd = covAsMathNetMatrix.Evd();

        float standardDeviationX = MathF.Sqrt((float) evd.EigenValues[0].Real);
        float standardDeviationY = MathF.Sqrt((float) evd.EigenValues[1].Real);
        float standardDeviationZ = MathF.Sqrt((float) evd.EigenValues[2].Real);

        // TODO: Make tightness of Gaussian Bounding boxes (The 3.0f here) configruable by the end user
        Vector3 s1 = 3.0f * standardDeviationX * new Vector3(evd.EigenVectors.Column(0)[0], evd.EigenVectors.Column(0)[1],
            evd.EigenVectors.Column(0)[2]);
        Vector3 s2 = 3.0f * standardDeviationY * new Vector3(evd.EigenVectors.Column(1)[0], evd.EigenVectors.Column(1)[1],
            evd.EigenVectors.Column(1)[2]);
        Vector3 s3 = 3.0f * standardDeviationZ * new Vector3(evd.EigenVectors.Column(2)[0], evd.EigenVectors.Column(2)[1],
            evd.EigenVectors.Column(2)[2]);


        aabb.min = gaussian.pos - s1 - s2 - s3;
        aabb.max = gaussian.pos + s1 + s2 + s3;
        
        return aabb;
    }

    private static void CreateParentNode(int leftIdx, int rightIdx, ref List<AABB> aabbs){
        AABB aabb = new AABB();

        aabb.primitiveCount = uint.MaxValue;
        aabb.leftChildIndex = (uint) leftIdx;
        aabb.rightChildIndex = (uint) rightIdx;   

        aabb.min = Vector3.Min(aabbs[leftIdx].min, aabbs[rightIdx].min);
        aabb.max = Vector3.Max(aabbs[leftIdx].max, aabbs[rightIdx].max);

        aabbs.Add(aabb);
    }

    private static void BuildAABBsFromTree(MortonPayload[] mortonCodeTree, ref List<AABB> aabbs, 
        Gaussian3D[] gaussians, int gaussianBufferIdx){
        // Create AABBs for every gaussian primitive
        int lastLayerStartIndex = aabbs.Count;
        int lastLayerCount = 0;
        foreach(MortonPayload mortonCode in mortonCodeTree){
            aabbs.Add(CreateAABBForGaussian(gaussians[mortonCode.gaussianIdx], (uint) (mortonCode.gaussianIdx + gaussianBufferIdx)));
            lastLayerCount++;
        }

        // Build BVH from AABBs
        while(lastLayerCount > 1){
            int thisLayerStartIndex = aabbs.Count;
            int thisLayerCount = 0;

            for(int i = 0; i < lastLayerCount - 1; i+=2){
                CreateParentNode(lastLayerStartIndex + i, lastLayerStartIndex + i +1, ref aabbs);
                thisLayerCount += 1;
            }

            // Handle an odd number by moving node up a layer
            if(lastLayerCount % 2 != 0){
                AABB aabb = new AABB();

                aabb.primitiveCount = uint.MaxValue;
                int childIdx = lastLayerStartIndex + lastLayerCount - 1;
                aabb.leftChildIndex = (uint) childIdx;
                aabb.rightChildIndex = uint.MaxValue;   

                aabb.min = aabbs[childIdx].min;
                aabb.max = aabbs[childIdx].max;

                aabbs.Add(aabb);

                thisLayerCount++;
            }

            lastLayerStartIndex = thisLayerStartIndex;
            lastLayerCount = thisLayerCount;
        }
        
    }


    public static uint BuildBVHForGaussians(Gaussian3D[] gaussians,
        ref List<AABB> aabbs, int gaussianBufferIdx){
        
        int starting = aabbs.Count;
        // Calculate morton codes for all Gaussians
        MortonPayload[] mortonCodes = CalcMortonCodes(gaussians);

        // Sort Morton codes to create tree
        Array.Sort(mortonCodes, new MortonPayloadComp());

        // Create AABBs 
        BuildAABBsFromTree(mortonCodes, ref aabbs, gaussians, gaussianBufferIdx);

        return (uint) (aabbs.Count - 1);

    }


}