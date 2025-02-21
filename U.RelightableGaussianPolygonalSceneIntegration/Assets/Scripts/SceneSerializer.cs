using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class SceneSerializer : MonoBehaviour
{
    public static void InitializeSceneDataBuffers(in Camera cam, ref ComputeBuffer cameraData, ref MeshRenderer[] meshRenderers, ref List<GameObjectData> gameObjectDatas, ref ComputeBuffer gameObjectDatasBuffer, ref ComputeBuffer aabbsBuffer, ref ComputeBuffer materialDatasBuffer, ref ComputeBuffer trianglesBuffer, ref ComputeBuffer verticesBuffer)
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
        List<Vertex> vertices = new List<Vertex>();
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
                Mesh mesh = meshFilter.sharedMesh;

                // we are only creating one AABB per mesh atm so aabb is root
                aabbRootIndex = (uint)aabbs.Count; 
                meshInstanceToAABB.Add(meshInstanceId, aabbs.Count);

                // add vertex data
                uint vertexStartIndex = (uint) vertices.Count;
                for (int i = 0; i < mesh.vertices.Length; i++)
                {
                    Vertex v;
                    v.position = mesh.vertices[i];
                    v.normal = mesh.normals[i];
                    v.albedoUV = mesh.uv[i];
                    vertices.Add(v);
                }

                int[] meshTriangles = mesh.triangles;

                AABB aabb = new AABB();
                aabb.triangleStartIndex = (uint) triangles.Count;
                aabb.triangleCount = (uint) meshTriangles.Length / 3;

                Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                for (int i = 0; i < meshTriangles.Length; i+=3)
                {
                    Triangle t = new Triangle
                    {
                        v0 = (uint) meshTriangles[i] + vertexStartIndex,
                        v1 = (uint) meshTriangles[i+1] + vertexStartIndex,
                        v2 = (uint) meshTriangles[i+2] + vertexStartIndex
                    };
                    triangles.Add(t);

                    min = Vector3.Min(min, vertices[(int)t.v0].position);
                    min = Vector3.Min(min, vertices[(int)t.v1].position);
                    min = Vector3.Min(min, vertices[(int)t.v2].position);
                    max = Vector3.Max(max, vertices[(int)t.v0].position);
                    max = Vector3.Max(max, vertices[(int)t.v1].position);
                    max = Vector3.Max(max, vertices[(int)t.v2].position);
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
        verticesBuffer = new ComputeBuffer(vertices.Count, Marshal.SizeOf(typeof(Vertex)));

        gameObjectDatasBuffer.SetData(gameObjectDatas);
        aabbsBuffer.SetData(aabbs);
        materialDatasBuffer.SetData(materialDatas);
        trianglesBuffer.SetData(triangles);
        verticesBuffer.SetData(vertices);
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