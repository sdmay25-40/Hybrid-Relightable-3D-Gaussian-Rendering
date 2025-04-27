using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


public struct Gaussian3D{
    public Vector3 pos;
    public Vector4 scaleSqrd;
    // Spherical Harmonics Coefficients
    public float shCoefficient0;
    public float shCoefficient1;
    public float shCoefficient2;
    public float shCoefficient3;
    public float shCoefficient4;
    public float shCoefficient5;
    public float shCoefficient6;
    public float shCoefficient7;
    public float shCoefficient8;
    public float shCoefficient9;
    public float shCoefficient10;
    public float shCoefficient11;
    public float shCoefficient12;
    public float shCoefficient13;
    public float shCoefficient14;
    public float shCoefficient15;
    public float shCoefficient16;
    public float shCoefficient17;
    public float shCoefficient18;
    public float shCoefficient19;
    public float shCoefficient20;
    public float shCoefficient21;
    public float shCoefficient22;
    public float shCoefficient23;
    public float shCoefficient24;
    public float shCoefficient25;
    public float shCoefficient26;
    public float opacity;
    public float padding;
}

public enum PlyFileFormat {
    BINARY_LITTLE_ENDIAN,
    BINARY_BIG_ENDIAN,
    ASCII
}

// Object which can be used to parse a Gaussian .ply file and return the retrieved Gaussians
public class GaussianPlyParser 
{

    public static Gaussian3D CreateGaussian3D(Vector3 centerPos, Vector3 scale, 
        float[] shCoefficients, float gaussOpacity)
    {
        Gaussian3D p = new()
        {
            pos = centerPos,
            scaleSqrd = new Vector4(scale.x * scale.x, scale.y * scale.y, scale.z * scale.z, 1),

            // Spherical Harmonics coefficents (This is a long block)
            shCoefficient0 = shCoefficients[0],
            shCoefficient1 = shCoefficients[1],
            shCoefficient2 = shCoefficients[2],
            shCoefficient3 = shCoefficients[3],
            shCoefficient4 = shCoefficients[4],
            shCoefficient5 = shCoefficients[5],
            shCoefficient6 = shCoefficients[6],
            shCoefficient7 = shCoefficients[7],
            shCoefficient8 = shCoefficients[8],
            shCoefficient9 = shCoefficients[9],
            shCoefficient10 = shCoefficients[10],
            shCoefficient11 = shCoefficients[11],
            shCoefficient12 = shCoefficients[12],
            shCoefficient13 = shCoefficients[13],
            shCoefficient14 = shCoefficients[14],
            shCoefficient15 = shCoefficients[15],
            shCoefficient16 = shCoefficients[16],
            shCoefficient17 = shCoefficients[17],
            shCoefficient18 = shCoefficients[18],
            shCoefficient19 = shCoefficients[19],
            shCoefficient20 = shCoefficients[20],
            shCoefficient21 = shCoefficients[21],
            shCoefficient22 = shCoefficients[22],
            shCoefficient23 = shCoefficients[23],
            shCoefficient24 = shCoefficients[24],
            shCoefficient25 = shCoefficients[25],
            shCoefficient26 = shCoefficients[26],
            opacity = gaussOpacity
        };

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

    private static byte[] HandleEndianness(byte[] binaryValue, PlyFileFormat fileFormat){
        if(BitConverter.IsLittleEndian && fileFormat == PlyFileFormat.BINARY_BIG_ENDIAN){
            return binaryValue.Reverse().ToArray();
        }
        else if(!BitConverter.IsLittleEndian && fileFormat == PlyFileFormat.BINARY_LITTLE_ENDIAN){
            return binaryValue.Reverse().ToArray();
        }
        else{
            return binaryValue;
        }
    }

    private static float ReadNextFloat(BinaryReader br, PlyFileFormat fileFormat){
        byte[] currBytes = br.ReadBytes(4);
        currBytes = HandleEndianness(currBytes, fileFormat);

        return BitConverter.ToSingle(currBytes);
    }



    //private static void ReadHeader(FileStream fs, ref )
    
    // Parse the Gaussian file this parser is pointed at
    public Gaussian3D[] ReadFile(float constScale = 1.0f){

        using FileStream fs = File.OpenRead(pathToGaussianFile);
        using BinaryReader binaryReader = new BinaryReader(fs);

        List<Gaussian3D> gaussians = new List<Gaussian3D>();

        // Read the header 
    
        binaryReader.ReadBytes(11);

        // Read file formnay 
        string nextChar = System.Text.Encoding.ASCII.GetString(binaryReader.ReadBytes(1));
        string fileFormatStr = "";
        while(nextChar != " "){
            fileFormatStr += nextChar;
            nextChar = System.Text.Encoding.ASCII.GetString(binaryReader.ReadBytes(1));
        }

        // Set file format
        PlyFileFormat fileFormat = fileFormatStr == "binary_little_endian" ? PlyFileFormat.BINARY_LITTLE_ENDIAN : PlyFileFormat.BINARY_BIG_ENDIAN;

        binaryReader.ReadBytes(12);

        // Read past element name
        nextChar = System.Text.Encoding.ASCII.GetString(binaryReader.ReadBytes(1));
        while(nextChar != " "){
            nextChar = System.Text.Encoding.ASCII.GetString(binaryReader.ReadBytes(1));
        }

        // Read number of elements 
        string numElementsStr = "";
        nextChar = System.Text.Encoding.ASCII.GetString(binaryReader.ReadBytes(1));
        while(nextChar != "\n"){
            numElementsStr += nextChar;
            nextChar = System.Text.Encoding.ASCII.GetString(binaryReader.ReadBytes(1));
        }
        int numElements = int.Parse(numElementsStr);

        // Read through to the rest of the header
        bool readingHeader = true;
        string buffer = "";
        while(readingHeader){
            buffer += System.Text.Encoding.ASCII.GetString(binaryReader.ReadBytes(1));

            if(buffer.Length >= 11){
                readingHeader = !(buffer.Substring(buffer.Length - 11, 11) == "end_header\n");
            }
        }
        
        // Read every element 
        for(int i =0; i < numElements; i++){

            // Read position
            Vector3 pos = new Vector3(ReadNextFloat(binaryReader, fileFormat), 
                ReadNextFloat(binaryReader, fileFormat), ReadNextFloat(binaryReader, fileFormat));

            // Read off normals (Aren't currently using them)
            /*
            ReadNextFloat(binaryReader, fileFormat);
            ReadNextFloat(binaryReader, fileFormat);
            ReadNextFloat(binaryReader, fileFormat);
            */
            

            // Read spherical harmonics coefficients
            
            float[] shCoefficients = new float[27];
            for(int j =0; j < 27; j++){
                shCoefficients[j] = ReadNextFloat(binaryReader, fileFormat);
            }

            // Read in scale and rotation
            Vector3 scale = new Vector3(ReadNextFloat(binaryReader, fileFormat), 
                ReadNextFloat(binaryReader, fileFormat), ReadNextFloat(binaryReader, fileFormat));
            scale *= constScale;

            // Read off opacity
            float opacity = ReadNextFloat(binaryReader, fileFormat);
        
            Gaussian3D g = CreateGaussian3D(pos, scale, shCoefficients, opacity);

            gaussians.Add(g);
        }
        Debug.Log(gaussians.Count);
        return gaussians.ToArray();
    }

    // Read in a Gaussian .ply file and return its results as Gaussian3D objects
    public static Gaussian3D[] ReadGaussianFile(string pathToGaussianFile){
        GaussianPlyParser parser = new GaussianPlyParser(pathToGaussianFile);
        return parser.ReadFile();
    }

    // Read in a Gaussian .ply file and return its results as Gaussian3D objects
    public static Gaussian3D[] ReadGaussianFile(string pathToGaussianFile, float scaleConst){
        GaussianPlyParser parser = new GaussianPlyParser(pathToGaussianFile);
        return parser.ReadFile(scaleConst);
    }


}
