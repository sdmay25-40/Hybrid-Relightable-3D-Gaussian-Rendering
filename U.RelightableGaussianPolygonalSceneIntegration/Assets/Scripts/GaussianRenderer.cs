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
    [SerializeField] private Camera cam;
    [SerializeField] private ComputeShader fillBufferSequentially;
    [SerializeField] private ComputeShader generatePrimaryPaths;
    [SerializeField] private ComputeShader getPathIntersections;
    [SerializeField] private ComputeShader samplePathIntersections;
    [SerializeField] private int pathsPerPixel = 1;
    private RenderTexture renderTexture;
    private CommandBuffer commandBuffer;
    private ComputeBuffer paths;
    private ComputeBuffer pathsEnd;
    private ComputeBuffer pathsContinue;

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
        paths = new ComputeBuffer(pathCount ,  Marshal.SizeOf(typeof(PathPayload)));
        pathsEnd = new ComputeBuffer(pathCount + 1, sizeof(uint));
        pathsContinue = new ComputeBuffer(pathCount + 1, sizeof(uint));

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

        // place all paths into pathsEnd
        int kernelIndex = fillBufferSequentially.FindKernel("CSMain");
        commandBuffer.SetComputeBufferParam(fillBufferSequentially, kernelIndex, "pathsEnd", pathsEnd);
        commandBuffer.SetComputeIntParam(fillBufferSequentially, "count", pathsEnd.count);
        float workGroupX = 32.0f;
        int threadGroupX = Mathf.CeilToInt(pathsEnd.count / workGroupX);
        commandBuffer.DispatchCompute(fillBufferSequentially, kernelIndex, threadGroupX, 1, 1);

        // gen primary paths
        kernelIndex = generatePrimaryPaths.FindKernel("CSMain");
        commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "paths", paths);

        // NOTE: Unity handles resource dependency-based synchronization
        commandBuffer.SetComputeBufferParam(generatePrimaryPaths, kernelIndex, "pathsEnd", pathsEnd);

        // TODO: move variables toa constant buffer, they are faster to access and update when compared to individual parameters
        // Only use parameters if infrequently updates or simplicity
        Vector4 cameraWorldPos = new Vector4(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z, 1);
        commandBuffer.SetComputeVectorParam(generatePrimaryPaths, "cameraWorldPos", cameraWorldPos); 
        commandBuffer.SetComputeFloatParam(generatePrimaryPaths, "tanFovHalf", Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f));
        commandBuffer.SetComputeIntParam(generatePrimaryPaths, "screenWidth", Screen.width);
        commandBuffer.SetComputeFloatParam(generatePrimaryPaths, "invScreenHeight", 1.0f / Screen.height);
        // uint pathsPerPixel

        // get path intersects

        // sample path intersects

        // commandBuffer.Blit(renderTexture, BuiltinRenderTextureType.CameraTarget);
        cam.AddCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
    }
}
