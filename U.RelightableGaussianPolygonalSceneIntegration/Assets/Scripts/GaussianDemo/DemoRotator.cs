using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoRotator : MonoBehaviour
{
    GameObject demo1;
    GameObject demo2;
    GameObject demo3;

    int selector;
    // Start is called before the first frame update
    void Start()
    {
        demo1 = GameObject.Find("Single Gaussian");
        demo2 = GameObject.Find("4 Gaussians");
        demo3 = GameObject.Find("RotatedGaussian");
        selector = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)){

            selector = (selector + 1) % 3;
            if (selector % 3 == 0){
                demo1.SetActive(true);
                demo2.SetActive(false);
                demo3.SetActive(false);
            }
            else if(selector % 3 == 1){
                demo1.SetActive(false);
                demo2.SetActive(true);
                demo3.SetActive(false);
            }
            else{
                demo1.SetActive(false);
                demo2.SetActive(false);
                demo3.SetActive(true);
            }


        }

    }
}
