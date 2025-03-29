using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class SceneSerializer : MonoBehaviour
{
    public static void InitializeSceneDataBuffers(in Camera cam, ref ComputeBuffer cameraData, ref MeshRenderer[] meshRenderers, ref List<GameObjectData> gameObjectDatas, ref ComputeBuffer gameObjectDatasBuffer, ref ComputeBuffer aabbsBuffer, ref ComputeBuffer materialDatasBuffer, ref ComputeBuffer trianglesBuffer, ref ComputeBuffer verticesBuffer, ref Texture2DArray texture2DArray)
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
        List<Texture2D> textures = new List<Texture2D>();
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

                aabbRootIndex = BuildBVH.BuildBVHForMesh(meshFilter.sharedMesh.triangles, 
                    ref aabbs, ref triangles, ref vertices, vertexStartIndex);

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

                MaterialData materialData = new MaterialData();

                MaterialType materialType = MaterialType.Diffuse;
                Material mat = meshRenderer.sharedMaterial;

                // albedo
                Color albedo = mat.GetColor("_Color");

                // albedo texture
                Texture2D albedoTexture = mat.GetTexture("_MainTex") as Texture2D;
                if (albedoTexture != null)
                {
                    materialType = MaterialType.Textured;
                    materialData.albedoTextureIndex = (uint) textures.Count;
                    textures.Add(albedoTexture);
                }

                // emission
                if (mat.IsKeywordEnabled("_EMISSION"))
                {
                    Color emissionColor = mat.GetColor("_EmissionColor");
                    if (emissionColor != Color.black)
                    {
                        materialType = MaterialType.Emissive;
                        albedo = emissionColor;
                    }
                }

                materialData.type = materialType;
                materialData.albedo = new Vector4(albedo.r, albedo.g, albedo.b, albedo.a);
 
                materialDatas.Add(materialData);
            }
            else
            {
                materialIndex = (uint)materialInstanceToMaterialData[materialInstanceId];
            }
            currGameObj.materialIndex = materialIndex;

            gameObjectDatas.Add(currGameObj);
        }

        // create texture 2D array
        texture2DArray = null;
        if (textures.Count > 0)
        {
            int texWidth = textures[0].width;
            int texHeight = textures[0].width;
            int texCount = textures.Count;
            TextureFormat texFormat = textures[0].format;
            texture2DArray = new Texture2DArray(texWidth, texHeight, texCount, texFormat, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            for (int i = 0; i < texCount; i++)
            {
                Graphics.CopyTexture(textures[i], 0, 0, texture2DArray, i, 0);
            }
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