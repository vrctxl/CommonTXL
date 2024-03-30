
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DebugState : EventBase
    {
        public string title = "Component State";
        public float updateInterval = 1;

        [Header("UI")]
        public Text[] titleText;
        public Text[] keyCol;
        public Text[] valCol;

        int index = 0;
        string[] keys = new string[50];
        string[] values = new string[50];
        string[] keyBuffer = new string[0];
        string[] valBuffer = new string[0];

        string[] handlerContexts;

        int currentContext = 0;

        public const int EVENT_UPDATE = 0;
        const int EVENT_COUNT = 1;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount => EVENT_COUNT;

        protected override void _Init()
        {
            if (Utilities.IsValid(titleText))
            {
                for (int i = 0; i < titleText.Length; i++)
                    titleText[i].text = title;
            }

            SendCustomEventDelayedSeconds("_Update", updateInterval);
        }

        protected override void _OnInitHandlers()
        {
            handlerContexts = new string[0];
        }

        public void _Update()
        {
            _Begin();
            if (handlerCount[EVENT_UPDATE] > 0)
            { 
                _UpdateHandlers();
                if (index > 0)
                    _End();
            }

            SendCustomEventDelayedSeconds("_Update", updateInterval);
        }

        public void _Begin()
        {
            index = 0;
        }

        public void _SetValue(string key, string value)
        {
            if (keys.Length == index)
            {
                keys = (string[])_SizeArray(keys, typeof(string), index + 50);
                values = (string[])_SizeArray(values, typeof(string), index + 50);
            }

            if (handlerCount[EVENT_UPDATE] > 1)
                keys[index] = $"{handlerContexts[currentContext]}:{key}";
            else
                keys[index] = key;
            values[index] = value;
            index += 1;
        }

        public void _End()
        {
            if (keyBuffer.Length != index)
            {
                keyBuffer = new string[index];
                valBuffer = new string[index];
            }

            Array.Copy(keys, keyBuffer, index);
            Array.Copy(values, valBuffer, index);

            string joinedKey = string.Join("\n", keyBuffer);
            string joinedVal = string.Join("\n", valBuffer);

            for (int i = 0; i < keyCol.Length; i++)
            {
                keyCol[i].text = joinedKey;
                valCol[i].text = joinedVal;
            }
        }

        [Obsolete("Use EventBase _Register, optionally with separate _SetContext call")]
        public void _Regsiter(Component handler, string eventName, string context)
        {
            _Register(EVENT_UPDATE, handler, eventName);
            _SetContext(handler, eventName, context);
        }

        override protected void _OnRegister(int eventIndex, int handlerIndex)
        {
            if (eventIndex == EVENT_UPDATE)
                handlerContexts = (string[])_AddElement(handlerContexts, $"Source {handlerIndex}", typeof(string));
        }

        protected override void _OnUnregister(int eventIndex, int handlerIndex)
        {
            if (eventIndex == EVENT_UPDATE)
                handlerContexts = (string[])_RemoveElement(handlerContexts, handlerIndex, typeof(string));
        }

        public void _SetContext(Component handler, string eventName, string context)
        {
            int index = _FindHandlerIndex(EVENT_UPDATE, handler, eventName);
            if (index == -1 || index >= handlerContexts.Length)
                return;

            handlerContexts[index] = context;
        }

        void _UpdateHandlers()
        {
            for (int i = 0; i < handlerCount[EVENT_UPDATE]; i++)
            {
                currentContext = i;
                UdonBehaviour script = (UdonBehaviour)handlers[EVENT_UPDATE][i];
                script.SendCustomEvent(handlerEvents[EVENT_UPDATE][i]);
            }
        }

        Array _SizeArray(Array arr, Type type, int size)
        {
            Array newArr;

            if (Utilities.IsValid(arr))
            {
                newArr = Array.CreateInstance(type, size);
                Array.Copy(arr, newArr, Math.Min(arr.Length, size));
            }
            else
                newArr = Array.CreateInstance(type, size);

            return newArr;
        }
    }
}
