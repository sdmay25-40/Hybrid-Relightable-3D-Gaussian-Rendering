#define FLT_MAX 3.402823e+38
#define UINT_MAX 4294967295U
#define EPSILON 0.001

// when updating, ensure structs in 'Scripts/GaussianRenderer.cs' & 'Scripts/SceneSerializer' are updated to match
// ensure structs satisfy 16-byte alignment; padding is only necessary for arrays
struct PathPayload
{
    float4 direction;
    float4 origin;
};

struct PathHitRecord
{
    float t;
    float u;
    float v;
    uint materialIndex;
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



// Check if a ray intersects a bounding box. 
bool rayAABBIntersect(PathPayload path, AABB b){

    // TODO: If this aab is empty don't intersect 

    // TODO: Confirm this is the best (most efficient) AABB intersection method
    float3 dirfrac;
    // r.dir is unit direction vector of ray
    dirfrac.x = 1.0f / path.direction.x;
    dirfrac.y = 1.0f / path.direction.y;
    dirfrac.z = 1.0f / path.direction.z;

    // Calculate t values
    float t1 = (b.min.x - path.origin.x) * dirfrac.x;
    float t2 = (b.max.x - path.origin.x) * dirfrac.x;
    float t3 = (b.min.y - path.origin.y) * dirfrac.y;
    float t4 = (b.max.y - path.origin.y) * dirfrac.y;
    float t5 = (b.min.z - path.origin.z) * dirfrac.z;
    float t6 = (b.max.z - path.origin.z) * dirfrac.z;

    // Build t min and max
    float tmin = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
    float tmax = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

    // Miss
    if (tmax < 0)
    {
        return false;
    }

    if (tmin > tmax)
    {
        return false;
    }

    return true;
}


