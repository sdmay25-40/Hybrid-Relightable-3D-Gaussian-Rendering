#define FLT_MAX 3.402823e+38
#define EPSILON 0.001

// when updating, ensure structs in 'Scripts/GaussianRenderer.cs' & 'Scripts/SceneSerializer' are updated to match
// ensure structs satisfy 16-byte alignment; padding is only necessary for arrays
struct PathPayload
{
    float4 direction;
    float3 origin;
    uint bounce;
    float4 throughput;
};

struct PathHitRecord
{
    float t;
    float u;
    float v;
    uint materialIndex;
    float4 normal;
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
    uint type;
    float3 padding;
};

struct Triangle
{
    float4 positions[3];
};

struct CameraData
{
    float4 position;
    float4 quaternion;
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

uint2 getPixelIndex(uint pathId, int pathsPerPixel, int screenWidth)
{
    uint x = (uint) ((pathId / pathsPerPixel) % screenWidth);
    uint y = (uint) ((pathId / pathsPerPixel) / screenWidth);
    return uint2(x,y);
}

float random(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

float2 generateRandomSample(float2 uv)
{
    float rand1 = random(uv);
    float rand2 = random(uv + float2(1.0, 0.0));
    return float2(rand1, rand2);
}