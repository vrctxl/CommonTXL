
using System.Linq;
using UnityEditor;

namespace Texel
{
    [CustomEditor(typeof(AccessEventBase), true)]
    internal class AccessEventBaseInspector : EventBaseInspector
    {
        AccessInspectorBlock accessBlock;
        string[] excludedPropCache = new string[0];

        static readonly string[] excludedAccess = {
            "accessControl", "includeAccessLogging", "enforceOwnershipTransfer", "reclaimOwnership", "syncGateEnabled"
        };

        protected override void OnEnableSetup()
        {
            base.OnEnableSetup();

            accessBlock = new AccessInspectorBlock(serializedObject, AccessBlockOptions.SyncedUngated);
        }

        protected override void DrawTrailingBlocks()
        {
            EditorGUILayout.Space();
            accessBlock.Draw(TXLGUI.Styles.BoldFoldout);

            base.DrawTrailingBlocks();
        }

        protected override string[] ExcludedProperties
        {
            get { return base.ExcludedProperties.Concat(excludedAccess).ToArray(); }
        }
    }
}
