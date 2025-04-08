using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatedGausianDisplay : MonoBehaviour
{
    ComputeBuffer gBuffer;

    // Start is called before the first frame update
    void Start()
    {
        Gaussian3D[] gaussians =  GaussianPlyParser.ReadGaussianFile(Application.streamingAssetsPath + "/3D/RotatedGaussian.ply");

        // Add Gaussians to buffer
        gBuffer = new ComputeBuffer(gaussians.Length,  GaussianHelper.GaussianStructSize);

        gBuffer.SetData(gaussians);

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
