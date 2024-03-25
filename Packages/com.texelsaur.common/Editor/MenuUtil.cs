using System.Collections;
using System.Collections.Generic;
using UdonSharp;
using UnityEditor;
using UnityEngine;

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
            instance.transform.parent = parent;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

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
    }
}
