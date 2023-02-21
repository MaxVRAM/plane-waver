using UnityEngine;
using UnityEditor;

namespace MaxVRAM.Assets
{
    public struct MaxAssets
    {
        public static string[] GetFilesFromPath(string filter, string path)
        {
            string[] assetGUIDs = AssetDatabase.FindAssets(filter, new[] { path });
            string[] assetFiles = new string[assetGUIDs.Length];

            for (int i = 0; i < assetGUIDs.Length; i++)
                assetFiles[i] = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);

            Debug.Log($"Returning ({assetFiles.Length}) asset file paths from '{path}'.");
            return assetFiles;
        }
    }
}
