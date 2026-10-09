using System;
using System.IO;
using CancerTrace.UI.Controllers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.EditorTools
{
    public static class ShiftSummarySceneRebuilder
    {
        private const string ScenePath = "Assets/Scenes/ShiftSummary.unity";
        private const string FontPath = "Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset";
        private const string BackgroundPath = "Assets/Art/Background/SharedBackground.png";
        private const string TitlePath = "Assets/Art/UI/TitleBanners/ui_title_result_summary.png";
        private const string StarPath = "Assets/Art/Icons/Common/icon_star.png";
        private const string TipPath = "Assets/Art/UI/Buttons/ui_tip_keep_going.png.png";
        private const string ReturnButtonPath = "Assets/Art/UI/Buttons/ui_btn_return_main_menu.png.png";
        private const string NextShiftButtonPath = "Assets/Art/UI/Buttons/ui_btn_start_new_shift.png.png";

        [MenuItem("CancerTrace/ShiftSummary/Rebuild ShiftSummary Safely")]
        public static void RebuildShiftSummarySafely()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("ShiftSummary rebuild cancelled before changing the open Scene.");
                return;
            }

            string originalGuid = AssetDatabase.AssetPathToGUID(ScenePath);
            if (string.IsNullOrEmpty(originalGuid))
            {
                throw new InvalidOperationException("ShiftSummary Scene GUID could not be resolved before rebuild.");
            }

            string backupPath = BackupCurrentScene();

            TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);
            Sprite backgroundSprite = LoadRequired<Sprite>(BackgroundPath);
            Sprite titleSprite = LoadRequired<Sprite>(TitlePath);
            Sprite starSprite = LoadRequired<Sprite>(StarPath);
            Sprite tipSprite = LoadRequired<Sprite>(TipPath);
            Sprite returnButtonSprite = LoadRequired<Sprite>(ReturnButtonPath);
            Sprite nextShiftButtonSprite = LoadRequired<Sprite>(NextShiftButtonPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();

            GameObject controllerObject = new GameObject("ShiftSummaryController");
            ShiftSummaryController controller = controllerObject.AddComponent<ShiftSummaryController>();

            Canvas canvas = CreateCanvas(controllerObject.transform);
            Image background = CreateStretchImage("Background", canvas.transform, backgroundSprite);
            background.preserveAspect = false;
            background.raycastTarget = false;
            background.transform.SetAsFirstSibling();

            RectTransform content = CreateStretchRect("SummaryContent", canvas.transform);

            Image title = CreateFixedImage(
                "ResultTitleBanner", content, titleSprite,
                new Vector2(0f, 365f), new Vector2(900f, 300f));
            title.raycastTarget = false;

            RectTransform scoreRow = CreateFixedRect(
                "ScoreRow", content, new Vector2(0f, 115f), new Vector2(1080f, 145f));
            Image scoreIcon = CreateFixedImage(
                "ScoreIcon", scoreRow, starSprite,
                new Vector2(-435f, 0f), new Vector2(120f, 120f));
            scoreIcon.raycastTarget = false;
            CreateFixedText(
                "ScoreLabel", scoreRow, font, "本轮得分", 42f,
                TextAlignmentOptions.MidlineLeft, new Color32(70, 35, 22, 255),
                new Vector2(-160f, 0f), new Vector2(360f, 110f));
            TMP_Text scoreValue = CreateFixedText(
                "ScoreValue", scoreRow, font, "700", 68f,
                TextAlignmentOptions.Center, new Color32(190, 48, 61, 255),
                new Vector2(330f, 0f), new Vector2(280f, 120f));

            RectTransform correctRow = CreateFixedRect(
                "CorrectDiagnosisRow", content, new Vector2(0f, -45f), new Vector2(1080f, 145f));
            TMP_Text correctIcon = CreateFixedText(
                "CorrectIcon", correctRow, font, "✓", 76f,
                TextAlignmentOptions.Center, new Color32(54, 147, 82, 255),
                new Vector2(-435f, 0f), new Vector2(120f, 120f));
            correctIcon.fontStyle = FontStyles.Bold;
            CreateFixedText(
                "CorrectDiagnosisLabel", correctRow, font, "正确诊断", 42f,
                TextAlignmentOptions.MidlineLeft, new Color32(70, 35, 22, 255),
                new Vector2(-160f, 0f), new Vector2(360f, 110f));
            TMP_Text correctValue = CreateFixedText(
                "CorrectDiagnosisValue", correctRow, font, "7 / 10", 62f,
                TextAlignmentOptions.Center, new Color32(0, 112, 129, 255),
                new Vector2(330f, 0f), new Vector2(320f, 120f));

            Image tip = CreateFixedImage(
                "KeepGoingTip", content, tipSprite,
                new Vector2(0f, -220f), new Vector2(760f, 250f));
            tip.raycastTarget = false;

            Button returnButton = CreateFixedButton(
                "ReturnMainMenuButton", content, returnButtonSprite,
                new Vector2(-290f, -400f), new Vector2(500f, 167f));
            Button nextShiftButton = CreateFixedButton(
                "StartNewShiftButton", content, nextShiftButtonSprite,
                new Vector2(290f, -400f), new Vector2(500f, 167f));

            CreateEventSystem();
            BindController(controller, scoreValue, correctValue, returnButton, nextShiftButton);
            ValidateCreatedScene(controller, canvas, background, scoreRow, correctRow);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath, false))
            {
                throw new InvalidOperationException("Unity failed to save the rebuilt ShiftSummary Scene.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string rebuiltGuid = AssetDatabase.AssetPathToGUID(ScenePath);
            if (!string.Equals(originalGuid, rebuiltGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "ShiftSummary Scene GUID changed unexpectedly. Before: " + originalGuid +
                    ", after: " + rebuiltGuid);
            }

            Selection.activeGameObject = controllerObject;
            Debug.Log(
                "ShiftSummary rebuilt and saved by Unity Editor API. " +
                "Scene GUID preserved: " + rebuiltGuid + ". Backup: " + backupPath);
        }

        private static string BackupCurrentScene()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string sourcePath = Path.Combine(projectRoot, ScenePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("ShiftSummary Scene is missing and cannot be backed up.", sourcePath);
            }

            string backupDirectory = Path.Combine(projectRoot, "SceneBackups");
            Directory.CreateDirectory(backupDirectory);
            string backupPath = Path.Combine(
                backupDirectory,
                "ShiftSummary.unity.pre-editor-rebuild-" +
                DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bak");
            File.Copy(sourcePath, backupPath, false);
            return backupPath;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(39, 49, 70, 255);
        }

        private static void CreateEventSystem()
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        private static RectTransform CreateStretchRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform CreateFixedRect(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image CreateStretchImage(string name, Transform parent, Sprite sprite)
        {
            RectTransform rect = CreateStretchRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            return image;
        }

        private static Image CreateFixedImage(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform rect = CreateFixedRect(name, parent, anchoredPosition, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            return image;
        }

        private static TMP_Text CreateFixedText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform rect = CreateFixedRect(name, parent, anchoredPosition, size);
            rect.gameObject.SetActive(false);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            rect.gameObject.SetActive(true);
            return text;
        }

        private static Button CreateFixedButton(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            Image image = CreateFixedImage(name, parent, sprite, anchoredPosition, size);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.86f, 1f);
            colors.pressedColor = new Color(0.86f, 0.78f, 0.70f, 1f);
            colors.disabledColor = new Color(0.68f, 0.68f, 0.68f, 0.72f);
            button.colors = colors;
            return button;
        }

        private static void BindController(
            ShiftSummaryController controller,
            TMP_Text scoreValue,
            TMP_Text correctValue,
            Button returnButton,
            Button nextShiftButton)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            SetObjectReference(serializedController, "scoreValueText", scoreValue);
            SetObjectReference(serializedController, "correctDiagnosisValueText", correctValue);
            SetObjectReference(serializedController, "returnMainMenuButton", returnButton);
            SetObjectReference(serializedController, "startNewShiftButton", nextShiftButton);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void SetObjectReference(
            SerializedObject serializedObject,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "ShiftSummaryController is missing serialized field: " + propertyName);
            }
            property.objectReferenceValue = value;
        }

        private static void ValidateCreatedScene(
            ShiftSummaryController controller,
            Canvas canvas,
            Image background,
            RectTransform scoreRow,
            RectTransform correctRow)
        {
            if (controller == null || canvas == null || background == null)
            {
                throw new InvalidOperationException("Required ShiftSummary root objects were not created.");
            }
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                throw new InvalidOperationException("ShiftSummary Canvas is not Screen Space - Overlay.");
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null ||
                scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.referenceResolution != new Vector2(1920f, 1080f))
            {
                throw new InvalidOperationException("ShiftSummary CanvasScaler is invalid.");
            }

            RectTransform backgroundRect = background.rectTransform;
            if (backgroundRect.anchorMin != Vector2.zero ||
                backgroundRect.anchorMax != Vector2.one ||
                backgroundRect.offsetMin != Vector2.zero ||
                backgroundRect.offsetMax != Vector2.zero)
            {
                throw new InvalidOperationException("ShiftSummary Background is not full-stretch.");
            }
            if (scoreRow.GetComponent<Image>() != null || correctRow.GetComponent<Image>() != null)
            {
                throw new InvalidOperationException("Score rows must remain transparent RectTransforms.");
            }
            if (UnityEngine.Object.FindObjectsOfType<Canvas>().Length != 1 ||
                UnityEngine.Object.FindObjectsOfType<EventSystem>().Length != 1)
            {
                throw new InvalidOperationException("ShiftSummary must contain exactly one Canvas and EventSystem.");
            }
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new FileNotFoundException(
                    "Required ShiftSummary asset could not be loaded: " + path, path);
            }
            return asset;
        }
    }
}
