using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Texel;
using VRC.SDKBase;

namespace Texel
{
    public abstract class AccessControlUserSource : EventBase
    {
        public const int EVENT_REVALIDATE = 0;
        public const int EVENT_COUNT = 1;

        protected override int EventCount { get => EVENT_COUNT; }

        void Start()
        {
            _EnsureInit();
        }

        public virtual bool _ContainsName(string name)
        {
            return false;
        }

        public virtual bool _ContainsPlayer(VRCPlayerApi player)
        {
            if (player == null || !player.IsValid())
                return false;

            return _ContainsName(player.displayName);
        }

        public virtual bool _ContainsAnyPlayerInWorld()
        {
            VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);

            for (int i = 0; i < players.Length; i++)
            {
                if (_ContainsPlayer(players[i]))
                    return true;
            }

            return false;
        }
    }
}
