
using UnityEditor;
using UnityEngine;

namespace Texel
{
    public class DebugInspectorBlock
    {
        public enum DebugBlockAddAction
        {
            None,
            LogProvider,
            DebugState,
        }

        static bool expanded = false;

        const int MAX_ROWS = 8;

        readonly SerializedProperty logProvider;
        readonly SerializedProperty eventLogging;
        readonly SerializedProperty debugState;

        readonly SerializedProperty[] rowProperties = new SerializedProperty[MAX_ROWS];
        readonly GUIContent[] rowLabels = new GUIContent[MAX_ROWS];
        readonly bool[] rowNeedsProvider = new bool[MAX_ROWS];
        readonly bool[] rowValid = new bool[MAX_ROWS];
        readonly int[] rowIndent = new int[MAX_ROWS];
        int rowCount = 0;

        readonly GUIContent labelSection;
        readonly GUIContent labelLogProvider;
        readonly GUIContent labelLogProviderAdd;
        readonly GUIContent labelEventLogging;
        readonly GUIContent labelDebugState;
        readonly GUIContent labelDebugStateAdd;

        readonly SerializedObject serializedObject;
        readonly UnityEngine.Object target;

        public bool Valid { get; private set; }

        public bool UsesDebugState { get; private set; }

        public bool HasProvider
        {
            get { return Valid && logProvider.objectReferenceValue != null; }
        }

        public bool HasDebugState
        {
            get { return debugState != null && debugState.objectReferenceValue != null; }
        }

        public DebugInspectorBlock(SerializedObject so, string logProviderField = "logProvider", string eventLoggingField = "includeEventLogging", string debugStateField = "debugState")
        {
            serializedObject = so;
            target = so.targetObject;

            logProvider = so.FindProperty(logProviderField);

            Valid = logProvider != null;
            if (!Valid)
                return;

            eventLogging = so.FindProperty(eventLoggingField);

            UsesDebugState = _TypeUsesDebugState(so.targetObject);

            if (debugStateField != null)
            {
                if (UsesDebugState)
                    debugState = so.FindProperty(debugStateField);
            }

            labelSection = new GUIContent("Logging & Debug");
            labelLogProvider = new GUIContent("Debug Log", "Destination for debug output from this component.");
            labelLogProviderAdd = new GUIContent("+", "Create new Debug Log");
            labelEventLogging = new GUIContent("Include Events", "Log event dispatch traffic from this component.  Verbose.");
            labelDebugState = new GUIContent("Debug State", "Panel this component reports its current state to.  Independent of the debug log.");
            labelDebugStateAdd = new GUIContent("+", "Create new Debug State");
        }

        static bool _TypeUsesDebugState(UnityEngine.Object target)
        {
            DebugEventBase behaviour = target as DebugEventBase;
            return behaviour != null && behaviour.UsesDebugState;
        }

        public void AddRow(SerializedProperty property, GUIContent label, bool valid = true, int indent = 0, bool needsProvider = true)
        {
            if (!Valid || property == null || label == null)
                return;
            if (rowCount >= MAX_ROWS)
                return;

            rowProperties[rowCount] = property;
            rowLabels[rowCount] = label;
            rowNeedsProvider[rowCount] = needsProvider;
            rowValid[rowCount] = valid;
            rowIndent[rowCount] = indent;
            rowCount += 1;
        }

        public void AddRow(SerializedProperty property, string label, string tooltip, bool valid = true, int indent = 0, bool needsProvider = true)
        {
            AddRow(property, new GUIContent(label, tooltip), valid, indent, needsProvider);
        }

        public void Draw(GUIStyle foldoutStyle)
        {
            if (!Valid)
                return;

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            expanded = EditorGUILayout.Foldout(expanded, labelSection, true, foldoutStyle);
            if (!expanded)
                return;

            EditorGUILayout.Space();

            if (debugState != null)
            {
                if (TXLGUI.DrawObjectFieldWithAdd(debugState, labelDebugState, labelDebugStateAdd))
                    _CreateDebugState();

                EditorGUILayout.Space();
            }

            if (TXLGUI.DrawObjectFieldWithAdd(logProvider, labelLogProvider, labelLogProviderAdd))
                _CreateLogProvider();

            bool hasProvider = HasProvider;

            EditorGUI.indentLevel += 1;
            using (new EditorGUI.DisabledScope(!hasProvider))
            {
                if (eventLogging != null)
                    EditorGUILayout.PropertyField(eventLogging, labelEventLogging);
            }
            EditorGUI.indentLevel -= 1;

            for (int i = 0; i < rowCount; i++)
            {
                EditorGUI.indentLevel += rowIndent[i];

                if (rowNeedsProvider[i] || !rowValid[i])
                {
                    using (new EditorGUI.DisabledScope(!hasProvider || !rowValid[i]))
                        EditorGUILayout.PropertyField(rowProperties[i], rowLabels[i]);
                }
                else
                    EditorGUILayout.PropertyField(rowProperties[i], rowLabels[i]);

                EditorGUI.indentLevel -= rowIndent[i];
            }
        }

        void _CreateLogProvider()
        {
            DebugLogProvider created = CommonMenu.AddDebugLogForComponent(target);
            if (!created)
                return;

            serializedObject.Update();
            logProvider.objectReferenceValue = created;
            serializedObject.ApplyModifiedProperties();
        }

        void _CreateDebugState()
        {
            DebugState created = CommonMenu.AddDebugStateForComponent(target);
            if (!created)
                return;

            serializedObject.Update();
            debugState.objectReferenceValue = created;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
