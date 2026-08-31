
using System.Runtime.CompilerServices;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common;

[assembly: InternalsVisibleTo("com.texelsaur.common.Editor")]
[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

namespace Texel
{
    public abstract class AccessEventBase : DebugEventBase
    {
        [Header("Access Control")]
        [SerializeField] protected internal AccessControl accessControl;
        [SerializeField] internal bool enforceOwnershipTransfer = true;
        [SerializeField] internal bool reclaimOwnership = true;
        [SerializeField] internal bool syncGateEnabled = false;

        [SerializeField] protected internal bool includeAccessLogging;

        protected bool hasAccessControl = false;

        private bool ae_reclaimQueued = false;
        private bool ae_useAccessDebug = false;
        private bool ae_bypassAccessCheck = false;
        private bool ae_bypassOwnershipCheck = false;
        private bool ae_shadowValid = false;
        private int ae_logAccessChannel = -1;

        private VRCPlayerApi[] ae_playerScratch;

        public void _SetAccessControl(AccessControl accessControl)
        {
            this.accessControl = accessControl;

            _RefreshDebugFlags();
        }

        public string OwnerName
        {
            get
            {
                VRCPlayerApi player = Networking.GetOwner(gameObject);
                if (Utilities.IsValid(player))
                    return player.displayName;

                return "[INVALID]";
            }
        }

        public bool AccessLogging
        {
            get { return includeAccessLogging; }
            set
            {
                includeAccessLogging = value;
                _RefreshDebugFlags();
            }
        }

        protected override void _RefreshDebugFlags()
        {
            base._RefreshDebugFlags();

            hasAccessControl = accessControl;
            ae_bypassAccessCheck = !hasAccessControl;
            ae_bypassOwnershipCheck = ae_bypassAccessCheck || !enforceOwnershipTransfer;

            if (logProvider)
                ae_logAccessChannel = logProvider._RegisterChannel(componentNamespace, componentName, "access");
            else
                ae_logAccessChannel = -1;

            ae_useAccessDebug = logProvider && includeAccessLogging;
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            if (ae_bypassOwnershipCheck)
                return true;

            bool requesteeCheck = _AccessCheck(requestedOwner);

            if (ae_useAccessDebug) logProvider._WriteInfo(ae_logAccessChannel, $"Ownership check: requestee={requesteeCheck}");

            return requesteeCheck;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (ae_useAccessDebug)
                logProvider._WriteInfo(ae_logAccessChannel, $"Ownership transferred to {_AE_PlayerNameId(player)}");

            _OnOwnerChanged();

            if (reclaimOwnership && !ae_bypassAccessCheck && !_OwnerHasAccess())
                _AE_ScheduleReclaim();
        }

        private string _AE_PlayerNameId(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return "--";

            return player.displayName + ":" + player.playerId;
        }

        protected virtual void _OnOwnerChanged() { }

        private void _AE_ScheduleReclaim()
        {
            if (ae_reclaimQueued)
                return;
            if (!_AccessCheck())
                return;
            if (!_AE_IsLowestAuthorizedPlayer())
                return;

            ae_reclaimQueued = true;
            SendCustomEventDelayedSeconds(nameof(_InternalReclaimOwnership), 0.5f);
        }

        [NetworkCallable]
        public void RequestOwnerSync()
        {
            if (ae_useAccessDebug) logProvider._WriteInfo(ae_logAccessChannel, "RequestOwnerSync");

            if (Networking.IsOwner(gameObject))
                RequestSerialization();
        }

        public void _InternalReclaimOwnership()
        {
            ae_reclaimQueued = false;

            if (!reclaimOwnership || ae_bypassAccessCheck)
                return;
            if (Networking.IsOwner(gameObject))
                return;
            if (_OwnerHasAccess())
                return;
            if (!_AccessCheck() || !_AE_IsLowestAuthorizedPlayer())
                return;

            if (ae_useAccessDebug)
            {
                VRCPlayerApi player = Networking.GetOwner(gameObject);
                logProvider._WriteInfo(ae_logAccessChannel, $"Reclaiming ownership from {_AE_PlayerNameId(player)}");
            }

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        private bool _AE_IsLowestAuthorizedPlayer()
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!Utilities.IsValid(local))
                return false;

            int count = VRCPlayerApi.GetPlayerCount();
            if (ae_playerScratch == null || ae_playerScratch.Length < count)
                ae_playerScratch = new VRCPlayerApi[count];

            VRCPlayerApi.GetPlayers(ae_playerScratch);

            int localId = local.playerId;
            for (int i = 0; i < count; i++)
            {
                VRCPlayerApi p = ae_playerScratch[i];
                if (!Utilities.IsValid(p) || p.playerId >= localId)
                    continue;
                if (accessControl._HasAccess(p))
                    return false;
            }

            return true;
        }

        protected bool _OwnerHasAccess()
        {
            return _AccessCheck(Networking.GetOwner(gameObject));
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            base.OnDeserialization(result);

            if (!syncGateEnabled || ae_bypassAccessCheck || _OwnerHasAccess())
            {
                _CaptureSyncShadow();
                ae_shadowValid = true;
                _OnSyncApplied(result);

                return;
            }

            _RestoreSyncShadow();

            if (ae_shadowValid)
                _OnSyncReverted(result);
            else
                _OnSyncBlocked(result);
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (!result.success)
            {
                if (ae_useAccessDebug) logProvider._WriteWarning(ae_logAccessChannel, "Failed to sync");
                return;
            }
        }

        protected virtual void _CaptureSyncShadow() { }

        protected virtual void _RestoreSyncShadow() { }

        protected virtual void _OnSyncApplied(DeserializationResult result) { }

        protected virtual void _OnSyncReverted(DeserializationResult result) { }

        protected virtual void _OnSyncBlocked(DeserializationResult result) { }

        protected virtual void _CaptureRequestSerialization()
        {
            _CaptureSyncShadow();
            RequestSerialization();
        }


        protected virtual bool _AccessCheck()
        {
            if (ae_bypassAccessCheck)
                return true;

            return accessControl._LocalHasAccess();
        }

        protected virtual bool _AccessCheck(VRCPlayerApi player)
        {
            if (ae_bypassAccessCheck)
                return true;

            return accessControl._HasAccess(player);
        }

        protected bool _AccessOwnershipCheck()
        {
            if (_AccessCheck())
            {
                if (!Networking.IsOwner(gameObject))
                {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                    _AccessOwnershipChange();
                }

                return true;
            }

            return false;
        }

        protected virtual void _AccessOwnershipChange() { }

        public AccessControl AccessControl
        {
            get { return accessControl; }
        }
    }
}
