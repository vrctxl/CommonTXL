
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-1)]
    public class AccessControl : EventBase
    {
        [Header("Optional Components")]
        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;
        public DebugState debugState;

        [Header("Access Options")]
        public bool allowInstanceOwner = true;
        public bool allowMaster = true;
        public bool restrictMasterIfOwnerPresent = false;
        public bool allowFirstJoin = false;
        public bool restrictFirstJoinIfOwnerPresent = false;
        public bool allowWhitelist = false;
        public bool allowAnyone = false;

        [Header("Default Options")]
        [Tooltip("Whether ACL is enforced.  When not enforced, access is always given.")]
        public bool enforce = true;
        [Tooltip("Whether to respond to player join and leave events.  If disabled, master and owner rules may not work correctly.\n\nEvents are automatically ignored if rule configuration does not need them.")]
        public bool handleJoinLeaveEvents = true;
        [Tooltip("Write out debug info to VRChat log")]
        public bool debugLogging = false;

        [Header("Access Whitelist")]
        [Tooltip("A list of admin users who have access when allow whitelist is enabled")]
        public string[] userWhitelist;
        [Tooltip("A list of user sources to check for whitelisted players")]
        public AccessControlUserSource[] whitelistSources;

        [Header("Access Handlers")]
        [Tooltip("A list of access handlers that will be checked as part of any player access lookup first")]
        public AccessControlHandler[] accessHandlers;

        [UdonSynced]
        private string syncFirstJoin = "";
        [UdonSynced]
        private int syncFirstJoinId = -1;

        bool _localPlayerWhitelisted = false;
        bool _localPlayerMaster = false;
        bool _localPlayerInstanceOwner = false;
        bool _localPlayerFirstJoin = false;
        bool _localCalculatedAccess = false;

        bool _worldHasOwner = false;

        VRCPlayerApi foundMaster = null;
        VRCPlayerApi foundInstanceOwner = null;
        int foundMasterCount = 0;
        int foundInstanceOwnerCount = 0;

        public const int EVENT_VALIDATE = 0;
        public const int EVENT_ENFORCE_UPDATE = 1;
        public const int EVENT_USER_SOURCE_VALIDATE = 2;
        public const int EVENT_COUNT = 3;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount => EVENT_COUNT;

        protected override void _Init()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (Utilities.IsValid(player))
            {
                if (_PlayerWhitelisted(player))
                    _localPlayerWhitelisted = true;

                _localPlayerMaster = player.isMaster;
                _localPlayerInstanceOwner = player.isInstanceOwner;

                if (string.IsNullOrEmpty(syncFirstJoin))
                {
                    if (Networking.IsOwner(gameObject))
                    {
                        _localPlayerFirstJoin = true;

                        syncFirstJoin = player.displayName;
                        syncFirstJoinId = player.playerId;
                        RequestSerialization();
                    }
                }
                else if (syncFirstJoin == player.displayName)
                    _localPlayerFirstJoin = true;
                
            }

            if (Utilities.IsValid(debugState))
            {
                debugState._Register(DebugState.EVENT_UPDATE, this, nameof(_UpdateDebugState));
                debugState._SetContext(this, nameof(_UpdateDebugState), "AccessControl");
            }

            DebugLog("Setting up access");
            if (allowInstanceOwner)
                DebugLog($"Instance Owner: {_localPlayerInstanceOwner}");
            if (allowMaster)
                DebugLog($"Instance Master: {_localPlayerMaster}");
            if (allowFirstJoin)
                DebugLog($"First Joined: {_localPlayerFirstJoin}");
            if (allowWhitelist)
                DebugLog($"Whitelist: {_localPlayerWhitelisted}");
            if (allowAnyone)
                DebugLog($"Anyone: True");

            _SearchInstanceOwner();
            _CalculateLocalAccess();

            if (Utilities.IsValid(whitelistSources))
            {
                foreach (AccessControlUserSource source in whitelistSources)
                {
                    if (Utilities.IsValid(source))
                        source._Register(AccessControlUserSource.EVENT_REVALIDATE, this, nameof(_RefreshWhitelistCheck));
                }
            }
            
            if (Utilities.IsValid(accessHandlers))
            {
                foreach (AccessControlHandler source in accessHandlers)
                {
                    if (Utilities.IsValid(source))
                        source._Register(AccessControlHandler.EVENT_REVALIDATE, this, nameof(_RefreshAccessHandlerCheck));
                }
            }
        }

        protected override void _PostInit()
        {
            _UpdateHandlers(EVENT_VALIDATE);
        }

        [Obsolete("Use Networking.InstanceOwner")]
        public VRCPlayerApi InstanceOwner
        {
            get { return foundInstanceOwner; }
        }

        [Obsolete("Use Networking.Master")]
        public VRCPlayerApi Master
        {
            get { return foundMaster; }
        }

        public VRCPlayerApi FirstJoin
        {
            get
            {
                if (syncFirstJoinId >= 0)
                    return VRCPlayerApi.GetPlayerById(syncFirstJoinId);
                return null;
            }
        }

        public string FirstJoinName
        {
            get { return syncFirstJoin; }
        }

        public void _AddUserSource(AccessControlUserSource source)
        {
            if (!source)
                return;

            foreach (var existing in whitelistSources)
            {
                if (existing == source)
                    return;
            }

            whitelistSources = (AccessControlUserSource[])UtilityTxl.ArrayAddElement(whitelistSources, source, source.GetType());
            source._Register(AccessControlUserSource.EVENT_REVALIDATE, this, nameof(_RefreshWhitelistCheck));

            _Validate();
        }

        public void _RemoveUserSource(AccessControlUserSource source)
        {
            if (!source)
                return;

            int index = -1;
            for (int i = 0; i < whitelistSources.Length; i++)
            {
                if (whitelistSources[i] == source)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
                return;

            whitelistSources = (AccessControlUserSource[])UtilityTxl.ArrayRemoveElement(whitelistSources, index, source.GetType());
            source._Unregister(AccessControlUserSource.EVENT_REVALIDATE, this, nameof(_RefreshWhitelistCheck));

            _Validate();
        }

        public void _AddAccessHandler(AccessControlHandler accessHandler)
        {
            if (!accessHandler)
                return;

            foreach (var existing in accessHandlers)
            {
                if (existing == accessHandler)
                    return;
            }

            accessHandlers = (AccessControlHandler[])UtilityTxl.ArrayAddElement(accessHandlers, accessHandler, accessHandler.GetType());
            accessHandler._Register(AccessControlHandler.EVENT_REVALIDATE, this, nameof(_RefreshAccessHandlerCheck));

            _Validate();
        }

        bool _CalculateLocalAccess()
        {
            bool prevAccess = _localCalculatedAccess;
            _localCalculatedAccess = false;

            if (allowInstanceOwner && _localPlayerInstanceOwner)
                _localCalculatedAccess = true;
            if (allowWhitelist && _localPlayerWhitelisted)
                _localCalculatedAccess = true;
            if (allowAnyone)
                _localCalculatedAccess = true;

            return prevAccess != _localCalculatedAccess;
        }

        bool _ShouldHandlePlayerEvents()
        {
            if (!handleJoinLeaveEvents || allowAnyone)
                return false;

            return allowMaster || allowInstanceOwner || allowFirstJoin;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!_ShouldHandlePlayerEvents())
                return;

            bool ownerMasterChange = _SearchInstanceOwner();
            bool localAccessChange = _CalculateLocalAccess();

            if (ownerMasterChange || localAccessChange)
                _UpdateHandlers(EVENT_VALIDATE);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!_ShouldHandlePlayerEvents())
                return;

            bool ownerMasterChange = _SearchInstanceOwner();
            bool localAccessChange = _CalculateLocalAccess();

            if (ownerMasterChange || localAccessChange)
                _UpdateHandlers(EVENT_VALIDATE);
        }

        bool _SearchInstanceOwner()
        {
            VRCPlayerApi prevOwner = foundInstanceOwner;
            VRCPlayerApi prevMaster = foundMaster;

            foundInstanceOwner = Networking.InstanceOwner;
            foundMaster = Networking.Master;
            _worldHasOwner = foundInstanceOwner != null;

            return prevOwner != foundInstanceOwner || prevMaster != foundMaster;
        }

        public void _Enforce(bool state)
        {
            enforce = state;

            _UpdateHandlers(EVENT_VALIDATE);
            _UpdateHandlers(EVENT_ENFORCE_UPDATE);
        }

        public void _RefreshWhitelistCheck()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (Utilities.IsValid(player))
            {
                _localPlayerWhitelisted = _PlayerWhitelisted(player);
                _CalculateLocalAccess();
            }

            DebugLog($"Refresh whitelist local={_localPlayerWhitelisted}");
            _UpdateHandlers(EVENT_USER_SOURCE_VALIDATE);
            _UpdateHandlers(EVENT_VALIDATE);
        }
        
        public void _RefreshAccessHandlerCheck()
        {
            DebugLog("Refresh access handler");
            _UpdateHandlers(EVENT_VALIDATE);
        }

        public bool _PlayerWhitelisted(VRCPlayerApi player)
        {
            string playerName = player.displayName;
            if (Utilities.IsValid(userWhitelist))
            {
                foreach (string user in userWhitelist)
                {
                    if (playerName == user)
                        return true;
                }
            }

            if (Utilities.IsValid(whitelistSources))
            {
                foreach (AccessControlUserSource source in whitelistSources)
                {
                    if (!Utilities.IsValid(source))
                        continue;

                    if (source._ContainsName(playerName))
                        return true;
                }
            }

            return false;
        }

        public bool _LocalWhitelisted()
        {
            return _localPlayerWhitelisted;
        }

        public bool _HasAccess(VRCPlayerApi player)
        {
            if (!enforce)
                return true;

            if (player == Networking.LocalPlayer)
                return _LocalHasAccess();

            if (!Utilities.IsValid(player))
                return false;

            AccessHandlerResult handlerResult = _CheckAccessHandlerAccess(player);
            if (handlerResult == AccessHandlerResult.Deny)
                return false;
            if (handlerResult == AccessHandlerResult.Allow)
                return true;

            if (allowAnyone)
                return true;
            if (allowInstanceOwner && player.isInstanceOwner)
                return true;
            if (allowMaster && player.isMaster && (!restrictMasterIfOwnerPresent || !_worldHasOwner))
                return true;
            if (allowFirstJoin && player.displayName == syncFirstJoin && (!restrictFirstJoinIfOwnerPresent || !_worldHasOwner))
                return true;
            if (allowWhitelist && _PlayerWhitelisted(player))
                return true;

            return false;
        }

        public bool _LocalHasAccess()
        {
            if (!enforce)
                return true;

            VRCPlayerApi player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player))
                return false;

            AccessHandlerResult handlerResult = _CheckAccessHandlerAccess(player);
            if (handlerResult == AccessHandlerResult.Deny)
                return false;
            if (handlerResult == AccessHandlerResult.Allow)
                return true;

            if (_localCalculatedAccess)
                return true;
            if (allowMaster && player.isMaster && (!restrictMasterIfOwnerPresent || !_worldHasOwner))
                return true;
            if (allowFirstJoin && player.displayName == syncFirstJoin && (!restrictFirstJoinIfOwnerPresent || !_worldHasOwner))
                return true;

            return false;
        }

        [Obsolete("Use _Register(AccessControl.EVENT_VALIDATE, ...)")]
        public void _RegisterValidateHandler(Component handler, string eventName)
        {
            _Register(EVENT_VALIDATE, handler, eventName);
        }

        public void _Validate()
        {
            _RefreshWhitelistCheck();
        }

        AccessHandlerResult _CheckAccessHandlerAccess(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(accessHandlers))
                return AccessHandlerResult.Pass;

            int handlerCount = accessHandlers.Length;
            if (handlerCount == 0)
                return AccessHandlerResult.Pass;

            for (int i = 0; i < handlerCount; i++)
            {
                AccessControlHandler handler = accessHandlers[i];
                if (!handler)
                    continue;

                AccessHandlerResult result = handler._CheckAccess(player);
                if (result != AccessHandlerResult.Pass)
                    return result;
            }

            return AccessHandlerResult.Pass;
        }

        public override void OnDeserialization()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (Utilities.IsValid(player) && !_localPlayerFirstJoin && syncFirstJoin == player.displayName)
            {
                _localPlayerFirstJoin = true;
                DebugLog("First Joined: true");

                _Validate();
            }
        }

        void DebugLog(string message)
        {
            if (debugLogging)
                Debug.Log("[Texel:AccessControl] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("AccessControl", message);
        }

        public void _UpdateDebugState()
        {
            debugState._SetValue("localMaster", _localPlayerMaster.ToString());
            debugState._SetValue("localInstanceOwner", _localPlayerInstanceOwner.ToString());
            debugState._SetValue("localWhitelisted", _localPlayerWhitelisted.ToString());
            debugState._SetValue("localCalculated", _localCalculatedAccess.ToString());
            debugState._SetValue("allowMaster", allowMaster.ToString());
            debugState._SetValue("allowInstanceOwner", allowInstanceOwner.ToString());
            debugState._SetValue("allowFirstJoin", allowFirstJoin.ToString());
            debugState._SetValue("allowWhitelist", allowWhitelist.ToString());
            debugState._SetValue("allowAnyone", allowAnyone.ToString());
            debugState._SetValue("restrictMaster", restrictMasterIfOwnerPresent.ToString());
            debugState._SetValue("restrictFirstJoin", restrictFirstJoinIfOwnerPresent.ToString());
            debugState._SetValue("enforce", enforce.ToString());
            debugState._SetValue("instanceOwner", Utilities.IsValid(foundInstanceOwner) ? foundInstanceOwner.displayName : "--");
            debugState._SetValue("instanceOwnerCount", foundInstanceOwnerCount.ToString());
            debugState._SetValue("master", Utilities.IsValid(foundMaster) ? foundMaster.displayName : "--");
            debugState._SetValue("masterCount", foundMasterCount.ToString());
            debugState._SetValue("firstJoin", Utilities.IsValid(syncFirstJoin) ? syncFirstJoin : "--");
        }
    }
}
