
using UdonSharp;
using UnityEngine;

namespace Texel
{
    public enum DebugLogLevel
    {
        Trace = -2,
        Verbose = -1,
        Info = 0,
        Warning = 1,
        Error = 2
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DebugLogProvider : UdonSharpBehaviour
    {
        [SerializeField] internal bool enableLogging = true;
        [SerializeField] internal bool vrcLogging;
        [SerializeField] internal DebugLogLevel minLogLevel = DebugLogLevel.Info;

        string[] channelNamespace;
        string[] channelName;
        string[] channelSuffix;
        string[] channelDisplayName;
        string[] channelVrcPrefix;
        int channelCount = 0;

        bool init = false;

        void _EnsureProviderInit()
        {
            if (init)
                return;

            init = true;

            channelNamespace = new string[4];
            channelName = new string[4];
            channelSuffix = new string[4];
            channelDisplayName = new string[4];
            channelVrcPrefix = new string[4];

            _OnProviderInit();
        }

        protected virtual void _OnProviderInit() { }

        public int _RegisterChannel(string ns, string name, string suffix)
        {
            _EnsureProviderInit();

            if (ns == null)
                ns = "";
            if (name == null)
                name = "";
            if (suffix == null)
                suffix = "";

            for (int i = 0; i < channelCount; i++)
            {
                if (channelNamespace[i] == ns && channelName[i] == name && channelSuffix[i] == suffix)
                    return i;
            }

            if (channelCount >= channelName.Length)
                _GrowChannels();

            int index = channelCount;
            channelCount += 1;

            channelNamespace[index] = ns;
            channelName[index] = name;
            channelSuffix[index] = suffix;

            if (suffix == "")
            {
                channelDisplayName[index] = name;
                channelVrcPrefix[index] = $"[{ns}:{name}] ";
            }
            else
            {
                channelDisplayName[index] = $"{name}:{suffix}";
                channelVrcPrefix[index] = $"[{ns}:{name}:{suffix}] ";
            }

            _OnChannelRegistered(index);

            return index;
        }

        protected virtual void _OnChannelRegistered(int channel) { }

        void _GrowChannels()
        {
            int cap = channelName.Length * 2;

            channelNamespace = (string[])UtilityTxl.ArraySetSize(channelNamespace, cap, typeof(string));
            channelName = (string[])UtilityTxl.ArraySetSize(channelName, cap, typeof(string));
            channelSuffix = (string[])UtilityTxl.ArraySetSize(channelSuffix, cap, typeof(string));
            channelDisplayName = (string[])UtilityTxl.ArraySetSize(channelDisplayName, cap, typeof(string));
            channelVrcPrefix = (string[])UtilityTxl.ArraySetSize(channelVrcPrefix, cap, typeof(string));
        }

        public void _WriteTrace(int channel, string message)
        {
            _Write(channel, message, DebugLogLevel.Trace);
        }

        public void _WriteVerbose(int channel, string message)
        {
            _Write(channel, message, DebugLogLevel.Verbose);
        }

        public void _WriteInfo(int channel, string message)
        {
            _Write(channel, message, DebugLogLevel.Info);
        }

        public void _WriteWarning(int channel, string message)
        {
            _Write(channel, message, DebugLogLevel.Warning);
        }

        public void _WriteError(int channel, string message)
        {
            _Write(channel, message, DebugLogLevel.Error);
        }

        public void _Write(int channel, string message, DebugLogLevel level)
        {
            if (!enableLogging)
                return;

            if (level < minLogLevel)
                return;

            if (channel < 0 || channel >= channelCount)
                channel = _FallbackChannel();

            _WriteEntry(channel, level, message);
        }

        protected virtual void _WriteVrcEntry(int channel, DebugLogLevel level, string message)
        {
            string full = channelVrcPrefix[channel] + message;

            if (level == DebugLogLevel.Info)
                Debug.Log(full);
            else if (level == DebugLogLevel.Warning)
                Debug.LogWarning(full);
            else
                Debug.LogError(full);
        }

        protected virtual void _WriteEntry(int channel, DebugLogLevel level, string message)
        {
            if (vrcLogging)
                _WriteVrcEntry(channel, level, message);
        }

        int _FallbackChannel()
        {
            _EnsureProviderInit();

            if (channelCount == 0)
                return _RegisterChannel("", "Unknown", "");

            return 0;
        }

        public virtual bool VrcLogging
        {
            get { return vrcLogging; }
            set { vrcLogging = value; }
        }

        public DebugLogLevel MinLogLevel
        {
            get { return minLogLevel; }
        }

        public int ChannelCount
        {
            get { return channelCount; }
        }

        public string _ChannelDisplayName(int channel)
        {
            if (channel < 0 || channel >= channelCount)
                return "";

            return channelDisplayName[channel];
        }

        public string _ChannelNamespace(int channel)
        {
            if (channel < 0 || channel >= channelCount)
                return "";

            return channelNamespace[channel];
        }

        public string _ChannelName(int channel)
        {
            if (channel < 0 || channel >= channelCount)
                return "";

            return channelName[channel];
        }

        public string _ChannelSuffix(int channel)
        {
            if (channel < 0 || channel >= channelCount)
                return "";

            return channelSuffix[channel];
        }
    }
}
