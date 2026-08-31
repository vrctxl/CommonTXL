using UdonSharpEditor;
using UnityEditor;

namespace Texel
{
    [CustomEditor(typeof(DebugEventBase), true)]
    internal class EventBaseInspector : Editor
    {
        DebugInspectorBlock debugBlock;
        string[] excludedPropCache = new string[0];

        static readonly string[] excludedEvent = {
            "m_Script", "logProvider", "includeEventLogging", "debugState"
        };

        void OnEnable()
        {
            OnEnableSetup();
            excludedPropCache = ExcludedProperties;
        }

        protected virtual void OnEnableSetup()
        {
            debugBlock = new DebugInspectorBlock(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            DrawLeadingBlocks();
            DrawPropertiesExcluding(serializedObject, excludedPropCache);
            DrawTrailingBlocks();

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawLeadingBlocks() { }

        protected virtual void DrawTrailingBlocks()
        {
            EditorGUILayout.Space();
            debugBlock.Draw(TXLGUI.Styles.BoldFoldout);
        }

        protected virtual string[] ExcludedProperties
        {
            get {  return excludedEvent; }
        }
    }
}
