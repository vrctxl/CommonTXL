
using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_2019
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Texel
{
    [InitializeOnLoad]
    public class UdonTools
    {
        [MenuItem("Tools/TXL/Udon Tools/Remove Orphan Behaviours", false, 200)]
        public static void RemoveOrphanBehaviours()
        {
            Transform t = Selection.activeTransform;
            if (t == null)
                return;

            UdonBehaviour[] udons = t.GetComponents<UdonBehaviour>();
            UdonSharpBehaviour[] usharps = t.GetComponents<UdonSharpBehaviour>();

            HashSet<UdonBehaviour> backingUdons = new HashSet<UdonBehaviour>();

            foreach (var usharp in usharps)
            {
                UdonBehaviour udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(usharp);
                if (udon)
                    backingUdons.Add(udon);
            }

            foreach (var udon in udons)
            {
                if (!backingUdons.Contains(udon))
                {
                    if (PrefabUtility.IsAddedComponentOverride(udon))
                    {
                        Debug.Log($"Removing orphan udon behaviour (prefab added component): {udon}");
                        PrefabUtility.RevertAddedComponent(udon, InteractionMode.AutomatedAction);

                        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(t);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(root);
                        EditorSceneManager.MarkSceneDirty(PrefabStageUtility.GetCurrentPrefabStage().scene);
                        return;
                    }

                    Debug.Log($"Removing orphan udon behaviour: {udon}");
                    GameObject.DestroyImmediate(udon);
                }
            }
        }
    }
}
