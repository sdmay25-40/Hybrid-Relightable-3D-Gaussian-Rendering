using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

// THIS IS DEPRECATED SINCE TEXTURING HAS BEEN ADDED.
// I MODIFIED IT TO COMPILE BUT NONE OF THE LOGIC HAS BEEN UPDATED.
public class TestBVHConstruction : MonoBehaviour
{
    public GameObject testObj;

    public GameObject testObj2;

    public List<Material> mats;


    /// <summary>
    /// Check that an AABB has valid left and right child indices for the tree its within and has 
    /// a valid min and max value.
    /// </summary>
    private static bool AABBValid(AABB aabb, List<AABB> tree){
        // If this AABB has valid spacial dimensions
        if(aabb.min == aabb.max){
            return false;
        }
        // If this is a leaf node
        if(aabb.triangleCount < 4294967295){
            return true;
        }
        // If this is a tree node
        else if(aabb.leftChildIndex != aabb.rightChildIndex && aabb.leftChildIndex < tree.Count
            && aabb.rightChildIndex < tree.Count){

            return true;        
        }

        return false;
    }

    private static List<AABB> FindNodesInSubtree(List<AABB> aabbs, int subtreeRootIndex) {
        List<AABB> foundNodes = new List<AABB>();
        foundNodes.Add(aabbs[subtreeRootIndex]);

        // If this is a tree node recurse and add found nodes
        if(aabbs[subtreeRootIndex].triangleCount >= 4294967295){
            List<AABB> leftTreeNodes = FindNodesInSubtree(aabbs, (int) aabbs[subtreeRootIndex].leftChildIndex);
            List<AABB> rightTreeNodes = FindNodesInSubtree(aabbs, (int) aabbs[subtreeRootIndex].rightChildIndex);
            foundNodes.AddRange(leftTreeNodes);
            foundNodes.AddRange(rightTreeNodes);
        }

        return foundNodes;
    }

    /// <summary>
    ///  Check if two AABBs are the same by checking if they have the same child indices
    /// </summary>
    private static bool AABBEquals(AABB a, AABB b){
        // If this is a tree node check for valid
        if(a.leftChildIndex == b.leftChildIndex && a.rightChildIndex == b.rightChildIndex){
            return true;
        }
        return false;
    }

    // Recursivesly move through an AABB and check if each node is within its parent
    // Returns true if all nodes are inside there parents 
    // Returns false is not
    private static bool CheckSubtreeWithinParent(int currIndex, int parentIndex, List<AABB> aabbs){
        AABB parent = aabbs[parentIndex];
        if(!Utils.AABBWithinMinMax(aabbs[currIndex], parent.min, parent.max)){
            Debug.Log(Utils.AABBToString(parent));
            Debug.Log(Utils.AABBToString(aabbs[currIndex]));
            return false;
        }

        // If this is a leaf node
        if(aabbs[currIndex].triangleCount <= 1000000){
            return true;
        }
        // If this is a tree node
        else{
            return CheckSubtreeWithinParent((int) aabbs[currIndex].leftChildIndex, currIndex, aabbs) 
                && CheckSubtreeWithinParent((int) aabbs[currIndex].rightChildIndex, currIndex, aabbs);
        }
    }


    private static bool TestAllTrisFoundInList(MeshFilter meshFilter, List<AABB> aabbs, List<Triangle> tris){
        List<Triangle> bvhTris = new List<Triangle>();
        List<Vertex> bvhVerts = new List<Vertex>();
        // Get a list of all the triangles listed as being contained in an AABB
        for(int i =0; i < aabbs.Count; i++){
            // If this is a leaf node add all triangles in it to the list
            if(aabbs[i].triangleCount < uint.MaxValue){
                int startIndex = (int) aabbs[i].triangleStartIndex;
                for(int j = 0; j < aabbs[i].triangleCount; j++){
                        bvhTris.Add(tris[startIndex + j]);
                }
            }
        }

        // Add vertex data
        for (int i = 0; i < meshFilter.mesh.vertices.Length; i++)
        {
            Vertex v;
            v.position = meshFilter.mesh.vertices[i];
            v.normal = meshFilter.mesh.normals[i];
            v.albedoUV = meshFilter.mesh.uv[i];
            bvhVerts.Add(v);
        }

        // For every triangle in the mesh 
        for(int i = 0; i < meshFilter.mesh.triangles.Length; i+= 3){
            Triangle t = new Triangle();
            t.v0 = (uint) meshFilter.mesh.triangles[i];
            t.v1 = (uint) meshFilter.mesh.triangles[i + 1];
            t.v2 = (uint) meshFilter.mesh.triangles[i + 2];

            bool bvhContains = false;
            // Check if the BVH contains this triangle
            for(int j = 0; j < bvhTris.Count && bvhContains == false; j++){
                if(Utils.TriangleEquals(bvhTris[j], t)){
                    
                    bvhContains = true;
                }
                
            } 
            // If the BVH doesn't contain this triangle the test fails
            if(!bvhContains){
                return false;
            }
        } 

        // If this point is reached the test has been passed
        return true;
    }


    private static bool TestTreesWellFormed(List<AABB> aabbs, int[] rootIndices){
        // Traverse the tree(s) and add all found nodes
        List<AABB> foundNodes = new List<AABB>();
        foreach(int currIndex in rootIndices){
            foundNodes.AddRange(FindNodesInSubtree(aabbs, currIndex));

        }

        // Confirm that all the nodes in the tree were found
        // For each node in the tree
        for(int i = 0; i < aabbs.Count; i++){
            // Check if it was found in the tree traversal
            bool nodeFound = false;
            for(int j = 0; j < foundNodes.Count; j++){
                if(AABBEquals(aabbs[i], foundNodes[j])){
                    nodeFound = true;
                    break;
                }
            }

            if(!nodeFound){
                return false;
            }
        }

        // If this line is reached all nodes were found in the traversal
        return true;

    }
    
    /// <summary>
    /// Check that every AABB in the tree has valid indices and spacial dimensions
    /// </summary>
    private static bool TestAllAABBsValid(List<AABB> aabbs){
        for(int i = 0; i < aabbs.Count; i++){
            if(!AABBValid(aabbs[i], aabbs)){
                return false;
            }
        }
        return true;
    }

    public static bool TestAABBSAllWithinParent(List<AABB> aabbs, int rootIndex){
        return CheckSubtreeWithinParent((int) aabbs[rootIndex].leftChildIndex, rootIndex, aabbs)
         && CheckSubtreeWithinParent((int) aabbs[rootIndex].rightChildIndex, rootIndex, aabbs);
    }

    public static bool TestAllTrianglesWithinAABB(List<AABB> aabbs, List<Triangle> tris, int currIndex, MeshFilter mesh, 
        List<Vertex> vertices){
        // If this is a tree node
        if(aabbs[currIndex].triangleCount >= 4294967295){
            // Recursively call
            return TestAllTrianglesWithinAABB(aabbs, tris, (int) aabbs[currIndex].leftChildIndex, mesh, vertices) 
                && TestAllTrianglesWithinAABB(aabbs, tris, (int) aabbs[currIndex].rightChildIndex, mesh, vertices);
        }
        // If this is a leaf node
        else{
            // Check all triangles are within this AABB
            for(int i = 0; i < aabbs[currIndex].triangleCount; i++){

                if(!Utils.TriangleWithinMinMax(tris[((int) aabbs[currIndex].triangleStartIndex) + i], aabbs[currIndex].min,
                    aabbs[currIndex].max, mesh, vertices)){
                    Debug.Log(Utils.TriangleToString(tris[((int) aabbs[currIndex].triangleStartIndex) + i], vertices));
                    Debug.Log(Utils.AABBToString(aabbs[currIndex]));
                    return false;
                }

            }

            return true;
        }

    }




    // Start is called before the first frame update
    void Start()
    {
        // Build a BVH for testing
        List<AABB> aabbs = new List<AABB>();
        List<Triangle> triangles = new List<Triangle>();
        List<Vertex> vertices= new List<Vertex>();
        MeshFilter meshFilter = testObj.GetComponent<MeshFilter>();
        uint rootIndex = BuildBVH.BuildBVHForMesh(meshFilter, ref aabbs, ref triangles, ref vertices);

        MeshFilter mf2 = testObj2.GetComponent<MeshFilter>();

        uint rootIndex2 = BuildBVH.BuildBVHForMesh(mf2, ref aabbs, ref triangles, ref vertices); 

        // Run tests
        bool tst1Pass = TestAllTrisFoundInList(meshFilter, aabbs, triangles);
        Debug.Log("Test 1: " + (tst1Pass ? "Pass" : "Fail"));

        bool tst2Pass = TestTreesWellFormed(aabbs, new int[]{(int) rootIndex, (int) rootIndex2});
        Debug.Log("Test 2: " + (tst2Pass ? "Pass" : "Fail"));

        bool tst3Pass = TestAllAABBsValid(aabbs);
        Debug.Log("Test 3: " + (tst3Pass ? "Pass" : "Fail"));

        bool tst4Pass = TestAABBSAllWithinParent(aabbs, (int) rootIndex);
        Debug.Log("Test 4: " + (tst4Pass ? "Pass" : "Fail"));

        bool tst5Pass = TestAllTrianglesWithinAABB(aabbs, triangles, (int) rootIndex, meshFilter, vertices);
        Debug.Log("Test 5: " + (tst5Pass ? "Pass" : "Fail"));

        bool tst6Pass = TestAllTrisFoundInList(mf2, aabbs, triangles);
        Debug.Log("Test 6: " + (tst6Pass ? "Pass" : "Fail"));

        bool tst7Pass = TestAllAABBsValid(aabbs);
        Debug.Log("Test 7: " + (tst7Pass ? "Pass" : "Fail"));

        bool tst8Pass = TestAABBSAllWithinParent(aabbs, (int) rootIndex2);
        Debug.Log("Test 8: " + (tst8Pass ? "Pass" : "Fail"));

        bool tst9Pass = TestAllTrianglesWithinAABB(aabbs, triangles, (int) rootIndex2, mf2, vertices);
        Debug.Log("Test 9: " + (tst9Pass ? "Pass" : "Fail"));        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
