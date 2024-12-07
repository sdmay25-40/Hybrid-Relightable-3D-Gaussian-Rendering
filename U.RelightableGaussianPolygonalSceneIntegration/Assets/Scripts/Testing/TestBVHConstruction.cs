using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBVHConstruction : MonoBehaviour
{
    public GameObject testObj;

    public List<Material> mats;

    // Start is called before the first frame update
    void Start()
    {
        List<AABB> aabbs = new List<AABB>();
        List<Triangle> triangles = new List<Triangle>();

        MeshFilter meshFilter = testObj.GetComponent<MeshFilter>();

        BuildBVH.BuildBVHForMesh(meshFilter, ref aabbs, ref triangles);

        
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
            worldRep.GetComponent<MeshRenderer>().material = mats[i % 5];
        }        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
