using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

// ImageDownloadQuad is a basic example of using the Image Download Manager.
//
// The manager abstracts VRC's image download interface and ensures images
// requested through the manager are fetched in first-in-first-out order.  If
// multiple scripts using the same manager request the same URL, the image will
// only be downloaded once and a single texture will be shared.  When all scripts
// using a shared texture release it, the underlying texture will also be freed.

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ImageDownloadQuad : EventBase
    {
        [Tooltip("Required.  A single Image Download Manager can be shared among multiple Image Download Quad instances, as well as other scripts.  Textures for the same URL will be shared.")]
        [SerializeField] internal ImageDownloadManager downloadManager;
        [SerializeField] internal VRCUrl imageUrl;
        [SerializeField] internal float loadDelay;
        [SerializeField] internal MeshRenderer mesh;

        private int imageClaim;
        private Texture2D image;
        private MaterialPropertyBlock block;

        public const int EVENT_IMAGE_CHANGED = 0;
        const int EVENT_COUNT = 1;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount
        {
            get { return EVENT_COUNT; }
        }

        protected override void _Init()
        {
            base._Init();

            block = new MaterialPropertyBlock();
            if (imageUrl != null)
            {
                if (loadDelay > 0)
                    SendCustomEventDelayedSeconds(nameof(_InternalLoadImage), loadDelay);
                else
                    _InternalLoadImage();
            }
        }

        public Texture2D LoadedImage
        {
            get { return image; }
        }

        public void _InternalLoadImage()
        {
            _LoadImage(imageUrl);
        }

        public void _LoadImage(VRCUrl url)
        {
            if (!downloadManager || url == null || url == VRCUrl.Empty)
                return;

            _ReleaseImage();

            imageClaim = downloadManager._RequestImage(url, this, nameof(_InternalOnImageReady));
        }

        public void _InternalOnImageReady()
        {
            image = downloadManager.CurrentImage;
            _UpdateMesh();
        }

        public void _ReleaseImage()
        {
            if (imageClaim <= 0)
                return;

            image = null;
            downloadManager._ReleaseImage(imageClaim);
            imageClaim = 0;

            _UpdateMesh();
        }

        void _UpdateMesh()
        {
            if (!mesh)
                return;

            mesh.GetPropertyBlock(block);
            block.SetTexture("_MainTex", image != null ? image : Texture2D.blackTexture);
            mesh.SetPropertyBlock(block);
        }
    }
}
