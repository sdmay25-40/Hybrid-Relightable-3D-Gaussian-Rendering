using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

// when updating, ensure structs in 'Shaders/utils.cginc' are updated to match
public struct PathPayload
{
    public Vector3 direction;
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
    private ComputeBuffer pathsContinue; // buffer of continued path indices
    private ComputeBuffer pathsContinueTmp; // buffer of temporary continued path indices
    private ComputeBuffer pathsEnd; // buffer of ended path indices

    private void Awake()
    {
        // disable camera's rendering
        if (cam != null)
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
        pathsContinue = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        pathsContinueTmp = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);
        pathsEnd = new ComputeBuffer(pathCount, sizeof(uint), ComputeBufferType.Counter);

        commandBuffer = new CommandBuffer();
        commandBuffer.name = "Hybrid Gaussian Raytracer";

        BuildCommandBuffer();
    }

    private void Update()
    {
        // if camera moves
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
        if (pathsContinue != null)
        {
            pathsContinue.Release();
            pathsContinue = null;
        }
        if (pathsContinueTmp != null)
        {
            pathsContinueTmp.Release();
            pathsContinueTmp = null;
        }
        if (pathsEnd != null)
        {
            pathsEnd.Release();
            pathsEnd = null;
        }
    }

    /// <summary>
    /// Builds and inserts Hybrid Gaussian Render Pipeline command buffer into `CameraEvent.BeforeImageEffects`
    /// </summary>
    private void BuildCommandBuffer()
    {
        commandBuffer.SetBufferCounterValue(pathsContinue, 0);
        commandBuffer.SetBufferCounterValue(pathsContinueTmp, 0);
        DispatchComputeFillBufferSequentially();

        // NOTE: Unity handles resource dependency-based synchronization
        DispatchComputeGeneratePrimaryPaths();

        for(uint i = 0; i < pathBounceLimit; i++)
        {
            DispatchComputeGetPathIntersections();
            DispatchComputeSamplePathIntersections();
        }

        // commandBuffer.Blit(renderTexture, BuiltinRenderTextureType.CameraTarget);
        cam.AddCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
    }

    private void DispatchComputeFillBufferSequentially()
    {
        int kernelIndex = fillBufferSequentially.FindKernel("CSMain");
        commandBuffer.SetComputeBufferParam(fillBufferSequentially, kernelIndex, "pathsEnd", pathsEnd);
        commandBuffer.SetComputeIntParam(fillBufferSequentially, "count", pathsEnd.count);
        float workGroupX = 32.0f;
        int threadGroupX = Mathf.CeilToInt(pathsEnd.count / workGroupX);
        commandBuffer.DispatchCompute(fillBufferSequentially, kernelIndex, threadGroupX, 1, 1);
    }

    private void DispatchComputeGeneratePrimaryPaths()
    {
        int kernelIndex = generatePrimaryPaths.FindKernel("CSMain");
        commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "paths", paths);
        commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "pathsEnd", pathsEnd);
        commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "pathsContinue", pathsContinue);

        // TODO: move variables toa constant buffer, they are faster to access and update when compared to individual parameters
        Vector4 cameraWorldPos = new Vector4(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z, 1);
        commandBuffer.SetComputeVectorParam(generatePrimaryPaths, "cameraWorldPos", cameraWorldPos); 
        commandBuffer.SetComputeFloatParam(generatePrimaryPaths, "tanFovHalf", Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f));
        commandBuffer.SetComputeIntParam(generatePrimaryPaths, "screenWidth", Screen.width);
        commandBuffer.SetComputeFloatParam(generatePrimaryPaths, "invScreenHeight", 1.0f / Screen.height);
        // uint pathsPerPixel
        // uint pathCount
        // float4 cameraQuat
        // write to pathsContinue
    }

    private void DispatchComputeGetPathIntersections()
    {
        // TODO
        // read from pathsContinue
        // write to pathsContinueTmp
    }

    private void DispatchComputeSamplePathIntersections()
    {
        // TODO
        // read from pathsContinueTmp
        // write to pathsContinue
        // write to renderTexture
    }
}
