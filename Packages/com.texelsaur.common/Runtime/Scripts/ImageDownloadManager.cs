using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Image;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ImageDownloadManager : EventBase
    {
        [SerializeField] protected internal DebugLog debugLog;
        [SerializeField] protected internal bool vrcLogging = false;

        protected const int STATE_QUEUED = 0;
        protected const int STATE_DOWNLOADING = 1;
        protected const int STATE_AVAILABLE = 2;
        protected const int STATE_ERROR = 3;

        protected int nextClaimToken = 1;

        protected VRCImageDownloader imageDownloader;
        protected TextureInfo defaultInfo;

        protected int dispatchClaim;
        protected IVRCImageDownload dispatchDownload;

        protected DataList requestQueue;
        protected DataDictionary imageData;
        protected DataDictionary claims;

        protected bool pendingDownload = false;

        public const int EVENT_IMAGE_DOWNLOADED = 0;
        public const int EVENT_IMAGE_ERROR = 1;
        public const int EVENT_IMAGE_DISPOSED = 2;
        const int EVENT_COUNT = 3;

        protected override void _Init()
        {
            imageDownloader = new VRCImageDownloader();

            defaultInfo = new TextureInfo();
            defaultInfo.GenerateMipMaps = true;

            requestQueue = new DataList();
            imageData = new DataDictionary();
            claims = new DataDictionary();
        }

        protected override int EventCount => EVENT_COUNT;

        private void OnDestroy()
        {
            if (imageDownloader != null)
            {
                imageDownloader.Dispose();
                imageDownloader = null;
            }
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            _DebugLog($"Image loaded: url={result.Url.Get()}, state={result.State}, size={result.SizeInMemoryBytes}");
            _ImageLoadEvent(result, false);

            dispatchClaim = 0;
            _UpdateHandlers(EVENT_IMAGE_DOWNLOADED);
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            _DebugLog($"Image load error: url={result.Url.Get()}, error={result.Error}, {result.ErrorMessage}");
            _ImageLoadEvent(result, true);

            dispatchClaim = 0;
            _UpdateHandlers(EVENT_IMAGE_ERROR);
        }

        public virtual int CurrentClaim
        {
            get { return dispatchClaim; }
        }

        public virtual VRCUrl CurrentUrl
        {
            get
            {
                if (dispatchDownload == null)
                    return null;

                return dispatchDownload.Url;
            }
        }

        public virtual Texture2D CurrentImage
        {
            get
            {
                if (dispatchDownload == null || dispatchDownload.State != VRCImageDownloadState.Complete)
                    return null;

                return dispatchDownload.Result;
            }
        }

        public virtual VRCImageDownloadError CurrentError
        {
            get { return dispatchDownload.Error; }
        }

        public virtual int _RequestImage(VRCUrl url, Component handler, string eventName)
        {
            _EnsureInit();
            DataToken urlToken = new DataToken(url);

            DataDictionary infoObj;
            if (imageData.TryGetValue(url.Get(), out var infoToken))
                infoObj = infoToken.DataDictionary;
            else
            {
                infoObj = new DataDictionary();
                infoObj["state"] = STATE_QUEUED;
                infoObj["claims"] = new DataList();

                imageData[url.Get()] = infoObj;

                if (!requestQueue.Contains(urlToken))
                    requestQueue.Add(urlToken);
            }

            int claim = nextClaimToken++;

            _DebugLog($"Request image: url={url.Get()}, handler={handler.gameObject.name}, event={eventName} -> claim={claim}");

            DataDictionary claimObj = new DataDictionary();
            claimObj["url"] = urlToken;
            claimObj["handler"] = new DataToken(handler);
            claimObj["event"] = eventName;
            claims[claim] = claimObj;

            infoObj["claims"].DataList.Add(claim);
            if (infoObj["state"] == STATE_AVAILABLE && imageData.TryGetValue("image", out var imageToken))
            {
                IVRCImageDownload image = (IVRCImageDownload)imageToken.Reference;
                if (image != null)
                    _DispatchImage(image, claim);
            }
            else
                _DownloadNextImage();

            return claim;
        }

        public virtual void _ReleaseImage(int claimToken)
        {
            _EnsureInit();
            if (claims.TryGetValue(claimToken, out var urlToken))
            {
                claims.Remove(claimToken);
                _ReleaseImage(claimToken, (VRCUrl)urlToken.Reference);
            }
        }

        private void _ImageLoadEvent(IVRCImageDownload result, bool error)
        {
            pendingDownload = false;
            _DispatchImage(result, error);
            _DownloadNextImage();
        }

        protected virtual void _DownloadNextImage()
        {
            if (pendingDownload || requestQueue.Count == 0)
                return;

            VRCUrl nextUrl = null;
            while (requestQueue.Count > 0)
            {
                nextUrl = (VRCUrl)requestQueue[0].Reference;
                requestQueue.RemoveAt(0);

                if (nextUrl != null && nextUrl.Get() != "")
                    break;

                if (requestQueue.Count == 0)
                    return;
            }

            pendingDownload = true;
            IVRCImageDownload download = imageDownloader.DownloadImage(nextUrl, null, (IUdonEventReceiver)this, defaultInfo);

            _DebugLog($"Downloading Image: {nextUrl.Get()}");

            if (imageData.TryGetValue(nextUrl.Get(), out var infoToken))
            {
                DataDictionary infoObj = infoToken.DataDictionary;
                infoObj["state"] = STATE_DOWNLOADING;

                if (infoObj.TryGetValue("image", out var imageToken))
                {
                    IVRCImageDownload image = (IVRCImageDownload)imageToken.Reference;
                    image.Dispose();
                }

                infoObj["image"] = new DataToken(download);
            }
        }

        protected virtual void _DispatchImage(IVRCImageDownload imageDownload, bool error)
        {
            if (imageData.TryGetValue(imageDownload.Url.Get(), out var infoToken))
            {
                DataDictionary infoObj = infoToken.DataDictionary;
                DataList infoClaims = infoObj["claims"].DataList;

                if (!error && imageDownload.State == VRCImageDownloadState.Complete)
                    infoObj["state"] = STATE_AVAILABLE;
                else
                    infoObj["state"] = STATE_ERROR;

                for (int i = 0; i < infoClaims.Count; i++)
                    _DispatchImage(imageDownload, infoClaims[i].Int);
            }
        }

        protected virtual void _DispatchImage(IVRCImageDownload imageDownload, int claim)
        {
            dispatchDownload = imageDownload;
            dispatchClaim = claim;

            if (claims.TryGetValue(dispatchClaim, out var claimInfoToken))
            {
                DataDictionary claimInfo = claimInfoToken.DataDictionary;
                UdonBehaviour handler = (UdonBehaviour)claimInfo["handler"].Reference;
                string handlerEvent = claimInfo["event"].String;

                handler.SendCustomEvent(handlerEvent);
            }
        }

        protected virtual void _ReleaseImage(int claimToken, VRCUrl url)
        {
            _DebugLog($"Release image: url={url.Get()}, claim={claimToken}");

            if (imageData.TryGetValue(url.Get(), out var infoToken))
            {
                DataDictionary infoObj = infoToken.DataDictionary;
                if (infoObj.TryGetValue("claims", out var infoClaimsToken))
                {
                    DataList claimList = infoClaimsToken.DataList;
                    claimList.Remove(claimToken);

                    if (claimList.Count == 0)
                        infoObj.Remove("claims");
                }

                if (!infoObj.ContainsKey("claims"))
                {
                    int state = STATE_AVAILABLE;
                    if (infoObj.TryGetValue("state", out var stateToken))
                        state = stateToken.Int;

                    if (state == STATE_QUEUED)
                        requestQueue.Remove(new DataToken(url));

                    if (infoObj.TryGetValue("image", out var imageToken))
                    {
                        IVRCImageDownload image = (IVRCImageDownload)imageToken.Reference;
                        if (image != null)
                        {
                            _DebugLog($"No claims left, disposing image: url={url.Get()}");
                            image.Dispose();

                            dispatchClaim = 0;
                            dispatchDownload = image;
                            _UpdateHandlers(EVENT_IMAGE_DISPOSED);
                        }

                        infoObj.Remove("image");
                    }

                    imageData.Remove(url.Get());
                }
            }
        }

        void _DebugLog(string message)
        {
            if (vrcLogging)
                Debug.Log("[CommonTXL:ImageDownloadManager] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("ImageDownloadManager", message);
        }
    }
}
