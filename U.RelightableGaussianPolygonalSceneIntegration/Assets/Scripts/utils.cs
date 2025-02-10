using UnityEngine;

// when updating, ensure structs in 'Shaders/utils.cginc' are updated to match
// ensure structs satisfy 16-byte alignment; padding is only necessary for arrays
public struct AABB
{
    public Vector3 min;
    public Vector3 max;
    public uint leftChildIndex;
    public uint rightChildIndex;
    public uint triangleCount; // if not a leaf node, set to uint.MaxValue
    public uint triangleStartIndex;
    private Vector2 padding;
}

public struct CameraData
{
    public Vector4 position;
    public Vector4 quaternion;
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
    public Vector4 albedo;
    public uint type;
    private Vector3 padding;
    // float metallic;
    // float roughness;
    // ...
}

public struct PathHitRecord
{
    public float t;
    public float u;
    public float v;
    public uint materialIndex;
    public Vector4 normal;
}

public struct PathPayload
{
    public Vector4 direction;
    public Vector3 origin;
    public uint bounce;
    public Vector4 throughput;
}

// TODO: Create vertex struct to hold attributes, triangle references vertex index
public struct Triangle
{
    public Vector4 position0;
    public Vector4 position1;
    public Vector4 position2;
}