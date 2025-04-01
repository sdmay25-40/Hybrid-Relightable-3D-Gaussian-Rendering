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
    private string FilePath;
    public string filePath
    {
        get
        {
            if (gaussianFile == null)
            {
                Debug.LogError("Gaussian *.ply file is NOT set! Please assign a vaild *.ply file in the Inspector");
            }
            return FilePath;
        }
    }

    private void OnValidate()
    {
        #if UNITY_EDITOR
        if (gaussianFile != null)
        {
            FilePath = AssetDatabase.GetAssetPath(gaussianFile);
            if (Path.GetExtension(FilePath).ToLower() != ".ply")
            {
                Debug.LogError("Selected file '" + FilePath + "' is not a *.ply file!");
                gaussianFile = null;
                FilePath = "";
            }
        }
        else
        {
            FilePath = "";
        }
        #endif
    }
}