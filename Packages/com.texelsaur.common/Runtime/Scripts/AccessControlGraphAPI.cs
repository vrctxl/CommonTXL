
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AccessControlGraphAPI : UdonSharpBehaviour
    {
        [HideInInspector] public AccessControlUserSource aclUserSourceArg;
        [HideInInspector] public VRCPlayerApi whitelistPlayerArg;
        [HideInInspector] public VRCPlayerApi accessPlayerArg;
        [HideInInspector] public Component componentArg;
        [HideInInspector] public bool enforceArg;

        [HideInInspector] public bool playerWhitelistedReturn;
        [HideInInspector] public bool localWhitelistedReturn;
        [HideInInspector] public bool playerHasAccessReturn;
        [HideInInspector] public bool localHasAccessReturn;

        public AccessControl accessControl;

        void Start()
        {
            if (!accessControl)
                accessControl = GetComponentInParent<AccessControl>();
        }

        public void _AddUserSource()
        {
            accessControl._AddUserSource(aclUserSourceArg);
        }

        public void _Enforce()
        {
            accessControl._Enforce(enforceArg);
        }

        public void _RefreshWhitelistCheck()
        {
            accessControl._RefreshWhitelistCheck();
        }

        public void _PlayerWhitelisted()
        {
            playerWhitelistedReturn = accessControl._PlayerWhitelisted(whitelistPlayerArg);
        }

        public void _LocalWhitelisted()
        {
            localWhitelistedReturn = accessControl._LocalWhitelisted();
        }

        public void _HasAccess(VRCPlayerApi player)
        {
            playerHasAccessReturn = accessControl._HasAccess(accessPlayerArg);
        }

        public void _LocalHasAccess()
        {
            localHasAccessReturn = accessControl._LocalHasAccess();
        }

        public void _Validate()
        {
            accessControl._Validate();
        }
    }
}
