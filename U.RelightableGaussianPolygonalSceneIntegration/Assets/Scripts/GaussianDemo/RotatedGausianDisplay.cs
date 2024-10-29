using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatedGausianDisplay : MonoBehaviour
{
    ComputeBuffer gBuffer;

    // Start is called before the first frame update
    void Start()
    {
        Gaussian3D[] gaussians =  GaussianPlyParser.ReadGaussianFile("Assets/Resources/3D/RotatedGaussian.ply");

        Gaussian3D.PasssableGaussian3D[] pGaussians = new Gaussian3D.PasssableGaussian3D[gaussians.Length];
        for(int i = 0; i < gaussians.Length; i++){
            pGaussians[i] = gaussians[i].GetPassableStruct();
        } 

        // Add Gaussians to buffer
        gBuffer = new ComputeBuffer(gaussians.Length, (sizeof(float) * 3) + (sizeof(float) * 16 * 2) 
            +  (sizeof(float) * 4));

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
