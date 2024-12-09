using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

// when updating, ensure structs in 'Shaders/utils.cginc' are updated to match
// ensure structs satisfy 16-byte alignment; padding is only necessary for arrays
public struct PathPayload
{
    public Vector4 direction;
    public Vector4 origin;
}

public struct PathHitRecord
{
    public float t;
    public float u;
    public float v;
    public uint materialIndex;
}

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
    // settings
    [SerializeField] private int pathsPerPixel = 1;
    [SerializeField] private int pathBounceLimit = 1;
    private RenderTexture renderTexture;
    private CommandBuffer commandBuffer;
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
    private ComputeBuffer debugBuffer;
    private int gameObjectDataCount;

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

        renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        if (!renderTexture.Create())
        {
            Debug.LogError("'GaussianRender': Failed to create 'RenderTexture'.");
        }

        int pathCount = Screen.width * Screen.height * pathsPerPixel;
        paths = new ComputeBuffer(pathCount,  Marshal.SizeOf(typeof(PathPayload)));
        pathHitRecords = new ComputeBuffer(pathCount, Marshal.SizeOf(typeof(PathHitRecord)));
        pathsContinueCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        pathsContinueCounterValue = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        pathsContinueTmpCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        pathsContinueTmpCounterValue = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        stackBuffer = new ComputeBuffer(pathCount * 500, Marshal.SizeOf(typeof(StackNode)));
        debugBuffer = new ComputeBuffer(paths.count, sizeof(float));

        commandBuffer = new CommandBuffer();
        commandBuffer.name = "Hybrid Gaussian Raytracer";

        SceneSerializer.GetSceneData(ref commandBuffer, ref gameObjectDatas, ref gameObjectDataCount, ref aabbs, ref materialDatas, ref triangles);
        BuildCommandBuffer();
    }

    private void Update()
    {
        float[] debugList = new float[paths.count];
        debugBuffer.GetData(debugList);
        Debug.Log("0: " + debugList[0]);
        Debug.Log("1: " + debugList[1]);
        // if camera moves or something moves in the scene...
        // cam.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
        // commandBuffer.Clear();
        // BuildCommandBuffer();
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
        commandBuffer.SetBufferCounterValue(pathsContinueCounter, 0);

        float workGroupX = 32.0f;
        int threadGroupX = Mathf.CeilToInt(paths.count / workGroupX);

        // generate primary paths
        {
            int kernelIndex = generatePrimaryPaths.FindKernel("CSMain");
            commandBuffer.SetComputeIntParam(generatePrimaryPaths, "pathCount", paths.count);
            commandBuffer.SetComputeIntParam(generatePrimaryPaths, "pathsPerPixel", pathsPerPixel);
            commandBuffer.SetComputeIntParam(generatePrimaryPaths, "screenWidth", Screen.width);
            commandBuffer.SetComputeFloatParam(generatePrimaryPaths, "invScreenHeight", 1.0f / Screen.height);
            commandBuffer.SetComputeFloatParam(generatePrimaryPaths, "tanFovHalf", Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f));
            commandBuffer.SetComputeVectorParam(generatePrimaryPaths, "cameraPosition", new Vector4(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z, 1.0f));
            commandBuffer.SetComputeVectorParam(generatePrimaryPaths, "cameraQuaternion", new Vector4(cam.transform.rotation.x, cam.transform.rotation.y, cam.transform.rotation.z, cam.transform.rotation.w));
            commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "pathsContinueCounter", pathsContinueCounter);
            commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "paths", paths);
            commandBuffer.DispatchCompute(generatePrimaryPaths, kernelIndex, threadGroupX, 1, 1);
        }

        for(uint i = 0; i < pathBounceLimit; i++)
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
                commandBuffer.SetComputeIntParam(getPathIntersections, "gameObjectDataCount", gameObjectDataCount);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "aabbs", aabbs);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "triangles", triangles);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathHitRecords", pathHitRecords);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathsContinueTmpCounter", pathsContinueTmpCounter);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "debugBuffer", debugBuffer);
                commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "stackBuffer", stackBuffer);
                commandBuffer.DispatchCompute(getPathIntersections, kernelIndex, threadGroupX, 1, 1);
            }

            commandBuffer.SetBufferCounterValue(pathsContinueCounter, 0);
            commandBuffer.CopyCounterValue(pathsContinueTmpCounter, pathsContinueTmpCounterValue, 0);

            // sample path intersections
            {
                int kernelIndex = samplePathIntersections.FindKernel("CSMain");
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathsContinueTmpCounter", pathsContinueTmpCounter);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathsContinueTmpCounterValue", pathsContinueTmpCounterValue);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathHitRecords", pathHitRecords);
                commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "materialDatas", materialDatas);
                commandBuffer.SetComputeIntParam(samplePathIntersections, "pathsPerPixel", pathsPerPixel);
                commandBuffer.SetComputeIntParam(samplePathIntersections, "screenWidth", Screen.width);
                commandBuffer.SetComputeTextureParam(samplePathIntersections, kernelIndex, "renderTexture", renderTexture);
                commandBuffer.DispatchCompute(samplePathIntersections, kernelIndex, threadGroupX, 1, 1);
            }
        }

        commandBuffer.Blit(renderTexture, null as RenderTexture);
        cam.AddCommandBuffer(CameraEvent.AfterEverything, commandBuffer);
    }
}