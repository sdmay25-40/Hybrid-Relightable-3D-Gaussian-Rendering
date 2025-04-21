using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GaussianBVHTest : MonoBehaviour
{
    [SerializeField] private GaussianScrpt GaussianScrpt;


    private static void ExploreGaussians(List<AABB> bvh, ref HashSet<Gaussian3D> foundGaussians, int idx,
        Gaussian3D[] gaussians){
        if(bvh[idx].primitiveCount < 4294967295U){
            Gaussian3D g = gaussians[bvh[idx].primitiveStartIndex];
            foundGaussians.Add(g);
            Debug.Log(g.pos);
            Debug.Log(bvh[idx].min);
            Debug.Log(bvh[idx].max);
            Debug.Log("-----");

        }
        else{
            ExploreGaussians(bvh, ref foundGaussians, (int) bvh[idx].leftChildIndex, gaussians);
            if(bvh[idx].rightChildIndex < 4294967295U){
                ExploreGaussians(bvh, ref foundGaussians, (int) bvh[idx].rightChildIndex, gaussians);
            }

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Gaussian3D[] gaussians = GaussianPlyParser.ReadGaussianFile(GaussianScrpt.FilePath);
        List<AABB> aabbs = new List<AABB>();

        uint rootNode = BuildBVH.BuildBVHForGaussians(gaussians, ref aabbs, 0);

        HashSet<Gaussian3D> foundGaussians = new HashSet<Gaussian3D>();

        //ExploreGaussians(aabbs, ref foundGaussians, (int) rootNode, gaussians);

        Debug.Log(foundGaussians.Count);
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
