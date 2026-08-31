using System;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    public class DebugInspectorBlock
    {
        static bool expanded = false;

        const int MAX_ROWS = 8;

        readonly SerializedProperty logProvider;
        readonly SerializedProperty eventLogging;

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

        public bool Valid { get; private set; }

        public bool HasProvider
        {
            get { return Valid && logProvider.objectReferenceValue != null; }
        }

        public DebugInspectorBlock(SerializedObject so, string logProviderField = "logProvider", string eventLoggingField = "includeEventLogging")
        {
            logProvider = so.FindProperty(logProviderField);

            Valid = logProvider != null;
            if (!Valid)
                return;

            eventLogging = so.FindProperty(eventLoggingField);

            labelSection = new GUIContent("Logging & Debug");
            labelLogProvider = new GUIContent("Debug Log", "Destination for debug output from this component.");
            labelLogProviderAdd = new GUIContent("+", "Create new Debug Log");
            labelEventLogging = new GUIContent("Include Events", "Log event dispatch traffic from this component.  Verbose.");
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

        public bool Draw(GUIStyle foldoutStyle)
        {
            if (!Valid)
                return false;

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            expanded = EditorGUILayout.Foldout(expanded, labelSection, true, foldoutStyle);
            if (!expanded)
                return false;

            EditorGUILayout.Space();

            bool add = TXLGUI.DrawObjectFieldWithAdd(logProvider, labelLogProvider, labelLogProviderAdd);
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

            return add;
        }
    }
}
