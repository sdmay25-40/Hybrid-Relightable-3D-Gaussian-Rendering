using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

struct StackNode{
    // Whether or not this stack node is currently on the stack
    // 0 = false (Not valid), 1 = true (valid)
    uint valid;
    // The data this node contains
    uint data;
    // The index of the next node in the stack in the backing array. 
    // If this node is at the bottom if the stack this will be STACK_MAX_SIZE
    uint nextIndex;
};

public class GaussianRenderer : MonoBehaviour
{
    // references
    [SerializeField] private Camera cam;
    [SerializeField] private ComputeShader generatePrimaryPaths;
    [SerializeField] private ComputeShader getPathIntersections;
    [SerializeField] private ComputeShader samplePathIntersections;
    [SerializeField] private ComputeShader accumulateRenderTexture;
    [SerializeField] private ComputeShader increment;
    // settings
    [SerializeField] private int pathsPerPixel = 1;
    [SerializeField] private int pathBounceLimit = 1;
    private CommandBuffer commandBuffer;
    private RenderTexture renderTexture;
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
    private ComputeBuffer gameObjectDatas;
    private ComputeBuffer stackBuffer;

    private int gameObjectDataCount;
    private ComputeBuffer cameraData;
    private MeshRenderer[] meshRenderers;
    private List<GameObjectData> gameObjectDatasList = new List<GameObjectData>();

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

        renderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        if (!renderTexture.Create())
        {
            Debug.LogError("'GaussianRender': Failed to create 'RenderTexture'.");
        }

        accumulationTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        accumulationTexture.enableRandomWrite = true;
        if (!accumulationTexture.Create())
        {
            Debug.LogError("'GaussianRender': Failed to create 'RenderTexture'.");
        }

        frameIndex = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);
        frameIndex.SetData(new uint[]{0});

        int pathCount = Screen.width * Screen.height * pathsPerPixel;
        paths = new ComputeBuffer(pathCount,  Marshal.SizeOf(typeof(PathPayload)));
        pathHitRecords = new ComputeBuffer(pathCount, Marshal.SizeOf(typeof(PathHitRecord)));
        pathsContinueCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        pathsContinueCounterValue = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        pathsContinueTmpCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        pathsContinueTmpCounterValue = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        stackBuffer = new ComputeBuffer(pathCount * 500, Marshal.SizeOf(typeof(StackNode)));

        commandBuffer = new CommandBuffer();
        commandBuffer.name = "Hybrid Gaussian Raytracer";

        SceneSerializer.InitializeSceneDataBuffers(cam, ref cameraData, ref meshRenderers, ref gameObjectDatasList, ref gameObjectDatas, ref aabbs, ref materialDatas, ref triangles);
        BuildCommandBuffer();

    }

    private void Update()
    {
        // TODO: if camera moves or something moves in the scene: update buffers + reset accumulation buffer and frame index
        // if (true)
        // {
        //     SceneSerializer.UpdateSceneDataBuffer(cam, ref cameraData, meshRenderers, ref gameObjectDatasList, ref gameObjectDatas);
        // }
    }

    private void OnDestroy()
    {
        cam.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
        if (commandBuffer != null)
        {
            commandBuffer.Release();
            commandBuffer = null;
        }
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
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
        if(aabbs != null)
        {
            aabbs.Release();
            aabbs = null;
        }
        if(materialDatas != null)
        {
            materialDatas.Release();
            materialDatas = null;
        }
        if(triangles != null)
        {
            triangles.Release();
            triangles = null;
        }
        if(stackBuffer != null){
            stackBuffer.Release();
            stackBuffer = null;
        }
    }

    /// <summary>
    /// Builds and inserts Hybrid Gaussian Render Pipeline command buffer into `CameraEvent.BeforeImageEffects`.
    /// Unity handles resource dependency-based synchronization.
    /// </summary>
    private void BuildCommandBuffer()
    {
        // reset values
        commandBuffer.SetRenderTarget(renderTexture);
        commandBuffer.ClearRenderTarget(true, true, Color.black);
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

        // TODO: remove bounce from Path struct (create a int buffer to keep count in the loop)
        for(uint i = 0; i < pathBounceLimit + 1; i++)
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
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathHitRecords", pathHitRecords);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathsContinueTmpCounter", pathsContinueTmpCounter);
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
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "materialDatas", materialDatas);
                commandBuffer.SetComputeIntParam(samplePathIntersections, "pathsPerPixel", pathsPerPixel);
                commandBuffer.SetComputeIntParam(samplePathIntersections, "screenWidth", Screen.width);
                commandBuffer.SetComputeIntParam(samplePathIntersections, "pathBounceLimit", pathBounceLimit);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "paths", paths);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathsContinueCounter", pathsContinueCounter);
                commandBuffer.SetComputeTextureParam(samplePathIntersections, kernelIndex, "renderTexture", renderTexture);
                commandBuffer.DispatchCompute(samplePathIntersections, kernelIndex, threadGroupX, 1, 1);
            }
        }

        // accumulate render texture
        {
            int kernelIndex = accumulateRenderTexture.FindKernel("CSMain");
            int threadGroupsX = Mathf.CeilToInt(renderTexture.width / 32.0f);
            int threadGroupsY = Mathf.CeilToInt(renderTexture.height / 32.0f);
            commandBuffer.SetComputeIntParam(accumulateRenderTexture, "screenWidth", Screen.width);
            commandBuffer.SetComputeIntParam(accumulateRenderTexture, "screenHeight", Screen.height);
            commandBuffer.SetComputeBufferParam(accumulateRenderTexture, kernelIndex, "frameIndex", frameIndex);
            commandBuffer.SetComputeTextureParam(accumulateRenderTexture, kernelIndex, "renderTexture", renderTexture);
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
}