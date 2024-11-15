using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatedGausianDisplay : MonoBehaviour
{
    ComputeBuffer gBuffer;

    // Start is called before the first frame update
    void Start()
    {
        BaseGaussian3D[] gaussians =  GaussianPlyParser.ReadGaussianFile(Application.streamingAssetsPath + "/3D/RotatedGaussian.ply");

        BaseGaussian3D.PasssableGaussian3D[] pGaussians = new BaseGaussian3D.PasssableGaussian3D[gaussians.Length];
        for(int i = 0; i < gaussians.Length; i++){
            pGaussians[i] = gaussians[i].GetPassableStruct();
        } 

        // Add Gaussians to buffer
        gBuffer = new ComputeBuffer(gaussians.Length,  Gaussian3D.PassableGaussianSize);

        gBuffer.SetData(pGaussians);

        // Set data on GPU
    
        Material m = Instantiate(Resources.Load("Materials/GaussianDemo/GaussianDemo") as Material);;
        m.SetBuffer("gaussians", gBuffer);
        m.SetInt("numGaussians", gaussians.Length);

        gameObject.GetComponent<MeshRenderer>().material = m;

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy(){
       gBuffer.Release();
    }
}
