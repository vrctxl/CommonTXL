
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Texel.Adapter
{
    public abstract class ImageDownloadAdapter : UdonSharpBehaviour
    {
        public abstract void _RequestImage(VRCUrl url, Component handler, string eventName, string textureArgName);

        public abstract void _ReleaseImage(Texture texture);
    }
}
