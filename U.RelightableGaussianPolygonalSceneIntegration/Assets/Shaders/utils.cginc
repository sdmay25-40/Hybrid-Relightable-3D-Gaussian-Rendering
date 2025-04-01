#define FLT_MAX 3.402823e+38
#define UINT_MAX 4294967295U
#define EPSILON 1e-6
#define PI 3.14159265359
#define EULER_NUM 2.71828
#define PRIM_TYPE_TRIANGLE 0
#define PRIM_TYPE_GAUSSIAN 1
#define MATERIAL_DIFFUSE 0
#define MATERIAL_EMISSIVE 1
#define MATERIAL_TEXTURED 2
#define STACK_MAX_SIZE 50
#define MAX_HIT 10
#define MIN_OPACITY 0.01
#define T_MIN 0.001

// when updating, ensure structs in 'Scripts/utils.cs' are updated to match
// ensure structs satisfy 16-byte alignment; padding is only necessary for arrays
struct AABB
{
    float3 min;
    float3 max;
    uint leftChildIndex;
    uint rightChildIndex;
    uint primitiveType;
    uint primitiveCount;
    uint primitiveStartIndex;
    uint padding;
};

struct CameraData
{
    float4 position;
    float4 quaternion;
};

struct GameObjectData
{
    float4x4 normalMatrix;
    float4x4 worldToObject;
    uint aabbRootIndex;
    uint materialIndex;
    float2 padding;
};

struct MaterialData
{
    uint type;
    float4 albedo;
    uint albedoTextureIndex;
    float2 padding;
};

struct PathHitRecord
{
    float t;
    uint materialType;
    float4 albedo;
    float3 normal;
    float3 padding;
};

struct PathPayload
{
    float4 direction;
    float3 origin;
    uint bounce;
    float4 throughput;
};

struct Triangle
{
    uint v0;
    uint v1;
    uint v2;
    uint padding;
};

struct Vertex
{
    float3 position;
    float3 normal;
    float2 albedoUV;
};

struct Gaussian
{
    float3 pos;
    float4x4 cov;
    float4x4 invCov;
    float4 color;
    uint shCoefficientsIndex; // index of Spherical harmonics coefficients in buffer
    uint shCoefficientsNum; // number of spherical harmonics coefficients
    float3 normal;
    // PBR Propeties (Color is used for albedo)
    float roughness;
    float metalness;
    float specular;
    float opacity;
    float ambientOcclusion;
    float refraction;
    float emissive;
    /*
    Used like an enum  to denote what type of Gaussian (in the .ply file) this is
    0 = simpleGaussian3D
    1 = gaussian3D
    2 = relightableGaussian3D
    */
    uint gaussianType;
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

/// <source> https://gamedev.stackexchange.com/a/18459 </source>
/// <summary> Check if a ray intersects a bounding box.  </summary>
bool rayAABBIntersect(float3 pathOrigin, float3 pathDir, AABB b)
{
    // TODO: Confirm this is the best (most efficient) AABB intersection method
    float3 dirfrac = rcp(pathDir);

    // calculate t values
    float t1 = (b.min.x - pathOrigin.x) * dirfrac.x;
    float t2 = (b.max.x - pathOrigin.x) * dirfrac.x;
    float t3 = (b.min.y - pathOrigin.y) * dirfrac.y;
    float t4 = (b.max.y - pathOrigin.y) * dirfrac.y;
    float t5 = (b.min.z - pathOrigin.z) * dirfrac.z;
    float t6 = (b.max.z - pathOrigin.z) * dirfrac.z;

    // build t min and max
    float tmin = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
    float tmax = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

    return (tmax >= 0 && tmin <= tmax);
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
/// <summary> Generates a random float2 where each component is between [0,1]. </summary>
float2 rand2(float2 uv, uint seed)
{
	float3 p3 = frac(float3(uv.xyx + seed) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

/// <source> https://pbr-book.org/3ed-2018/Monte_Carlo_Integration/2D_Sampling_with_Multidimensional_Transformations#SamplingaUnitDisk </source>
/// <summary> Generates a uniformly sampled random position on a disk. </summary>
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