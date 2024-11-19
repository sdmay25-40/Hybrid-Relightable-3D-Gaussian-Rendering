using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

// when updating, ensure structs in 'Shaders/utils.cginc' are updated to match
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

// TODO: break up vertex positions from other attributes
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
                // we are only creating one AABB per mesh so aabb is root
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
                    // Debug.Log($"Triangle {i / 3}:\n    Vertex 0: {position0}\n    Vertex 1: {position1}\n    Vertex 2: {position2}\n\n");

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
                // Debug.Log($"AABB:\n    Min: {aabb.min}\n    Max: {aabb.max}\n    leftChildIndex: {aabb.leftChildIndex}\n    rightChildIndex: {aabb.rightChildIndex}\n    triangleCount: {aabb.triangleCount}\n    triangleStartIndex: {aabb.triangleStartIndex}\n\n");
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

        // // debugging
        // gameObjectDatasBuffer.SetData(gameObjectDatas.ToArray());
        // aabbsBuffer.SetData(aabbs.ToArray());
        // materialDatasBuffer.SetData(materialDatas.ToArray());
        // trianglesBuffer.SetData(triangles.ToArray());
        // Triangle[] data = new Triangle[trianglesBuffer.count];
        // trianglesBuffer.GetData(data);
        // for (int i = 0; i < trianglesBuffer.count; i++)
        // {
        //     Debug.Log($"Triangle[{i}]:\n   Index0: {data[i].position0}\n   Index1: {data[i].position1}\n   Index2: {data[i].position2}\n");
        // }
    }
}