using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Rendering;

// when updating, ensure structs in 'Shaders/utils.cginc' are updated to match
// ensure structs satisfy 16-byte alignment; padding is only necessary for arrays
public struct GameObjectData
{
    public Matrix4x4 objectToWorld;
    public Matrix4x4 worldToObject;
    public uint aabbRootIndex;
    public uint materialIndex;
    private Vector2 padding;
}

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

public struct MaterialData
{
    public Vector4 albedo;
    public uint type;
    private Vector3 padding;
    // float metallic;
    // float roughness;
    // ...
}

// TODO: break up vertex positions from other attributes when expanded
public struct Triangle
{
    // you cannot do public Vector4 positions[3] in C#
    public Vector4 position0;
    public Vector4 position1;
    public Vector4 position2;
    // ...
}

public struct CameraData
{
    public Vector4 position;
    public Vector4 quaternion;
}

public class SceneSerializer : MonoBehaviour
{
    public static void InitializeSceneDataBuffers(in Camera cam, ref ComputeBuffer cameraData, ref MeshRenderer[] meshRenderers, ref List<GameObjectData> gameObjectDatas, ref ComputeBuffer gameObjectDatasBuffer, ref ComputeBuffer aabbsBuffer, ref ComputeBuffer materialDatasBuffer, ref ComputeBuffer trianglesBuffer)
    {
        // init camera buffer        
        cameraData = new ComputeBuffer(1, Marshal.SizeOf(typeof(CameraData)));
        CameraData camData = new CameraData
        {
            position = new Vector4(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z, 1.0f),
            quaternion = new Vector4(cam.transform.rotation.x, cam.transform.rotation.y, cam.transform.rotation.z, cam.transform.rotation.w)
        };
        cameraData.SetData(new CameraData[]{camData});

        List<AABB> aabbs = new List<AABB>();
        List<MaterialData> materialDatas = new List<MaterialData>();
        List<Triangle> triangles = new List<Triangle>();
        Dictionary<int, int> meshInstanceToAABB = new Dictionary<int, int>();
        Dictionary<int, int> materialInstanceToMaterialData = new Dictionary<int, int>();

        meshRenderers = FindObjectsOfType<MeshRenderer>();
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            GameObjectData currGameObj = new GameObjectData();

            Transform transform = meshRenderer.gameObject.transform;
            currGameObj.objectToWorld = transform.localToWorldMatrix;
            currGameObj.worldToObject = transform.worldToLocalMatrix;

            MeshFilter meshFilter = meshRenderer.gameObject.GetComponent<MeshFilter>();
            if (!meshFilter)
            {
                Debug.LogError($"'SceneSerializer': {meshRenderer.gameObject.name} does not contain a MeshFilter");
            }

            // create AABB for each unique mesh
            uint aabbRootIndex;
            int meshInstanceId = meshFilter.sharedMesh.GetInstanceID();
            if (!meshInstanceToAABB.ContainsKey(meshInstanceId))
            {
                // we are only creating one AABB per mesh atm so aabb is root
                aabbRootIndex = (uint)aabbs.Count; 
                meshInstanceToAABB.Add(meshInstanceId, aabbs.Count);

                int[] meshTriangles = meshFilter.sharedMesh.triangles;

                AABB aabb = new AABB();
                aabb.triangleCount = (uint) meshTriangles.Length / 3;
                aabb.triangleStartIndex = (uint) triangles.Count;

                Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                for(int i = 0; i < meshTriangles.Length; i+=3)
                {
                    Vector3 position0 = meshFilter.sharedMesh.vertices[meshTriangles[i]];
                    Vector3 position1 = meshFilter.sharedMesh.vertices[meshTriangles[i+1]];
                    Vector3 position2 = meshFilter.sharedMesh.vertices[meshTriangles[i+2]];

                    Triangle t;
                    t.position0 = position0;
                    t.position1 = position1;
                    t.position2 = position2;
                    triangles.Add(t);

                    min = Vector3.Min(min, position0);
                    min = Vector3.Min(min, position1);
                    min = Vector3.Min(min, position2);
                    max = Vector3.Max(max, position0);
                    max = Vector3.Max(max, position1);
                    max = Vector3.Max(max, position2);
                }

                aabb.min = min;
                aabb.max = max;
                aabb.leftChildIndex = uint.MaxValue;
                aabb.rightChildIndex = uint.MaxValue;

                aabbs.Add(aabb);
            }
            else
            {
                aabbRootIndex = (uint)meshInstanceToAABB[meshInstanceId];
            }
            currGameObj.aabbRootIndex = aabbRootIndex;

            // create material data for each unique material
            uint materialIndex;
            int materialInstanceId = meshRenderer.sharedMaterial.GetInstanceID();
            if (!materialInstanceToMaterialData.ContainsKey(materialInstanceId))
            {
                materialIndex = (uint)materialDatas.Count;
                materialInstanceToMaterialData.Add(materialInstanceId, materialDatas.Count);

                Material meshMaterial = meshRenderer.sharedMaterial;

                uint materialType = 0;
                Color albedo = meshMaterial.GetColor("_Color");
                if (meshMaterial.IsKeywordEnabled("_EMISSION"))
                {
                    Color emissionColor = meshMaterial.GetColor("_EmissionColor");
                    if (emissionColor != Color.black)
                    {
                        materialType = 1;
                        albedo = emissionColor;
                    }
                }

                MaterialData materialData = new MaterialData();
                materialData.albedo = new Vector4(albedo.r, albedo.g, albedo.b, albedo.a);
                materialData.type = materialType;

                materialDatas.Add(materialData);
            }
            else
            {
                materialIndex = (uint)materialInstanceToMaterialData[materialInstanceId];
            }
            currGameObj.materialIndex = materialIndex;

            gameObjectDatas.Add(currGameObj);
        }

        gameObjectDatasBuffer = new ComputeBuffer(gameObjectDatas.Count, Marshal.SizeOf(typeof(GameObjectData)));
        aabbsBuffer = new ComputeBuffer(aabbs.Count, Marshal.SizeOf(typeof(AABB)));
        materialDatasBuffer = new ComputeBuffer(materialDatas.Count, Marshal.SizeOf(typeof(MaterialData)));
        trianglesBuffer = new ComputeBuffer(triangles.Count, Marshal.SizeOf(typeof(Triangle)));

        gameObjectDatasBuffer.SetData(gameObjectDatas);
        aabbsBuffer.SetData(aabbs);
        materialDatasBuffer.SetData(materialDatas);
        trianglesBuffer.SetData(triangles);
    }

    // can be expanded to update all necessary scene data (lights, camera, ...)
    public static void UpdateSceneDataBuffer(in Camera cam, ref ComputeBuffer cameraData, in MeshRenderer[] meshRenderers, ref List<GameObjectData> gameObjectDatas, ref ComputeBuffer gameObjectDatasBuffer)
    {
        CameraData camData = new CameraData
        {
            position = new Vector4(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z, 1.0f),
            quaternion = new Vector4(cam.transform.rotation.x, cam.transform.rotation.y, cam.transform.rotation.z, cam.transform.rotation.w)
        };
        cameraData.SetData(new CameraData[]{camData});

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            GameObjectData currGameObj = gameObjectDatas[i];
            Transform transform = meshRenderers[i].gameObject.transform;
            currGameObj.objectToWorld = transform.localToWorldMatrix;
            currGameObj.worldToObject = transform.worldToLocalMatrix;
            gameObjectDatas[i] = currGameObj;
        }
        gameObjectDatasBuffer.SetData(gameObjectDatas);
    }
}