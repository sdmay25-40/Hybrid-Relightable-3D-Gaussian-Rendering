// when updating, ensure structs in 'Scripts/GaussianRenderer.cs' are updated to match
struct PathPayload
{
    float4 direction;
};

public struct CameraParams
{
    public Vector3 cameraWorldPos;
    public float tanFovHalf;
    public int screenWidth;
    public float invScreenHeight;
    public uint pathsPerPixel;
    public uint pathCount;
    public Vector4 cameraQuaternion;
};

