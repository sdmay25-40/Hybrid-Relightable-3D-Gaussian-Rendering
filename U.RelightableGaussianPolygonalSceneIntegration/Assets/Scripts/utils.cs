using N = System.Numerics;
using UnityEngine;
using System.Collections.Generic;

public enum PrimType : uint
{
    Triangle = 0,
    Gaussian = 1
}

public enum MaterialType : uint
{
    Diffuse = 0,
    Emissive = 1,
    Textured = 2
}

// when updating, ensure structs in 'Shaders/utils.cginc' are updated to match
// ensure structs satisfy 16-byte alignment; padding is only necessary for arrays
public struct AABB
{
    public Vector3 min;
    public Vector3 max;
    public uint leftChildIndex;
    public uint rightChildIndex;
    public PrimType primitiveType;
    public uint primitiveCount; // if not a leaf node, set to uint.MaxValue
    public uint primitiveStartIndex;
    private uint padding;
}

public struct CameraData
{
    public Vector4 position;
    public Quaternion quaternion;
}

public struct GameObjectData
{
    public Matrix4x4 normalMatrix;
    public Matrix4x4 worldToObject;
    public uint aabbRootIndex;
    public uint materialIndex;
    private Vector2 padding;
}

public struct MaterialData
{
    public MaterialType type;
    public Vector4 albedo;
    public uint albedoTextureIndex;
    private Vector2 padding;
}

public struct PathHitRecord
{
    public float t;
    public uint materialType;
    public Vector4 albedo;
    public Vector3 normal;
    private Vector3 padding;
}

public struct PathPayload
{
    public Vector4 direction;
    public Vector4 origin;
    public Vector4 throughput;
}

public struct Triangle
{
    public uint v0;
    public uint v1;
    public uint v2;
    private uint padding;
}

public struct Vertex
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 albedoUV;
}

public class MortonPayloadComp : IComparer<MortonPayload>
{

    public int Compare(MortonPayload x, MortonPayload y)
    {
        return x.mortonCode.CompareTo(y.mortonCode);
    }
}

public struct MortonPayload{
    public N.BigInteger mortonCode;
    public int gaussianIdx;
};

public struct SimpleTransform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

public static class Utils
{
    public const int STACK_SIZE = 50;
    public const int MAX_HIT = 10;

    public static Vector4 GetCameraPosition(in Camera cam)
    {
        Vector3 pos = cam.transform.position;
        return new Vector4(pos.x, pos.y, pos.z, 1.0f);
    }
}