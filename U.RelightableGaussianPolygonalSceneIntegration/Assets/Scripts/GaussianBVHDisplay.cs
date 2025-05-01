using System.Collections.Generic;
using UnityEngine;

public class GaussianBVHDisplay : MonoBehaviour
{

    

    public GaussianScrpt gs;


    private void Traverse(List<AABB> bvh, int i, GameObject parent){


        AABB a = bvh[i];
        Bounds b = new Bounds();
        b.SetMinMax(a.min, a.max);

        GameObject worldRep = GameObject.CreatePrimitive(PrimitiveType.Cube);
        worldRep.transform.localScale = new Vector3(b.size.x, b.size.y, b.size.z);
        worldRep.transform.position = b.center; 
        worldRep.transform.parent = parent.transform;

        // If this isnt a leaf recurse
        if(a.primitiveCount == uint.MaxValue){
            Traverse(bvh, (int) a.leftChildIndex, worldRep);
            Traverse(bvh, (int) a.rightChildIndex, worldRep);
        }
    
    }

    // Start is called before the first frame update
    void Start()
    {
        List<AABB> bvh = new List<AABB>();
        Gaussian3D[] g = GaussianPlyParser.ReadGaussianFile(gs.FilePath);
        uint rootIdx = BuildBVH.BuildBVHForGaussians(g, ref bvh, 0);

        /*
        foreach(AABB a in bvh){
            // Only create a box rep for this AABB if it contains triangles
            Debug.Log("min: " + a.min + " max: " + a.max);
            Bounds b = new Bounds();
            b.SetMinMax(a.min, a.max);

            GameObject worldRep = GameObject.CreatePrimitive(PrimitiveType.Cube);

            worldRep.transform.localScale = new Vector3(b.size.x, b.size.y, b.size.z);

            worldRep.transform.position = b.center; 
            //worldRep.GetComponent<MeshRenderer>().material = TempMat;      
        }
        */
        
        AABB a = bvh[(int) rootIdx];
        Bounds b = new Bounds();
        b.SetMinMax(a.min, a.max);

        GameObject worldRep = GameObject.CreatePrimitive(PrimitiveType.Cube);
        worldRep.transform.localScale = new Vector3(b.size.x, b.size.y, b.size.z);
        worldRep.transform.position = b.center; 
        //worldRep.GetComponent<MeshRenderer>().material = TempMat;
        Debug.Log(a.leftChildIndex);
        Debug.Log(bvh.Count);
        Traverse(bvh, (int) a.leftChildIndex, worldRep);  
        Traverse(bvh, (int) a.rightChildIndex, worldRep);    
  
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
