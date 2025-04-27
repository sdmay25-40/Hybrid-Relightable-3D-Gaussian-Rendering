using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GaussianScrpt : MonoBehaviour
{
    [Tooltip("Drop a Gaussian *.ply file here")]
    [SerializeField]
    private DefaultAsset gaussianFile;

    [Header("References")]
    [Tooltip("Scale applied to gaussians individually.")]
    [SerializeField]
    private float gaussianSpaceScale = 1.0f;


    public float GetGScale(){
        return gaussianSpaceScale;
    }

    private string filePath;
    public string FilePath
    {
        get
        {
            if (gaussianFile == null)
            {
                Debug.LogError("Gaussian *.ply file is NOT set! Please assign a vaild *.ply file in the Inspector");
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