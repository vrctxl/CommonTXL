
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DebugLog : DebugLogProvider
    {
        public string title;
        public Text titleText;
        public Text debugText;
        public int lineCount = 27;
        public bool timestamp = false;

        string[] debugLines;
        int debugIndex = 0;

        private bool debugInit = false;
        private string[] joinScratch;
        private int debugFilled = 0;
        bool queueBuild = false;

        private void Start()
        {
            if (Utilities.IsValid(titleText))
                titleText.text = title;

            debugInit = true;
            _QueueUpdate();
        }

        private void OnEnable()
        {
            if (!debugInit)
                return;

            _QueueUpdate();
        }

        protected override void _OnProviderInit()
        {
            _EnsureBuffer();
        }

        protected override void _WriteEntry(int channel, DebugLogLevel level, string message)
        {
            base._WriteEntry(channel, level, message);

            _Write(_ChannelDisplayName(channel), message, level);
        }

        public void _Write(string component, string message)
        {
            _Write(component, message, (string)null);
        }

        public void _Write(string component, string message, string color)
        {
            _EnsureBuffer();

            string stamp = "";
            if (timestamp)
                stamp = string.Format("[{0,9:F3}] ", Time.time);

            debugLines[debugIndex] = string.IsNullOrEmpty(color)
                ? $"{stamp}[{component}] {message}"
                : $"<color=#{color}>{stamp}[{component}] {message}</color>";

            debugIndex++;
            if (debugIndex >= debugLines.Length)
                debugIndex = 0;

            if (debugFilled < debugLines.Length)
                debugFilled++;

            _QueueUpdate();
        }

        public void _WriteWarning(string component, string message)
        {
            _Write(component, message, "FFFF00");
        }

        public void _WriteError(string component, string message)
        {
            _Write(component, message, "FF0000");
        }

        public void _Write(string component, string message, DebugLogLevel level)
        { 
            if (level == DebugLogLevel.Warning)
                _Write(component, message, "FFFF00");
            else if (level == DebugLogLevel.Error)
                _Write(component, message, "FF0000");
            else
                _Write(component, message, (string)null);
        }

        void _EnsureBuffer()
        {
            if (debugLines != null && debugLines.Length == lineCount)
                return;

            debugLines = new string[lineCount];
            debugIndex = 0;
            debugFilled = 0;
        }

        public void _Refresh()
        {
            _QueueUpdate();
        }

        private void _QueueUpdate()
        {
            if (queueBuild)
                return;

            queueBuild = true;
            SendCustomEventDelayedFrames(nameof(_InternalRebuildBuffer), 1);
        }

        public void _InternalRebuildBuffer()
        {
            queueBuild = false;

            if (!Utilities.IsValid(debugText) || !debugText.gameObject.activeInHierarchy)
                return;

            int count = debugFilled;
            if (count == 0)
            {
                debugText.text = "";
                return;
            }

            int start = (count == debugLines.Length) ? debugIndex : 0;

            if (joinScratch == null || joinScratch.Length != count)
                joinScratch = new string[count];

            for (int i = 0; i < count; i++)
            {
                int n = (start + i) % debugLines.Length;
                joinScratch[i] = debugLines[n];
            }

            debugText.text = string.Join("\n", joinScratch);
        }
    }
}