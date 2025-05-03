using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GaussianScrpt : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign a 3D Gaussian *.ply file here.")]
    [SerializeField]
    private Object gaussianFile;
    [Tooltip("Scale applied to gaussians individually.")]
    public float gaussianSpaceScale = 1.0f;

    private string filePath;
    public string FilePath
    {
        get
        {
            if (gaussianFile == null)
            {
                Debug.LogError("3D Gaussian *.ply file is NOT set! Please assign a vaild *.ply file in the Inspector.");
            }
            return filePath;
        }
    }

    private void OnValidate()
    {
        #if UNITY_EDITOR
        if (gaussianFile != null)
        {
            filePath = AssetDatabase.GetAssetPath(gaussianFile);
            if (Path.GetExtension(filePath).ToLower() != ".ply")
            {
                Debug.LogError("Selected file '" + filePath + "' is not a *.ply file!");
                gaussianFile = null;
                filePath = "";
            }
        }
        else
        {
            filePath = "";
        }
        #endif
    }
}