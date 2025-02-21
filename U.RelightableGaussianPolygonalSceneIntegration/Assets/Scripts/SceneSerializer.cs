using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


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
            currGameObj.normalMatrix = transform.localToWorldMatrix.inverse.transpose;
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
            currGameObj.normalMatrix = transform.localToWorldMatrix;
            currGameObj.worldToObject = transform.worldToLocalMatrix;
            gameObjectDatas[i] = currGameObj;
        }
        gameObjectDatasBuffer.SetData(gameObjectDatas);
    }
}