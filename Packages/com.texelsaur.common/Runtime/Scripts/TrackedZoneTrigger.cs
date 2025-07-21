
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TrackedZoneTrigger : ZoneTrigger
    {
        [SerializeField] protected internal ZoneTriggerMode triggerMode;
        [SerializeField] protected internal float positionRadius = 0;
        [SerializeField] protected internal float monitorTriggerInterval = 0.1f;
        [SerializeField] protected internal float membershipCheckRate = 5f;
        [SerializeField] protected internal float addCheckRate = 0f;

        protected DataDictionary trackingData;
        protected DataDictionary monitoringAdd;
        protected DataDictionary monitoringRemove;

        private bool monitorQueued = false;

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
            monitoringAdd = new DataDictionary();
            monitoringRemove = new DataDictionary();

            if (membershipCheckRate > 0)
            {
                trackedCheckInterval = 1f / membershipCheckRate;
                _InternalCheckMembership();
            }

            if (addCheckRate > 0)
            {
                playerCheckInterval = 1f / addCheckRate;
                _InternalCheckAdd();
            }
        }

        public override bool IsTracking
        {
            get { return true; }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            base.OnPlayerLeft(player);

            if (Utilities.IsValid(player))
                _RemovePlayer(player);  
        }

        public override void _PlayerTriggerEnter(VRCPlayerApi player)
        {
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
                if (trackingData.ContainsKey(player.displayName))
                    _RemovePlayer(player);

                if (triggered)
                    _MonitorPlayer(player, true);

                return;
            }

            _AddPlayer(player);
        }

        protected void _AddPlayer(VRCPlayerApi player)
        {
            string name = player.displayName;

            if (!trackingData.ContainsKey(name))
            {
                trackingData.Add(name, player.playerId);

                _UpdateHandlers(EVENT_PLAYER_ENTER, player);
            }

            if (monitoringRemove.ContainsKey(name))
                monitoringRemove.Remove(name);
        }

        protected void _CheckedRemovePlayer(VRCPlayerApi player, bool triggered = false)
        {
            if (_PlayerPositionInZoneTriggering(player, positionRadius))
            {
                if (!trackingData.ContainsKey(player.displayName))
                    _AddPlayer(player);

                if (triggered)
                    _MonitorPlayer(player, false);

                return;
            }

            _RemovePlayer(player);
        }

        protected void _RemovePlayer(VRCPlayerApi player)
        {
            string name = player.displayName;

            if (trackingData.ContainsKey(name))
            {
                trackingData.Remove(name);

                _UpdateHandlers(EVENT_PLAYER_LEAVE, player);
            }

            if (monitoringAdd.ContainsKey(name))
                monitoringAdd.Remove(name);
        }

        protected void _MonitorPlayer(VRCPlayerApi player, bool add)
        {
            string name = player.displayName;

            if (add)
            {
                if (!monitoringAdd.ContainsKey(name))
                    monitoringAdd.Add(name, player.playerId);
            } else
            {
                if (!monitoringRemove.ContainsKey(name))
                    monitoringRemove.Add(name, player.playerId);
            }

            if (!monitorQueued)
            {
                monitorQueued = true;
                SendCustomEventDelayedSeconds(nameof(_InternalOnMonitorUpdate), monitorTriggerInterval);
            }
        }

        public void _InternalOnMonitorUpdate()
        {
            monitorQueued = false;

            foreach (var token in monitoringAdd.GetKeys())
            {
                string name = token.String;
                bool clear = false;

                if (monitoringAdd.TryGetValue(name, out DataToken idToken))
                {
                    VRCPlayerApi player = VRCPlayerApi.GetPlayerById(idToken.Int);
                    if (Utilities.IsValid(player))
                    {
                        if (_PlayerPositionInZoneTriggering(player, positionRadius))
                        {
                            _AddPlayer(player);
                            clear = true;
                        }
                    }
                    else
                        clear = true;
                }
                else
                    clear = true;

                if (clear)
                    monitoringAdd.Remove(name);
            }

            foreach (var token in monitoringRemove.GetKeys())
            {
                string name = token.String;
                bool clear = false;

                if (monitoringRemove.TryGetValue(name, out DataToken idToken))
                {
                    VRCPlayerApi player = VRCPlayerApi.GetPlayerById(idToken.Int);
                    if (Utilities.IsValid(player))
                    {
                        if (!_PlayerPositionInZoneTriggering(player, positionRadius))
                        {
                            _RemovePlayer(player);
                            clear = true;
                        }
                    }
                    else
                        clear = true;
                }
                else
                    clear = true;

                if (clear)
                    monitoringRemove.Remove(name);
            }

            if (monitoringAdd.Count > 0 || monitoringRemove.Count > 0)
            {
                monitorQueued = true;
                SendCustomEventDelayedSeconds(nameof(_InternalOnMonitorUpdate), monitorTriggerInterval);
            }
        }

        private int trackedCheckIndex = 0;
        public void _InternalCheckMembership()
        {
            _CheckNextMember();
            SendCustomEventDelayedSeconds(nameof(_InternalCheckMembership), trackedCheckInterval);
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

            if (trackedCheckList.TryGetValue(trackedCheckIndex, out DataToken nameToken))
            {
                string name = nameToken.String;
                if (trackingData.TryGetValue(name, out DataToken idToken))
                {
                    int playerId = idToken.Int;
                    VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerId);

                    if (Utilities.IsValid(player))
                        _CheckedRemovePlayer(player, false);
                }
            }

            trackedCheckIndex += 1;
        }

        private int playerCheckIndex = 0;
        public void _InternalCheckAdd()
        {
            _CheckNextPlayer();
            SendCustomEventDelayedSeconds(nameof(_InternalCheckAdd), playerCheckInterval);
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
            if (Utilities.IsValid(player) && !trackingData.ContainsKey(player.displayName))
                _CheckedAddPlayer(player, false);

            playerCheckIndex += 1;
        }
    }
}
