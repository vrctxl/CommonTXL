using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    [InitializeOnLoad]
    public class CommonDefineManager
    {
        static CommonDefineManager()
        {
            AddDefinesIfMissing(EditorUserBuildSettings.selectedBuildTargetGroup, new string[] { "COMMON_TXL" });
        }

        private static void AddDefinesIfMissing(BuildTargetGroup buildGroup, params string[] newDefines)
        {
            bool definesChanged = false;
            string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup).Split(';');

            HashSet<string> defineSet = new HashSet<string>(defines);
            foreach (string newDefine in newDefines)
                definesChanged |= defineSet.Add(newDefine);

            if (definesChanged)
            {
                string finalDefineString = string.Join(";", defineSet.ToArray());
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, finalDefineString);
                Debug.Log("Added COMMON_TXL for selected build target");
            }
        }
    }
}
