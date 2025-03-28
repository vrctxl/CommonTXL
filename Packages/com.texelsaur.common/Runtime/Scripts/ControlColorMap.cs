
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    public enum ControlColorBank
    {
        BUTTON_BG_ACTIVE,
        BUTTON_BG_INACTIVE,
        BUTTON_BG_DISABLED,
        BUTTON_LABEL_ACTIVE,
        BUTTON_LABEL_INACTIVE,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ControlColorMap : UdonSharpBehaviour
    {
        [SerializeField] internal int[] buttonColorIndex;
        [SerializeField] internal Color[] buttonColorActiveBG;
        [SerializeField] internal Color[] buttonColorInactiveBG;
        [SerializeField] internal Color[] buttonColorDisabledBG;
        [SerializeField] internal Color[] buttonColorActiveLabel;
        [SerializeField] internal Color[] buttonColorInactiveLabel;

        Color[][] colorBanks;
        int[][] entityMap;
        int[][] reverseEntityMap;

        int[] reverseButtonMap;

        private void Start()
        {
            if (colorBanks == null)
                _Init();
        }

        void _Init()
        {
            colorBanks = new Color[5][];
            entityMap = new int[5][];
            reverseEntityMap = new int[5][];

            colorBanks[0] = buttonColorActiveBG;
            colorBanks[1] = buttonColorInactiveBG;
            colorBanks[2] = buttonColorDisabledBG;
            colorBanks[3] = buttonColorActiveLabel;
            colorBanks[4] = buttonColorInactiveLabel;

            for (int i = 0; i < 5; i++)
                entityMap[i] = buttonColorIndex;

            int[] reverseButtonMap = new int[ControlBase.MAX_COLOR_COUNT];
            for (int i = 0; i < reverseButtonMap.Length; i++)
                reverseButtonMap[i] = -1;

            for (int i = 0; i < buttonColorIndex.Length; i++)
            {
                int index = buttonColorIndex[i];
                if (index >= 0 && index < reverseButtonMap.Length)
                    reverseButtonMap[index] = i;
            }

            for (int i = 0; i < 5; i++)
                reverseEntityMap[i] = reverseButtonMap;
        }

        public Color _GetColor(ControlColorBank colorBank, int index)
        {
            if (colorBanks == null)
                _Init();

            Color[] bank = colorBanks[(int)colorBank];
            int[] map = reverseEntityMap[(int)colorBank];

            if (index < 0 || index >= map.Length)
                return Color.black;

            int entityIndex = map[index];
            if (entityIndex < 0 || entityIndex >= bank.Length)
                return Color.black;

            return bank[entityIndex];
        }

        public void _ApplyToControlBase(ControlBase control)
        {
            if (colorBanks == null)
                _Init();

            for (int i = 0; i < buttonColorIndex.Length; i++)
            {
                int index = buttonColorIndex[i];
                control.colorLookupActive[index] = buttonColorActiveBG[i];
                control.colorLookupInactive[index] = buttonColorInactiveBG[i];
                control.colorLookupDisabled[index] = buttonColorDisabledBG[i];
                control.colorLookupActiveLabel[index] = buttonColorActiveLabel[i];
                control.colorLookupInactiveLabel[index] = buttonColorInactiveLabel[i];
            }
        }
    }
}
