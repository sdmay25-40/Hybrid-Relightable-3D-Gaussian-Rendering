using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HybridGaussianRenderer : MonoBehaviour
{
    // references
    [SerializeField] private Camera cam;
    [SerializeField] private ComputeShader clearCurrentFrameBuffer;
    [SerializeField] private ComputeShader generatePrimaryPaths;
    [SerializeField] private ComputeShader getPathIntersections;
    [SerializeField] private ComputeShader samplePathIntersections;
    [SerializeField] private ComputeShader accumulateRenderTexture;
    [SerializeField] private ComputeShader increment;
    // settings
    [SerializeField] private int pathsPerPixel = 1;
    [SerializeField] private int pathBounceLimit = 1;
    private CommandBuffer commandBuffer;
    private ComputeBuffer currentFrameBuffer;
    private RenderTexture accumulationTexture;
    private ComputeBuffer frameIndex;
    private ComputeBuffer paths;
    private ComputeBuffer pathHitRecords;
    private ComputeBuffer pathsContinueCounter; // buffer of continued path indices
    private ComputeBuffer pathsContinueCounterValue; // continued path list size
    private ComputeBuffer pathsContinueTmpCounter; // buffer of temporary continued path indices
    private ComputeBuffer pathsContinueTmpCounterValue; // temporary continued path list size
    private ComputeBuffer aabbs;
    private ComputeBuffer materialDatas;
    private ComputeBuffer triangles;
    private ComputeBuffer vertices;
    private Texture2DArray textures;
    private ComputeBuffer gameObjectDatas;
    private ComputeBuffer stackBuffer;
    private ComputeBuffer cameraData;
    private ComputeBuffer gaussians;
    private ComputeBuffer sortedHitsBuffer;
    private List<GameObjectData> gameObjectDatasList = new List<GameObjectData>();
    private Dictionary<Transform, SimpleTransform> transformToPrevTransform = new Dictionary<Transform, SimpleTransform>();
    private CameraData prevCameraData;

    private void Awake()
    {
        // disable camera's rendering
        if (cam == null)
        { 
            Debug.LogError("'GaussianRender': 'cam' reference not set to an instance of a 'Camera'.");
        }
        cam.clearFlags = CameraClearFlags.Nothing;
        cam.cullingMask = 0;
        cam.depthTextureMode = DepthTextureMode.None;

        // create buffers
        accumulationTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        accumulationTexture.enableRandomWrite = true;
        if (!accumulationTexture.Create())
        {
            Debug.LogError("'GaussianRender': Failed to create 'RenderTexture'.");
        }

        frameIndex = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);
        frameIndex.SetData(new uint[]{0});

        int pathCount = Screen.width * Screen.height * pathsPerPixel;

        // currentFrameBuffer = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        currentFrameBuffer = new ComputeBuffer(Screen.width * Screen.height, sizeof(uint) * 3);
        paths = new ComputeBuffer(pathCount,  Marshal.SizeOf(typeof(PathPayload)));
        pathHitRecords = new ComputeBuffer(pathCount, Marshal.SizeOf(typeof(PathHitRecord)));
        pathsContinueCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        pathsContinueCounterValue = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        pathsContinueTmpCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        pathsContinueTmpCounterValue = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        stackBuffer = new ComputeBuffer(pathCount *  Utils.STACK_SIZE, sizeof(uint));
        sortedHitsBuffer = new ComputeBuffer(pathCount * Utils.MAX_HIT, Marshal.SizeOf(typeof(PathHitRecord)));

        SceneSerializer.InitializeSceneDataBuffers(cam, ref cameraData, ref prevCameraData, ref transformToPrevTransform, ref gameObjectDatasList, ref gameObjectDatas, ref aabbs, ref materialDatas, ref triangles, ref vertices, ref gaussians, ref textures);

        // build and insert Hybrid Gaussian Renderer command buffer into `CameraEvent.AfterEverything`.
        // Unity handles resource dependency-based synchronization.

        commandBuffer = new CommandBuffer();
        commandBuffer.name = "Hybrid Gaussian Raytracer";

        // clear current frame buffer
        {
            int kernelIndex = clearCurrentFrameBuffer.FindKernel("CSMain");
            int threadGroupsX = Mathf.CeilToInt(Screen.width / 32.0f);
            int threadGroupsY = Mathf.CeilToInt(Screen.height / 32.0f);
            commandBuffer.SetComputeIntParam(clearCurrentFrameBuffer, "screenWidth", Screen.width);
            commandBuffer.SetComputeIntParam(clearCurrentFrameBuffer, "screenHeight", Screen.height);
            commandBuffer.SetComputeBufferParam(clearCurrentFrameBuffer, kernelIndex, "currentFrameBuffer", currentFrameBuffer);
            commandBuffer.DispatchCompute(clearCurrentFrameBuffer, kernelIndex, threadGroupsX, threadGroupsY, 1);
        }

        // set paths continue size to zero
        commandBuffer.SetBufferCounterValue(pathsContinueCounter, 0);

        float workGroupX = 128.0f;
        int threadGroupX = Mathf.CeilToInt(paths.count / workGroupX);

        // generate primary paths
        {
            int kernelIndex = generatePrimaryPaths.FindKernel("CSMain");
            commandBuffer.SetComputeIntParam(generatePrimaryPaths, "pathCount", paths.count);
            commandBuffer.SetComputeIntParam(generatePrimaryPaths, "pathsPerPixel", pathsPerPixel);
            commandBuffer.SetComputeIntParam(generatePrimaryPaths, "screenWidth", Screen.width);
            commandBuffer.SetComputeFloatParam(generatePrimaryPaths, "invScreenHeight", 1.0f / Screen.height);
            commandBuffer.SetComputeFloatParam(generatePrimaryPaths, "tanFovHalf", Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f));
            commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "cameraData", cameraData);
            commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "pathsContinueCounter", pathsContinueCounter);
            commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "paths", paths);
            commandBuffer.DispatchCompute(generatePrimaryPaths, kernelIndex, threadGroupX, 1, 1);
        }

        for (int i = 0; i < pathBounceLimit + 1; i++)
        {
            commandBuffer.SetBufferCounterValue(pathsContinueTmpCounter, 0);
            commandBuffer.CopyCounterValue(pathsContinueCounter, pathsContinueCounterValue, 0);

            // get path intersections
            {
                int kernelIndex = getPathIntersections.FindKernel("CSMain");
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "paths", paths);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathsContinueCounter", pathsContinueCounter);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathsContinueCounterValue", pathsContinueCounterValue);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "gameObjectDatas", gameObjectDatas);
                commandBuffer.SetComputeIntParam(getPathIntersections, "gameObjectDataCount", gameObjectDatas.count);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "aabbs", aabbs);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "triangles", triangles);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "vertices", vertices);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "materialDatas", materialDatas);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "gaussians", gaussians);
                commandBuffer.SetComputeTextureParam(getPathIntersections, kernelIndex, "textures", textures);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathHitRecords", pathHitRecords);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathsContinueTmpCounter", pathsContinueTmpCounter);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "sortedHitsBuffer", sortedHitsBuffer);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "stackBuffer", stackBuffer);
                commandBuffer.DispatchCompute(getPathIntersections, kernelIndex, threadGroupX, 1, 1);
            }

            commandBuffer.SetBufferCounterValue(pathsContinueCounter, 0);
            commandBuffer.CopyCounterValue(pathsContinueTmpCounter, pathsContinueTmpCounterValue, 0);

            // sample path intersections
            {
                int kernelIndex = samplePathIntersections.FindKernel("CSMain");
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "frameIndex", frameIndex);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathsContinueTmpCounter", pathsContinueTmpCounter);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathsContinueTmpCounterValue", pathsContinueTmpCounterValue);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathHitRecords", pathHitRecords);
                commandBuffer.SetComputeIntParam(samplePathIntersections, "pathsPerPixel", pathsPerPixel);
                commandBuffer.SetComputeIntParam(samplePathIntersections, "screenWidth", Screen.width);
                commandBuffer.SetComputeIntParam(samplePathIntersections, "pathBounce", i);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "paths", paths);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathsContinueCounter", pathsContinueCounter);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "currentFrameBuffer", currentFrameBuffer);
                commandBuffer.DispatchCompute(samplePathIntersections, kernelIndex, threadGroupX, 1, 1);
            }
        }

        // accumulate render texture
        {
            int kernelIndex = accumulateRenderTexture.FindKernel("CSMain");
            int threadGroupsX = Mathf.CeilToInt(Screen.width / 32.0f);
            int threadGroupsY = Mathf.CeilToInt(Screen.height / 32.0f);
            commandBuffer.SetComputeIntParam(accumulateRenderTexture, "screenWidth", Screen.width);
            commandBuffer.SetComputeIntParam(accumulateRenderTexture, "screenHeight", Screen.height);
            commandBuffer.SetComputeBufferParam(accumulateRenderTexture, kernelIndex, "frameIndex", frameIndex);
            commandBuffer.SetComputeBufferParam(accumulateRenderTexture, kernelIndex, "currentFrameBuffer", currentFrameBuffer);
            commandBuffer.SetComputeTextureParam(accumulateRenderTexture, kernelIndex, "accumulationTexture", accumulationTexture);
            commandBuffer.DispatchCompute(accumulateRenderTexture, kernelIndex, threadGroupsX, threadGroupsY, 1);
        }

        commandBuffer.Blit(accumulationTexture, null as RenderTexture);

        // increment frame index
        {
            int kernelIndex = increment.FindKernel("CSMain");
            commandBuffer.SetComputeBufferParam(increment, kernelIndex, "buffer", frameIndex);
            commandBuffer.DispatchCompute(increment, kernelIndex, 1, 1, 1);
        }

        cam.AddCommandBuffer(CameraEvent.AfterEverything, commandBuffer);
    }

    private void Update()
    {
        // check if cameraData needs to be updated
        Vector4 camPos = Utils.GetCameraPosition(cam);
        Quaternion camRot = cam.transform.rotation;
        if (!camPos.Equals(prevCameraData.position) || !camRot.Equals(prevCameraData.quaternion))
        {
            SceneSerializer.SetCameraDataBuffer(camPos, camRot, ref cameraData, ref prevCameraData);
            frameIndex.SetData(new uint[]{0});
        }

        // check if gameObjectsData needs to be updated
        bool dirtyData = false;
        List<Transform> keys = new List<Transform>(transformToPrevTransform.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            Transform t = keys[i];
            SimpleTransform prevTransform = transformToPrevTransform[t];

            if (!t.position.Equals(prevTransform.position)
                || !t.rotation.Equals(prevTransform.rotation)
                || !t.localScale.Equals(prevTransform.scale))
            {
                dirtyData = true;

                GameObjectData currGameObj = gameObjectDatasList[i];
                currGameObj.normalMatrix = t.localToWorldMatrix.inverse.transpose;
                currGameObj.worldToObject = t.worldToLocalMatrix;
                gameObjectDatasList[i] = currGameObj;

                transformToPrevTransform[t] = new SimpleTransform
                {
                    position = t.position,
                    rotation = t.rotation,
                    scale = t.localScale
                };
            }
        }
        if (dirtyData)
        {
            gameObjectDatas.SetData(gameObjectDatasList);
            frameIndex.SetData(new uint[]{0});
        }
    }

    private void OnDestroy()
    {
        if (commandBuffer != null)
        {
            cam.RemoveCommandBuffer(CameraEvent.AfterEverything, commandBuffer);
            commandBuffer.Release();
            commandBuffer = null;
        }
        if (currentFrameBuffer != null)
        {
            currentFrameBuffer.Release();
            currentFrameBuffer = null;
        }
        if (accumulationTexture != null)
        {
            accumulationTexture.Release();
            accumulationTexture = null;
        }
        if (frameIndex != null)
        {
            frameIndex.Release();
            frameIndex = null;
        }
        if (paths != null)
        {
            paths.Release();
            paths = null;
        }
        if (pathHitRecords != null)
        {
            pathHitRecords.Release();
            pathHitRecords = null;
        }
        if (pathsContinueCounter != null)
        {
            pathsContinueCounter.Release();
            pathsContinueCounter = null;
        }
        if (pathsContinueCounterValue != null)
        {
            pathsContinueCounterValue.Release();
            pathsContinueCounterValue = null;
        }
        if (pathsContinueTmpCounter != null)
        {
            pathsContinueTmpCounter.Release();
            pathsContinueTmpCounter = null;
        }
        if (pathsContinueTmpCounterValue != null)
        {
            pathsContinueTmpCounterValue.Release();
            pathsContinueTmpCounterValue = null;
        }
        if (cameraData != null)
        {
            cameraData.Release();
            cameraData = null;
        }
        if (gameObjectDatas != null)
        {
            gameObjectDatas.Release();
            gameObjectDatas = null;
        }
        if (aabbs != null)
        {
            aabbs.Release();
            aabbs = null;
        }
        if (materialDatas != null)
        {
            materialDatas.Release();
            materialDatas = null;
        }
        if (triangles != null)
        {
            triangles.Release();
            triangles = null;
        }
        if(stackBuffer != null){
            stackBuffer.Release();
            stackBuffer = null;
        }
        if (vertices != null)
        {
            vertices.Release();
            vertices = null;
        }
        if (textures != null)
        {
            Destroy(textures);
            textures = null;
        }
        if (gaussians != null)
        {
            gaussians.Release();
            gaussians = null;
        }
        if (sortedHitsBuffer != null)
        {
            sortedHitsBuffer.Release();
            sortedHitsBuffer = null;
        }
    }
}