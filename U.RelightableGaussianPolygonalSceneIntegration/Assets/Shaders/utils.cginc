#define FLT_MAX 3.402823e+38
#define EPSILON 1e-6
#define PI 3.14159265359

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

/// <summary> Converts pathId to pixelcoordinates (x,y) </summary>
uint2 getPixelIndex(uint pathId, int pathsPerPixel, int screenWidth)
{
    uint x = (uint) ((pathId / pathsPerPixel) % screenWidth);
    uint y = (uint) ((pathId / pathsPerPixel) / screenWidth);
    return uint2(x,y);
}

/// <source> https://www.songho.ca/opengl/gl_quaternion.html </source>
/// <summary> Converts a normalized quaternion to a rotation matrix. </summary>
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

/// <summary>
/// Generates a unique seed for each frame and bounce iteration. Seed values above 500,000 generate visual
/// artifacts caused by floating-point precision issues in rand2(). Values +86,213,428 generate no output.
/// </summary>
uint getSeed(uint bounce, uint frameIndex)
{
    // TODO: add pathId as unique identifier; multiple paths per pixel will generate the same random number
    return ((frameIndex * 73856093u) ^ (bounce * 19349663u)) % 500000u;
}

/// <source> https://www.shadertoy.com/view/4djSRW </source>
float2 rand2(float2 uv, uint seed)
{
	float3 p3 = frac(float3(uv.xyx + seed) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

/// <source> https://pbr-book.org/3ed-2018/Monte_Carlo_Integration/2D_Sampling_with_Multidimensional_Transformations#SamplingaUnitDisk </source>
float2 randDiskSample(float2 uv, uint seed)
{
    // map rand numbers to [-1,1]
    float2 rand = 2.0 * rand2(uv, seed) - 1.0;

    // divide by zero edge case
    if (rand.x == 0 && rand.y == 0)
    {
        return float2(0,0);
    }

    // concentric mapping to polar coordinates
    float theta, r;
    if (abs(rand.x) > abs(rand.y))
    {
        r = rand.x;
        theta = (PI / 4.0) * (rand.y / rand.x);
    }
    else
    {
        r = rand.y;
        theta = (PI / 2.0) - (PI / 4.0) * (rand.x / rand.y);
    }

    return r * float2(cos(theta), sin(theta));
}

/// <source> https://pbr-book.org/3ed-2018/Monte_Carlo_Integration/2D_Sampling_with_Multidimensional_Transformations#Cosine-WeightedHemisphereSampling </source>
/// <summary> Generates a cosine weighted sample of a hemisphere using Malley's Method </summary>
float3 randCosHemisphereSample(float3 normal, float2 uv, int seed)
{
    // generate random, cosine-weighted direction above xy-plane
    // float2 rand = randDiskSample(uv, seed) * 0.0;
    float2 rand = randDiskSample(uv, seed);
    float z = sqrt(max(0, 1 - rand.x * rand.x - rand.y * rand.y));
    float3 sampleTangentSpace = float3(rand, z);

    // rotate the z-axis to align with the normal of the surface
    float3 tangent;
    if (abs(normal.y) > 1 - EPSILON)
    {
        tangent = float3(1,0,0);
    }
    else
    {
        tangent = normalize(cross(float3(0,1,0), normal));
    }

    float3 bitangent = cross(normal, tangent);

    // transpose(float3x3(tangent, bitangent, normal))
    return sampleTangentSpace.x * tangent + sampleTangentSpace.y * bitangent + sampleTangentSpace.z * normal;
}