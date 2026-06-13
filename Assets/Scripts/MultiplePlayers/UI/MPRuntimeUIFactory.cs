using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MultiplePlayers
{
    internal static class MPRuntimeUIFactory
    {
        private static Font fallbackFont;

        public static Canvas EnsureOverlayCanvas(string name, int sortingOrder = 0)
        {
            Canvas[] canvases =
                UnityEngine.Object.FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (Canvas existingCanvas in canvases)
            {
                if (existingCanvas != null && existingCanvas.name == name)
                {
                    return existingCanvas;
                }
            }

            GameObject root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        public static GameObject CreatePanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            out CanvasGroup canvasGroup)
        {
            GameObject panel = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));

            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = panel.GetComponent<Image>();
            image.color = color;

            canvasGroup = panel.GetComponent<CanvasGroup>();
            return panel;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Text label = go.GetComponent<Text>();
            label.font = GetFallbackFont();
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string labelText,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color? color = null)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));

            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = buttonObject.GetComponent<Image>();
            image.color = color ?? new Color(0.2f, 0.45f, 0.7f, 0.95f);

            CreateText(
                buttonObject.transform,
                "Label",
                labelText,
                24,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            return buttonObject.GetComponent<Button>();
        }

        public static InputField CreateInputField(
            Transform parent,
            string name,
            string defaultText,
            string placeholder,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject fieldObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(InputField));

            fieldObject.transform.SetParent(parent, false);

            RectTransform rect = fieldObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image background = fieldObject.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.95f);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(fieldObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 6f);
            textRect.offsetMax = new Vector2(-12f, -6f);

            Text text = textObject.GetComponent<Text>();
            text.font = GetFallbackFont();
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.black;
            text.supportRichText = false;

            GameObject placeholderObject =
                new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderObject.transform.SetParent(fieldObject.transform, false);

            RectTransform placeholderRect =
                placeholderObject.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(12f, 6f);
            placeholderRect.offsetMax = new Vector2(-12f, -6f);

            Text placeholderText = placeholderObject.GetComponent<Text>();
            placeholderText.font = GetFallbackFont();
            placeholderText.fontSize = 24;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.color = new Color(0.25f, 0.25f, 0.25f, 0.7f);
            placeholderText.text = placeholder;
            placeholderText.supportRichText = false;

            InputField inputField = fieldObject.GetComponent<InputField>();
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            inputField.text = defaultText;
            inputField.lineType = InputField.LineType.SingleLine;
            return inputField;
        }

        public static Slider CreateSlider(
            Transform parent,
            string name,
            float minValue,
            float maxValue,
            float value,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject sliderObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Slider));

            sliderObject.transform.SetParent(parent, false);

            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            RectTransform background = CreateSliderGraphic(
                sliderObject.transform,
                "Background",
                new Color(0.12f, 0.16f, 0.2f, 0.95f),
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, 8f),
                new Vector2(0f, -8f));

            RectTransform fillArea = new GameObject("Fill Area", typeof(RectTransform))
                .GetComponent<RectTransform>();
            fillArea.SetParent(sliderObject.transform, false);
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = new Vector2(8f, 8f);
            fillArea.offsetMax = new Vector2(-8f, -8f);

            RectTransform fill = CreateSliderGraphic(
                fillArea,
                "Fill",
                new Color(0.2f, 0.45f, 0.7f, 0.95f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            RectTransform handleArea = new GameObject("Handle Slide Area", typeof(RectTransform))
                .GetComponent<RectTransform>();
            handleArea.SetParent(sliderObject.transform, false);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);

            RectTransform handle = CreateSliderGraphic(
                handleArea,
                "Handle",
                Color.white,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(-8f, -14f),
                new Vector2(8f, 14f));

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = Mathf.Clamp(value, minValue, maxValue);
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.fillRect = fill;
            slider.handleRect = handle;

            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.raycastTarget = true;
            return slider;
        }

        public static Toggle CreateToggle(
            Transform parent,
            string name,
            string labelText,
            bool isOn,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject toggleObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Toggle));

            toggleObject.transform.SetParent(parent, false);

            RectTransform rect = toggleObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            RectTransform background = CreateSliderGraphic(
                toggleObject.transform,
                "Background",
                new Color(0.12f, 0.16f, 0.2f, 0.95f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, -13f),
                new Vector2(26f, 13f));

            RectTransform checkmark = CreateSliderGraphic(
                background,
                "Checkmark",
                new Color(0.2f, 0.65f, 0.35f, 0.95f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-8f, -8f),
                new Vector2(8f, 8f));

            CreateText(
                toggleObject.transform,
                "Label",
                labelText,
                22,
                TextAnchor.MiddleLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(36f, 0f),
                Vector2.zero);

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            toggle.isOn = isOn;
            return toggle;
        }

        public static Dropdown CreateDropdown(
            Transform parent,
            string name,
            IReadOnlyList<string> options,
            int value,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject dropdownObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Dropdown));

            dropdownObject.transform.SetParent(parent, false);

            RectTransform rect = dropdownObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = dropdownObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.16f, 0.2f, 0.95f);

            Text label = CreateText(
                dropdownObject.transform,
                "Label",
                string.Empty,
                22,
                TextAnchor.MiddleLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(12f, 0f),
                new Vector2(-36f, 0f));

            Text arrow = CreateText(
                dropdownObject.transform,
                "Arrow",
                "v",
                22,
                TextAnchor.MiddleCenter,
                new Vector2(1f, 0f),
                Vector2.one,
                new Vector2(-32f, 0f),
                Vector2.zero);

            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.captionText = label;
            dropdown.itemText = CreateDropdownTemplate(dropdownObject.transform, out RectTransform template);
            dropdown.template = template;
            dropdown.targetGraphic = image;
            dropdown.options.Clear();

            if (options != null)
            {
                foreach (string option in options)
                {
                    dropdown.options.Add(new Dropdown.OptionData(option));
                }
            }

            dropdown.value = Mathf.Clamp(value, 0, Mathf.Max(0, dropdown.options.Count - 1));
            dropdown.RefreshShownValue();
            arrow.raycastTarget = false;
            return dropdown;
        }

        public static RectTransform CreateVerticalContainer(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            float spacing,
            TextAnchor childAlignment = TextAnchor.UpperCenter)
        {
            GameObject container = new GameObject(
                name,
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));

            container.transform.SetParent(parent, false);

            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.childAlignment = childAlignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return rect;
        }

        public static GridLayoutGroup CreateGrid(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Vector2 cellSize,
            Vector2 spacing,
            int columns)
        {
            GameObject container = new GameObject(
                name,
                typeof(RectTransform),
                typeof(GridLayoutGroup));

            container.transform.SetParent(parent, false);

            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(8, 8, 8, 8);
            return grid;
        }

        public static LayoutElement AddFixedHeight(Button button, float height)
        {
            LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            return element;
        }

        public static LayoutElement AddFixedHeight(InputField inputField, float height)
        {
            LayoutElement element = inputField.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            return element;
        }

        public static LayoutElement AddFixedHeight(Slider slider, float height)
        {
            LayoutElement element = slider.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            return element;
        }

        public static LayoutElement AddFixedHeight(Toggle toggle, float height)
        {
            LayoutElement element = toggle.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            return element;
        }

        public static LayoutElement AddFixedHeight(Dropdown dropdown, float height)
        {
            LayoutElement element = dropdown.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            return element;
        }

        public static void AddClick(Button button, Action action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.AddListener(() => action());
        }

        private static Font GetFallbackFont()
        {
            if (fallbackFont == null)
            {
                fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return fallbackFont;
        }

        private static RectTransform CreateSliderGraphic(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject graphicObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            graphicObject.transform.SetParent(parent, false);

            RectTransform rect = graphicObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = graphicObject.GetComponent<Image>();
            image.color = color;
            return rect;
        }

        private static Text CreateDropdownTemplate(Transform parent, out RectTransform template)
        {
            GameObject templateObject = new GameObject(
                "Template",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect));
            templateObject.transform.SetParent(parent, false);

            template = templateObject.GetComponent<RectTransform>();
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.offsetMin = new Vector2(0f, -160f);
            template.offsetMax = Vector2.zero;

            Image templateImage = templateObject.GetComponent<Image>();
            templateImage.color = new Color(0.08f, 0.11f, 0.15f, 0.98f);

            GameObject viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            viewportObject.transform.SetParent(templateObject.transform, false);

            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;

            Mask mask = viewportObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);

            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject itemObject = new GameObject(
                "Item",
                typeof(RectTransform),
                typeof(Toggle),
                typeof(Image));
            itemObject.transform.SetParent(contentObject.transform, false);

            RectTransform item = itemObject.GetComponent<RectTransform>();
            item.anchorMin = new Vector2(0f, 0.5f);
            item.anchorMax = new Vector2(1f, 0.5f);
            item.sizeDelta = new Vector2(0f, 34f);

            Image itemImage = itemObject.GetComponent<Image>();
            itemImage.color = new Color(0.12f, 0.16f, 0.2f, 0.95f);

            Text itemLabel = CreateText(
                itemObject.transform,
                "Item Label",
                string.Empty,
                20,
                TextAnchor.MiddleLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(12f, 0f),
                new Vector2(-12f, 0f));

            Toggle itemToggle = itemObject.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemImage;
            itemToggle.graphic = null;

            ScrollRect scrollRect = templateObject.GetComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;

            templateObject.SetActive(false);
            return itemLabel;
        }
    }
}
