using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

// An Object which reprsents a Gaussian in 3D space
public class Gaussian3D{

    //NOTE: I need to research if this necessary. I don't know if the Gaussian3D class can be passed
    // to the GPU as is or if this is necessary
    public struct PasssableGaussian3D{
        public Vector3 pos;
        public Matrix4x4 cov;
        public Matrix4x4 invCov;
        public Vector4 color;
    }

    private Vector3 _pos;

    private Matrix4x4 _covariance;
    
    //TODO: Expand this to be PBR materials when the .ply is updated
    private Vector4 _color;

    public Vector3 Position {
        get { return _pos; }
        //set { _pos = value; }
    }

    public Matrix4x4 Covariance {
        get { return _covariance; }
        //set { _covariance = value; }
    }

    // TODO: Expand this to be PBR materials when the .ply is updated
    public Vector4 Color {
        get { return _color; }
        //set { _color = value; }
    }

    // Create a new Gaussian3D object by providing the position vector, covariance matrix, and color
    public Gaussian3D(Vector3 pos, Matrix4x4 cov, Vector4 color) {
        _pos = pos;
        _covariance = cov;
        _color = color;
    }

    // Create a new Gaussian3D object by providing the components of the component vectors and matrices retrieved 
    // from the .ply file
    public Gaussian3D(float x, float y, float z, float xScale, float yScale, float zScale, 
        float alpha, float beta, float gamma, float r, float g, float b, float a) 
    {   
        _pos = new Vector3(x, y, z);

        // Create scale and rotation matrices
        Matrix4x4 scaleMat = Matrix4x4.Scale(new Vector3(xScale, yScale, zScale));
        Matrix4x4 rotMat = Matrix4x4.Rotate(Quaternion.Euler(alpha, beta, gamma));


        // Create covariance matrix from scale and rotation 
        _covariance = rotMat * scaleMat * Matrix4x4.Transpose(scaleMat) * Matrix4x4.Transpose(rotMat);

        _color = new Vector4(r, g, b, a);
        
    }

    //NOTE: I need to research if this necessary. I don't know if the Gaussian3D class can be passed
    // to the GPU as is or if this is necessary

    //Return a simple struct equivalent to the Gaussian which can be passed to the GPU
    public PasssableGaussian3D GetPassableStruct(){
        PasssableGaussian3D pg3D = new PasssableGaussian3D();
        pg3D.pos = _pos;
        pg3D.cov = _covariance;
        pg3D.invCov = _covariance.inverse;
        pg3D.color = _color;

        return pg3D;

    }

    public override string ToString() {
        return "Gaussian3D:\n{\n" + 
            "Position:\n" + _pos + "\n" +
            "Covariance:\n" +  _covariance + "\n" +
            "Color:\n" + _color + "\n";
    }

}


// Object which can be used to parse a Gaussian .ply file and return the retrieved Gaussians
public class GaussianPlyParser 
{
    private string pathToGaussianFile;

    // Create a new GaussianPlyParser to read in the file at the given path
    public GaussianPlyParser(string pathToGaussianFile){
        // Confirm that the given file exists and throw an exception if it doesn't
        if(!File.Exists(pathToGaussianFile)){
            throw new FileNotFoundException("File located at " + pathToGaussianFile + " does not exist");
        }

        this.pathToGaussianFile = pathToGaussianFile;
    }

    // Parse the Gaussian file this parser to pointed at
    public Gaussian3D[] ReadFile(){

        using FileStream fs = File.OpenRead(pathToGaussianFile);
        using StreamReader sr = new StreamReader(fs);

        // Read the header to find the number of Gaussians in the file
        int numGaussians = -1;
        int lineNumber = 1;
        string line = sr.ReadLine();
        while (line != "end header")
        {
            string[] splitLine = line.Split(" ");

            // If this line is the start of Gaussian description section of the header determine the number of 
            // Gaussians to read in from it
            if (splitLine[0] == "element" && splitLine[1] == "gaussian3D")
            {
                numGaussians = int.Parse(splitLine[2]);
            }

            lineNumber += 1; 
            line = sr.ReadLine();
        }

        // If the header does not contain the line `gaussian3D {number of gaussians in the file here}`
        // Thrown an exception
        if(numGaussians == -1){
            throw new InvalidOperationException("Error: Failed to read in file located at " + pathToGaussianFile +
                ". The files header does not specify a number of gaussian3d elements to read in,");
        }

        // Parse the rest of the Gaussians
        List<Gaussian3D> readGaussians = new List<Gaussian3D>();

        int gaussiansRead = 0;
        // TODO: If other elements are specified before the gaussians skip past them.
        while(gaussiansRead < numGaussians){
            line = sr.ReadLine();
            string[] splitLine = line.Split(" ");

            // If this line isn't a comment
            if(splitLine[0] != "comment"){
                try{
                    readGaussians.Add(new Gaussian3D(float.Parse(splitLine[0]), float.Parse(splitLine[1]), 
                    float.Parse(splitLine[2]), float.Parse(splitLine[3]), float.Parse(splitLine[4]), 
                    float.Parse(splitLine[5]), float.Parse(splitLine[6]), float.Parse(splitLine[7]), 
                    float.Parse(splitLine[8]), float.Parse(splitLine[9]), float.Parse(splitLine[10]), 
                    float.Parse(splitLine[11]), float.Parse(splitLine[12])));
                }
                catch(IndexOutOfRangeException e){
                    throw new InvalidOperationException("Error: The entry on line " + lineNumber + " of " +
                        pathToGaussianFile + "is not a valid gaussian3D. Root Cause: " + e);
                }

                gaussiansRead += 1;
            }

            lineNumber += 1;
        }

        return readGaussians.ToArray();
    }

    // Read in a Gaussian .ply file and return its results as Gaussian3D objects
    public static Gaussian3D[] ReadGaussianFile(string pathToGaussianFile){
        GaussianPlyParser parser = new GaussianPlyParser(pathToGaussianFile);
        return parser.ReadFile();
    }
}
