
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public abstract class EventBase : UdonSharpBehaviour
    {
        protected int[] handlerCount;
        protected Component[][] handlers;
        protected string[][] handlerEvents;
        protected string[][] handlerArg1;
        protected string[][] handlerArg2;

        bool init = false;
        bool handlersInit = false;
        int handlerUpdateLevel = 0;

        protected DebugLog eventDebugLog;

        protected virtual int EventCount { get; }

        public void _EnsureInit()
        {
            if (init)
                return;

            init = true;

            _PreInit();
            _InitHandlers();
            _Init();

            SendCustomEventDelayedFrames(nameof(_InternalPostInit), 1);
        }

        protected virtual void _PreInit() { }

        protected virtual void _Init() { }

        protected virtual void _PostInit() { }

        public void _InternalPostInit()
        {
            _PostInit();
        }

        protected bool Initialized
        {
            get { return init; }
        }

        protected void _InitHandlers()
        {
            if (handlersInit)
                return;

            handlersInit = true;
            int eventCount = EventCount;

            handlerCount = new int[eventCount];
            handlers = new Component[eventCount][];
            handlerEvents = new string[eventCount][];
            handlerArg1 = new string[eventCount][];
            handlerArg2 = new string[eventCount][];

            for (int i = 0; i < eventCount; i++)
            {
                handlers[i] = new Component[0];
                handlerEvents[i] = new string[0];
                handlerArg1[i] = new string[0];
                handlerArg2[i] = new string[0];
            }

            _OnInitHandlers();
        }

        protected virtual void _OnInitHandlers() { }

        public void _Register(int eventIndex, Component handler, string eventName, params string[] args)
        {
            if (!Utilities.IsValid(handler) || !Utilities.IsValid(eventName))
                return;

            _InitHandlers();

            if (eventIndex < 0 || eventIndex >= handlerCount.Length)
            {
                Debug.LogError($"GameObject {gameObject.name} tried to register out-of-range event {eventIndex} from origin {handler.gameObject.name}:{eventName}!");
                return;
            }

            if (handlerUpdateLevel > 0)
            {
                Debug.LogError($"GameObject {gameObject.name} tried to register event {eventIndex} from origin {handler.gameObject.name}:{eventName} while handler update in progress!");
                return;
            }

            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                if (handlers[eventIndex][i] == handler && handlerEvents[eventIndex][i] == eventName)
                    return;
            }

            handlers[eventIndex] = (Component[])_AddElement(handlers[eventIndex], handler, typeof(Component));
            handlerEvents[eventIndex] = (string[])_AddElement(handlerEvents[eventIndex], eventName, typeof(string));

            handlerArg1[eventIndex] = (string[])_AddElement(handlerArg1[eventIndex], "", typeof(string));
            handlerArg2[eventIndex] = (string[])_AddElement(handlerArg2[eventIndex], "", typeof(string));

            if (Utilities.IsValid(args) && args.Length >= 1)
                handlerArg1[eventIndex][handlerArg1[eventIndex].Length - 1] = args[0];
            if (Utilities.IsValid(args) && args.Length >= 2)
                handlerArg2[eventIndex][handlerArg2[eventIndex].Length - 1] = args[1];

            handlerCount[eventIndex] += 1;

            _OnRegister(eventIndex, handlerCount[eventIndex] - 1);
        }

        protected virtual void _OnRegister(int eventIndex, int handlerIndex)
        {

        }

        public void _Unregister(int eventIndex, Component handler, string eventName)
        {
            if (!Utilities.IsValid(handler) || !Utilities.IsValid(eventName))
                return;

            _InitHandlers();

            if (eventIndex < 0 || eventIndex >= handlerCount.Length)
            {
                Debug.LogError($"GameObject {gameObject.name} tried to unregister out-of-range event {eventIndex} from origin {handler.gameObject.name}:{eventName}!");
                return;
            }

            if (handlerUpdateLevel > 0)
            {
                Debug.LogError($"GameObject {gameObject.name} tried to unregister event {eventIndex} from origin {handler.gameObject.name}:{eventName} while handler update in progress!");
                return;
            }

            int index = _FindHandlerIndex(eventIndex, handler, eventName);
            if (index == -1)
                return;

            handlers[eventIndex] = (Component[])_RemoveElement(handlers[eventIndex], index, typeof(Component));
            handlerEvents[eventIndex] = (string[])_RemoveElement(handlerEvents[eventIndex], index, typeof(string));

            handlerArg1[eventIndex] = (string[])_RemoveElement(handlerArg1[eventIndex], index, typeof(string));
            handlerArg2[eventIndex] = (string[])_RemoveElement(handlerArg2[eventIndex], index, typeof(string));

            handlerCount[eventIndex] -= 1;

            _OnUnregister(eventIndex, index);
        }

        protected virtual void _OnUnregister(int eventIndex, int handlerIndex)
        {

        }

        protected void _UpdateHandlers(int eventIndex)
        {
            if (handlerCount == null)
                return;

            if (eventIndex < 0 || eventIndex >= handlerCount.Length)
            {
                string errStr = $"GameObject {gameObject.name} tried to trigger out-of-range event {eventIndex}!";
                Debug.LogError(errStr);
                if (eventDebugLog)
                    eventDebugLog._Write("  event", errStr);
                return;
            }

            if (handlerUpdateLevel == 0)
                _InternalUpdateHandlers1(eventIndex);
            else if (handlerUpdateLevel == 1)
                _InternalUpdateHandlers2(eventIndex);
            else if (handlerUpdateLevel == 2)
                _InternalUpdateHandlers3(eventIndex);
            else
            {
                string errStr = $"[{handlerUpdateLevel}] [{gameObject.name}:{eventIndex}] [X] -> Event call depth exceeded!";
                Debug.LogError(errStr);
                if (eventDebugLog)
                    eventDebugLog._Write("  event", errStr);
            }
        }

        public void _InternalUpdateHandlers1(int eventIndex)
        {
            handlerUpdateLevel += 1;
            int count = handlerCount[eventIndex];
            for (int i = 0; i < count; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[eventIndex][i];
                if (!script)
                {
                    Debug.LogError($"EventBase [{gameObject.name}:{eventIndex}] registered handler missing or destroyed");
                    continue;
                }
                if (eventDebugLog)
                    eventDebugLog._Write("  event", $"[{handlerUpdateLevel}] [{gameObject.name}:{eventIndex}] [{i + 1}/{count}] -> {script.gameObject.name}:{handlerEvents[eventIndex][i]}");
                script.SendCustomEvent(handlerEvents[eventIndex][i]);
            }
            handlerUpdateLevel -= 1;
        }

        public void _InternalUpdateHandlers3(int eventIndex)
        {
            handlerUpdateLevel += 1;
            int count = handlerCount[eventIndex];
            for (int i = 0; i < count; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[eventIndex][i];
                if (!script)
                {
                    Debug.LogError($"EventBase [{gameObject.name}:{eventIndex}] registered handler missing or destroyed");
                    continue;
                }
                if (eventDebugLog)
                    eventDebugLog._Write("  event", $"[{handlerUpdateLevel}] [{gameObject.name}:{eventIndex}] [{i + 1}/{count}] -> {script.gameObject.name}:{handlerEvents[eventIndex][i]}");
                script.SendCustomEvent(handlerEvents[eventIndex][i]);
            }
            handlerUpdateLevel -= 1;
        }

        public void _InternalUpdateHandlers2(int eventIndex)
        {
            handlerUpdateLevel += 1;
            int count = handlerCount[eventIndex];
            for (int i = 0; i < count; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[eventIndex][i];
                if (!script)
                {
                    Debug.LogError($"EventBase [{gameObject.name}:{eventIndex}] registered handler missing or destroyed");
                    continue;
                }
                if (eventDebugLog)
                    eventDebugLog._Write("  event", $"[{handlerUpdateLevel}] [{gameObject.name}:{eventIndex}] [{i + 1}/{count}] -> {script.gameObject.name}:{handlerEvents[eventIndex][i]}");
                script.SendCustomEvent(handlerEvents[eventIndex][i]);
            }
            handlerUpdateLevel -= 1;
        }

        [RecursiveMethod]
        protected void _UpdateHandlers(int eventIndex, object arg1)
        {
            if (handlerCount == null)
                return;

            if (eventIndex < 0 || eventIndex >= handlerCount.Length)
            {
                Debug.LogError($"GameObject {gameObject.name} tried to trigger out-of-range event {eventIndex}!");
                return;
            }

            handlerUpdateLevel += 1;
            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[eventIndex][i];
                string argName = handlerArg1[eventIndex][i];
                if (argName != null && argName != "")
                    script.SetProgramVariable(argName, arg1);

                script.SendCustomEvent(handlerEvents[eventIndex][i]);
            }
            handlerUpdateLevel -= 1;
        }

        protected Array _AddElement(Array arr, object elem, Type type)
        {
            Array newArr;
            int count = 0;

            if (Utilities.IsValid(arr))
            {
                count = arr.Length;
                newArr = Array.CreateInstance(type, count + 1);
                Array.Copy(arr, newArr, count);
            }
            else
                newArr = Array.CreateInstance(type, 1);

            newArr.SetValue(elem, count);
            return newArr;
        }

        protected Array _RemoveElement(Array arr, int index, Type type)
        {
            if (index < 0 || index >= arr.Length)
                return arr;

            Array newArr;

            if (Utilities.IsValid(arr))
            {
                int newCount = arr.Length - 1;
                newArr = Array.CreateInstance(type, newCount);
                Array.Copy(arr, 0, newArr, 0, index);
                if (index < newCount)
                    Array.Copy(arr, index + 1, newArr, index, newCount - index);
            }
            else
                newArr = Array.CreateInstance(type, 0);

            return newArr;
        }

        protected int _FindHandlerIndex(int eventIndex, Component handler, string eventName)
        {
            int index = -1;
            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                if (handlers[eventIndex][i] == handler && handlerEvents[eventIndex][i] == eventName)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }
    }
}
