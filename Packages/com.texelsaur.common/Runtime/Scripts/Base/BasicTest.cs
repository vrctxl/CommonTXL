
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public abstract class BasicTest : UdonSharpBehaviour
    {
        public abstract bool _Test();
    }
}
