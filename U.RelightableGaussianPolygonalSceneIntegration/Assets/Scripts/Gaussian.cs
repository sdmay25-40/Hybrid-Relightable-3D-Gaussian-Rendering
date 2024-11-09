using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

// Abstract class which stores item common across all types of Gaussians
public abstract class BaseGaussian3D{

    // Size of the PasssableGaussian3D type
    public static int PassableGaussianSize {
        get { return (2 * 3 * sizeof(float)) +
                     (4 * sizeof(float)) +
                     (2 * 16 * sizeof(float))  +
                     (7 * sizeof(float)) + 
                     (3 * sizeof(uint)); }
    }

    public struct PasssableGaussian3D{
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

    protected Vector3 _pos;

    protected Matrix4x4 _covariance;
    
    public Vector3 Position {
        get { return _pos; }
    }

    public Matrix4x4 Covariance {
        get { return _covariance; }
    }

    // Create a new Gaussian3D object by providing the position vector and covariance matrix
    protected BaseGaussian3D(Vector3 pos, Matrix4x4 cov) {
        _pos = pos;
        _covariance = cov;
    }

    // Create a new SimpleGaussian3D object by providing the components of each element retrieved 
    // from the .ply file
    protected BaseGaussian3D(float x, float y, float z, float xScale, float yScale, float zScale, 
        float qX, float qY, float qZ, float qW) 
    {   
        _pos = new Vector3(x, y, z);

        // Create scale and rotation matrices
        Matrix4x4 scaleMat = Matrix4x4.Scale(new Vector3(xScale, yScale, zScale));
        Matrix4x4 rotMat = Matrix4x4.Rotate(new Quaternion(qX, qY, qZ, qW));


        // Create covariance matrix from scale and rotation 
        _covariance = rotMat * scaleMat * Matrix4x4.Transpose(scaleMat) * Matrix4x4.Transpose(rotMat);        
    }

    //Return a simple struct equivalent to the Gaussian which can be passed to the GPU
    public abstract PasssableGaussian3D GetPassableStruct();

    public override string ToString() {
        return "Gaussian3D:\n{\n" + 
            "Position:\n" + _pos + "\n" +
            "Covariance:\n" +  _covariance + "\n}";
    }

}

// Gaussain which has a single color
public class SimpleGaussian3D : BaseGaussian3D {
    protected Vector4 _color;

    public Vector4 Color{
        get { return _color; }
    }

    // Create a new SimpleGaussian3D object by providing the components of each element retrieved 
    // from the .ply file
    public SimpleGaussian3D(float x, float y, float z, float xScale, float yScale, float zScale, 
        float qX, float qY, float qZ, float qW, float r, float g, float b, float a) 
        :  base(x, y, z, xScale, yScale, zScale, qX, qY, qZ, qW)
    {   
        _color = new Vector4(r, g, b, a);
        
    }

    // Create a new Gaussian3D object by providing the position vector, covariance matrix, and color
    public SimpleGaussian3D(Vector3 pos, Matrix4x4 cov, Vector4 color) : base(pos, cov) {
        _color = color;
    }

    public override PasssableGaussian3D GetPassableStruct()
    {
        PasssableGaussian3D p = new();
        p.pos = _pos;
        p.cov = _covariance;
        p.invCov = _covariance.inverse;
        p.color = _color;
        p.gaussianType = 0;

        return p;
    }


        public override string ToString() {
        return "Gaussian3D:\n{\n" + 
            "Position:\n" + _pos + "\n" +
            "Covariance:\n" +  _covariance + "\n" +
            "Color:\n" + _color +"}";
        }

}

// Gaussian which has its color from different view angles encoded by spherical harmonics
public class Gaussian3D : BaseGaussian3D {
    // Used to access the spherical harmonics coefficients in a shared buffer
    private uint _shCoefficientsNum;
    private uint _shCoefficientsIndex;

    public uint SHCoefficientsNum {
        get { return _shCoefficientsNum;}
    }

    public uint SHCoefficientsIndex {
        get{ return _shCoefficientsIndex;}
    }



    // Create a new SimpleGaussian3D object by providing the components of each element retrieved 
    // from the .ply file
    public Gaussian3D(float x, float y, float z, float xScale, float yScale, float zScale, 
        float qX, float qY, float qZ, float qW, uint shCoefficientsNum, uint shoefficientsIndex) 
        :  base(x, y, z, xScale, yScale, zScale, qX, qY, qZ, qW)
    {   
        _shCoefficientsNum = shCoefficientsNum;
        _shCoefficientsIndex = shoefficientsIndex;
    }

    // Create a new Gaussian3D object by providing the position vector, covariance matrix, and 
    // the index of its spherical harmonocs coefficients in a shared external buffer.
    public Gaussian3D(Vector3 pos, Matrix4x4 cov, uint shCoefficientsNum, uint shCoefficientsIndex) : base(pos, cov) {
        _shCoefficientsNum = shCoefficientsNum;
        _shCoefficientsIndex = shCoefficientsIndex;
    }

    public override PasssableGaussian3D GetPassableStruct()
    {
        PasssableGaussian3D p = new();
        p.pos = _pos;
        p.cov = _covariance;
        p.invCov = _covariance.inverse;
        p.shCoefficientsNum = _shCoefficientsNum;
        p.shCoefficientsIndex = _shCoefficientsIndex;
        p.gaussianType = 1;

        return p;
    }

    public override string ToString() {
        return "Gaussian3D:\n{\n" + 
            "Position:\n" + _pos + "\n" +
            "Covariance:\n" +  _covariance + "\n" +
            "SHCoefficientsNum:\n" + _shCoefficientsNum + "\n" + 
            "SHCoefficientsIndex:\n" + _shCoefficientsIndex +"}";
    }


}

public class RelightableGausssian3D : BaseGaussian3D {

    private Vector3 _normal;

    // PBR Propeties
    private Vector4 _albedo;
    private float _roughness;
    private float _metalness;
    private float _specular;
    private float _opacity;
    private float _ambientOcclusion;
    private float _refraction;
    private float _emissive;

    public Vector3 Normal{
        get { return _normal; }
    }
    public Vector4 Albedo {
        get { return _albedo;}
    }
    public float Roughness {
        get { return _roughness;}
    }
    public float Metalness {
        get { return _metalness;}
    }
    public float Specular {
        get { return _specular;}
    }
    public float Opacity {
        get { return _opacity;}
    }
    public float AmbientOcclusion {
        get { return _ambientOcclusion;}
    }
    public float Refraction {
        get { return _refraction;}
    }
    public float Emissive{
        get { return _emissive;}
    }
    
    // Create a new SimpleGaussian3D object by providing the components of each element retrieved 
    // from the .ply file
    public RelightableGausssian3D(float x, float y, float z, float xScale, float yScale, float zScale, 
        float qX, float qY, float qZ, float qW, float r, float g, float b, float a,
        float nX, float nY, float nZ,
        float roughness, float metalness, float specular,  float opacity, float ambientOcclusion, float refraction, float emissive) 
        : base(x, y, z, xScale, yScale, zScale, qX, qY, qZ, qW) 
    {
        _normal = new Vector3(nX, nY, nZ);
        _albedo = new Vector4(r, g, b ,a);
        _roughness = roughness;
        _metalness = metalness;
        _specular = specular;
        _opacity = opacity;
        _ambientOcclusion = ambientOcclusion;
        _refraction = refraction;
        _emissive = emissive;
    }


    // Create a new Gaussian3D object by providing the position vector, covariance matrix, and 
    // the PBR properties with the albedo as a vector4
    public RelightableGausssian3D(Vector3 pos, Matrix4x4 cov, Vector4 albedo, Vector3 normal,
        float roughness, float metalness, float specular,  float opacity, float ambientOcclusion, float refraction, float emissive) 
        : base(pos, cov) 
    {
        _normal = normal;
        _albedo = albedo;
        _roughness = roughness;
        _metalness = metalness;
        _specular = specular;
        _opacity = opacity;
        _ambientOcclusion = ambientOcclusion;
        _refraction = refraction;
        _emissive = emissive;
    }

    public override PasssableGaussian3D GetPassableStruct()
    {
        PasssableGaussian3D p = new();
        p.pos = _pos;
        p.cov = _covariance;
        p.invCov = _covariance.inverse;
        p.normal = _normal;
        p.color = _albedo;
        p.roughness = _roughness;
        p.metalness = _metalness;
        p.specular = _specular;
        p.opacity = _opacity;
        p.ambientOcclusion = _ambientOcclusion;
        p.refraction = _refraction;
        p.emissive = _emissive;
        p.gaussianType = 2;

        return p;
    }

    public override string ToString() {
        return "Gaussian3D:\n{\n" + 
            "Position:\n" + _pos + "\n" +
            "Covariance:\n" +  _covariance + "\n" +
            "Normal:\n" + _normal + "\n" +
            "Albedo:\n" +  _albedo + "\n" +
            "Roughness:\n" + _roughness + "\n" +
            "Metalness:\n" +  _metalness + "\n" +
            "Specular:\n" + _specular + "\n" +
            "Opacity:\n" + _opacity + "\n" +
            "AmbientOcclusion:\n" + _ambientOcclusion + "\n" +
            "Refraction:\n" +  _refraction + "\n" +
            "Emissive:\n" +  _emissive + "}"; 
    }


}


// Object which can be used to parse a Gaussian .ply file and return the retrieved Gaussians
public class GaussianPlyParser 
{
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
    private List<SimpleGaussian3D> readSimpleGaussians(StreamReader sr, int numGaussians, ref int lineNumber){
        // Parse the rest of the Gaussians
        List<SimpleGaussian3D> readGaussians = new List<SimpleGaussian3D>();

        int gaussiansRead = 0;

        while(gaussiansRead < numGaussians){
            string line = sr.ReadLine();
            string[] splitLine = line.Split(" ");

            // If this line isn't a comment
            if(splitLine[0] != "comment"){
                try{
                    readGaussians.Add(new SimpleGaussian3D(
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
                    

                    readGaussians.Add(new Gaussian3D(
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
    private List<RelightableGausssian3D> readRelightableGaussians(StreamReader sr, int numGaussians, ref int lineNumber){
        // Parse the rest of the Gaussians
        List<RelightableGausssian3D> readGaussians = new List<RelightableGausssian3D>();

        int gaussiansRead = 0;

        while(gaussiansRead < numGaussians){
            string line = sr.ReadLine();
            string[] splitLine = line.Split(" ");

            // If this line isn't a comment
            if(splitLine[0] != "comment"){
                try{
                    readGaussians.Add(new RelightableGausssian3D(
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
    public BaseGaussian3D[] ReadFile(){

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
        List<BaseGaussian3D> allGaussians = new List<BaseGaussian3D>();
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
    public static BaseGaussian3D[] ReadGaussianFile(string pathToGaussianFile){
        GaussianPlyParser parser = new GaussianPlyParser(pathToGaussianFile);
        return parser.ReadFile();
    }
}
