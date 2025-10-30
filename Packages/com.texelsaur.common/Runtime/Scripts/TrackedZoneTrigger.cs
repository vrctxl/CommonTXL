
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

        protected DataDictionary trackingData;
        protected DataDictionary monitoring;

        private bool monitorQueued = false;
        private int playerCount = 1;
        private int trackingCount = 0;

        // Used for checking if players are still within the zone
        private DataList trackedCheckList;
        private float trackedCheckInterval;

        // Used for checking if players are still outside the zone
        private VRCPlayerApi[] playerCheckList;
        private float playerCheckInterval;

        protected override void _Init()
        {
            base._Init();

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
            {
                if (!localPlayerOnly || player.isLocal)
                    _CheckedAddPlayer(player);
            }

            playerCount = VRCPlayerApi.GetPlayerCount();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            base.OnPlayerLeft(player);

            if (Utilities.IsValid(player))
                _RemovePlayer(player);

            playerCount = VRCPlayerApi.GetPlayerCount();
        }

        public override void _PlayerTriggerEnter(VRCPlayerApi player)
        {
            _EnsureInit();
            if (!Utilities.IsValid(player))
                return;

            if (localPlayerOnly && !player.isLocal)
                return;

            if (triggerMode == ZoneTriggerMode.CapsuleTrigger)
            {
                _AddPlayer(player);
                return;
            }

            _CheckedAddPlayer(player, true);
        }

        public override void _PlayerTriggerExit(VRCPlayerApi player)
        {
            _EnsureInit();
            if (!Utilities.IsValid(player))
                return;

            if (localPlayerOnly && !player.isLocal)
                return;

            if (triggerMode == ZoneTriggerMode.CapsuleTrigger)
            {
                _RemovePlayer(player);
                return;
            }

            _CheckedRemovePlayer(player, true);
        }

        protected void _CheckedAddPlayer(VRCPlayerApi player, bool triggered = false)
        {
            if (!_PlayerPositionInZoneTriggering(player, positionRadius))
            {
                if (trackingData.ContainsKey(player.playerId))
                    _RemovePlayer(player);

                if (triggered)
                    _MonitorPlayer(player, true);

                return;
            }

            _AddPlayer(player);
        }

        protected void _AddPlayer(VRCPlayerApi player)
        {
            int playerId = player.playerId;

            monitoring.Remove(playerId);

            if (!trackingData.ContainsKey(playerId))
            {
                trackingData.Add(playerId, new DataToken(player));
                trackingCount = trackingData.Count;

                _UpdateHandlers(EVENT_PLAYER_ENTER, player);
            }
        }

        protected void _CheckedRemovePlayer(VRCPlayerApi player, bool triggered = false)
        {
            if (_PlayerPositionInZoneTriggering(player, positionRadius))
            {
                if (!trackingData.ContainsKey(player.playerId))
                    _AddPlayer(player);

                if (triggered)
                    _MonitorPlayer(player, false);

                return;
            }

            _RemovePlayer(player);
        }

        protected void _RemovePlayer(VRCPlayerApi player)
        {
            int playerId = player.playerId;

            monitoring.Remove(playerId);

            if (trackingData.ContainsKey(playerId))
            {
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
                                _AddPlayer(player);
                            else if (!isAdd && !inZone)
                                _RemovePlayer(player);
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
                    _CheckedRemovePlayer(player, false);
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
                _CheckedAddPlayer(player, false);

            playerCheckIndex += 1;
        }
    }
}
