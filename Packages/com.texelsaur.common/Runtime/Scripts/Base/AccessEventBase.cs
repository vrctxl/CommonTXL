
using Texel;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public abstract class AccessEventBase : EventBase
    {
        [SerializeField] internal AccessControl accessControl;
        [SerializeField] internal bool enforceOwnershipTransfer = true;

        protected DebugLog accessDebugLog;
        protected bool accessDebugLowLevel = false;

        protected string componentName = "Component";

        private bool isOwner = false;

        protected override void _Init()
        {
            base._Init();

            if (Networking.IsOwner(gameObject))
                isOwner = true;
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            if (!accessControl || !enforceOwnershipTransfer)
                return true;

            bool requesterCheck = accessControl._HasAccess(requestingPlayer) || Networking.IsOwner(requestingPlayer, gameObject);
            bool requesteeCheck = accessControl._HasAccess(requestedOwner);

            _DebugLowLevel($"Ownership check: requester={requesterCheck}, requestee={requesteeCheck}");

            return requesterCheck && requesteeCheck;
        }

        protected string OwnerName
        {
            get
            {
                VRCPlayerApi player = Networking.GetOwner(gameObject);
                if (Utilities.IsValid(player))
                    return player.displayName;
                return "[INVALID]";
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (isOwner)
            {
                // SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(RequestOwnerSync));
                isOwner = false;
            }

            string name = Utilities.IsValid(player) ? player.displayName : "";
            _DebugLowLevel($"Ownserhip transferred to {name}");

            if (Networking.IsOwner(gameObject))
                isOwner = true;
        }

        public void RequestOwnerSync()
        {
            _DebugLog("RequestOwnerSync");
            if (Networking.IsOwner(gameObject))
                RequestSerialization();
        }

        protected virtual bool _AccessCheck()
        {
            return !accessControl || accessControl._LocalHasAccess();
        }

        protected virtual bool _AccessCheck(VRCPlayerApi player)
        {
            return !accessControl || accessControl._HasAccess(player);
        }

        protected bool _AccessOwnershipCheck()
        {
            if (!_AccessCheck())
                return false;

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                _AccessOwnershipChange();
            }

            return true;
        }

        protected virtual void _AccessOwnershipChange() { }

        protected void _DebugLog(string message)
        {
            if (accessDebugLog)
                accessDebugLog._Write(componentName, message);
        }

        protected void _DebugLowLevel(string message)
        {
            if (accessDebugLog && accessDebugLowLevel)
                accessDebugLog._Write(componentName, message);
        }
    }
}
