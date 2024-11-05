using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedParseTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GaussianPlyParser parser = new GaussianPlyParser("Assets/Resources/3D/MixedGaussians.ply");
        BaseGaussian3D[] g = parser.ReadFile();

        for(int i =0; i<g.Length; i++){
            Debug.Log(g[i]);
        }

        string tmp = "";
        for(int i =0;  i < parser.CoefficientsBuffer.Count; i++){
            tmp = tmp + parser.CoefficientsBuffer[i] + ", ";
        }

        Debug.Log(tmp);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
