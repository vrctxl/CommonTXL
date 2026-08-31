
using System;
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[assembly: InternalsVisibleTo("com.texelsaur.common.Editor")]

namespace Texel
{
    public abstract class EventBase : UdonSharpBehaviour
    {
        [Obsolete("Use LogProvider from DebugEventBase or override log virtuals")]
        [HideInInspector] protected DebugLog eventDebugLog;

        protected int[] handlerCount;
        protected Component[][] handlers;
        protected string[][] handlerEvents;
        protected string[][] handlerArg1;

        const int MAX_EVENT_DEPTH = 3;

        const int ERR_RANGE = 0;
        const int ERR_DEPTH = 1;
        const int ERR_HANDLER = 2;

        int[] eb_depthEvent;
        int[] eb_depthIndex;
        int[] eb_depthCount;
        object[] eb_depthArg;
        bool[] eb_depthHasArg;

        bool eb_eventInit = false;
        bool eb_eventInitDone = false;
        bool eb_postInitDone = false;
        bool eb_handlersInit = false;
        bool eb_eventSuppress = false;
        bool eb_blockingEvents = true;
        bool eb_eventDispatching = false;

        int eb_handlerUpdateLevel = 0;
        int eb_eventCount = 0;

        protected bool eb_useEventDebug;

        protected virtual int EventCount
        {
            get { return 0; }
        }

        public void _EnsureInit()
        {
            if (eb_eventInit)
                return;

            eb_eventInit = true;

            _PreInit();
            _OnInitDebug();
            _InitHandlers();
            _Init();

            eb_eventInitDone = true;

            SendCustomEventDelayedFrames(nameof(_InternalPostInit), 1);
        }

        protected virtual void _PreInit() { }

        protected virtual void _OnInitDebug() { }

        protected virtual void _Init() { }

        protected virtual void _PostInit() { }

        public void _InternalPostInit()
        {
            _PostInit();

            eb_postInitDone = true;
        }

        public bool Initialized
        {
            get { return eb_eventInitDone; }
        }

        public bool PostInitialized
        {
            get { return eb_postInitDone; }
        }

        public bool SuppressEvents
        {
            get { return eb_eventSuppress; }
            set
            {
                eb_eventSuppress = value;
                eb_blockingEvents = value || !eb_handlersInit;
            }
        }

        protected void _InitHandlers()
        {
            if (eb_handlersInit)
                return;

            eb_handlersInit = true;
            eb_eventCount = EventCount;

            handlerCount = new int[eb_eventCount];
            handlers = new Component[eb_eventCount][];
            handlerEvents = new string[eb_eventCount][];
            handlerArg1 = new string[eb_eventCount][];

            for (int i = 0; i < eb_eventCount; i++)
            {
                handlers[i] = new Component[0];
                handlerEvents[i] = new string[0];
                handlerArg1[i] = new string[0];
            }

            int depthSlots = MAX_EVENT_DEPTH + 1;

            eb_depthEvent = new int[depthSlots];
            eb_depthIndex = new int[depthSlots];
            eb_depthCount = new int[depthSlots];
            eb_depthArg = new object[depthSlots];
            eb_depthHasArg = new bool[depthSlots];

            eb_blockingEvents = eb_eventSuppress;

            _OnInitHandlers();
        }

        protected virtual void _OnInitHandlers() { }

        public void _Register(int eventIndex, Component handler, string eventName, params string[] args)
        {
            if (_EB_CheckRegistration(eventIndex, handler, eventName, true) != -1)
                return;

            handlers[eventIndex] = (Component[])UtilityTxl.ArrayAddElement(handlers[eventIndex], handler, typeof(Component));
            handlerEvents[eventIndex] = (string[])UtilityTxl.ArrayAddElement(handlerEvents[eventIndex], eventName, typeof(string));

            string arg1 = "";
            if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                arg1 = args[0];

            handlerArg1[eventIndex] = (string[])UtilityTxl.ArrayAddElement(handlerArg1[eventIndex], arg1, typeof(string));
            handlerCount[eventIndex] += 1;

            _OnRegister(eventIndex, handlerCount[eventIndex] - 1);
        }

        protected virtual void _OnRegister(int eventIndex, int handlerIndex)
        {

        }

        public void _Unregister(int eventIndex, Component handler, string eventName)
        {
            int index = _EB_CheckRegistration(eventIndex, handler, eventName, false);
            if (index < 0)
                return;

            handlers[eventIndex] = (Component[])UtilityTxl.ArrayRemoveElement(handlers[eventIndex], index, typeof(Component));
            handlerEvents[eventIndex] = (string[])UtilityTxl.ArrayRemoveElement(handlerEvents[eventIndex], index, typeof(string));
            handlerArg1[eventIndex] = (string[])UtilityTxl.ArrayRemoveElement(handlerArg1[eventIndex], index, typeof(string));
            handlerCount[eventIndex] -= 1;

            _OnUnregister(eventIndex, index);
        }

        protected virtual void _OnUnregister(int eventIndex, int handlerIndex)
        {

        }

        private int _EB_CheckRegistration(int eventIndex, Component handler, string eventName, bool registering)
        {
            if (!Utilities.IsValid(handler) || string.IsNullOrEmpty(eventName))
                return -2;

            _InitHandlers();

            if (eventIndex < 0 || eventIndex >= eb_eventCount)
            {
                _EB_RegistrationError(eventIndex, handler, eventName, registering, false);
                return -2;
            }

            if (eb_eventDispatching)
            {
                _EB_RegistrationError(eventIndex, handler, eventName, registering, true);
                return -2;
            }

            return _FindHandlerIndex(eventIndex, handler, eventName);
        }

        protected void _UpdateHandlers(int eventIndex)
        {
            _InternalUpdateHandlers(eventIndex, null, false);
        }

        protected void _UpdateHandlers(int eventIndex, object arg1)
        {
            _InternalUpdateHandlers(eventIndex, arg1, true);
        }

        private void _InternalUpdateHandlers(int eventIndex, object arg1, bool hasArg)
        {
            if (eb_blockingEvents)
                return;

            if (eventIndex >= eb_eventCount)
            {
                _EB_DispatchError(eventIndex, 0, ERR_RANGE);
                return;
            }

            int count = handlerCount[eventIndex];
            if (count == 0)
                return;

            if (eb_eventDispatching)
                _EB_DispatchNested(eventIndex, count, arg1, hasArg);
            else
                _EB_DispatchFast(eventIndex, count, arg1, hasArg);
        }

        private void _EB_DispatchFast(int eventIndex, int count, object arg1, bool hasArg)
        {
            eb_eventDispatching = true;

            Component[] handlerList = handlers[eventIndex];
            string[] eventList = handlerEvents[eventIndex];
            string[] argList = null;
            if (hasArg)
                argList = handlerArg1[eventIndex];

            for (int i = 0; i < count; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlerList[i];
                if (script)
                {
                    if (hasArg)
                    {
                        string argName = argList[i];
                        if (argName != "")
                            script.SetProgramVariable(argName, arg1);
                    }

                    string eventName = eventList[i];
                    if (eb_useEventDebug)
                        _EB_DispatchLog(1, eventIndex, i, count, script, eventName);

                    script.SendCustomEvent(eventName);
                }
                else
                    _EB_DispatchError(eventIndex, 1, ERR_HANDLER);
            }

            eb_eventDispatching = false;
        }

        private void _EB_DispatchNested(int eventIndex, int count, object arg1, bool hasArg)
        {
            int level = eb_handlerUpdateLevel;
            if (level >= MAX_EVENT_DEPTH)
            {
                _EB_DispatchError(eventIndex, level + 1, ERR_DEPTH);
                return;
            }

            level += 1;
            eb_handlerUpdateLevel = level;

            // [RecursiveMethod] did not seem reliable for reentrancy, so we maintain our own local stack
            eb_depthEvent[level] = eventIndex;
            eb_depthIndex[level] = 0;
            eb_depthCount[level] = count;
            eb_depthHasArg[level] = hasArg;

            if (hasArg)
                eb_depthArg[level] = arg1;

            while (eb_depthIndex[eb_handlerUpdateLevel] < eb_depthCount[eb_handlerUpdateLevel])
            {
                int lvl = eb_handlerUpdateLevel;
                int e = eb_depthEvent[lvl];
                int i = eb_depthIndex[lvl];

                eb_depthIndex[lvl] = i + 1;

                UdonBehaviour script = (UdonBehaviour)handlers[e][i];
                if (script)
                {
                    if (eb_depthHasArg[lvl])
                    {
                        string argName = handlerArg1[e][i];
                        if (argName != "")
                            script.SetProgramVariable(argName, eb_depthArg[lvl]);
                    }

                    string eventName = handlerEvents[e][i];
                    if (eb_useEventDebug)
                        _EB_DispatchLog(lvl + 1, e, i, eb_depthCount[lvl], script, eventName);

                    script.SendCustomEvent(eventName);
                }
                else
                    _EB_DispatchError(e, lvl + 1, ERR_HANDLER);
            }

            eb_handlerUpdateLevel -= 1;
        }

        protected int _FindHandlerIndex(int eventIndex, Component handler, string eventName)
        {
            Component[] handlerList = handlers[eventIndex];
            string[] eventList = handlerEvents[eventIndex];
            int count = handlerCount[eventIndex];

            for (int i = 0; i < count; i++)
            {
                if (handlerList[i] == handler && eventList[i] == eventName)
                    return i;
            }

            return -1;
        }

        protected virtual void _EventLogInfo(string message) { }

        protected virtual void _EventLogError(string message)
        {
            Debug.LogError(message);
        }

        private void _EB_DispatchLog(int level, int eventIndex, int index, int count, UdonBehaviour script, string eventName)
        {
            _EventLogInfo($"[{level}] [{gameObject.name}:{eventIndex}] [{index + 1}/{count}] -> {script.gameObject.name}:{eventName}");
        }

        private void _EB_DispatchError(int eventIndex, int level, int code)
        {
            string detail;
            if (code == ERR_RANGE)
                detail = "out-of-range event";
            else if (code == ERR_DEPTH)
                detail = "call depth exceeded";
            else
                detail = "handler missing or destroyed";

            _EventLogError($"EventBase [{gameObject.name}:{eventIndex}] [{level}] {detail}");
        }

        private void _EB_RegistrationError(int eventIndex, Component handler, string eventName, bool registering, bool inUpdate)
        {
            string action = registering ? "register" : "unregister";
            string detail = inUpdate ? " while handler update in progress" : ", out-of-range event index";

            _EventLogError($"GameObject {gameObject.name} tried to {action} event {eventIndex} from origin {handler.gameObject.name}:{eventName}{detail}!");
        }
    }
}
