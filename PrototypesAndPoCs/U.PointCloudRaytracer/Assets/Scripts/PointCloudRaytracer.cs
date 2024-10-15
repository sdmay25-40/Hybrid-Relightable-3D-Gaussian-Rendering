using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PointCloudRaytracer : MonoBehaviour
{    
    // Buffer to send point data to the GPU
    private ComputeBuffer pBuffer;

    // Shader to run the create texture
    public ComputeShader Shader;

    // The number of points in the point cloud being rendered
    private int numPoints;
    

    private RenderTexture _tex;

    float rot = 0;


    private struct  Point
    {
        public Vector3 coords;
        public Vector4 color;
    }

    private Point[] ReadFile(string filePath){

        // Read in the file 
        string[] lines = File.ReadAllLines(filePath);

        // Read the first line in the file
        string line = lines[0];

        // Store the number of vertices
        int? numVerticesN = null;

        // Track the index in the header
        int headerIndex = 1;

        // Read the header
        while(line != "end_header"){
            // Divide the line
            string[] lineByWords = line.Split(' ');

            // If this is the vertice marker 
            if(lineByWords[0] == "element" && lineByWords[1] == "vertex"){
                // Get number of vertices from the file
                numVerticesN = int.Parse(lineByWords[2]);
            }

            // Advance the index and get the next line
            headerIndex++;
            line = lines[headerIndex];
        }

        if(numVerticesN == null){
            Debug.Log("Invalid File");
            return null;
        }

        int numVertices = (int) numVerticesN;

        Point[] readPoints = new Point[numVertices];

        int lineCounter = 0;
        int addedVerts = 0;


        // Read the vertices
        while(addedVerts < numVertices){
            // Get the next line
            line = lines[headerIndex + lineCounter + 1];


            // Split the line
            string[] lineByWords = line.Split(' ');

            if(lineByWords[0] != "#" && lineByWords.Length > 1){

                // Create a point
                Point p = new Point();
                
                p.coords = new Vector3(float.Parse(lineByWords[0]), float.Parse(lineByWords[1]), float.Parse(lineByWords[2]));
                p.color = new Vector4(float.Parse(lineByWords[3]), float.Parse(lineByWords[4]), 
                    float.Parse(lineByWords[5]), float.Parse(lineByWords[6]));

                readPoints[addedVerts] = p;
                addedVerts++;
            }

            
            lineCounter += 1;
        }

        return readPoints;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Read in points
        Point[] points = ReadFile("pointCloud.ply");


        // Setup compute buffer
        int typeSize = (sizeof(float) * 3) + (sizeof(float) * 4);
        pBuffer = new ComputeBuffer(points.Length, typeSize);

        // Load data to buffer
        pBuffer.SetData(points);

        // Save number of points
        numPoints = points.Length;
        
    }

    // Update is called once per frame
    void Update()
    {
        rot += 90 * Time.deltaTime;
    }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        
        // Get the texture for this frame by running compute shader   

        // Create the texture to apply if it doesn't exist
        if( _tex == null)
        {
            _tex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _tex.enableRandomWrite = true;
            _tex.Create();

        }

        // Get shader and set its output
        int kernel = Shader.FindKernel("CSMain");
        Shader.SetTexture(kernel, "Result", _tex);

        // Setup viewport
        float aspect = ((float) Screen.width) / ((float) Screen.height);
        float worldHeight = 1;
        float worldWidth = worldHeight * aspect;


        // Set up configuration constants
        Shader.SetVector("backgroundColor", new Vector4(0.13f, 0.13f, 0.13f, 1f));
        Shader.SetVector("camLoc", new Vector3(0, 0, -3));
        Shader.SetInt("numPoints", numPoints);
        Shader.SetInt("screenWidthPixels", Screen.width);
        Shader.SetInt("screenHeightPixels", Screen.height);
        Shader.SetFloat("screenWidthCoords", worldWidth);
        Shader.SetFloat("screenHeightCoords", worldHeight);
        Matrix4x4 m = Matrix4x4.Rotate(Quaternion.Euler(0, rot, 0));
        Shader.SetMatrix("transform", m);

        // Set buffer data
        Shader.SetBuffer(kernel, "Points", pBuffer);

        // Dispatch shader
        int workgroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int workgroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        Shader.Dispatch(kernel, workgroupsX, workgroupsY, 1);




        // Set the destination texture to the shaders output
        Graphics.Blit(_tex, destination);
    }

    void OnDestroy(){
        pBuffer.Release();
    }
}
