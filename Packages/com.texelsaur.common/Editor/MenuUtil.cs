using System.Collections;
using System.Collections.Generic;
using UdonSharp;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Texel
{
    public class MenuUtil
    {
        public static Transform GetComponentRoot<M, T>() 
            where T : UdonSharpBehaviour 
            where M : UdonSharpBehaviour
        {
            M parent = GetObjectOrParent<M>();
            if (!parent)
                return null;

            T target = parent.GetComponentInChildren<T>();
            if (!target)
                return null;

            return target.transform;
        }

        public static GameObject AddPrefabToScene(string path)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
                Undo.RecordObject(instance, "Rename Instance");
                instance.name = GetUniqueName(instance.name, instance.scene);

                EditorUtility.SetDirty(instance);
                EditorGUIUtility.PingObject(instance);
                return instance;
            }

            return null;
        }

        public static GameObject AddPrefabToActiveOrScene(string path)
        {
            GameObject active = GetActiveObject();
            if (active)
                return AddPrefabToObject(path, active.transform);
            else
                return AddPrefabToScene(path);
        }

        public static GameObject AddPrefabToObject(string path, Transform parent)
        {
            if (!parent)
                return null;

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset)
                return null;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
            Undo.RecordObject(parent, "Parent Prefab");
            Undo.RecordObject(instance.transform, "Update Transform");
            Undo.RecordObject(instance, "Rename Instance");

            instance.transform.parent = parent;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            instance.name = GetUniqueName(instance.name, instance.transform.parent);

            EditorUtility.SetDirty(instance);
            EditorUtility.SetDirty(instance.transform);
            EditorUtility.SetDirty(parent);
            EditorGUIUtility.PingObject(instance);
            return instance;
        }

        public static T GetObjectOrParent<T>() where T : UdonSharpBehaviour
        {
            if (!Selection.activeTransform)
                return null;
            T com = Selection.activeTransform.GetComponent<T>();
            if (!com)
                com = Selection.activeTransform.GetComponentInParent<T>();

            return com;
        }

        public static GameObject GetActiveObject()
        {
            if (!Selection.activeTransform)
                return null;

            return Selection.activeTransform.gameObject;
        }

        public static string GetUniqueName(string baseName, Transform parent)
        {
            if (parent == null)
                return baseName;

            HashSet<string> siblingNames = new HashSet<string>();
            foreach (Transform child in parent)
                siblingNames.Add(child.name);

            return GetUniqueName(baseName, siblingNames);
        }

        public static string GetUniqueName(string baseName, Scene scene)
        {
            if (scene == null)
                return baseName;

            HashSet<string> siblingNames = new HashSet<string>();
            foreach (GameObject obj in scene.GetRootGameObjects())
                siblingNames.Add(obj.name);

            return GetUniqueName(baseName, siblingNames);
        }

        public static string GetUniqueName(string baseName, HashSet<string> siblingNames)
        {
            if (!siblingNames.Contains(baseName))
                return baseName;

            int suffix = 1;
            string candidateName;
            do
            {
                candidateName = $"{baseName} ({suffix})";
                suffix++;
            } while (siblingNames.Contains(candidateName));

            return candidateName;
        }
    }
}
