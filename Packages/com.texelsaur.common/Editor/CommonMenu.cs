using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Texel
{
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

        [MenuItem("GameObject/TXL/CommonTXL/Components/Debug Log", false, 101)]
        public static void AddDebugLogToScene()
        {
            _AddToSelectionOrScene("Packages/com.texelsaur.common/Runtime/Prefabs/DebugLog.prefab", "Add Debug Log");
        }

        [MenuItem("GameObject/TXL/CommonTXL/Components/Debug State", false, 102)]
        public static void AddDebugStateToScene()
        {
            _AddToSelectionOrScene("Packages/com.texelsaur.common/Runtime/Prefabs/DebugState.prefab", "Add Debug State");
        }

        public static AccessControl AddAccessControlForComponent(Object target)
        {
            GameObject obj = _AddForComponent("Packages/com.texelsaur.common/Runtime/Prefabs/Access Control.prefab", target, "Add Access Control");
            return obj ? obj.GetComponent<AccessControl>() : null;
        }

        public static DebugLogProvider AddDebugLogForComponent(Object target)
        {
            GameObject obj = _AddForComponent("Packages/com.texelsaur.common/Runtime/Prefabs/DebugLog.prefab", target, "Add Debug Log");
            return obj ? obj.GetComponent<DebugLogProvider>() : null;
        }

        public static DebugState AddDebugStateForComponent(Object target)
        {
            GameObject obj = _AddForComponent("Packages/com.texelsaur.common/Runtime/Prefabs/DebugState.prefab", target, "Add Debug State");
            return obj ? obj.GetComponent<DebugState>() : null;
        }

        static GameObject _AddForComponent(string path, Object target, string undoName)
        {
            Undo.SetCurrentGroupName(undoName);
            int undoGroup = Undo.GetCurrentGroup();

            Transform parent = _ParentFor(target);

            GameObject obj = parent
                ? MenuUtil.AddPrefabToObject(path, parent)
                : MenuUtil.AddPrefabToScene(path);

            Undo.CollapseUndoOperations(undoGroup);

            if (!obj)
                Debug.LogWarning($"Could not load prefab at {path}");

            return obj;
        }

        static void _AddToSelectionOrScene(string path, string undoName)
        {
            Undo.SetCurrentGroupName(undoName);
            int undoGroup = Undo.GetCurrentGroup();

            GameObject obj = Selection.activeTransform
                ? MenuUtil.AddPrefabToObject(path, Selection.activeTransform)
                : MenuUtil.AddPrefabToScene(path);

            if (obj)
                Selection.activeObject = obj;

            Undo.CollapseUndoOperations(undoGroup);
        }

        static Transform _ParentFor(Object target)
        {
            Component com = target as Component;
            if (com)
                return com.transform;

            GameObject go = target as GameObject;
            if (go)
                return go.transform;

            return null;
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
}
