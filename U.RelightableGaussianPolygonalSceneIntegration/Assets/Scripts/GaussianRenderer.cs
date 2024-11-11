using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

// when updating, ensure structs in 'Shaders/utils.cginc' are updated to match
// ensure structs satisfy 16-byte alignment; padding is only necessary for arrays
public struct PathPayload
{
    public Vector4 direction;
}

public struct PathHitRecord
{
    public float t;
    public float u;
    public float v;
    public uint materialIndex;
}

// public struct CameraParams
// {
//     public Vector3 position;
//     public uint pathCount;
// }

// public struct PrimaryGenData
// {
//     public float tanFovHalf;
//     public int screenWidth;
//     public float invScreenHeight;
//     public uint pathsPerPixel;
//     public Vector4 quaternion;
// }

public class GaussianRenderer : MonoBehaviour
{
    // references
    [SerializeField] private Camera cam;
    [SerializeField] private ComputeShader fillBufferSequentially;
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
    private ComputeBuffer pathsContinueTmpCounter; // buffer of temporary continued path indices
    // private ComputeBuffer pathsEndCounter; // buffer of ended path indices
    // private ComputeBuffer cameraParamsConst;
    // private ComputeBuffer primaryGenDataConst;
    private ComputeBuffer aabbs;
    private ComputeBuffer materialDatas;
    private ComputeBuffer triangles;
    private ComputeBuffer gameObjectDatas;
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
        pathsContinueTmpCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);

        // pathsEndCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        // primaryGenDataConst = new ComputeBuffer(1, Marshal.SizeOf(typeof(PrimaryGenData)), ComputeBufferType.Constant);
        // cameraParamsConst = new ComputeBuffer(1, Marshal.SizeOf(typeof(CameraParams)), ComputeBufferType.Constant);

        commandBuffer = new CommandBuffer();
        commandBuffer.name = "Hybrid Gaussian Raytracer";

        // SceneSerializer.GetSceneData(ref commandBuffer, ref gameObjectDatas, ref gameObjectDataCount, ref aabbs, ref materialDatas, ref triangles);
        BuildCommandBuffer();
    }

    private void Update()
    {
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
        if (pathsContinueTmpCounter != null)
        {
            pathsContinueTmpCounter.Release();
            pathsContinueTmpCounter = null;
        }
        // if (pathsEndCounter != null)
        // {
        //     pathsEndCounter.Release();
        //     pathsEndCounter = null;
        // }
        // if (cameraParamsConst != null)
        // {
        //     cameraParamsConst.Release();
        //     cameraParamsConst = null;
        // }
        // if (primaryGenDataConst != null)
        // {
        //     primaryGenDataConst.Release();
        //     primaryGenDataConst = null;
        // }
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
    }

    /// <summary>
    /// Builds and inserts Hybrid Gaussian Render Pipeline command buffer into `CameraEvent.BeforeImageEffects`.
    /// Unity handles resource dependency-based synchronization.
    /// </summary>
    private void BuildCommandBuffer()
    {
        // commandBuffer.SetBufferCounterValue(pathsContinueCounter, 0);
        // commandBuffer.SetBufferCounterValue(pathsContinueTmpCounter, 0);
        // // debugging
        // counterComputeBuffer.SetCounterValue(0);
        // ComputeBuffer counterVal = new ComputeBuffer(1,sizeof(int),ComputeBufferType.Raw);
        // ComputeBuffer.CopyCount(counterComputeBuffer,counterVal,0);
        // int[] data = new int[1];
        // counterVal.GetData(data);
        // Debug.Log(data[0]);

        float workGroupX = 32.0f;
        int threadGroupX = Mathf.CeilToInt(paths.count / workGroupX);

        // TODO: delete
        // commandBuffer.SetBufferCounterValue(pathsEndCounter, 0);
        // // fill pathsEnd buffer sequentially
        // {
        //     int kernelIndex = fillBufferSequentially.FindKernel("CSMain");
        //     commandBuffer.SetComputeBufferParam(fillBufferSequentially, kernelIndex, "counterBuffer", pathsEndCounter);
        //     commandBuffer.SetComputeIntParam(fillBufferSequentially, "count", pathsEndCounter.count);
        //     commandBuffer.DispatchCompute(fillBufferSequentially, kernelIndex, threadGroupX, 1, 1);
        //     pathsEndCounter.SetCounterValue(0);
        //     // debugging
        //     fillBufferSequentially.SetBuffer(kernelIndex, "counterBuffer", pathsEndCounter);
        //     fillBufferSequentially.SetInt("count",pathsEndCounter.count);
        //     fillBufferSequentially.Dispatch(kernelIndex, threadGroupX, 1, 1);
        //     uint[] data = new uint[pathsEndCounter.count];
        //     pathsEndCounter.GetData(data);
        //     for (int i = 0; i < 1000; i++)
        //     {
        //         Debug.Log(data[i]);
        //     }
        // }

        // generate primary paths
        {
            // int kernelIndex = generatePrimaryPaths.FindKernel("CSMain");
            // commandBuffer.SetComputeIntParam(generatePrimaryPaths, "pathCount", paths.count);
            // commandBuffer.SetComputeIntParam(generatePrimaryPaths, "pathsPerPixel", pathsPerPixel);
            // commandBuffer.SetComputeIntParam(generatePrimaryPaths, "screenWidth", Screen.width);
            // commandBuffer.SetComputeFloatParam(generatePrimaryPaths, "invScreenHeight", 1.0f / Screen.height);
            // commandBuffer.SetComputeFloatParam(generatePrimaryPaths, "tanFovHalf", Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f));
            // commandBuffer.SetComputeFloatParams(generatePrimaryPaths, "cameraQuaternion", new float[] {cam.transform.rotation.x, cam.transform.rotation.y, cam.transform.rotation.z, cam.transform.rotation.w});
            // commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "pathsContinueCounter", pathsContinueCounter);
            // commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "paths", paths);
            // commandBuffer.DispatchCompute(generatePrimaryPaths, kernelIndex, threadGroupX, 1, 1);
            // // debugging
            // generatePrimaryPaths.SetInt("pathCount", paths.count);
            // generatePrimaryPaths.SetInt("pathsPerPixel", pathsPerPixel);
            // generatePrimaryPaths.SetInt("screenWidth", Screen.width);
            // generatePrimaryPaths.SetFloat("invScreenHeight", 1.0f / Screen.height);
            // generatePrimaryPaths.SetFloat("tanFovHalf", Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f));
            // generatePrimaryPaths.SetFloats("cameraQuaternion", new float[] {cam.transform.rotation.x, cam.transform.rotation.y, cam.transform.rotation.z, cam.transform.rotation.w});
            // generatePrimaryPaths.SetBuffer(kernelIndex, "pathsContinueCounter", pathsContinueCounter);
            // generatePrimaryPaths.SetBuffer(kernelIndex, "paths", paths);
            // generatePrimaryPaths.Dispatch(kernelIndex, threadGroupX, 1, 1);
            // PathPayload[] data = new PathPayload[paths.count];
            // paths.GetData(data);
            // for (int i = 0; i < 4000; i++)
            // {
            //     Debug.Log(i + ": " + data[i].direction);
            // }
        }

        for(uint i = 0; i < pathBounceLimit; i++)
        {
            // get path intersections
            {
                // int kernelIndex = getPathIntersections.FindKernel("CSMain");
                // commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "paths", paths);
                // commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathHitRecords", pathHitRecords);
                // commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathsContinueCounter", pathsContinueCounter);
                // commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "pathsContinueTmpCounter", pathsContinueTmpCounter);
                // commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "aabbs", aabbs);
                // commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "triangles", triangles);
                // commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "gameObjectDatas", gameObjectDatas);
                // commandBuffer.SetComputeIntParam(getPathIntersections, "gameObjectDataCount", gameObjectDataCount);
                // CameraParams cameraParams = new CameraParams
                // {
                //     position = new Vector3(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z),
                //     pathCount = (uint) paths.count
                // };
                // cameraParamsConst.SetData(new CameraParams[] { cameraParams });
                // commandBuffer.SetComputeBufferParam(getPathIntersections, kernelIndex, "cameraParamsConst", cameraParamsConst);
                // commandBuffer.DispatchCompute(getPathIntersections, kernelIndex, threadGroupX, 1, 1);


            }

            // commandBuffer.SetBufferCounterValue(pathsContinueCounter, 0);

            // // sample path intersections
            // {
            //     int kernelIndex = samplePathIntersections.FindKernel("CSMain");
            //     commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathsContinueTmpCounter", pathsContinueTmpCounter);
            //     commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "cameraParamsConst", cameraParamsConst);
            //     commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathHitRecords", pathHitRecords);
            //     commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "materialDatas", materialDatas);
            //     commandBuffer.SetComputeIntParam(samplePathIntersections, "screenWidth", Screen.width);
            //     commandBuffer.SetComputeIntParam(samplePathIntersections, "pathsPerPixel", pathsPerPixel);
            //     commandBuffer.SetComputeBufferParam(samplePathIntersections, kernelIndex, "pathsContinueCounter", pathsContinueCounter);
            //     commandBuffer.SetComputeTextureParam(samplePathIntersections, kernelIndex, "renderTexture", renderTexture);
            //     commandBuffer.DispatchCompute(samplePathIntersections, kernelIndex, threadGroupX, 1, 1);
            // }
        }

        // to get the count from a computebuffertype == counter
        // commandBuffer.CopyCounterValue(src, dst, dstOffsetBytes);

        int kernelIndex1 = samplePathIntersections.FindKernel("CSMain");
        commandBuffer.SetComputeIntParam(samplePathIntersections, "pathCount", paths.count);
        commandBuffer.SetComputeIntParam(samplePathIntersections, "screenWidth", Screen.width);
        commandBuffer.SetComputeIntParam(samplePathIntersections, "pathsPerPixel", pathsPerPixel);
        commandBuffer.SetComputeTextureParam(samplePathIntersections, kernelIndex1, "renderTexture", renderTexture);
        commandBuffer.DispatchCompute(samplePathIntersections, kernelIndex1, threadGroupX, 1, 1);

     
        commandBuffer.Blit(renderTexture, null as RenderTexture);
        cam.AddCommandBuffer(CameraEvent.AfterEverything, commandBuffer);
        // Graphics.ExecuteCommandBuffer(commandBuffer);
    }
}