using System.Collections.Generic;
using System.Runtime.InteropServices;
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


public class SceneSerializer : MonoBehaviour
{
    public static void GetSceneData(ref CommandBuffer commandBuffer, ref ComputeBuffer gameObjectDatasBuffer, ref int gameObjectDataCount, ref ComputeBuffer aabbsBuffer, ref ComputeBuffer materialDatasBuffer, ref ComputeBuffer trianglesBuffer)
    {
        List<GameObjectData> gameObjectDatas = new List<GameObjectData>();
        List<AABB> aabbs = new List<AABB>();
        List<MaterialData> materialDatas = new List<MaterialData>();
        List<Triangle> triangles = new List<Triangle>();
        Dictionary<int, int> meshInstanceToAABB = new Dictionary<int, int>();
        Dictionary<int, int> materialInstanceToMaterialData = new Dictionary<int, int>();

        MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            // Debug.Log(meshRenderer.gameObject.name);
            GameObjectData currGameObj = new GameObjectData();

            Transform transform = meshRenderer.gameObject.transform;
            currGameObj.objectToWorld = transform.localToWorldMatrix;
            currGameObj.worldToObject = transform.worldToLocalMatrix;
            // Debug.Log(currGameObj.objectToWorld);
            // Debug.Log(currGameObj.worldToObject);

            MeshFilter meshFilter = meshRenderer.gameObject.GetComponent<MeshFilter>();
            if (!meshFilter)
            {
                Debug.LogError($"'SceneSerializer': {meshRenderer.gameObject.name} does not contain a MeshFilter");
            }

            // create AABB for each unique mesh
            uint aabbRootIndex = uint.MaxValue;
            int meshInstanceId = meshFilter.sharedMesh.GetInstanceID();
            if (!meshInstanceToAABB.ContainsKey(meshInstanceId))
            {
                aabbRootIndex = BuildBVH.BuildBVHForMesh(meshFilter, ref aabbs, ref triangles);

                // we are only creating one AABB per mesh atm so aabb is root
                meshInstanceToAABB.Add(meshInstanceId, (int) aabbRootIndex);

            }
            else
            {
                aabbRootIndex = (uint)meshInstanceToAABB[meshInstanceId];
            }
            currGameObj.aabbRootIndex = aabbRootIndex;

            // create material data for each unique material
            uint materialIndex = uint.MaxValue;
            int materialInstanceId = meshRenderer.sharedMaterial.GetInstanceID();
            if (!materialInstanceToMaterialData.ContainsKey(materialInstanceId))
            {
                materialIndex = (uint)materialDatas.Count;
                materialInstanceToMaterialData.Add(materialInstanceId, materialDatas.Count);

                Color albedo = meshRenderer.sharedMaterial.GetColor("_Color");

                MaterialData materialData = new MaterialData();
                materialData.albedo = new Vector4(albedo.r, albedo.g, albedo.b, albedo.a);
                // Debug.Log($"Color: {materialData.albedo}");

                materialDatas.Add(materialData);
            }
            else
            {
                materialIndex = (uint)materialInstanceToMaterialData[materialInstanceId];
            }
            currGameObj.materialIndex = materialIndex;

            gameObjectDatas.Add(currGameObj);
        }        
        gameObjectDataCount = gameObjectDatas.Count;

        gameObjectDatasBuffer = new ComputeBuffer(gameObjectDatas.Count, Marshal.SizeOf(typeof(GameObjectData)));
        aabbsBuffer = new ComputeBuffer(aabbs.Count, Marshal.SizeOf(typeof(AABB)));
        materialDatasBuffer = new ComputeBuffer(materialDatas.Count, Marshal.SizeOf(typeof(MaterialData)));
        trianglesBuffer = new ComputeBuffer(triangles.Count, Marshal.SizeOf(typeof(Triangle)));
        commandBuffer.SetBufferData<GameObjectData>(gameObjectDatasBuffer, gameObjectDatas);
        commandBuffer.SetBufferData<AABB>(aabbsBuffer, aabbs);
        commandBuffer.SetBufferData<MaterialData>(materialDatasBuffer,materialDatas);
        commandBuffer.SetBufferData<Triangle>(trianglesBuffer, triangles);

    }
}