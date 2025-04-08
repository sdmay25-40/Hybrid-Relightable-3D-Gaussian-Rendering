using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class GaussianHelper{
    // Size of the PasssableGaussian3D type
    public static int GaussianStructSize 
    {
        get { return (2 * 3 * sizeof(float)) +
            (4 * sizeof(float)) +
            (2 * 16 * sizeof(float))  +
            (7 * sizeof(float)) + 
            (3 * sizeof(uint)); 
        }
    }


}



public struct Gaussian3D{
    public Vector3 pos;
    public Matrix4x4 cov;
    public Matrix4x4 invCov;
    public Vector4 color;
        
    // Index of Spherical harmonics coefficients in buffer
    public uint shCoefficientsIndex;
    // Number of spherical harmonics coefficients
    public uint shCoefficientsNum;

    public Vector3 normal;

    // PBR Propeties (Color is used for albedo)
    public float roughness;
    public float metalness;
    public float specular;
    public float opacity;
    public float ambientOcclusion;
    public float refraction;
    public float emissive;
    /*
    Used like an enum  to denote what type of Gaussian (in the .ply file) this is
    0 = simpleGaussian3D
    1 = gaussian3D
    2 = relightableGaussian3D
    */
    public uint gaussianType;
}



// Object which can be used to parse a Gaussian .ply file and return the retrieved Gaussians
public class GaussianPlyParser 
{

    public static Matrix4x4 CreateCovarianceMatrix(float xScale, float yScale, float zScale, 
        float qX, float qY, float qZ, float qW)
    {
        Matrix4x4 scaleMat = Matrix4x4.Scale(new Vector3(xScale, yScale, zScale));
        Matrix4x4 rotMat = Matrix4x4.Rotate(new Quaternion(qX, qY, qZ, qW));


        // Create covariance matrix from scale and rotation 
       return rotMat * scaleMat * Matrix4x4.Transpose(scaleMat) * Matrix4x4.Transpose(rotMat);        
    }


    public static Gaussian3D CreateSimpleGaussian3D(float x, float y, float z, 
        float xScale, float yScale, float zScale, 
        float qX, float qY, float qZ, float qW, 
        float r, float g, float b, float a)
    {
        Matrix4x4 covariance = CreateCovarianceMatrix(xScale, yScale, zScale, qX, qY, qZ, qW);
        Gaussian3D p = new()
        {
            pos = new Vector3(x, y, z),
            cov = covariance,
            invCov = covariance.inverse,
            color = new Vector4(r, g, b, a),
            gaussianType = 0
        };

        return p;
    }


    public static Gaussian3D CreateGaussian3D(float x, float y, float z, 
        float xScale, float yScale, float zScale, 
        float qX, float qY, float qZ, float qW, 
        uint shCoefficientsNum, uint shCoefficientsIndex)
    {
        Matrix4x4 covariance = CreateCovarianceMatrix(xScale, yScale, zScale, qX, qY, qZ, qW);
        Gaussian3D p = new()
        {
            pos = new Vector3(x, y, z),
            cov = covariance,
            invCov = covariance.inverse,
            shCoefficientsNum = shCoefficientsNum,
            shCoefficientsIndex = shCoefficientsIndex,
            gaussianType = 1
        };

        return p;
    }

    public static Gaussian3D CreateRelightableGaussian3D(float x, float y, float z, 
        float xScale, float yScale, float zScale, 
        float qX, float qY, float qZ, float qW, float r, float g, float b, float a,
        float nX, float nY, float nZ,
        float roughness, float metalness, float specular,  float opacity, float ambientOcclusion, float refraction, float emissive) 
    {
        Matrix4x4 covariance = CreateCovarianceMatrix(xScale, yScale, zScale, qX, qY, qZ, qW);
        Gaussian3D p = new();
        p.pos = new Vector3(x, y, z);
        p.cov = covariance;
        p.invCov = covariance.inverse;
        p.normal = new Vector3(nX, nY, nZ);
        p.color = new Vector4(r, g, b, a);
        p.roughness = roughness;
        p.metalness = metalness;
        p.specular = specular;
        p.opacity = opacity;
        p.ambientOcclusion = ambientOcclusion;
        p.refraction = refraction;
        p.emissive = emissive;
        p.gaussianType = 2;

        return p;
    }


    private List<float> coefficientsBuffer; 

    public List<float> CoefficientsBuffer{
        get { return coefficientsBuffer; }
    }

    private string pathToGaussianFile;

    // Create a new GaussianPlyParser to read in the file at the given path
    public GaussianPlyParser(string pathToGaussianFile){
        // Confirm that the given file exists and throw an exception if it doesn't
        if(!File.Exists(pathToGaussianFile)){
            throw new FileNotFoundException("File located at " + pathToGaussianFile + " does not exist");
        }

        this.pathToGaussianFile = pathToGaussianFile;
        coefficientsBuffer = new List<float>();
    }

    // Parse a given number of simpleGaussian3Ds from the file 
    private List<Gaussian3D> readSimpleGaussians(StreamReader sr, int numGaussians, ref int lineNumber){
        // Parse the rest of the Gaussians
        List<Gaussian3D> readGaussians = new List<Gaussian3D>();

        int gaussiansRead = 0;

        while(gaussiansRead < numGaussians){
            string line = sr.ReadLine();
            string[] splitLine = line.Split(" ");

            // If this line isn't a comment
            if(splitLine[0] != "comment"){
                try{
                    readGaussians.Add(CreateSimpleGaussian3D(
                    float.Parse(splitLine[0]), float.Parse(splitLine[1]), float.Parse(splitLine[2]), 
                    float.Parse(splitLine[3]), float.Parse(splitLine[4]), float.Parse(splitLine[5]), 
                    float.Parse(splitLine[6]), float.Parse(splitLine[7]), float.Parse(splitLine[8]), float.Parse(splitLine[9]), 
                    float.Parse(splitLine[10]), float.Parse(splitLine[11]), float.Parse(splitLine[12]), float.Parse(splitLine[13])));
                }
                catch(IndexOutOfRangeException e){
                    throw new InvalidOperationException("Error: The entry on line " + lineNumber + " of " +
                        pathToGaussianFile + "is not a valid simpleGaussian3D. Root Cause: " + e);
                }

                gaussiansRead += 1;
            }

            lineNumber += 1;
        }
        return readGaussians;
    }

    // Parse a given number of gaussian3Ds from the file 
    private List<Gaussian3D> readGaussians(StreamReader sr, int numGaussians, ref int lineNumber){
        // Parse the rest of the Gaussians
        List<Gaussian3D> readGaussians = new List<Gaussian3D>();

        int gaussiansRead = 0;

        while(gaussiansRead < numGaussians){
            string line = sr.ReadLine();
            string[] splitLine = line.Split(" ");

            // If this line isn't a comment
            if(splitLine[0] != "comment"){
                try{
                    // Read in the given spherical harmonics coefficients
                    uint numCoefficients = uint.Parse(splitLine[10]);
                    uint coeffiecntLoc = (uint) coefficientsBuffer.Count;
                    for(int i = 1; i <= numCoefficients; i++){
                        coefficientsBuffer.Add(float.Parse(splitLine[10 + i]));
                    }
                    

                    readGaussians.Add(CreateGaussian3D(
                    float.Parse(splitLine[0]), float.Parse(splitLine[1]), float.Parse(splitLine[2]), 
                    float.Parse(splitLine[3]), float.Parse(splitLine[4]), float.Parse(splitLine[5]), 
                    float.Parse(splitLine[6]), float.Parse(splitLine[7]), float.Parse(splitLine[8]), float.Parse(splitLine[9]), 
                    numCoefficients, coeffiecntLoc));
                }
                catch(IndexOutOfRangeException e){
                    throw new InvalidOperationException("Error: The entry on line " + lineNumber + " of " +
                        pathToGaussianFile + "is not a valid gaussian3D. Root Cause: " + e);
                }

                gaussiansRead += 1;
            }

            lineNumber += 1;
        }
        return readGaussians;
    }

    // Parse a given number of relightableGaussian3Ds from the file 
    private List<Gaussian3D> readRelightableGaussians(StreamReader sr, int numGaussians, ref int lineNumber){
        // Parse the rest of the Gaussians
        List<Gaussian3D> readGaussians = new List<Gaussian3D>();

        int gaussiansRead = 0;

        while(gaussiansRead < numGaussians){
            string line = sr.ReadLine();
            string[] splitLine = line.Split(" ");

            // If this line isn't a comment
            if(splitLine[0] != "comment"){
                try{
                    readGaussians.Add(CreateRelightableGaussian3D(
                    float.Parse(splitLine[0]), float.Parse(splitLine[1]), float.Parse(splitLine[2]), 
                    float.Parse(splitLine[3]), float.Parse(splitLine[4]), float.Parse(splitLine[5]), 
                    float.Parse(splitLine[6]), float.Parse(splitLine[7]), float.Parse(splitLine[8]), float.Parse(splitLine[9]), 
                    float.Parse(splitLine[10]), float.Parse(splitLine[11]), float.Parse(splitLine[12]), float.Parse(splitLine[13]),
                    float.Parse(splitLine[14]), float.Parse(splitLine[15]), float.Parse(splitLine[16]), 
                    float.Parse(splitLine[17]), float.Parse(splitLine[18]), float.Parse(splitLine[19]), float.Parse(splitLine[20]),
                    float.Parse(splitLine[21]), float.Parse(splitLine[22]), float.Parse(splitLine[23])));
                }
                catch(IndexOutOfRangeException e){
                    throw new InvalidOperationException("Error: The entry on line " + lineNumber + " of " +
                        pathToGaussianFile + "is not a valid relightableGaussian3D. Root Cause: " + e);
                }

                gaussiansRead += 1;
            }

            lineNumber += 1;
        }
        return readGaussians;
    }



    // Parse the Gaussian file this parser is pointed at
    public Gaussian3D[] ReadFile(){

        using FileStream fs = File.OpenRead(pathToGaussianFile);
        using StreamReader sr = new StreamReader(fs);

        // Read the header to find the number of Gaussians of each type in the file
        Queue<string> typeOrder = new Queue<string>();
        Queue<int> typeNumbers = new Queue<int>();
        
        int lineNumber = 1;
        string line = sr.ReadLine();
        while (line != "end header")
        {
            if(line == ""){
                lineNumber += 1;
                line = sr.ReadLine();
                continue;
            }

            string[] splitLine = line.Split(" ");
            
            // If this line is the start the description for the simpleGaussian3D type get the number of simple gaussians
            if(splitLine[0] == "element" && splitLine[1] == "simpleGaussian3D"){
                typeOrder.Enqueue("simpleGaussian3D");
                typeNumbers.Enqueue(int.Parse(splitLine[2]));
            }
            // If this line is the start the description for the gaussian3D type get the number of gaussians
            else if (splitLine[0] == "element" && splitLine[1] == "gaussian3D")
            {
                typeOrder.Enqueue("gaussian3D");
                typeNumbers.Enqueue(int.Parse(splitLine[2]));
            }
            // If this line is the start the description for the relightableGaussian3D type get the number of 
            // relightable gaussians
            else if (splitLine[0] == "element" && splitLine[1] == "relightableGaussian3D")
            {
                typeOrder.Enqueue("relightableGaussian3D");
                typeNumbers.Enqueue(int.Parse(splitLine[2]));
            }

            lineNumber += 1; 
            line = sr.ReadLine();
        }

        // If the header does not contain the line `gaussian3D {number of gaussians in the file here}`
        // Thrown an exception
        if(typeNumbers.Count == 0){
            throw new InvalidOperationException("Error: Failed to read in file located at " + pathToGaussianFile +
                ". The files header does not specify a number of gaussian3d elements to read in,");
        }

        // Get all specified gaussians
        List<Gaussian3D> allGaussians = new List<Gaussian3D>();
        // TODO: If other elements are specified before the gaussians skip past them.
        while(typeOrder.Count > 0){
            string currType = typeOrder.Dequeue();
            int numGaussians = typeNumbers.Dequeue();

            if(currType == "simpleGaussian3D"){
                allGaussians.AddRange(readSimpleGaussians(sr, numGaussians, ref lineNumber));
            }
            else if(currType == "gaussian3D"){
                allGaussians.AddRange(readGaussians(sr, numGaussians, ref lineNumber));
            }
            else{
                allGaussians.AddRange(readRelightableGaussians(sr, numGaussians, ref lineNumber));
            }
        }

        return allGaussians.ToArray();
    }

    // Read in a Gaussian .ply file and return its results as Gaussian3D objects
    public static Gaussian3D[] ReadGaussianFile(string pathToGaussianFile){
        GaussianPlyParser parser = new GaussianPlyParser(pathToGaussianFile);
        return parser.ReadFile();
    }
}
