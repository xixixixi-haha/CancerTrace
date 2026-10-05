using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CancerTrace.UI.Controllers
{
    public static class CancerTraceUiFactory
    {
        public static readonly Color Ink = new Color32(55, 45, 62, 255);
        public static readonly Color Paper = new Color32(255, 249, 225, 244);
        public static readonly Color Accent = new Color32(196, 80, 99, 255);
        public static readonly Color Teal = new Color32(61, 138, 139, 255);
        public static readonly Color Muted = new Color32(123, 107, 120, 255);

        public static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventObject = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventObject.transform.SetParent(parent, false);
            }

            return canvas;
        }

        public static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite == null ? Image.Type.Simple : Image.Type.Sliced;
            return image;
        }

        public static Image CreateBackground(Transform parent, Sprite sprite)
        {
            Image image = CreateImage(
                "Background",
                parent,
                sprite,
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            return image;
        }

        public static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            float size,
            TextAlignmentOptions alignment,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            // TMP assigns its global default font during OnEnable. Build the component while
            // inactive so formal scene generation can persist the project font deterministically.
            rect.gameObject.SetActive(false);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.font = font;
            rect.gameObject.SetActive(true);
            return text;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            TMP_FontAsset font,
            Sprite sprite,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            Image image = CreateImage(
                name,
                parent,
                sprite,
                sprite == null ? new Color32(246, 198, 142, 255) : Color.white,
                anchorMin,
                anchorMax,
                offsetMin,
                offsetMax);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.78f, 1f);
            colors.pressedColor = new Color(0.85f, 0.78f, 0.7f, 1f);
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.7f);
            button.colors = colors;

            if (!string.IsNullOrEmpty(label))
            {
                TMP_Text text = CreateText(
                    "Label",
                    image.transform,
                    font,
                    label,
                    27f,
                    TextAlignmentOptions.Center,
                    Ink,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(18f, 10f),
                    new Vector2(-18f, -10f));
                text.fontStyle = FontStyles.Bold;
            }
            return button;
        }

        public static TMP_Text GetButtonLabel(Button button)
        {
            return button == null ? null : button.GetComponentInChildren<TMP_Text>(true);
        }

        public static void SetButtonLabel(Button button, string label)
        {
            TMP_Text text = GetButtonLabel(button);
            if (text != null) text.text = label;
        }

        public static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Object.Destroy(parent.GetChild(index).gameObject);
            }
        }
    }
}
