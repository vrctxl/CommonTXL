
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public enum ZoneTriggerMode
    {
        CapsuleTrigger,
        Position,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ZoneTrigger : EventBase
    {
        [Tooltip("If enabled, specify event handlers at edit time.  Handlers can still be registered at runtime.")]
        public bool configureEvents = false;
        [Tooltip("The Udon Behavior to send messages to on enter and leave events")]
        public UdonBehaviour targetBehavior;
        [Tooltip("Whether colliders should only recognize the local player")]
        public bool localPlayerOnly = true;
        [Tooltip("The event message to send on a player trigger enter event.  Leave blank to do nothing.")]
        public string playerEnterEvent;
        [Tooltip("The event message to send on a player trigger leave event.  Leave blank to do nothing.")]
        public string playerLeaveEvent;
        [Tooltip("Variable in remote script to write player reference before calling an enter or leave event.  Leave blank to not set player reference.")]
        public string playerTargetVariable;

        protected Collider[] cachedColliders;

        public const int EVENT_PLAYER_ENTER = 0;
        public const int EVENT_PLAYER_LEAVE = 1;
        const int EVENT_COUNT = 2;

        bool triggered = false;

        protected override int EventCount { get => EVENT_COUNT; }

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            cachedColliders = GetComponents<Collider>();

            if (configureEvents)
            {
                if (Utilities.IsValid(targetBehavior) && playerEnterEvent != null && playerEnterEvent != "")
                    _Register(EVENT_PLAYER_ENTER, targetBehavior, playerEnterEvent, playerTargetVariable);
                if (Utilities.IsValid(targetBehavior) && playerLeaveEvent != null && playerLeaveEvent != "")
                    _Register(EVENT_PLAYER_LEAVE, targetBehavior, playerLeaveEvent, playerTargetVariable);
            }
        }

        public virtual bool IsTracking
        {
            get { return false; }
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            _PlayerTriggerEnter(player);
        }

        public virtual void _PlayerTriggerEnter(VRCPlayerApi player)
        {
            if (localPlayerOnly && !player.isLocal)
                return;

            if (localPlayerOnly)
                triggered = true;

            _UpdateHandlers(EVENT_PLAYER_ENTER, player);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            _PlayerTriggerExit(player);
        }

        public virtual void _PlayerTriggerExit(VRCPlayerApi player)
        {
            if (localPlayerOnly && !player.isLocal)
                return;

            if (localPlayerOnly)
                triggered = false;

            _UpdateHandlers(EVENT_PLAYER_LEAVE, player);
        }

        public virtual bool _LocalPlayerInZone()
        {
            if (!localPlayerOnly)
                return false;

            return triggered;
        }

        public virtual bool _PlayerInZone(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer)
                return triggered;

            return false;
        }

        public virtual bool _PlayerPositionInZone(VRCPlayerApi player, float radius = 0)
        {
            _EnsureInit();

            if (!Utilities.IsValid(player))
                return false;

            Vector3 pos = player.GetPosition();

            foreach (var c in cachedColliders) { 
                if (!c.enabled)
                    continue;

                Vector3 closest = c.ClosestPoint(pos);
                if ((closest - pos).sqrMagnitude < radius + Mathf.Epsilon)
                    return true;
            }

            return false;
        }

        public virtual bool _PlayerPositionInZoneTriggering(VRCPlayerApi player, float radius = 0)
        {
            return _PlayerPositionInZone(player, radius);
        }
    }
}
