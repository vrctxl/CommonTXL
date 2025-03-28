using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Texel
{
    public abstract class ControlBase : UdonSharpBehaviour
    {
        public const int COLOR_RED = 0;
        public const int COLOR_YELLOW = 1;
        public const int COLOR_GREEN = 2;
        public const int COLOR_CYAN = 3;
        public const int COLOR_WHITE = 4;
        [Obsolete("Use correctly typed constant")]
        public const int COLOR_WHTIE = 4;
        public const int COLOR_PURPLE = 5;
        public const int COLOR_1 = 6;
        public const int COLOR_2 = 7;
        public const int COLOR_3 = 8;
        public const int MAX_COLOR_COUNT = 9;

        [SerializeField] protected internal ControlColorMap colorMap;

        internal Color[] colorLookupActive;
        internal Color[] colorLookupInactive;
        internal Color[] colorLookupDisabled;
        internal Color[] colorLookupActiveLabel;
        internal Color[] colorLookupInactiveLabel;

        Image[] buttonBackground;
        Image[] buttonIcon;
        Text[] buttonText;
        TextMeshProUGUI[] buttonTMP;
        int[] buttonColorIndex;

        Slider[] sliders;
        InputField[] inputFields;

        bool init = false;
        bool controlsInit = false;

        protected virtual int ButtonCount { get; }
        protected virtual int SliderCount { get; }
        protected virtual int InputFieldCount { get; }

        public void _EnsureInit()
        {
            if (init)
                return;

            init = true;

            _PreInit();
            _InitControls();
            _Init();

            SendCustomEventDelayedFrames(nameof(_InternalPostInit), 1);
        }

        protected virtual void _PreInit() { }

        protected virtual void _Init() { }

        protected virtual void _PostInit() { }

        protected void _InitControls()
        {
            if (controlsInit)
                return;

            controlsInit = true;

            Color activeYellow = Color.HSVToRGB(60 / 360f, .8f, .9f);
            Color activeRed = Color.HSVToRGB(0, .7f, .9f);
            Color activeGreen = Color.HSVToRGB(100 / 360f, .8f, .9f);
            Color activeCyan = Color.HSVToRGB(180 / 360f, .8f, .9f);
            Color activeWhite = Color.HSVToRGB(0, 0, .9f);
            Color activePurple = Color.HSVToRGB(280 / 360f, .5f, 1f);

            Color activeYellowLabel = Color.HSVToRGB(60 / 360f, .8f, .5f);
            Color activeRedLabel = Color.HSVToRGB(0, .7f, .5f);
            Color activeGreenLabel = Color.HSVToRGB(110 / 360f, .8f, .5f);
            Color activeCyanLabel = Color.HSVToRGB(180 / 360f, .8f, .5f);
            Color activeWhiteLabel = Color.HSVToRGB(0, 0, .5f);
            Color activePurpleLabel = Color.HSVToRGB(280 / 360f, .5f, .5f);

            Color inactiveYellow = Color.HSVToRGB(60 / 360f, .35f, .5f);
            Color inactiveRed = Color.HSVToRGB(0, .35f, .5f);
            Color inactiveGreen = Color.HSVToRGB(110 / 360f, .35f, .5f);
            Color inactiveCyan = Color.HSVToRGB(180 / 360f, .40f, .5f);
            Color inactiveWhite = Color.HSVToRGB(0, 0, .5f);
            Color inactivePurple = Color.HSVToRGB(280 / 360f, .35f, .5f);

            Color inactiveYellowLabel = Color.HSVToRGB(60 / 360f, .35f, .2f);
            Color inactiveRedLabel = Color.HSVToRGB(0, .35f, .2f);
            Color inactiveGreenLabel = Color.HSVToRGB(110 / 360f, .35f, .2f);
            Color inactiveCyanLabel = Color.HSVToRGB(180 / 360f, .35f, .2f);
            Color inactiveWhiteLabel = Color.HSVToRGB(0, 0, .2f);
            Color inactivePurpleLabel = Color.HSVToRGB(280 / 360f, .35f, .2f);

            colorLookupActive = new Color[] { activeRed, activeYellow, activeGreen, activeCyan, activeWhite, activePurple, activeWhite, activeWhite, activeWhite };
            colorLookupInactive = new Color[] { inactiveRed, inactiveYellow, inactiveGreen, inactiveCyan, inactiveWhite, inactivePurple, inactiveWhite, inactiveWhite, inactiveWhite };
            colorLookupDisabled = new Color[] { inactiveRed, inactiveYellow, inactiveGreen, inactiveCyan, inactiveWhite, inactivePurple, inactiveWhite, inactiveWhite, inactiveWhite };

            colorLookupActiveLabel = new Color[] { activeRedLabel, activeYellowLabel, activeGreenLabel, activeCyanLabel, activeWhiteLabel, activePurpleLabel, activeWhiteLabel, activeWhiteLabel, activeWhiteLabel };
            colorLookupInactiveLabel = new Color[] { inactiveRedLabel, inactiveYellowLabel, inactiveGreenLabel, inactiveCyanLabel, inactiveWhiteLabel, inactivePurpleLabel, inactiveWhiteLabel, inactiveWhiteLabel, inactiveWhiteLabel };

            if (colorMap)
                colorMap._ApplyToControlBase(this);

            int buttonCount = ButtonCount;

            buttonColorIndex = new int[ButtonCount];
            buttonBackground = new Image[ButtonCount];
            buttonIcon = new Image[ButtonCount];
            buttonText = new Text[ButtonCount];
            buttonTMP = new TextMeshProUGUI[ButtonCount];

            sliders = new Slider[SliderCount];
            inputFields = new InputField[InputFieldCount];
        }

        public void _InternalPostInit()
        {
            _PostInit();
        }

        public void _SetColor(int colorIndex, Color bgActive, Color bgInactive, Color bgDisabled, Color labelActive, Color labelInactive)
        {
            if (colorIndex < 0 || colorIndex >= MAX_COLOR_COUNT)
                return;

            colorLookupActive[colorIndex] = bgActive;
            colorLookupInactive[colorIndex] = bgInactive;
            colorLookupDisabled[colorIndex] = bgDisabled;
            colorLookupActiveLabel[colorIndex] = labelActive;
            colorLookupInactiveLabel[colorIndex] = labelInactive;
        }

        protected void _DiscoverButton(int index, GameObject button, int colorIndex)
        {
            if (!button)
                return;

            buttonColorIndex[index] = colorIndex;
            buttonBackground[index] = button.GetComponent<Image>();
            int childCount = button.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = button.transform.GetChild(i);
                if (!buttonIcon[index])
                    buttonIcon[index] = child.GetComponent<Image>();
                if (!buttonText[index])
                    buttonText[index] = child.GetComponent<Text>();
                if (!buttonTMP[index])
                    buttonTMP[index] = child.GetComponent<TextMeshProUGUI>();
            }

            _SetButton(index, false);
        }

        protected void _SetButton(int buttonIndex, bool state)
        {
            if (buttonIndex < 0 || buttonIndex >= ButtonCount)
                return;

            int colorIndex = buttonColorIndex[buttonIndex];
            Image bg = buttonBackground[buttonIndex];
            if (bg)
                bg.color = state ? colorLookupActive[colorIndex] : colorLookupInactive[colorIndex];

            Image icon = buttonIcon[buttonIndex];
            if (icon)
                icon.color = state ? colorLookupActiveLabel[colorIndex] : colorLookupInactiveLabel[colorIndex];

            Text text = buttonText[buttonIndex];
            if (text)
                text.color = state ? colorLookupActiveLabel[colorIndex] : colorLookupInactiveLabel[colorIndex];

            TextMeshProUGUI tmp = buttonTMP[buttonIndex];
            if (tmp)
                tmp.color = state ? colorLookupActiveLabel[colorIndex] : colorLookupInactiveLabel[colorIndex];
        }

        protected void _SetButton(int buttonIndex, bool state, int colorIndex)
        {
            if (buttonIndex < 0 || buttonIndex >= ButtonCount)
                return;

            buttonColorIndex[buttonIndex] = colorIndex;
            _SetButton(buttonIndex, state);
        }

        protected void _SetButtonText(int buttonIndex, string value)
        {
            if (buttonIndex < 0 || buttonIndex >= ButtonCount)
                return;

            Text text = buttonText[buttonIndex];
            if (text)
                text.text = value;

            TextMeshProUGUI tmp = buttonTMP[buttonIndex];
            if (tmp)
                tmp.text = value;
        }

        protected void _DiscoverSlider(int index, GameObject slider)
        {
            if (!slider)
                return;

            sliders[index] = slider.GetComponent<Slider>();
        }

        protected Slider _GetSlider(int sliderIndex)
        {
            if (sliderIndex < 0 || sliderIndex >= SliderCount)
                return null;

            return sliders[sliderIndex];
        }

        protected void _DiscoverInputField(int index, GameObject inputField)
        {
            if (!inputField)
                return;

            inputFields[index] = inputField.GetComponent<InputField>();
        }

        protected InputField _GetInputField(int index)
        {
            if (index < 0 || index >= InputFieldCount)
                return null;

            return inputFields[index];
        }
    }
}
