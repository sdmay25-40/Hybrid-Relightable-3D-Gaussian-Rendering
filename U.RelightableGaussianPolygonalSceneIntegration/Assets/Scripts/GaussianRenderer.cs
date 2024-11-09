using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

// when updating, ensure structs in 'Shaders/utils.cginc' are updated to match
// ensure structs satisfy 16-byte alignment
public struct PathPayload
{
    public Vector4 direction;
}

public struct CameraParams
{
    public Vector3 position;
    public float tanFovHalf;
    public int screenWidth;
    public float invScreenHeight;
    public uint pathsPerPixel;
    public uint pathCount;
    public Vector4 quaternion;
}

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
    private ComputeBuffer pathsContinueCounter; // buffer of continued path indices
    private ComputeBuffer pathsContinueTmpCounter; // buffer of temporary continued path indices
    private ComputeBuffer pathsEndCounter; // buffer of ended path indices
    private ComputeBuffer cameraParamsConst;

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

        renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        renderTexture.enableRandomWrite = true;
        if (!renderTexture.Create())
        {
            Debug.LogError("'GaussianRender': Failed to create 'RenderTexture'.");
        }

        int pathCount = Screen.width * Screen.height * pathsPerPixel;
        paths = new ComputeBuffer(pathCount,  Marshal.SizeOf(typeof(PathPayload)));
        pathsContinueCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        pathsContinueTmpCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        pathsEndCounter = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        cameraParamsConst = new ComputeBuffer(1, Marshal.SizeOf(typeof(CameraParams)), ComputeBufferType.Constant);

        commandBuffer = new CommandBuffer();
        commandBuffer.name = "Hybrid Gaussian Raytracer";

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
        if (pathsEndCounter != null)
        {
            pathsEndCounter.Release();
            pathsEndCounter = null;
        }
    }

    /// <summary>
    /// Builds and inserts Hybrid Gaussian Render Pipeline command buffer into `CameraEvent.BeforeImageEffects`.
    /// NOTE: Unity handles resource dependency-based synchronization!
    /// </summary>
    private void BuildCommandBuffer()
    {
        commandBuffer.SetBufferCounterValue(pathsContinueCounter, 0);
        commandBuffer.SetBufferCounterValue(pathsContinueTmpCounter, 0);

        // fill pathsEnd buffer sequentially
        {
            int kernelIndex = fillBufferSequentially.FindKernel("CSMain");
            commandBuffer.SetComputeBufferParam(fillBufferSequentially, kernelIndex, "counterBuffer", pathsEndCounter);
            commandBuffer.SetComputeIntParam(fillBufferSequentially, "count", pathsEndCounter.count);

            float workGroupX = 32.0f;
            int threadGroupX = Mathf.CeilToInt(pathsEndCounter.count / workGroupX);
            commandBuffer.DispatchCompute(fillBufferSequentially, kernelIndex, threadGroupX, 1, 1);
        }

        // generate primary paths
        {
            int kernelIndex = generatePrimaryPaths.FindKernel("CSMain");
            commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "paths", paths);
            commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "pathsEndCounter", pathsEndCounter);
            commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "pathsContinueCounter", pathsContinueCounter);
            CameraParams cameraParams = new CameraParams
            {
                position = new Vector3(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z),
                tanFovHalf = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f),
                screenWidth = Screen.width,
                invScreenHeight = 1.0f / Screen.height,
                pathsPerPixel = (uint)pathsPerPixel,
                pathCount = (uint) (Screen.width * Screen.height * pathsPerPixel),
                quaternion = new Vector4(cam.transform.rotation.x, cam.transform.rotation.y, cam.transform.rotation.z, cam.transform.rotation.w)
            };
            cameraParamsConst.SetData(new CameraParams[] { cameraParams });
            commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "cameraParamsConst", cameraParamsConst);

            float workGroupX = 32.0f;
            int threadGroupX = Mathf.CeilToInt(pathsEndCounter.count / workGroupX);
            commandBuffer.DispatchCompute(generatePrimaryPaths, kernelIndex, threadGroupX, 1, 1);
        }

        for(uint i = 0; i < pathBounceLimit; i++)
        {
            // get path intersections
            {
                // TODO
                // read from pathsContinue
                // write to pathsContinueTmp
            }

            // sample path intersections
            {
                // TODO
                // read from pathsContinueTmp
                // write to pathsContinue
                // write to renderTexture
            }
        }

        // commandBuffer.Blit(renderTexture, BuiltinRenderTextureType.CameraTarget);
        cam.AddCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
    }
}
