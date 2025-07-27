
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel.Demo
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ZoneTriggerDemo : UdonSharpBehaviour
    {
        public ZoneTrigger basicZoneTrigger;
        public TrackedZoneTrigger trackedZoneTrigger;

        public DebugLog basicZoneInfo;
        public DebugLog trackedZoneInfo;

        public VRCPlayerApi playerArg;

        void Start()
        {
            basicZoneTrigger._Register(ZoneTrigger.EVENT_PLAYER_ENTER, this, nameof(_BasicZoneEnter), nameof(playerArg));
            basicZoneTrigger._Register(ZoneTrigger.EVENT_PLAYER_LEAVE, this, nameof(_BasicZoneLeave), nameof(playerArg));

            trackedZoneTrigger._Register(ZoneTrigger.EVENT_PLAYER_ENTER, this, nameof(_TrackedZoneEnter), nameof(playerArg));
            trackedZoneTrigger._Register(ZoneTrigger.EVENT_PLAYER_LEAVE, this, nameof(_TrackedZoneLeave), nameof(playerArg));
        }

        public void _BasicZoneEnter()
        {
            basicZoneInfo._Write("Enter", $"Player entered: {playerArg.displayName}#{playerArg.playerId}");
        }

        public void _BasicZoneLeave()
        {
            basicZoneInfo._Write("Leave", $"Player left: {playerArg.displayName}#{playerArg.playerId}");
        }

        public void _TrackedZoneEnter()
        {
            trackedZoneInfo._Write("Enter", $"Player entered: {playerArg.displayName}#{playerArg.playerId}");
            trackedZoneInfo._Write("Count", $"Tracked: {trackedZoneTrigger.TrackedPlayerCount}, Monitored: {trackedZoneTrigger.MonitoringPlayerCount}");
        }

        public void _TrackedZoneLeave()
        {
            trackedZoneInfo._Write("Leave", $"Player left: {playerArg.displayName}#{playerArg.playerId}");
            trackedZoneInfo._Write("Count", $"Tracked: {trackedZoneTrigger.TrackedPlayerCount}, Monitored: {trackedZoneTrigger.MonitoringPlayerCount}");
        }
    }
}
