
using UnityEditor;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(ZoneTrigger))]
    public class ZoneTriggerInspector : Editor
    {
        protected SerializedProperty configureEventsProperty;
        protected SerializedProperty targetBehaviorProperty;
        protected SerializedProperty localPlayerOnlyProperty;
        protected SerializedProperty playerEnterEventProperty;
        protected SerializedProperty playerLeaveEventProperty;
        protected SerializedProperty playerTargetVariableProperty;

        private void OnEnable()
        {
            InitProperties();
        }

        protected virtual void InitProperties()
        {
            configureEventsProperty = serializedObject.FindProperty(nameof(ZoneTrigger.configureEvents));
            targetBehaviorProperty = serializedObject.FindProperty(nameof(ZoneTrigger.targetBehavior));
            localPlayerOnlyProperty = serializedObject.FindProperty(nameof(ZoneTrigger.localPlayerOnly));
            playerEnterEventProperty = serializedObject.FindProperty(nameof(ZoneTrigger.playerEnterEvent));
            playerLeaveEventProperty = serializedObject.FindProperty(nameof(ZoneTrigger.playerLeaveEvent));
            playerTargetVariableProperty = serializedObject.FindProperty(nameof(ZoneTrigger.playerTargetVariable));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            ZoneTriggerFields();

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }

        protected virtual void ZoneTriggerFields()
        {
            EditorGUILayout.PropertyField(configureEventsProperty);
            if (configureEventsProperty.boolValue)
            {
                EditorGUILayout.PropertyField(targetBehaviorProperty);
                EditorGUILayout.PropertyField(playerEnterEventProperty);
                EditorGUILayout.PropertyField(playerLeaveEventProperty);
                EditorGUILayout.PropertyField(playerTargetVariableProperty);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(localPlayerOnlyProperty);
        }
    }
}
