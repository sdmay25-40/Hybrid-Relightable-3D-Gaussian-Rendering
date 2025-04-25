using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class SceneSerializer : MonoBehaviour
{
    public static void InitializeSceneDataBuffers(in Camera cam, ref ComputeBuffer cameraData, ref CameraData prevCameraData, ref Dictionary<Transform, SimpleTransform> transformToPrevTransform, ref List<GameObjectData> gameObjectDatas, ref ComputeBuffer gameObjectDatasBuffer, ref ComputeBuffer aabbsBuffer, ref ComputeBuffer materialDatasBuffer, ref ComputeBuffer trianglesBuffer, ref ComputeBuffer verticesBuffer, ref ComputeBuffer gaussiansBuffer, ref Texture2DArray albedoTexture2DArray, ref Texture2DArray normalTexture2DArray, ref Texture2DArray metallicSmoothnessTexture2DArray)
    {
        // init camera buffer
        cameraData = new ComputeBuffer(1, Marshal.SizeOf(typeof(CameraData)));
        SetCameraDataBuffer(Utils.GetCameraPosition(cam), cam.transform.rotation, ref cameraData, ref prevCameraData);

        List<AABB> aabbs = new List<AABB>();
        List<Triangle> triangles = new List<Triangle>();
        List<Vertex> vertices = new List<Vertex>();
        List<MaterialData> materialDatas = new List<MaterialData>();
        List<Texture2D> albedoTextures = new List<Texture2D>();
        List<Texture2D> normalTextures = new List<Texture2D>();
        List<Texture2D> metallicSmoothnessTextures = new List<Texture2D>();
        Dictionary<int, int> meshInstanceToAABB = new Dictionary<int, int>();
        Dictionary<int, int> materialInstanceToMaterialData = new Dictionary<int, int>();

        // create game object mesh data
        MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();
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
                    v.uv = mesh.uv[i];
                    vertices.Add(v);
                }

                aabbRootIndex = BuildBVH.BuildBVHForMesh(meshFilter.sharedMesh.triangles, ref aabbs, ref triangles, ref vertices, vertexStartIndex);
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

                uint materialType = 0u;
                Material mat = meshRenderer.sharedMaterial;

                // albedo
                Color albedo = mat.GetColor("_Color");

                // emission
                if (mat.IsKeywordEnabled("_EMISSION"))
                {
                    Color emissionColor = mat.GetColor("_EmissionColor");
                    if (emissionColor != Color.black)
                    {
                        materialType |= (uint) MaterialType.IsEmissive;
                        albedo = emissionColor;
                    }
                }

                materialData.albedo = new Vector4(albedo.r, albedo.g, albedo.b, albedo.a);

                // albedo texture
                Texture2D albedoTexture = mat.GetTexture("_MainTex") as Texture2D;
                if (albedoTexture != null)
                {
                    materialType |= (uint) MaterialType.HasAlbedoTex;
                    materialData.albedoTextureIndex = (uint) albedoTextures.Count;
                    albedoTextures.Add(albedoTexture);
                }

                // normal texture
                Texture2D normalTexture = mat.GetTexture("_BumpMap") as Texture2D;
                if (normalTexture != null)
                {
                    materialType |= (uint) MaterialType.HasNormalTex;
                    materialData.normalTextureIndex = (uint) normalTextures.Count;
                    normalTextures.Add(normalTexture);
                }

                // metallic smoothness texture
                Texture2D metallicSmoothnessTexture = mat.GetTexture("_MetallicGlossMap") as Texture2D;
                if (metallicSmoothnessTexture != null)
                {
                    materialType |= (uint) MaterialType.HasMetallicSmoothnessTex;
                    materialData.metallicSmoothnessTextureIndex = (uint) metallicSmoothnessTextures.Count;
                    metallicSmoothnessTextures.Add(metallicSmoothnessTexture);
                }

                materialData.type = materialType;
                materialDatas.Add(materialData);
            }
            else
            {
                materialIndex = (uint)materialInstanceToMaterialData[materialInstanceId];
            }
            currGameObj.materialIndex = materialIndex;

            gameObjectDatas.Add(currGameObj);

            Transform t = meshRenderer.gameObject.transform;
            SimpleTransform st = new SimpleTransform
            {
                position = t.position,
                rotation = t.rotation,
                scale = t.localScale
            };
            transformToPrevTransform.Add(t, st);
        }

        // initialize dummy ComputeBuffer to avoid null reference in forced loop unroll at compile time in insertionSortAndCull()
        if (triangles.Count > 0)
        {
            trianglesBuffer = new ComputeBuffer(triangles.Count, Marshal.SizeOf(typeof(Triangle)));
            trianglesBuffer.SetData(triangles);
        }
        else
        {
            trianglesBuffer = new ComputeBuffer(1, sizeof(uint));
        }
        if (vertices.Count > 0)
        {
            verticesBuffer = new ComputeBuffer(vertices.Count, Marshal.SizeOf(typeof(Vertex)));
            verticesBuffer.SetData(vertices);
        }
        else
        {
            verticesBuffer = new ComputeBuffer(1, sizeof(uint));
        }
        if (materialDatas.Count > 0)
        {
            materialDatasBuffer = new ComputeBuffer(materialDatas.Count, Marshal.SizeOf(typeof(MaterialData)));
            materialDatasBuffer.SetData(materialDatas);
        }
        else
        {
            materialDatasBuffer = new ComputeBuffer(1, sizeof(uint));
        }

        // create texture 2D arrays
        Debug.Log("ALBEDO");
        CreateTextureArray2D(ref albedoTexture2DArray, albedoTextures, TextureFormat.DXT1, false);
        Debug.Log("NORMAL");
        CreateTextureArray2D(ref normalTexture2DArray, normalTextures, TextureFormat.RGBAHalf, true);
        Debug.Log("MET/SMOOTH");
        CreateTextureArray2D(ref metallicSmoothnessTexture2DArray, metallicSmoothnessTextures, TextureFormat.RGBAHalf, true);

        // create Gaussian data
        List<BaseGaussian3D.PasssableGaussian3D> gaussians = new List<BaseGaussian3D.PasssableGaussian3D>();
        GaussianScrpt[] gaussianScrpts = FindObjectsOfType<GaussianScrpt>();
        foreach (GaussianScrpt gaussianScrpt in gaussianScrpts)
        {
            BaseGaussian3D[] gaussiansTmp = GaussianPlyParser.ReadGaussianFile(gaussianScrpt.FilePath);
            foreach (BaseGaussian3D g in gaussiansTmp)
            {
                GameObjectData currGameObj = new GameObjectData();
                Transform transform = gaussianScrpt.gameObject.transform;
                currGameObj.normalMatrix = transform.localToWorldMatrix.inverse.transpose;
                currGameObj.worldToObject = transform.worldToLocalMatrix;
                currGameObj.aabbRootIndex = (uint)aabbs.Count;
                gameObjectDatas.Add(currGameObj);

                SimpleTransform st = new SimpleTransform
                {
                    position = transform.position,
                    rotation = transform.rotation,
                    scale = transform.localScale
                };
                transformToPrevTransform.Add(transform, st);
                
                AABB aabb = new AABB();
                aabb.primitiveType = PrimType.Gaussian;
                aabb.primitiveStartIndex = (uint)gaussians.Count;
                aabb.primitiveCount = 1u;
                aabbs.Add(aabb);

                gaussians.Add(g.GetPassableStruct());
            }
        }
        
        if (gaussians.Count > 0)
        {
            gaussiansBuffer = new ComputeBuffer(gaussians.Count, Marshal.SizeOf(typeof(BaseGaussian3D.PasssableGaussian3D)));
            gaussiansBuffer.SetData(gaussians);
        }
        else
        {
            gaussiansBuffer = new ComputeBuffer(1, sizeof(uint));
        }
        gameObjectDatasBuffer = null;
        if (gameObjectDatas.Count > 0)
        {
            gameObjectDatasBuffer = new ComputeBuffer(gameObjectDatas.Count, Marshal.SizeOf(typeof(GameObjectData)));
            gameObjectDatasBuffer.SetData(gameObjectDatas);
        }
        aabbsBuffer = null;
        if (aabbs.Count > 0)
        {
            aabbsBuffer = new ComputeBuffer(aabbs.Count, Marshal.SizeOf(typeof(AABB)));
            aabbsBuffer.SetData(aabbs);
        }
    }

    public static void SetCameraDataBuffer(in Vector4 camPos, in Quaternion camRot, ref ComputeBuffer cameraData, ref CameraData prevCameraData)
    {
        prevCameraData = new CameraData
        {
            position = camPos,
            quaternion = camRot
        };
        cameraData.SetData(new CameraData[]{prevCameraData});
    }

    private static void CreateTextureArray2D(ref Texture2DArray texture2DArray, in List<Texture2D> textures, TextureFormat format, bool linear)
    {
        if (textures.Count <= 0)
        {
            texture2DArray = null;
            return;
        }

        // get largest texture size
        int texCount = textures.Count;
        int texWidth = textures[0].width;
        int texHeight = textures[0].width;
        for (int i = 1; i < textures.Count; i++)
        {
            texWidth = Math.Max(texWidth, textures[i].width);
            texHeight = Math.Max(texHeight, textures[i].height);
        }

        texture2DArray = new Texture2DArray(texWidth, texHeight, texCount, format, false, linear)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };

        for (int i = 0; i < texCount; i++)
        {
            Texture2D currTex = textures[i];
            if (currTex.width != texWidth || currTex.height != texHeight)
            {
                Debug.LogError("Texture size doesn't match");
                // TODO: write a resize texture function
                // users needs to ensure format is the same otherwise
            }
            Graphics.CopyTexture(currTex, 0, 0, texture2DArray, i, 0);
        }
    }
}