using System.Collections;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    [CustomEditor(typeof(TrackedZoneTrigger))]
    public class TrackedZoneTriggerInspector : ZoneTriggerInspector
    {
        protected SerializedProperty triggerModeProperty;
        protected SerializedProperty positionRadiusProperty;
        protected SerializedProperty monitorTriggerIntervalProperty;
        protected SerializedProperty checkForRemoveProperty;
        protected SerializedProperty removeCheckIntervalProperty;
        protected SerializedProperty checkForAddProperty;
        protected SerializedProperty addCheckIntervalProperty;

        protected override void InitProperties()
        {
            base.InitProperties();

            triggerModeProperty = serializedObject.FindProperty(nameof(TrackedZoneTrigger.triggerMode));
            positionRadiusProperty = serializedObject.FindProperty(nameof(TrackedZoneTrigger.positionRadius));
            monitorTriggerIntervalProperty = serializedObject.FindProperty(nameof(TrackedZoneTrigger.monitorTriggerInterval));
            checkForRemoveProperty = serializedObject.FindProperty(nameof(TrackedZoneTrigger.checkForRemove));
            removeCheckIntervalProperty = serializedObject.FindProperty(nameof(TrackedZoneTrigger.removeCheckInterval));
            checkForAddProperty = serializedObject.FindProperty(nameof(TrackedZoneTrigger.checkForAdd));
            addCheckIntervalProperty = serializedObject.FindProperty(nameof(TrackedZoneTrigger.addCheckInterval));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            ZoneTriggerFields();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(triggerModeProperty, new GUIContent("Trigger Mode", "Capsule Trigger: Relies only on the trigger events of the player or remote player capsules.\nPosition: Uses the player position to determine if players are actually inside the zone."));

            bool isPositionMode = triggerModeProperty.enumValueIndex == (int)ZoneTriggerMode.Position;
            if (isPositionMode)
            {
                EditorGUILayout.PropertyField(positionRadiusProperty, new GUIContent("Position Radius", "The radius of the 'trigger sphere' around the player's position.  If any part of the sphere is within the zone, the player is considered to be contained."));
                EditorGUILayout.PropertyField(monitorTriggerIntervalProperty, new GUIContent("Monitor Trigger Interval", "The interval (in seconds) a player's position will be checked following a normal capsule trigger event.\n\nIn position trigger mode, trigger events may fire before a player has moved far enough into or out of the zone to count.  These players are monitored at a fixed interval until their position is consistent with the trigger events."));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Position Polling", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(checkForRemoveProperty, new GUIContent("Check For Removed Players", "Enables periodically checking tracked players to see if they are no longer inside the zone.\n\nPlayers can leave zones without triggering leave events if they are in stations (like avatar chairs)"));
                if (checkForRemoveProperty.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(removeCheckIntervalProperty, new GUIContent("Removal Check Interval", "The interval (in seconds) a tracked player will be checked.\n\nChecks are spread out across all tracked players to reduce impact on a single frame."));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(checkForAddProperty, new GUIContent("Check For Added Players", "Enables periodically checking all players to see if they are inside the zone.\n\nPlayers can enter zones without triggering enter events if they are in stations (like avatar chairs)"));
                if (checkForAddProperty.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(addCheckIntervalProperty, new GUIContent("Added Check Interval", "The interval (in seconds) a tracked player will be checked.\n\nChecks are spread out across all tracked players to reduce impact on a single frame."));
                    EditorGUI.indentLevel--;
                }
            }

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
}
