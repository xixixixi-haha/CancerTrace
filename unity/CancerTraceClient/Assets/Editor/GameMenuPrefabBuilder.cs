using System;
using CancerTrace.UI.GameMenu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CancerTrace.EditorTools
{
    /// <summary>
    /// Builds only the reusable game-menu prefab. It never opens, edits, or saves a Scene.
    /// </summary>
    public static class GameMenuPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Resources/UI";
        private const string PrefabPath = PrefabFolder + "/GameMenuOverlay.prefab";
        private const string FontPath = "Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset";
        private const string GearPath = "Assets/Art/Icons/Common/icon_settings_gear.png";
        private const string VolumeIconPath = "Assets/Art/Icons/Common/icon_volume.png";
        private const string PanelPath = "Assets/Art/UI/Panels/ui_panel_game_settings.png";
        private const string ResumePath = "Assets/Art/UI/Buttons/ui_btn_menu_continue.png";
        private const string MainMenuPath = "Assets/Art/UI/Buttons/ui_btn_menu_mainmenu.png";
        private const string QuitPath = "Assets/Art/UI/Buttons/ui_btn_menu_quit.png";
        private const string ClosePath = "Assets/Art/UI/Buttons/ui_btn_close_settings.png";
        private const string SliderThumbPath = "Assets/Art/UI/Controls/ui_slider_thumb_paw.png";
        private const string CheckedPath = "Assets/Art/UI/Controls/ui_checkbox_checked.png";
        private const string UncheckedPath = "Assets/Art/UI/Controls/ui_checkbox_unchecked.png";

        private static readonly Color TextBrown = new Color32(91, 55, 38, 255);
        private static readonly Color ErrorRed = new Color32(169, 52, 45, 255);
        private static readonly Color TrackBeige = new Color32(225, 205, 169, 255);
        private static readonly Color TrackGreen = new Color32(132, 174, 116, 255);

        [MenuItem("CancerTrace/Game Menu/Build Game Menu Prefab")]
        public static void BuildPrefab()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(PrefabFolder);

            TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);
            Sprite gear = LoadRequired<Sprite>(GearPath);
            Sprite volumeIconSprite = LoadRequired<Sprite>(VolumeIconPath);
            Sprite panelSprite = LoadRequired<Sprite>(PanelPath);
            Sprite resumeSprite = LoadRequired<Sprite>(ResumePath);
            Sprite mainMenuSprite = LoadRequired<Sprite>(MainMenuPath);
            Sprite quitSprite = LoadRequired<Sprite>(QuitPath);
            Sprite closeSprite = LoadRequired<Sprite>(ClosePath);
            Sprite thumbSprite = LoadRequired<Sprite>(SliderThumbPath);
            Sprite checkedSprite = LoadRequired<Sprite>(CheckedPath);
            Sprite uncheckedSprite = LoadRequired<Sprite>(UncheckedPath);

            GameObject host = new GameObject("GameMenuHost");
            try
            {
                GameMenuController controller = host.AddComponent<GameMenuController>();

                RectTransform canvasRect = CreateRect("GameMenuCanvas", host.transform);
                Stretch(canvasRect);
                Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = false;
                canvas.sortingOrder = 30000;
                CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                canvasRect.gameObject.AddComponent<GraphicRaycaster>();

                Button entryButton = CreateIconButton(
                    "MenuEntryButton", canvasRect, gear, new Vector2(100f, 100f), new Vector2(100f, 100f));
                RectTransform entryRect = (RectTransform)entryButton.transform;
                entryRect.anchorMin = Vector2.one;
                entryRect.anchorMax = Vector2.one;
                entryRect.pivot = Vector2.one;
                entryRect.anchoredPosition = new Vector2(-15f, -11f);

                RectTransform overlay = CreateRect("GameMenuOverlay", canvasRect);
                Stretch(overlay);
                CanvasGroup overlayGroup = overlay.gameObject.AddComponent<CanvasGroup>();
                overlayGroup.interactable = true;
                overlayGroup.blocksRaycasts = true;

                Image dim = CreateImage("FullScreenDim", overlay, null, new Color(0.035f, 0.05f, 0.045f, 0.74f));
                Stretch(dim.rectTransform);
                dim.raycastTarget = true;

                Image panel = CreateImage("SettingsPanel", overlay, panelSprite, Color.white);
                SetCentered(panel.rectTransform, new Vector2(760f, 950f), Vector2.zero);
                panel.preserveAspect = true;
                panel.raycastTarget = true;

                Button closeButton = CreateSpriteButton(
                    "CloseButton", panel.rectTransform, closeSprite, new Vector2(150f, 150f), new Vector2(303f, 281f));
                Button resumeButton = CreateSpriteButton(
                    "ResumeButton", panel.rectTransform, resumeSprite, new Vector2(491f, 161.688f), new Vector2(-8f, 160f));
                Button mainMenuButton = CreateSpriteButton(
                    "ReturnMainMenuButton", panel.rectTransform, mainMenuSprite, new Vector2(469.23f, 166.84f), new Vector2(-7f, 26f));
                Button quitButton = CreateSpriteButton(
                    "QuitButton", panel.rectTransform, quitSprite, new Vector2(597.2988f, 150.71f), new Vector2(0f, -116f));

                Image volumeIcon = CreateImage("VolumeIcon", panel.rectTransform, volumeIconSprite, Color.white);
                SetCentered(volumeIcon.rectTransform, new Vector2(58f, 58f), new Vector2(-247f, -233.5f));
                volumeIcon.preserveAspect = true;
                volumeIcon.raycastTarget = false;

                TMP_Text volumeLabel = CreateText(
                    "VolumeLabel", panel.rectTransform, font, "音效音量", 32f,
                    TextAlignmentOptions.MidlineLeft, TextBrown);
                SetCentered(volumeLabel.rectTransform, new Vector2(220f, 58f), new Vector2(-89f, -233.5f));

                TMP_Text percentText = CreateText(
                    "VolumePercentText", panel.rectTransform, font, "70%", 32f,
                    TextAlignmentOptions.MidlineRight, TextBrown);
                SetCentered(percentText.rectTransform, new Vector2(110f, 58f), new Vector2(223f, -233.5f));

                Slider slider = CreateSlider(panel.rectTransform, thumbSprite);
                SetCentered((RectTransform)slider.transform, new Vector2(500f, 64f), new Vector2(22f, -287.5f));

                Button checkbox = CreateCheckbox(panel.rectTransform, checkedSprite);
                SetCentered((RectTransform)checkbox.transform, new Vector2(90f, 90f), new Vector2(-118.5f, -336.5f));
                Image checkboxImage = checkbox.GetComponent<Image>();

                TMP_Text checkboxLabel = CreateText(
                    "SfxCheckboxLabel", panel.rectTransform, font, "开启音效", 30f,
                    TextAlignmentOptions.MidlineLeft, TextBrown);
                SetCentered(checkboxLabel.rectTransform, new Vector2(270f, 58f), new Vector2(93.5f, -336.5f));

                TMP_Text errorText = CreateText(
                    "ErrorText", panel.rectTransform, font, string.Empty, 21f,
                    TextAlignmentOptions.Center, ErrorRed);
                SetCentered(errorText.rectTransform, new Vector2(590f, 66f), new Vector2(0f, -452f));
                errorText.enableWordWrapping = true;
                errorText.raycastTarget = false;
                errorText.gameObject.SetActive(false);

                SerializedObject serialized = new SerializedObject(controller);
                SetObject(serialized, "menuEntryButton", entryButton.gameObject);
                SetObject(serialized, "gameMenuOverlay", overlay.gameObject);
                SetObject(serialized, "closeButton", closeButton);
                SetObject(serialized, "resumeButton", resumeButton);
                SetObject(serialized, "returnMainMenuButton", mainMenuButton);
                SetObject(serialized, "quitButton", quitButton);
                SetObject(serialized, "volumeSlider", slider);
                SetObject(serialized, "volumePercentText", percentText);
                SetObject(serialized, "sfxCheckbox", checkbox);
                SetObject(serialized, "sfxCheckboxImage", checkboxImage);
                SetObject(serialized, "sfxCheckedSprite", checkedSprite);
                SetObject(serialized, "sfxUncheckedSprite", uncheckedSprite);
                SetObject(serialized, "errorText", errorText);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                overlay.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(host, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidatePrefab();
            Debug.Log("Game menu prefab built and validated at " + PrefabPath + ". No Scene was modified.");
        }

        [MenuItem("CancerTrace/Game Menu/Validate Game Menu Prefab")]
        public static void ValidatePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                // Unity names the loaded prefab root after the asset file. The runtime Bootstrap
                // renames the one instantiated object to GameMenuHost before it is used.
                Require(root.name == "GameMenuOverlay", "Prefab root must match its asset name.");
                Require(root.GetComponent<GameMenuController>() != null, "GameMenuController is missing.");
                Require(root.GetComponentInChildren<EventSystem>(true) == null, "Prefab must not contain an EventSystem.");

                Transform canvasTransform = RequirePath(root.transform, "GameMenuCanvas");
                Canvas canvas = canvasTransform.GetComponent<Canvas>();
                CanvasScaler scaler = canvasTransform.GetComponent<CanvasScaler>();
                Require(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay,
                    "GameMenuCanvas must be Screen Space Overlay.");
                Require(canvas.sortingOrder == 30000,
                    "GameMenuCanvas sorting must keep the menu above Scene UI.");
                Require(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                        scaler.referenceResolution == new Vector2(1920f, 1080f),
                    "GameMenuCanvas scaler is invalid.");

                Transform entry = RequirePath(canvasTransform, "MenuEntryButton");
                Require(entry.GetComponent<Button>() != null, "MenuEntryButton must be clickable.");
                Require(RequirePath(entry, "Icon").GetComponent<Image>().sprite == LoadRequired<Sprite>(GearPath),
                    "MenuEntryButton does not reference the settings gear Sprite.");

                Transform overlay = RequirePath(canvasTransform, "GameMenuOverlay");
                Image dim = RequirePath(overlay, "FullScreenDim").GetComponent<Image>();
                Require(dim != null && dim.raycastTarget, "FullScreenDim must intercept raycasts.");
                Transform panel = RequirePath(overlay, "SettingsPanel");
                Require(panel.GetComponent<Image>().sprite == LoadRequired<Sprite>(PanelPath),
                    "SettingsPanel Sprite is invalid.");

                string[] requiredPanelChildren =
                {
                    "CloseButton", "ResumeButton", "ReturnMainMenuButton", "QuitButton",
                    "VolumeSlider", "VolumeIcon", "VolumeLabel", "VolumePercentText",
                    "SfxCheckbox", "SfxCheckboxLabel", "ErrorText"
                };
                for (int index = 0; index < requiredPanelChildren.Length; index++)
                {
                    RequirePath(panel, requiredPanelChildren[index]);
                }

                Slider slider = panel.Find("VolumeSlider").GetComponent<Slider>();
                Require(slider != null && Mathf.Approximately(slider.minValue, 0f) &&
                        Mathf.Approximately(slider.maxValue, 1f), "VolumeSlider range must be 0..1.");
                Require(slider.handleRect.GetComponent<Image>().sprite == LoadRequired<Sprite>(SliderThumbPath),
                    "VolumeSlider handle Sprite is invalid.");
                Require(panel.Find("SfxCheckbox").GetComponent<Image>().sprite == LoadRequired<Sprite>(CheckedPath),
                    "SfxCheckbox initial Sprite is invalid.");

                TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                Require(texts.Length == 4, "The prefab must contain exactly four dynamic TMP labels.");
                for (int index = 0; index < texts.Length; index++)
                {
                    Require(texts[index].font == font, texts[index].name + " does not use the formal TMP font.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log("Game menu prefab validation passed.");
        }

        public static void BuildFromCommandLine()
        {
            BuildPrefab();
        }

        private static Slider CreateSlider(Transform parent, Sprite thumbSprite)
        {
            RectTransform root = CreateRect("VolumeSlider", parent);
            Slider slider = root.gameObject.AddComponent<Slider>();

            Image background = CreateImage("Background", root, null, TrackBeige);
            SetCentered(background.rectTransform, new Vector2(500f, 18f), Vector2.zero);
            background.raycastTarget = false;

            RectTransform fillArea = CreateRect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(9f, -9f);
            fillArea.offsetMax = new Vector2(-9f, 9f);
            Image fill = CreateImage("Fill", fillArea, null, TrackGreen);
            Stretch(fill.rectTransform);
            fill.raycastTarget = false;

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(28f, 0f);
            handleArea.offsetMax = new Vector2(-28f, 0f);
            Image handle = CreateImage("Handle", handleArea, thumbSprite, Color.white);
            SetCentered(handle.rectTransform, new Vector2(56f, 56f), Vector2.zero);
            handle.preserveAspect = true;

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 0.7f;
            return slider;
        }

        private static Button CreateCheckbox(Transform parent, Sprite sprite)
        {
            Image image = CreateImage("SfxCheckbox", parent, sprite, Color.white);
            image.preserveAspect = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Button CreateIconButton(
            string name, Transform parent, Sprite sprite, Vector2 hitSize, Vector2 iconSize)
        {
            Image hitArea = CreateImage(name, parent, null, new Color(1f, 1f, 1f, 0f));
            hitArea.rectTransform.sizeDelta = hitSize;
            Button button = hitArea.gameObject.AddComponent<Button>();
            button.targetGraphic = hitArea;

            Image icon = CreateImage("Icon", hitArea.rectTransform, sprite, Color.white);
            SetCentered(icon.rectTransform, iconSize, Vector2.zero);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return button;
        }

        private static Button CreateSpriteButton(
            string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position)
        {
            Image image = CreateImage(name, parent, sprite, Color.white);
            SetCentered(image.rectTransform, size, position);
            image.preserveAspect = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException("Missing serialized field: " + propertyName);
            property.objectReferenceValue = value;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required asset is missing: " + path);
            return asset;
        }

        private static Transform RequirePath(Transform parent, string path)
        {
            Transform child = parent.Find(path);
            if (child == null) throw new InvalidOperationException("Required prefab object is missing: " + path);
            return child;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
