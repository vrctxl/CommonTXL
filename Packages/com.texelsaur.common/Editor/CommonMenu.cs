using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CommonMenu
{
    [MenuItem("Tools/TXL/Reimport Scripts", false, 1000)]
    public static void ReimportAllScripts()
    {
        string[] paths =
        {
            "Packages/com.texelsaur.common/Runtime/Scripts",
            "Packages/com.texelsaur.access/Runtime/Scripts",
            "Packages/com.texelsaur.misc/Runtime/Scripts",
            "Packages/com.texelsaur.playeraudio/Runtime/Scripts",
            "Packages/com.texelsaur.video/Runtime/Scripts",
            "Packages/com.texelsaur.portal/Runtime/Scripts",
        };

        foreach (string path in paths)
        {
            if (AssetDatabase.IsValidFolder(path))
                ReimportScripts(path);
        }
    }

    [MenuItem("Tools/TXL/CommonTXL/Reimport Scripts", false, 1100)]
    public static void ReimportScripts()
    {
        ReimportScripts("Packages/com.texelsaur.common/Runtime/Scripts");
    }

    public static void ReimportScripts(string rootPath)
    {
        if (!AssetDatabase.IsValidFolder(rootPath))
            return;

        string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { rootPath });
        if (guids.Length == 0)
            return;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Reimporting C# Scripts", path, (float)i / guids.Length);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"Reimported {guids.Length} scripts under '{rootPath}'.");
    }
}
