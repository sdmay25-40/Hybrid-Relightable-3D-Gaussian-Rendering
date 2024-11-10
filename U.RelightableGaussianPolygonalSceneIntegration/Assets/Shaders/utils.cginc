const float FLT_MAX = 3.402823e+38;
const float EPSILON = 0.001;

// when updating, ensure structs in 'Scripts/GaussianRenderer.cs' & 'Scripts/SceneSerializer' are updated to match
struct PathPayload
{
    float4 direction;
};

struct PathHitRecord
{
    float t;
    float u;
    float v;
    uint materialIndex;
};

struct CameraParams
{
    float3 position;
    uint pathCount;
};

struct PrimaryGenData
{
    float tanFovHalf;
    int screenWidth;
    float invScreenHeight;
    uint pathsPerPixel;
    float4 quaternion;
};

struct GameObjectData
{
    float4x4 objectToWorld;
    float4x4 worldToObject;
    uint aabbRootIndex;
    uint materialIndex;
    float2 padding;
};

struct AABB
{
    float3 min;
    float3 max;
    uint leftChildIndex;
    uint rightChildIndex;
    uint triangleCount;
    uint triangleStartIndex;
    float2 padding;
};

struct MaterialData
{
    float4 albedo;
};

struct Triangle
{
    float4 positions[3];
};

/// <source> https://www.songho.ca/opengl/gl_quaternion.html </source>
/// <summary> Converts a normalized quaternion to a rotation matrix.</summary>
float3x3 quatToRotMatrix(float4 q)
{
    return float3x3(
        // row 1
        (1 - 2 * q.y * q.y - 2 * q.z * q.z), (2 * q.x * q.y - 2 * q.z * q.w), (2 * q.x * q.z + 2 * q.y * q.w),
        // row 2
        (2 * q.x * q.y + 2 * q.z * q.w), (1 - 2 * q.x * q.x - 2 * q.z * q.z), (2 * q.y * q.z - 2 * q.x * q.w),
        // row 3
        (2 * q.x * q.z - 2 * q.y * q.w), (2 * q.y * q.z + 2 * q.x * q.w), (1 - 2 * q.x * q.x - 2 * q.y * q.y)
    );
}