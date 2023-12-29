using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace Texel
{
    public enum AccessHandlerResult
    {
        Allow,
        Deny,
        Pass,
    }

    public abstract class AccessControlHandler : EventBase
    {
        public const int EVENT_REVALIDATE = 0;
        public const int EVENT_COUNT = 1;

        protected override int EventCount { get => EVENT_COUNT; }

        void Start()
        {
            _EnsureInit();
        }

        public virtual AccessHandlerResult _CheckAccess(VRCPlayerApi player)
        {
            return AccessHandlerResult.Pass;
        }
    }
}