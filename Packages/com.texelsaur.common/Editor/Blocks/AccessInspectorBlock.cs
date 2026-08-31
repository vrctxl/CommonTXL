using System;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    [Flags]
    public enum AccessBlockOptions
    {
        None = 0,

        ObjectField = 1 << 0,
        Ownership = 1 << 1,
        SyncGate = 1 << 2,
        Validation = 1 << 3,

        Synced = ObjectField | Ownership | SyncGate | Validation,
        SyncedUngated = ObjectField | Ownership | Validation,
        Unsynced = ObjectField | Validation,

        All = ~0
    }

    public class AccessInspectorBlock
    {
        static bool expanded = true;

        readonly AccessBlockOptions options;

        readonly SerializedProperty accessControl;

        readonly SerializedProperty enforceOwnershipTransfer;
        readonly SerializedProperty reclaimOwnership;
        readonly SerializedProperty syncGateEnabled;
        readonly SerializedProperty accessLogging;

        readonly GUIContent labelSection;
        readonly GUIContent labelAccessControl;
        readonly GUIContent labelAccessControlAdd;
        readonly GUIContent labelEnforce;
        readonly GUIContent labelReclaim;
        readonly GUIContent labelGate;
        readonly GUIContent labelAccessLogging;

        public bool Valid { get; private set; }

        public bool HasAccessControl
        {
            get { return Valid && accessControl.objectReferenceValue != null; }
        }

        public AccessInspectorBlock(SerializedObject so, AccessBlockOptions options, string accessControlField = "accessControl", string accessLoggingField = "includeAccessLogging")
        {
            this.options = options;

            accessControl = so.FindProperty(accessControlField);
            Valid = accessControl != null;
            if (!Valid)
                return;

            if ((options & AccessBlockOptions.Ownership) != 0)
            {
                enforceOwnershipTransfer = so.FindProperty("enforceOwnershipTransfer");
                reclaimOwnership = so.FindProperty("reclaimOwnership");
            }

            if ((options & AccessBlockOptions.SyncGate) != 0)
                syncGateEnabled = so.FindProperty("syncGateEnabled");

            if (accessLoggingField != null)
                accessLogging = so.FindProperty(accessLoggingField);

            labelSection = new GUIContent("Security & Access Control");

            labelAccessControl = new GUIContent("Access Control", "Control access to object based on player type or whitelist.");
            labelAccessControlAdd = new GUIContent("+", "Create new Access Control");
            labelEnforce = new GUIContent("Enforce Ownership Transfer", "Reject ownership transfers to players without access.  Cannot prevent all cases of objects being transfered to non-authorized players.");
            labelReclaim = new GUIContent("Reclaim Unauthorized Ownership", "When an unauthorized player becomes owner, an authorized player attempts to take ownership back and republishes known-good state.");
            labelGate = new GUIContent("Validate Synced State", "Revert synced state received from an unauthorized owner to the last known-good snapshot.  Will likely lead to client desync.  Use with caution.");

            labelAccessLogging = new GUIContent("Include Access", "Include access decisions and ownership changes in the debug log.");
        }

        public void ContributeDebugRows(DebugInspectorBlock debug)
        {
            if (debug == null)
                return;

            debug.AddRow(accessLogging, labelAccessLogging, Valid, indent: 1);
        }

        public bool Draw(GUIStyle foldoutStyle)
        {
            if (!Valid)
                return false;

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            expanded = EditorGUILayout.Foldout(expanded, labelSection, true, foldoutStyle);
            if (!expanded)
                return false;

            bool add = DrawObjectField();
            DrawOptions();

            return add;
        }

        public bool DrawObjectField()
        {
            if (!Valid || (options & AccessBlockOptions.ObjectField) == 0)
                return false;

            bool add = TXLGUI.DrawObjectFieldWithAdd(accessControl, labelAccessControl, labelAccessControlAdd);

            return add;
        }

        public void DrawOptions()
        {
            if (!Valid)
                return;

            bool hasAccess = HasAccessControl;

            using (new EditorGUI.DisabledScope(!hasAccess))
            {
                if (enforceOwnershipTransfer != null)
                    EditorGUILayout.PropertyField(enforceOwnershipTransfer, labelEnforce);
                if (reclaimOwnership != null)
                    EditorGUILayout.PropertyField(reclaimOwnership, labelReclaim);

                if (syncGateEnabled != null)
                    EditorGUILayout.PropertyField(syncGateEnabled, labelGate);
            }

            if ((options & AccessBlockOptions.Validation) != 0)
                DrawValidation();
        }

        // Safe to call on its own, e.g. near the top with other integrity checks.
        public void DrawValidation()
        {
            if (!Valid)
                return;

            if (syncGateEnabled != null && syncGateEnabled.boolValue)
                EditorGUILayout.HelpBox(
                    "Synced state validation is enabled.  This will prevent state from being synced by unauthorized users, but will lead to client desync if no authorized users are present.  Use with caution.",
                    MessageType.Warning);
        }
    }
}
