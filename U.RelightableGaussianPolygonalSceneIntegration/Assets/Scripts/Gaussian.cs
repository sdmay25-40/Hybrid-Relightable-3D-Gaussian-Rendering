using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// An Object which reprsents a Gaussian in 3D space
public class Gaussian3D{

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
        // Create position vector 
        _pos = new Vector3(x, y, z);

        // Create scale and rotation matrices
        Matrix4x4 scaleMat = Matrix4x4.Scale(new Vector3(xScale, yScale, zScale));
        Matrix4x4 rotMat = Matrix4x4.Rotate(Quaternion.Euler(alpha, beta, gamma));


        // Create covariance matrix from scale and rotation 
        _covariance = rotMat * scaleMat * Matrix4x4.Transpose(scaleMat) * Matrix4x4.Transpose(rotMat);

        // Create color
        _color = new Vector4(r, g, b, a);
        
    }


}


// Object which can be used to parse a Gaussian .ply file and return the retrieved Gaussians
public class GaussianPlyParser 
{
    private string targetPath;

    // Create a new GaussianPlyParser to read in the file at the given path
    public GaussianPlyParser(string targetPath){
        // Confirm that the given file exists and throw an exception if it doesn't
        if(!File.Exists(targetPath)){
            throw new FileNotFoundException("File located at " + targetPath + " does not exist");
        }

        this.targetPath = targetPath;
    }
    
    // Parse the Gaussian file this parser to pointed at
    public Gaussian3D[] ParseGaussianFile(){
        throw new NotImplementedException();
    }
}
