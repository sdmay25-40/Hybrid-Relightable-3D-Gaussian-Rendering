using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orbit : MonoBehaviour
{
    private float theta = 0;
    private readonly float speed = 1;
    private float r;
    public GameObject toOrbit;

    // Start is called before the first frame update
    void Start()
    {
        r = Vector3.Distance(toOrbit.transform.position, transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        theta += (speed * Time.deltaTime);

        float newX = r * Mathf.Cos(theta);
        float newY = r * Mathf.Sin(theta);

        transform.position = new Vector3(newX, newY, transform.position.z);

        transform.LookAt(toOrbit.transform);
        
    }
}
