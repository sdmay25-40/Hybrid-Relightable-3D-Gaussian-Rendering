using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleGaussianDisplay : MonoBehaviour
{

    ComputeBuffer gBuffer;

    // Start is called before the first frame update
    void Start()
    {
        BaseGaussian3D[] gaussians =  GaussianPlyParser.ReadGaussianFile("Assets/Resources/3D/SingleGaussian.ply");

        BaseGaussian3D.PasssableGaussian3D[] pGaussians = new BaseGaussian3D.PasssableGaussian3D[gaussians.Length];
        for(int i = 0; i < gaussians.Length; i++){
            pGaussians[i] = gaussians[i].GetPassableStruct();
        } 

        // Add Gaussians to buffer
        gBuffer = new ComputeBuffer(gaussians.Length, Gaussian3D.PassableGaussianSize);

        gBuffer.SetData(pGaussians);

        // Set data on GPU
        Material m = Resources.Load("Materials/GaussianDemo/GaussianDemo") as Material;
        m.SetBuffer("gaussians", gBuffer);
        m.SetInt("numGaussians", gaussians.Length);
        m.SetMatrix("transformInverse", Matrix4x4.identity);

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
