
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("com.texelsaur.common.Editor")]

namespace Texel
{
    public abstract class DebugEventBase : EventBase
    {
        [SerializeField] protected internal DebugLogProvider logProvider;
        [SerializeField] protected internal bool includeEventLogging = false;
        [SerializeField] protected internal DebugState debugState;

        protected string componentName = "";
        protected string componentNamespace = "CommonTXL";

        protected int logChannel = -1;
        int de_logEventChannel = -1;

        protected bool _usingError;
        protected bool _usingWarning;
        protected bool _usingDebug;
        protected bool _usingVerbose;
        protected bool _usingTrace;

        protected override void _OnInitDebug()
        {
            _RefreshDebugFlags();

            if (UsesDebugState && debugState)
                DebugState = debugState;
        }

        public virtual DebugLogProvider LogProvider
        {
            get { return logProvider; }
            set
            {
                logProvider = value;
                _RefreshDebugFlags();
            }
        }

        public bool EventLogging
        {
            get { return includeEventLogging; }
            set
            {
                includeEventLogging = value;
                _RefreshDebugFlags();
            }
        }

        public virtual void _SetComponentName(string componentName, string componentNamespace)
        {
            this.componentName = componentName;
            this.componentNamespace = componentNamespace;

            _RefreshDebugFlags();
        }

        protected virtual void _RefreshDebugFlags()
        {
            bool useDebug = logProvider;
            eb_useEventDebug = useDebug && includeEventLogging;

            logChannel = _RegisterLogChannel(null);
            de_logEventChannel = _RegisterLogChannel("event");

            int level = (int)(useDebug ? logProvider.MinLogLevel : DebugLogLevel.Info);
            _usingError = useDebug && (level <= (int)DebugLogLevel.Error);
            _usingWarning = useDebug && (level <= (int)DebugLogLevel.Warning);
            _usingDebug = useDebug && (level <= (int)DebugLogLevel.Info);
            _usingVerbose = useDebug && (level <= (int)DebugLogLevel.Verbose);
            _usingTrace = useDebug && (level <= (int)DebugLogLevel.Trace);
        }

        protected int _RegisterLogChannel(string suffix)
        {
            if (!logProvider)
                return -1;

            return logProvider._RegisterChannel(componentNamespace, componentName, suffix);
        }

        protected override void _EventLogInfo(string message)
        {
            logProvider._WriteVerbose(de_logEventChannel, message);
        }

        protected override void _EventLogError(string message)
        {
            if (_usingError)
                logProvider._WriteError(de_logEventChannel, message);
            else
                Debug.LogError(message);
        }

        protected void _DebugLog(string message)
        {
            if (_usingDebug)
                logProvider._WriteInfo(logChannel, message);
        }

        protected void _DebugWarning(string message)
        {
            if (_usingWarning)
                logProvider._WriteWarning(logChannel, message);
        }

        protected void _DebugError(string message)
        {
            if (_usingError)
                logProvider._WriteError(logChannel, message);
            else
                Debug.LogError(message);
        }

        protected void _DebugVerbose(string message)
        {
            if (_usingVerbose)
                logProvider._WriteVerbose(logChannel, message);
        }

        protected void _DebugTrace(string message)
        {
            if (_usingTrace)
                logProvider._WriteTrace(logChannel, message);
        }

        public virtual bool UsesDebugState 
        {
            get { return false; }
        }

        public DebugState DebugState
        {
            get { return debugState; }
            set
            {
                if (debugState)
                {
                    debugState._Unregister(DebugState.EVENT_UPDATE, this, nameof(_InternalUpdateDebugState));
                    debugState = null;
                }

                debugState = value;
                if (!debugState)
                    return;

                debugState._Register(DebugState.EVENT_UPDATE, this, nameof(_InternalUpdateDebugState));
                debugState._SetContext(this, nameof(_InternalUpdateDebugState), componentName);
            }
        }

        public void _InternalUpdateDebugState()
        {
            _UpdateDebugState();
        }

        protected virtual void _UpdateDebugState() { }
    }
}
