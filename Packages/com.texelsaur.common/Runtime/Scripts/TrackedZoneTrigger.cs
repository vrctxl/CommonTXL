
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

[assembly: InternalsVisibleTo("com.texelsaur.common.Editor")]

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TrackedZoneTrigger : ZoneTrigger
    {
        [SerializeField] protected internal ZoneTriggerMode triggerMode = ZoneTriggerMode.Position;
        [SerializeField] protected internal float positionRadius = 0.1f;
        [SerializeField] protected internal float monitorTriggerInterval = 0.1f;
        [SerializeField] protected internal bool checkForRemove = true;
        [SerializeField] protected internal float removeCheckInterval = 5f;
        [SerializeField] protected internal bool checkForAdd = false;
        [SerializeField] protected internal float addCheckInterval = 5f;

        [SerializeField] protected internal DebugLog debugLog;
        [SerializeField] protected internal bool vrcLog;

        protected DataDictionary trackingData;
        protected DataDictionary monitoring;

        private bool usingDebug = false;
        private bool monitorQueued = false;
        private int playerCount = 1;
        private int trackingCount = 0;

        // Used for checking if players are still within the zone
        private DataList trackedCheckList;
        private float trackedCheckInterval;

        // Used for checking if players are still outside the zone
        private VRCPlayerApi[] playerCheckList;
        private float playerCheckInterval;

        private const string SOURCE_TRIGGER = "trigger";
        private const string SOURCE_MONITOR = "monitor";
        private const string SOURCE_WORLD = "world";
        private const string SOURCE_SCAN = "scan";
        private const string SOURCE_TYPECHANGE = "typechange";

        protected override void _Init()
        {
            base._Init();

            usingDebug = vrcLog || Utilities.IsValid(debugLog);

            trackingData = new DataDictionary();
            monitoring = new DataDictionary();

            if (checkForRemove && removeCheckInterval > 0)
            {
                trackedCheckInterval = 1f / removeCheckInterval;
                SendCustomEventDelayedSeconds(nameof(_InternalCheckMembership), trackedCheckInterval * Random.value);
            }

            if (checkForAdd && addCheckInterval > 0)
            {
                playerCheckInterval = 1f / addCheckInterval;
                SendCustomEventDelayedSeconds(nameof(_InternalCheckAdd), playerCheckInterval * Random.value);
            }
        }

        public override bool IsTracking
        {
            get { return true; }
        }

        // Tracked players are players last known to be within the zone.  If players have entered or exited the zone without causing native
        // triggers to fire, tracking may be inaccurate until periodic membership scanning happens.
        public virtual int TrackedPlayerCount
        {
            get 
            {
                _EnsureInit();
                return trackingData.Count; 
            }
        }

        // Monitored players are players that triggered native collisions with the zone but don't actually meet the position threshold to be
        // added or removed.  These players are continuously checked at a higher rate until their position is consistent with the original trigger event.
        public virtual int MonitoringPlayerCount
        {
            get 
            {
                _EnsureInit();
                return monitoring.Count; 
            }
        }

        public override ZonePlayerType PlayerType 
        {
            set
            {
                if (value == ZonePlayerType.Legacy)
                    return;

                if (playerType != value)
                {
                    ZonePlayerType prev = playerType;
                    playerType = value;
                    _UpdateHandlers(EVENT_PLAYER_TYPE_CHANGED);

                    bool updateAll = false;
                    bool removePlayer = false;
                    bool addPlayer = false;

                    if (usingDebug) _DebugLog($"Set PlayerType = {value}");

                    if (value == ZonePlayerType.None)
                    {
                        removePlayer = true;
                        updateAll = true;
                    }
                    else if (value == ZonePlayerType.Local)
                    {
                        if (prev == ZonePlayerType.Remote || prev == ZonePlayerType.None)
                            addPlayer = true;
                        updateAll = true;
                    } else if (value == ZonePlayerType.Remote)
                    {
                        if (prev == ZonePlayerType.Local || prev == ZonePlayerType.Both)
                            removePlayer = true;
                        updateAll = true;
                    } else if (value == ZonePlayerType.Both)
                    {
                        if (prev == ZonePlayerType.Remote || prev == ZonePlayerType.None)
                            addPlayer = true;
                        if (prev == ZonePlayerType.Local || prev == ZonePlayerType.None)
                            updateAll = true;
                    }

                    // When players change, try to remove local player first, and add local player last,
                    // relative to bulk add/remove of remaining players

                    if (removePlayer)
                        _CheckedRemovePlayer(Networking.LocalPlayer, SOURCE_TYPECHANGE);

                    if (updateAll)
                        _UpdateAllPlayers(false, SOURCE_TYPECHANGE);

                    if (addPlayer)
                        _CheckedAddPlayer(Networking.LocalPlayer, SOURCE_TYPECHANGE);
                }
            }
        }

        public override bool _PlayerInZone(VRCPlayerApi player)
        {
            _EnsureInit();
            return trackingData.ContainsKey(player.playerId);
        }

        public virtual DataList _GetTrackedPlayers()
        {
            _EnsureInit();
            return trackingData.GetValues();
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            base.OnPlayerJoined(player);

            if (Utilities.IsValid(player) && triggerMode == ZoneTriggerMode.Position)
                _CheckedAddPlayer(player, SOURCE_WORLD);

            playerCount = VRCPlayerApi.GetPlayerCount();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            base.OnPlayerLeft(player);

            if (Utilities.IsValid(player))
                _RemovePlayer(player, SOURCE_WORLD);

            playerCount = VRCPlayerApi.GetPlayerCount();
        }

        public override void _PlayerTriggerEnter(VRCPlayerApi player)
        {
            _EnsureInit();
            if (!Utilities.IsValid(player))
                return;

            if (usingDebug) _DebugLog($"Player Trigger Enter: {player.displayName} [{player.playerId}] [local: {player.isLocal}]");

            if (triggerMode == ZoneTriggerMode.CapsuleTrigger)
            {
                if (!_PlayerValidForType(player))
                    return;

                _AddPlayer(player, SOURCE_TRIGGER);
                return;
            }

            _CheckedAddPlayer(player, SOURCE_TRIGGER);
        }

        public override void _PlayerTriggerExit(VRCPlayerApi player)
        {
            _EnsureInit();
            if (!Utilities.IsValid(player))
                return;

            if (usingDebug) _DebugLog($"Player Trigger Exit: {player.displayName} [{player.playerId}] [local: {player.isLocal}]");

            if (!_PlayerValidForType(player))
                return;

            if (triggerMode == ZoneTriggerMode.CapsuleTrigger)
            {
                _RemovePlayer(player, SOURCE_TRIGGER);
                return;
            }

            _CheckedRemovePlayer(player, SOURCE_TRIGGER);
        }

        bool _PlayerValidForType(VRCPlayerApi player)
        {
            if (playerType == ZonePlayerType.None)
                return false;
            if (player.isLocal && playerType == ZonePlayerType.Remote)
                return false;
            if (!player.isLocal && playerType == ZonePlayerType.Local)
                return false;

            return true;
        }

        protected void _CheckedAddPlayer(VRCPlayerApi player, string source)
        {
            bool typeValid = _PlayerValidForType(player);

            if (!_PlayerPositionInZoneTriggering(player, positionRadius))
            {
                if (trackingData.ContainsKey(player.playerId))
                    _RemovePlayer(player, source);

                if (source == SOURCE_TRIGGER && typeValid)
                    _MonitorPlayer(player, true);

                return;
            }

            if (typeValid)
                _AddPlayer(player, source);
        }

        protected void _AddPlayer(VRCPlayerApi player, string source)
        {
            int playerId = player.playerId;

            monitoring.Remove(playerId);

            if (!trackingData.ContainsKey(playerId))
            {
                if (usingDebug) _DebugLog($"Add tracked player by {source}: {player.displayName} [{playerId}] [C: {TrackedPlayerCount}]");
                trackingData.Add(playerId, new DataToken(player));
                trackingCount = trackingData.Count;

                _UpdateHandlers(EVENT_PLAYER_ENTER, player);
            }
        }

        protected void _CheckedRemovePlayer(VRCPlayerApi player, string source)
        {
            bool typeValid = _PlayerValidForType(player);

            if (_PlayerPositionInZoneTriggering(player, positionRadius))
            {
                if (!trackingData.ContainsKey(player.playerId) && typeValid)
                    _AddPlayer(player, source);

                if (source == SOURCE_TRIGGER)
                    _MonitorPlayer(player, false);

                if (typeValid)
                    return;
            }

            _RemovePlayer(player, source);
        }

        protected void _RemovePlayer(VRCPlayerApi player, string source)
        {
            int playerId = player.playerId;

            monitoring.Remove(playerId);

            if (trackingData.ContainsKey(playerId))
            {
                if (usingDebug) _DebugLog($"Remove tracked player by {source}: {player.displayName} [{playerId}] [C: {TrackedPlayerCount}]");
                trackingData.Remove(playerId);
                trackingCount = trackingData.Count;

                _UpdateHandlers(EVENT_PLAYER_LEAVE, player);
            }
        }

        protected void _MonitorPlayer(VRCPlayerApi player, bool add)
        {
            monitoring.SetValue(player.playerId, add ? 1 : -1);

            if (!monitorQueued)
            {
                monitorQueued = true;
                SendCustomEventDelayedSeconds(nameof(_InternalOnMonitorUpdate), monitorTriggerInterval);
            }
        }

        public void _InternalOnMonitorUpdate()
        {
            monitorQueued = false;

            int count = monitoring.Count;
            DataList monitorKeys = monitoring.GetKeys();

           for (int i = 0; i < count; i++)
           {
                if (monitorKeys.TryGetValue(i, out DataToken token))
                {
                    int playerId = token.Int;
                    bool clear = true;

                    if (monitoring.TryGetValue(playerId, out DataToken actionToken))
                    {
                        int action = actionToken.Int;
                        bool isAdd = action > 0;

                        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerId);
                        if (Utilities.IsValid(player))
                        {
                            bool inZone = _PlayerPositionInZoneTriggering(player, positionRadius);
                            if (isAdd && inZone)
                                _AddPlayer(player, SOURCE_MONITOR);
                            else if (!isAdd && !inZone)
                                _RemovePlayer(player, SOURCE_MONITOR);
                            else
                                clear = false;
                        }
                    }

                    if (clear)
                        monitoring.Remove(playerId);
                }
            }

            if (monitoring.Count > 0)
            {
                monitorQueued = true;
                SendCustomEventDelayedSeconds(nameof(_InternalOnMonitorUpdate), monitorTriggerInterval);
            }
        }

        private int trackedCheckIndex = 0;
        public void _InternalCheckMembership()
        {
            _CheckNextMember();

            float interval = trackingCount > 0 ? removeCheckInterval / trackingCount : removeCheckInterval;
            SendCustomEventDelayedSeconds(nameof(_InternalCheckMembership), interval);
        }

        void _CheckNextMember()
        {
            if (trackedCheckList == null || trackedCheckIndex >= trackedCheckList.Count)
            {
                trackedCheckIndex = 0;
                if (trackingData.Count == 0)
                    trackedCheckList = null;
                else
                    trackedCheckList = trackingData.GetKeys();

                if (trackedCheckList == null)
                    return;
            }

            if (trackedCheckList.TryGetValue(trackedCheckIndex, out DataToken idToken))
            {
                int playerId = idToken.Int;
                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerId);

                if (Utilities.IsValid(player))
                    _CheckedRemovePlayer(player, SOURCE_SCAN);
            }

            trackedCheckIndex += 1;
        }

        private int playerCheckIndex = 0;
        public void _InternalCheckAdd()
        {
            _CheckNextPlayer();

            float interval = addCheckInterval / playerCount;
            SendCustomEventDelayedSeconds(nameof(_InternalCheckAdd), interval);
        }

        void _CheckNextPlayer()
        {
            if (playerCheckList == null || playerCheckIndex >= playerCheckList.Length)
            {
                playerCheckIndex = 0;

                int count = VRCPlayerApi.GetPlayerCount();
                if (count == 0)
                    playerCheckList = null;
                else
                {
                    if (playerCheckList == null || playerCheckList.Length != count)
                        playerCheckList = new VRCPlayerApi[count];
                    playerCheckList = VRCPlayerApi.GetPlayers(playerCheckList);
                }

                if (playerCheckList == null)
                    return;
            }

            VRCPlayerApi player = playerCheckList[playerCheckIndex];
            if (Utilities.IsValid(player) && !trackingData.ContainsKey(player.playerId))
                _CheckedAddPlayer(player, SOURCE_SCAN);

            playerCheckIndex += 1;
        }

        void _UpdateAllPlayers(bool includeLocal, string source)
        {
            int count = VRCPlayerApi.GetPlayerCount();
            VRCPlayerApi[] playerCheckList = new VRCPlayerApi[count];
            playerCheckList = VRCPlayerApi.GetPlayers(playerCheckList);

            for (int i = 0; i < count; i++)
            {
                VRCPlayerApi player = playerCheckList[i];
                if (!Utilities.IsValid(player))
                    continue;
                if (!includeLocal && player.isLocal)
                    continue;

                if (!trackingData.ContainsKey(player.playerId))
                    _CheckedAddPlayer(player, source);
                else
                    _CheckedRemovePlayer(player, source);
            }
        }

        void _DebugLog(string message)
        {
            if (vrcLog)
                Debug.Log("[Texel:TrackedZoneTrigger] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("TrackedZoneTrigger", message);
        }
    }
}
