using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Render the wireframe of all BVH's for a scene
public class WireframeBVHDisplay : MonoBehaviour
{

    public Material TempMat;

    // Start is called before the first frame update
    void Start()
    {  
        List<AABB> aabbs = new List<AABB>();
        List<Triangle> triangles = new List<Triangle>();

        Dictionary<int, int> meshInstanceToAABB = new Dictionary<int, int>();

        // Set up BVHs for all meshes
        MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {

            MeshFilter meshFilter = meshRenderer.gameObject.GetComponent<MeshFilter>();
            if (!meshFilter)
            {
                Debug.LogError($"'SceneSerializer': {meshRenderer.gameObject.name} does not contain a MeshFilter");
            }

            // create AABB for each unique mesh
            uint aabbRootIndex = uint.MaxValue;
            int meshInstanceId = meshFilter.sharedMesh.GetInstanceID();
            if (!meshInstanceToAABB.ContainsKey(meshInstanceId))
            {
                aabbRootIndex = BuildBVH.BuildBVHForMesh(meshFilter, ref aabbs, ref triangles);

                // we are only creating one AABB per mesh atm so aabb is root
                meshInstanceToAABB.Add(meshInstanceId, (int) aabbRootIndex);
            }

        }

        // Add all represnetations of all AABBs to world 
        for(int i =0; i < aabbs.Count; i++){
            // Only create a box rep for this AABB if it contains triangles
            if(aabbs[i].min == new Vector3(0, 0, 0) && aabbs[i].max == new Vector3(0, 0, 0)){
                continue;
            }

            Debug.Log("min: " + aabbs[i].min + " max: " + aabbs[i].max);
            Bounds b = new Bounds();
            b.SetMinMax(aabbs[i].min, aabbs[i].max);

            GameObject worldRep = GameObject.CreatePrimitive(PrimitiveType.Cube);

            worldRep.transform.localScale = new Vector3(b.size.x, b.size.y, b.size.z);

            worldRep.transform.position = b.center; 
            worldRep.GetComponent<MeshRenderer>().material = TempMat;
        }        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
