using System;
using CancerTrace.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.EditorTools
{
    public static class TutorialGuideInstaller
    {
        private const string ScenePath = "Assets/Scenes/CaseAnalysis.unity";
        private const string GalaxyScenePath = "Assets/Scenes/CancerGalaxy.unity";
        private const string ResultSummaryScenePath = "Assets/Scenes/ResultSummary.unity";
        private const string DialogPath = "Assets/Art/UI/Tutorial/ui_tutorial_dialog.png";
        private const string ArrowPath = "Assets/Art/UI/Tutorial/ui_tutorial_arrow.png";
        private const string NextButtonPath = "Assets/Art/UI/Buttons/ui_btn_next_normal.png.png";
        private const string CompleteTutorialButtonPath =
            "Assets/Art/UI/Buttons/ui_btn_complete_tutorial.png";
        private const string FontPath = "Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset";

        [MenuItem("CancerTrace/Tutorial/Install Tutorial Guide Layer")]
        public static void InstallTutorialGuideLayer()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Open Assets/Scenes/CaseAnalysis.unity before installing the Tutorial Guide Layer.");
            }

            Canvas canvas = FindSingleInScene<Canvas>(scene, "Canvas");
            if (FindDirectChild(canvas.transform, "TutorialGuideLayer") != null)
            {
                throw new InvalidOperationException(
                    "TutorialGuideLayer already exists. The installer will not overwrite manual scene adjustments.");
            }

            RectTransform rpTarget = RequireRect(canvas.transform, "RP");
            RectTransform evidenceBoard = RequireRect(canvas.transform, "EvidenceBoard");
            RequireRect(evidenceBoard, "InitialClueText0");
            RequireRect(evidenceBoard, "InitialClueText1");
            RectTransform geneScanEvidenceTarget = RequireRect(evidenceBoard, "GeneScanEvidenceSlot");
            RectTransform geneScanTarget = RequireRect(canvas.transform, "GeneScan");
            RectTransform cancerGalaxyTarget = RequireRect(canvas.transform, "CancerGalaxy");
            RectTransform aiAssistantTarget = RequireRect(canvas.transform, "AiAssistant");
            RectTransform aiEvidenceTarget = RequireRect(evidenceBoard, "AiAssistantEvidenceSlot");
            RectTransform diagnosisTarget = RequireRect(canvas.transform, "DiagnosisArea");
            RectTransform submitTarget = RequireRect(diagnosisTarget, "Submit");

            EnsureSpriteImport(DialogPath);
            EnsureSpriteImport(ArrowPath);
            Sprite dialogSprite = LoadRequired<Sprite>(DialogPath);
            Sprite arrowSprite = LoadRequired<Sprite>(ArrowPath);
            Sprite nextSprite = LoadRequired<Sprite>(NextButtonPath);
            TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Tutorial Guide Layer");
            try
            {
                RectTransform initialCluesTarget = CreateRect(
                    "TutorialTarget_InitialClues",
                    evidenceBoard,
                    new Vector2(0.07f, 0.48f),
                    new Vector2(0.90f, 0.74f));

                RectTransform layer = CreateRect("TutorialGuideLayer", canvas.transform, Vector2.zero, Vector2.one);
                layer.SetAsLastSibling();

                RectTransform dimMask = CreateRect("DimMask", layer, Vector2.zero, Vector2.one);
                RectTransform dimTop = CreateMask("DimTop", dimMask);
                RectTransform dimBottom = CreateMask("DimBottom", dimMask);
                RectTransform dimLeft = CreateMask("DimLeft", dimMask);
                RectTransform dimRight = CreateMask("DimRight", dimMask);

                Image highlight = CreateImage("HighlightFrame", layer, null, Color.clear);
                highlight.raycastTarget = false;
                EnsureHighlightBorder(highlight.rectTransform);

                Image arrow = CreateImage("TutorialArrow", layer, arrowSprite, Color.white);
                arrow.type = Image.Type.Simple;
                arrow.preserveAspect = true;
                arrow.raycastTarget = false;
                arrow.rectTransform.sizeDelta = new Vector2(190f, 190f);

                Image dialog = CreateImage("TutorialDialog", layer, dialogSprite, Color.white);
                dialog.type = Image.Type.Simple;
                dialog.preserveAspect = true;
                dialog.raycastTarget = false;
                dialog.rectTransform.sizeDelta = new Vector2(714.6097f, 522.799f);

                TMP_Text tutorialText = CreateTutorialText(dialog.rectTransform, font);
                Button nextButton = CreateNextButton(layer, nextSprite);

                TutorialGuideController controller = Undo.AddComponent<TutorialGuideController>(layer.gameObject);
                ConfigureController(
                    controller, layer, dimTop, dimBottom, dimLeft, dimRight, highlight.rectTransform,
                    arrow.rectTransform, dialog.rectTransform, tutorialText, nextButton,
                    rpTarget, initialCluesTarget, geneScanTarget, geneScanEvidenceTarget,
                    cancerGalaxyTarget, aiAssistantTarget, aiEvidenceTarget,
                    diagnosisTarget, submitTarget);

                layer.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                controller.RefreshEditModePreview();
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = layer.gameObject;
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log(
                    "TutorialGuideLayer installed. Review the Step 2 preview, adjust serialized positions if needed, then press Ctrl+S.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        [MenuItem("CancerTrace/Tutorial/Install Cancer Galaxy Tutorial Guide Layer")]
        public static void InstallCancerGalaxyTutorialGuideLayer()
        {
            RequireEditMode();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != GalaxyScenePath)
            {
                throw new InvalidOperationException(
                    "Open Assets/Scenes/CancerGalaxy.unity before installing its Tutorial Guide Layer.");
            }

            Canvas canvas = FindSingleInScene<Canvas>(scene, "Canvas");
            if (FindDirectChild(canvas.transform, "TutorialGuideLayer") != null)
            {
                throw new InvalidOperationException(
                    "CancerGalaxy TutorialGuideLayer already exists; manual adjustments were not overwritten.");
            }

            RectTransform plotTarget = RequireRect(canvas.transform, "PlotArea");
            RectTransform backTarget = RequireRect(canvas.transform, "Back");
            EnsureSpriteImport(DialogPath);
            EnsureSpriteImport(ArrowPath);
            Sprite dialogSprite = LoadRequired<Sprite>(DialogPath);
            Sprite arrowSprite = LoadRequired<Sprite>(ArrowPath);
            Sprite nextSprite = LoadRequired<Sprite>(NextButtonPath);
            TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Cancer Galaxy Tutorial Guide Layer");
            try
            {
                RectTransform layer = CreateRect("TutorialGuideLayer", canvas.transform, Vector2.zero, Vector2.one);
                layer.SetAsLastSibling();
                RectTransform dimMask = CreateRect("DimMask", layer, Vector2.zero, Vector2.one);
                RectTransform dimTop = CreateMask("DimTop", dimMask);
                RectTransform dimBottom = CreateMask("DimBottom", dimMask);
                RectTransform dimLeft = CreateMask("DimLeft", dimMask);
                RectTransform dimRight = CreateMask("DimRight", dimMask);

                Image highlight = CreateImage("HighlightFrame", layer, null, Color.clear);
                highlight.raycastTarget = false;
                EnsureHighlightBorder(highlight.rectTransform);

                Image arrow = CreateImage("TutorialArrow", layer, arrowSprite, Color.white);
                arrow.preserveAspect = true;
                arrow.raycastTarget = false;
                Image dialog = CreateImage("TutorialDialog", layer, dialogSprite, Color.white);
                dialog.preserveAspect = true;
                dialog.raycastTarget = false;
                dialog.rectTransform.sizeDelta = new Vector2(714.6097f, 522.799f);
                TMP_Text tutorialText = CreateTutorialText(dialog.rectTransform, font);
                Button nextButton = CreateNextButton(layer, nextSprite);

                TutorialGuideController controller = Undo.AddComponent<TutorialGuideController>(layer.gameObject);
                SerializedObject serialized = new SerializedObject(controller);
                SetObject(serialized, "guideRect", layer);
                SetObject(serialized, "dimTop", dimTop);
                SetObject(serialized, "dimBottom", dimBottom);
                SetObject(serialized, "dimLeft", dimLeft);
                SetObject(serialized, "dimRight", dimRight);
                SetObject(serialized, "highlightFrame", highlight.rectTransform);
                SetObject(serialized, "tutorialArrow", arrow.rectTransform);
                SetObject(serialized, "tutorialDialog", dialog.rectTransform);
                SetObject(serialized, "tutorialText", tutorialText);
                SetObject(serialized, "nextButton", nextButton);
                serialized.FindProperty("previewTutorialInPlayMode").boolValue = false;
                serialized.FindProperty("editModePreviewStep").intValue = 0;
                serialized.FindProperty("nextButtonOffset").vector2Value =
                    new Vector2(184.75f, -174f);

                SerializedProperty steps = serialized.FindProperty("steps");
                steps.arraySize = 2;
                ConfigureStep(
                    steps.GetArrayElementAtIndex(0), "galaxy_review", plotTarget, TutorialStepType.NextButton,
                    "星图展示当前病例与参考样本在表达特征空间中的位置关系。\n距离越近表示空间上越接近，但不代表诊断概率。\n右侧会列出距离当前病例最近的参考样本。",
                    new Vector2(555.25f, -214f), new Vector2(315f, 2f), 194.165f,
                    new Vector2(210f, 210f), Vector2.one, new Vector4(12f, 12f, 12f, 12f),
                    new Vector2(714.6097f, 522.799f));
                ConfigureStep(
                    steps.GetArrayElementAtIndex(1), "galaxy_return", backTarget, TutorialStepType.TargetInteraction,
                    "看完星图后，\n点击“返回”继续调查。",
                    new Vector2(-261f, -210f), new Vector2(-598f, -290f), 247.17401f,
                    new Vector2(210f, 210f), Vector2.one, new Vector4(12f, 12f, 12f, 12f),
                    new Vector2(491.03f, 342.4446f));
                serialized.ApplyModifiedPropertiesWithoutUndo();

                layer.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                controller.RefreshEditModePreview();
                MarkTutorialObjectsDirty(layer);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = layer.gameObject;
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log("CancerGalaxy TutorialGuideLayer installed. Adjust its two local steps, then press Ctrl+S.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        [MenuItem("CancerTrace/Tutorial/Refresh Cancer Galaxy Tutorial Preview")]
        public static void RefreshCancerGalaxyTutorialPreview()
        {
            RequireEditMode();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != GalaxyScenePath)
            {
                throw new InvalidOperationException("Open Assets/Scenes/CancerGalaxy.unity first.");
            }

            Canvas canvas = FindSingleInScene<Canvas>(scene, "Canvas");
            Transform layerTransform = FindDirectChild(canvas.transform, "TutorialGuideLayer");
            TutorialGuideController controller =
                layerTransform == null ? null : layerTransform.GetComponent<TutorialGuideController>();
            if (controller == null)
            {
                throw new InvalidOperationException("CancerGalaxy TutorialGuideLayer is not installed.");
            }

            RectTransform layer = (RectTransform)layerTransform;
            Undo.RegisterFullObjectHierarchyUndo(layer.gameObject, "Refresh Cancer Galaxy Tutorial Preview");
            Canvas.ForceUpdateCanvases();
            controller.RefreshEditModePreview();
            MarkTutorialObjectsDirty(layer);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = layer.gameObject;
            Debug.Log("CancerGalaxy Tutorial preview refreshed. Review it, then press Ctrl+S.");
        }

        [MenuItem("CancerTrace/Tutorial/Install Result Summary Tutorial Guide Layer")]
        public static void InstallResultSummaryTutorialGuideLayer()
        {
            RequireEditMode();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ResultSummaryScenePath)
            {
                throw new InvalidOperationException(
                    "Open Assets/Scenes/ResultSummary.unity before installing its Tutorial Guide Layer.");
            }

            Canvas canvas = FindSingleInScene<Canvas>(scene, "Canvas");
            if (FindDirectChild(canvas.transform, "TutorialGuideLayer") != null)
            {
                throw new InvalidOperationException(
                    "ResultSummary TutorialGuideLayer already exists; manual adjustments were not overwritten.");
            }

            RectTransform resultContent = RequireRect(canvas.transform, "ResultContentArea");
            EnsureSpriteImport(DialogPath);
            EnsureSpriteImport(ArrowPath);
            EnsureSpriteImport(CompleteTutorialButtonPath);
            Sprite dialogSprite = LoadRequired<Sprite>(DialogPath);
            Sprite arrowSprite = LoadRequired<Sprite>(ArrowPath);
            Sprite nextSprite = LoadRequired<Sprite>(NextButtonPath);
            Sprite completeTutorialSprite = LoadRequired<Sprite>(CompleteTutorialButtonPath);
            TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Result Summary Tutorial Guide Layer");
            try
            {
                RectTransform resultReviewTarget = CreateRect(
                    "TutorialTarget_ResultReview", resultContent,
                    new Vector2(0.04f, 0.17f), new Vector2(0.96f, 0.72f));
                RectTransform layer = CreateRect("TutorialGuideLayer", canvas.transform, Vector2.zero, Vector2.one);
                layer.SetAsLastSibling();
                RectTransform dimMask = CreateRect("DimMask", layer, Vector2.zero, Vector2.one);
                RectTransform dimTop = CreateMask("DimTop", dimMask);
                RectTransform dimBottom = CreateMask("DimBottom", dimMask);
                RectTransform dimLeft = CreateMask("DimLeft", dimMask);
                RectTransform dimRight = CreateMask("DimRight", dimMask);

                Image highlight = CreateImage("HighlightFrame", layer, null, Color.clear);
                highlight.raycastTarget = false;
                EnsureHighlightBorder(highlight.rectTransform);

                Image arrow = CreateImage("TutorialArrow", layer, arrowSprite, Color.white);
                arrow.preserveAspect = true;
                arrow.raycastTarget = false;
                arrow.rectTransform.sizeDelta = new Vector2(210f, 210f);

                Image dialog = CreateImage("TutorialDialog", layer, dialogSprite, Color.white);
                dialog.preserveAspect = true;
                dialog.raycastTarget = false;
                dialog.rectTransform.sizeDelta = new Vector2(714.6097f, 522.799f);
                TMP_Text tutorialText = CreateTutorialText(dialog.rectTransform, font);
                tutorialText.rectTransform.anchoredPosition = new Vector2(28f, 17f);
                Button nextButton = CreateNextButton(layer, nextSprite);

                TutorialGuideController controller = Undo.AddComponent<TutorialGuideController>(layer.gameObject);
                SerializedObject serialized = new SerializedObject(controller);
                SetObject(serialized, "guideRect", layer);
                SetObject(serialized, "dimTop", dimTop);
                SetObject(serialized, "dimBottom", dimBottom);
                SetObject(serialized, "dimLeft", dimLeft);
                SetObject(serialized, "dimRight", dimRight);
                SetObject(serialized, "highlightFrame", highlight.rectTransform);
                SetObject(serialized, "tutorialArrow", arrow.rectTransform);
                SetObject(serialized, "tutorialDialog", dialog.rectTransform);
                SetObject(serialized, "tutorialText", tutorialText);
                SetObject(serialized, "nextButton", nextButton);
                serialized.FindProperty("previewTutorialInPlayMode").boolValue = false;
                serialized.FindProperty("editModePreviewStep").intValue = 0;
                serialized.FindProperty("nextButtonOffset").vector2Value =
                    new Vector2(166.445068f, -148.1005f);

                SerializedProperty steps = serialized.FindProperty("steps");
                steps.arraySize = 2;
                ConfigureStep(
                    steps.GetArrayElementAtIndex(0), "result_review", resultReviewTarget,
                    TutorialStepType.NextButton,
                    "结算页会展示你的诊断结果和真实诊断。\n\n这里还会提供 AI 辅助结果，\n帮助你回顾本次判断过程。",
                    new Vector2(-618.5f, -299.49997f), new Vector2(-796f, -5f), 200.629f,
                    new Vector2(210f, 210f), new Vector2(-1.1449f, 1f),
                    new Vector4(11.99998f, 91.85405f, 183.32292f, 183.32304f),
                    nextButtonSprite: nextSprite);
                ConfigureStep(
                    steps.GetArrayElementAtIndex(1), "tutorial_complete", null,
                    TutorialStepType.NextButton,
                    "教学完成！\n\n现在你已经了解调查的基本流程，\n可以开始正式调查了。",
                    new Vector2(0f, -245f), new Vector2(0f, 120f), 180f,
                    new Vector2(210f, 210f), Vector2.one, new Vector4(12f, 12f, 12f, 12f),
                    showHighlight: false, showArrow: false,
                    nextButtonSprite: completeTutorialSprite);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                layer.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                controller.RefreshEditModePreview();
                MarkTutorialObjectsDirty(layer);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = layer.gameObject;
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log(
                    "ResultSummary TutorialGuideLayer installed with ResultReview and TutorialComplete previews. " +
                    "Adjust them in Edit Mode, then press Ctrl+S.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        public static void InstallResultSummaryTutorialGuideLayerFromCommandLine()
        {
            Scene scene = EditorSceneManager.OpenScene(ResultSummaryScenePath, OpenSceneMode.Single);
            InstallResultSummaryTutorialGuideLayer();
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Could not save ResultSummary after Tutorial installation.");
            }
        }

        [MenuItem("CancerTrace/Tutorial/Refresh Result Summary Tutorial Preview")]
        public static void RefreshResultSummaryTutorialPreview()
        {
            RequireEditMode();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ResultSummaryScenePath)
            {
                throw new InvalidOperationException("Open Assets/Scenes/ResultSummary.unity first.");
            }

            Canvas canvas = FindSingleInScene<Canvas>(scene, "Canvas");
            Transform layerTransform = FindDirectChild(canvas.transform, "TutorialGuideLayer");
            TutorialGuideController controller =
                layerTransform == null ? null : layerTransform.GetComponent<TutorialGuideController>();
            if (controller == null)
            {
                throw new InvalidOperationException("ResultSummary TutorialGuideLayer is not installed.");
            }

            RectTransform layer = (RectTransform)layerTransform;
            Undo.RegisterFullObjectHierarchyUndo(layer.gameObject, "Refresh Result Summary Tutorial Preview");
            Canvas.ForceUpdateCanvases();
            controller.RefreshEditModePreview();
            MarkTutorialObjectsDirty(layer);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = layer.gameObject;
            Debug.Log("ResultSummary Tutorial preview refreshed. Review it, then press Ctrl+S.");
        }

        [MenuItem("CancerTrace/Tutorial/Update Tutorial Guide Visuals")]
        public static void UpdateTutorialGuideVisuals()
        {
            RequireEditMode();
            TutorialGuideController controller = FindInstalledController();
            RectTransform layer = (RectTransform)controller.transform;
            RectTransform highlight = RequireDirectRect(layer, "HighlightFrame");
            RectTransform dialog = RequireDirectRect(layer, "TutorialDialog");
            RectTransform nextButton = RequireDirectRect(layer, "NextButton");

            Undo.RegisterFullObjectHierarchyUndo(layer.gameObject, "Update Tutorial Guide Visuals");
            Image highlightImage = highlight.GetComponent<Image>();
            if (highlightImage == null)
            {
                throw new InvalidOperationException("HighlightFrame Image is missing.");
            }
            highlightImage.color = Color.clear;
            highlightImage.raycastTarget = false;

            Outline outline = highlight.GetComponent<Outline>();
            if (outline != null) Undo.DestroyObjectImmediate(outline);
            EnsureHighlightBorder(highlight);
            dialog.sizeDelta = new Vector2(714.6097f, 522.799f);
            nextButton.sizeDelta = new Vector2(283.5538f, 134.9104f);

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("nextButtonOffset").vector2Value = new Vector2(166.445068f, -148.1005f);
            SerializedProperty steps = serialized.FindProperty("steps");
            if (steps == null || steps.arraySize != 9)
            {
                throw new InvalidOperationException("Expected the existing nine-step Tutorial preview.");
            }

            SerializedProperty step2 = steps.GetArrayElementAtIndex(2);
            step2.FindPropertyRelative("dialogPosition").vector2Value = new Vector2(-121f, -18f);
            step2.FindPropertyRelative("arrowPosition").vector2Value = new Vector2(-486f, 113f);
            step2.FindPropertyRelative("arrowRotation").floatValue = -127.504f;
            step2.FindPropertyRelative("arrowSize").vector2Value = new Vector2(210f, 210f);
            step2.FindPropertyRelative("arrowScale").vector2Value = Vector2.one;
            step2.FindPropertyRelative("highlightPadding").vector4Value = new Vector4(12f, 12f, 12f, 12f);
            serialized.ApplyModifiedProperties();

            Canvas.ForceUpdateCanvases();
            controller.RefreshEditModePreview();
            MarkTutorialObjectsDirty(layer);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Selection.activeGameObject = layer.gameObject;
            Debug.Log("Tutorial Step 2 visual fixes applied. Review the preview, then press Ctrl+S.");
        }

        [MenuItem("CancerTrace/Tutorial/Refresh Tutorial Preview")]
        public static void RefreshTutorialPreview()
        {
            RequireEditMode();
            TutorialGuideController controller = FindInstalledController();
            RectTransform layer = (RectTransform)controller.transform;
            Undo.RegisterFullObjectHierarchyUndo(layer.gameObject, "Refresh Tutorial Preview");
            Canvas.ForceUpdateCanvases();
            controller.RefreshEditModePreview();
            MarkTutorialObjectsDirty(layer);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log("Tutorial Step 2 Edit Mode preview refreshed.");
        }

        [MenuItem("CancerTrace/Tutorial/Validate Tutorial Guide Layer")]
        public static void ValidateTutorialGuideLayer()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException("Open CaseAnalysis.unity before validation.");
            }

            Canvas canvas = FindSingleInScene<Canvas>(scene, "Canvas");
            Transform layer = FindDirectChild(canvas.transform, "TutorialGuideLayer");
            if (layer == null) throw new InvalidOperationException("TutorialGuideLayer is missing.");
            if (layer.GetSiblingIndex() != canvas.transform.childCount - 1)
            {
                throw new InvalidOperationException("TutorialGuideLayer is not the Canvas top layer.");
            }

            string[] paths =
            {
                "DimMask/DimTop", "DimMask/DimBottom", "DimMask/DimLeft", "DimMask/DimRight",
                "HighlightFrame", "TutorialArrow", "TutorialDialog", "TutorialDialog/TutorialText", "NextButton"
            };
            foreach (string path in paths)
            {
                if (layer.Find(path) == null) throw new InvalidOperationException("Missing tutorial object: " + path);
            }

            TutorialGuideController controller = layer.GetComponent<TutorialGuideController>();
            if (controller == null) throw new InvalidOperationException("TutorialGuideController is missing.");

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty steps = serialized.FindProperty("steps");
            if (steps == null || steps.arraySize != 9)
            {
                throw new InvalidOperationException("Tutorial preview must contain exactly nine steps.");
            }
            for (int index = 0; index < steps.arraySize; index++)
            {
                SerializedProperty target = steps.GetArrayElementAtIndex(index).FindPropertyRelative("target");
                if (target == null || target.objectReferenceValue == null)
                {
                    throw new InvalidOperationException("Tutorial step target is missing at index " + index + ".");
                }
            }

            foreach (Transform child in layer.Find("DimMask"))
            {
                Image image = child.GetComponent<Image>();
                if (image == null || !image.raycastTarget)
                {
                    throw new InvalidOperationException(child.name + " must be a raycast-blocking Image.");
                }
            }

            if (layer.Find("HighlightFrame").GetComponent<Image>().raycastTarget ||
                layer.Find("TutorialArrow").GetComponent<Image>().raycastTarget ||
                layer.Find("TutorialDialog").GetComponent<Image>().raycastTarget)
            {
                throw new InvalidOperationException("Tutorial presentation graphics must not receive raycasts.");
            }

            RectTransform highlight = (RectTransform)layer.Find("HighlightFrame");
            Image highlightImage = highlight.GetComponent<Image>();
            if (highlightImage == null || highlightImage.color.a != 0f || highlight.GetComponent<Outline>() != null)
            {
                throw new InvalidOperationException("HighlightFrame must be transparent and must not use UI Outline.");
            }
            string[] borderNames = { "BorderTop", "BorderBottom", "BorderLeft", "BorderRight" };
            foreach (string borderName in borderNames)
            {
                Transform border = highlight.Find(borderName);
                Image borderImage = border == null ? null : border.GetComponent<Image>();
                if (borderImage == null || borderImage.raycastTarget)
                {
                    throw new InvalidOperationException("Highlight border is missing or receives raycasts: " + borderName);
                }
            }

            TMP_Text text = layer.Find("TutorialDialog/TutorialText").GetComponent<TMP_Text>();
            if (text == null || text.font != LoadRequired<TMP_FontAsset>(FontPath))
            {
                throw new InvalidOperationException("TutorialText does not use the formal TMP font.");
            }

            Debug.Log("TutorialGuideLayer validation passed.");
        }

        private static void ConfigureController(
            TutorialGuideController controller,
            RectTransform layer,
            RectTransform dimTop,
            RectTransform dimBottom,
            RectTransform dimLeft,
            RectTransform dimRight,
            RectTransform highlight,
            RectTransform arrow,
            RectTransform dialog,
            TMP_Text text,
            Button nextButton,
            RectTransform rpTarget,
            RectTransform initialCluesTarget,
            RectTransform geneScanTarget,
            RectTransform geneScanEvidenceTarget,
            RectTransform cancerGalaxyTarget,
            RectTransform aiAssistantTarget,
            RectTransform aiEvidenceTarget,
            RectTransform diagnosisTarget,
            RectTransform submitTarget)
        {
            SerializedObject serialized = new SerializedObject(controller);
            SetObject(serialized, "guideRect", layer);
            SetObject(serialized, "dimTop", dimTop);
            SetObject(serialized, "dimBottom", dimBottom);
            SetObject(serialized, "dimLeft", dimLeft);
            SetObject(serialized, "dimRight", dimRight);
            SetObject(serialized, "highlightFrame", highlight);
            SetObject(serialized, "tutorialArrow", arrow);
            SetObject(serialized, "tutorialDialog", dialog);
            SetObject(serialized, "tutorialText", text);
            SetObject(serialized, "nextButton", nextButton);
            serialized.FindProperty("previewTutorialInPlayMode").boolValue = false;
            serialized.FindProperty("editModePreviewStep").intValue = 2;
            serialized.FindProperty("nextButtonOffset").vector2Value = new Vector2(166.445068f, -148.1005f);

            SerializedProperty steps = serialized.FindProperty("steps");
            steps.arraySize = 9;
            ConfigureStep(
                steps.GetArrayElementAtIndex(0), "rp", rpTarget, TutorialStepType.NextButton,
                "调查点是正式调查中的共享资源。\n每轮调查初始拥有 200 RP。",
                new Vector2(250f, 100f), new Vector2(430f, 260f), 95f,
                new Vector2(180f, 180f), Vector2.one, new Vector4(14f, 12f, 14f, 12f));
            ConfigureStep(
                steps.GetArrayElementAtIndex(1), "initial_clues", initialCluesTarget, TutorialStepType.NextButton,
                "每个病例会先提供两条免费的基因线索。\nHIGH / LOW 表示对应基因的表达状态。",
                new Vector2(-370.44507f, -277.8995f), new Vector2(0f, -150f), 130f,
                new Vector2(190f, 190f), Vector2.one, new Vector4(16f, 16f, 29.4368f, 80.72278f));
            ConfigureStep(
                steps.GetArrayElementAtIndex(2), "gene_scan", geneScanTarget, TutorialStepType.TargetInteraction,
                "点击“扫描”可以获得 3 条额外的基因线索。\n正式调查中，扫描需要消耗 10 RP。\n请点击左侧高亮的“扫描”按钮继续。",
                new Vector2(-121f, -18f), new Vector2(-486f, 113f), -127.504f,
                new Vector2(210f, 210f), Vector2.one, new Vector4(12f, 12f, 12f, 12f));
            ConfigureStep(
                steps.GetArrayElementAtIndex(3), "gene_scan_evidence", geneScanEvidenceTarget, TutorialStepType.NextButton,
                "新获得的基因线索会整理到证据板中，\n可以和初始线索一起判断。",
                new Vector2(219f, -158f), new Vector2(-142f, -9f), -124.191f,
                new Vector2(210f, 210f), new Vector2(1.0412f, 0.94280666f),
                new Vector4(28.68711f, 12f, 29.55099f, 26.30066f));
            ConfigureStep(
                steps.GetArrayElementAtIndex(4), "cancer_galaxy", cancerGalaxyTarget, TutorialStepType.TargetInteraction,
                "癌症星图可以查看当前病例附近的参考样本。\n正式调查中，观察星图需要消耗 25 RP。\n请点击左侧高亮的“观察星图”按钮继续。",
                new Vector2(-29.5f, -108.5f), new Vector2(-433f, -6f), -112.757f,
                new Vector2(210f, 210f), Vector2.one, new Vector4(12f, 12f, 12f, 12f));
            ConfigureStep(
                steps.GetArrayElementAtIndex(5), "ai_assistant", aiAssistantTarget, TutorialStepType.TargetInteraction,
                "AI辅助会提供预测结果、置信度和前三候选。\n正式调查中，AI辅助需要消耗 40 RP。\n请点击左侧高亮的“AI辅助”按钮继续。",
                new Vector2(-29.5f, -150f), new Vector2(-433f, -158f), -112.757f,
                new Vector2(210f, 210f), Vector2.one, new Vector4(12f, 12f, 12f, 12f));
            ConfigureStep(
                steps.GetArrayElementAtIndex(6), "ai_evidence", aiEvidenceTarget, TutorialStepType.NextButton,
                "AI辅助结果已经整理到证据板中。\n它可以作为判断参考，但最终诊断仍需要结合其他证据。",
                new Vector2(-293f, 220.25f), new Vector2(151f, 54f), 97.546f,
                new Vector2(206.6942f, 191.1258f), new Vector2(-1.1935f, 1f),
                new Vector4(44.44835f, 11.99987f, 12.00005f, 36.33623f));
            ConfigureStep(
                steps.GetArrayElementAtIndex(7), "diagnosis", diagnosisTarget, TutorialStepType.TargetInteraction,
                "结合已经获得的线索、星图和 AI 辅助结果，选择你认为最可能的癌症来源。\n\n请选择右侧一个诊断结果继续。",
                new Vector2(-245.25f, 13.5f), new Vector2(224f, -63f), 45f,
                new Vector2(210f, 210f), Vector2.one, new Vector4(12f, 12f, 12f, 12f));
            ConfigureStep(
                steps.GetArrayElementAtIndex(8), "submit", submitTarget, TutorialStepType.TargetInteraction,
                "确认选择后，点击“提交判断”。\n\n每个病例只能提交一次。",
                new Vector2(5f, -166f), new Vector2(420f, -374f), 45f,
                new Vector2(210f, 210f), Vector2.one, new Vector4(12f, 12f, 12f, 12f),
                default(Vector2), diagnosisTarget);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStep(
            SerializedProperty step,
            string id,
            RectTransform target,
            TutorialStepType stepType,
            string text,
            Vector2 dialogPosition,
            Vector2 arrowPosition,
            float arrowRotation,
            Vector2 arrowSize,
            Vector2 arrowScale,
            Vector4 padding,
            Vector2 dialogSize = default(Vector2),
            RectTransform interactionRegion = null,
            bool showHighlight = true,
            bool showArrow = true,
            Sprite nextButtonSprite = null)
        {
            step.FindPropertyRelative("id").stringValue = id;
            step.FindPropertyRelative("target").objectReferenceValue = target;
            step.FindPropertyRelative("interactionRegion").objectReferenceValue = interactionRegion;
            step.FindPropertyRelative("text").stringValue = text;
            step.FindPropertyRelative("stepType").enumValueIndex = (int)stepType;
            step.FindPropertyRelative("dialogPosition").vector2Value = dialogPosition;
            step.FindPropertyRelative("dialogSize").vector2Value = dialogSize;
            step.FindPropertyRelative("arrowPosition").vector2Value = arrowPosition;
            step.FindPropertyRelative("arrowRotation").floatValue = arrowRotation;
            step.FindPropertyRelative("arrowSize").vector2Value = arrowSize;
            step.FindPropertyRelative("arrowScale").vector2Value = arrowScale;
            step.FindPropertyRelative("showHighlight").boolValue = showHighlight;
            step.FindPropertyRelative("showArrow").boolValue = showArrow;
            step.FindPropertyRelative("nextButtonSprite").objectReferenceValue = nextButtonSprite;
            step.FindPropertyRelative("highlightPadding").vector4Value = padding;
        }

        private static void SetObject(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            serialized.FindProperty(name).objectReferenceValue = value;
        }

        private static RectTransform CreateMask(string name, Transform parent)
        {
            Image image = CreateImage(name, parent, null, new Color(0f, 0f, 0f, 0.58f));
            image.raycastTarget = true;
            return image.rectTransform;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Image image = Undo.AddComponent<Image>(rect.gameObject);
            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Simple;
            return image;
        }

        private static TMP_Text CreateTutorialText(RectTransform dialog, TMP_FontAsset font)
        {
            RectTransform rect = CreateRect(
                "TutorialText", dialog, new Vector2(0.12f, 0.16f), new Vector2(0.88f, 0.66f));
            TextMeshProUGUI text = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            text.font = font;
            text.fontSize = 25f;
            text.color = new Color32(65, 52, 57, 255);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateNextButton(Transform parent, Sprite sprite)
        {
            Image image = CreateImage("NextButton", parent, sprite, Color.white);
            image.preserveAspect = true;
            image.raycastTarget = true;
            image.rectTransform.sizeDelta = new Vector2(283.5538f, 134.9104f);
            Button button = Undo.AddComponent<Button>(image.gameObject);
            button.targetGraphic = image;
            return button;
        }

        private static void EnsureHighlightBorder(RectTransform highlight)
        {
            const float thickness = 5f;
            Color color = new Color32(255, 224, 105, 235);
            ConfigureBorder(FindOrCreateBorder("BorderTop", highlight),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, thickness), Vector2.zero, color);
            ConfigureBorder(FindOrCreateBorder("BorderBottom", highlight),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, thickness), Vector2.zero, color);
            ConfigureBorder(FindOrCreateBorder("BorderLeft", highlight),
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(thickness, 0f), Vector2.zero, color);
            ConfigureBorder(FindOrCreateBorder("BorderRight", highlight),
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(thickness, 0f), Vector2.zero, color);
        }

        private static Image FindOrCreateBorder(string name, RectTransform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Image existingImage = existing.GetComponent<Image>();
                if (existingImage == null)
                {
                    throw new InvalidOperationException(name + " exists but has no Image component.");
                }
                return existingImage;
            }
            return CreateImage(name, parent, null, Color.white);
        }

        private static void ConfigureBorder(
            Image image,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta,
            Vector2 anchoredPosition,
            Color color)
        {
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            image.color = color;
            image.raycastTarget = false;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform RequireRect(Transform root, string name)
        {
            RectTransform match = null;
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rect in rects)
            {
                if (rect.name != name) continue;
                if (match != null) throw new InvalidOperationException("Multiple RectTransforms named " + name + ".");
                match = rect;
            }
            if (match == null) throw new InvalidOperationException("Required CaseAnalysis object is missing: " + name + ".");
            return match;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == name) return child;
            }
            return null;
        }

        private static RectTransform RequireDirectRect(Transform parent, string name)
        {
            Transform child = FindDirectChild(parent, name);
            RectTransform rect = child as RectTransform;
            if (rect == null) throw new InvalidOperationException("Required Tutorial object is missing: " + name + ".");
            return rect;
        }

        private static TutorialGuideController FindInstalledController()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException("Open Assets/Scenes/CaseAnalysis.unity first.");
            }

            Canvas canvas = FindSingleInScene<Canvas>(scene, "Canvas");
            Transform layer = FindDirectChild(canvas.transform, "TutorialGuideLayer");
            TutorialGuideController controller = layer == null ? null : layer.GetComponent<TutorialGuideController>();
            if (controller == null)
            {
                throw new InvalidOperationException("The installed TutorialGuideLayer or its controller is missing.");
            }
            return controller;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Tutorial preview editor actions are available only in Edit Mode.");
            }
        }

        private static void MarkTutorialObjectsDirty(RectTransform layer)
        {
            RectTransform[] rects = layer.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rect in rects) EditorUtility.SetDirty(rect);
            Graphic[] graphics = layer.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics) EditorUtility.SetDirty(graphic);
            EditorUtility.SetDirty(layer.GetComponent<TutorialGuideController>());
        }

        private static T FindSingleInScene<T>(Scene scene, string objectName) where T : Component
        {
            T match = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T[] components = root.GetComponentsInChildren<T>(true);
                foreach (T component in components)
                {
                    if (component.gameObject.name != objectName) continue;
                    if (match != null) throw new InvalidOperationException("Multiple objects named " + objectName + ".");
                    match = component;
                }
            }
            if (match == null) throw new InvalidOperationException("Required object is missing: " + objectName + ".");
            return match;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required asset is missing or not imported correctly: " + path);
            return asset;
        }

        private static void EnsureSpriteImport(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null) throw new InvalidOperationException("Tutorial texture is missing: " + path);
            if (importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
