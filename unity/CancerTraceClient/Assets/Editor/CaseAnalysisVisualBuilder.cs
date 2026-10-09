using System;
using CancerTrace.UI.Controllers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CancerTrace.EditorTools
{
    public static class CaseAnalysisVisualBuilder
    {
        private const string ScenePath = "Assets/Scenes/CaseAnalysis.unity";
        private const string FontPath = "Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset";
        private const string BackgroundPath = "Assets/Art/Background/bg_caseanalysis.png";
        private const string GeneButtonPath = "Assets/Art/UI/Buttons/ui_btn_gene_scan_normal.png.png";
        private const string ObserveButtonPath = "Assets/Art/UI/Buttons/ui_btn_observe_normal.png.png";
        private const string HintButtonPath = "Assets/Art/UI/Buttons/ui_btn_hint_normal.png.png";
        private const string SubmitButtonPath = "Assets/Art/UI/Buttons/ui_btn_submit_normal.png.png";
        private const string LockedEvidencePath = "Assets/Art/UI/TitleBanners/ui_evidence_locked.png";

        [MenuItem("CancerTrace/CaseAnalysis/Rebuild Visual Skeleton")]
        public static void RebuildVisualSkeleton()
        {
            TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMainCamera();
            GameObject root = new GameObject("CaseAnalysisController");
            CaseAnalysisController controller = root.AddComponent<CaseAnalysisController>();
            controller.BuildUi(
                font,
                LoadRequired<Sprite>(BackgroundPath),
                null,
                null,
                null,
                LoadRequired<Sprite>(GeneButtonPath),
                LoadRequired<Sprite>(ObserveButtonPath),
                LoadRequired<Sprite>(HintButtonPath),
                LoadRequired<Sprite>(SubmitButtonPath),
                LoadRequired<Sprite>(LockedEvidencePath));

            ApplyFormalFont(root, font);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Could not save CaseAnalysis scene.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CASE_ANALYSIS_BACKGROUND_REBUILT");
        }

        [MenuItem("CancerTrace/CaseAnalysis/Validate Visual Skeleton")]
        public static void ValidateVisualSkeleton()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("CaseAnalysisController");
            CaseAnalysisController controller = root == null ? null : root.GetComponent<CaseAnalysisController>();
            if (controller == null)
            {
                throw new InvalidOperationException("CaseAnalysisController is missing.");
            }
            SerializedObject bindings = new SerializedObject(controller);
            string[] requiredBindings =
            {
                "shiftText", "caseText", "caseIdText", "rpText", "scoreText", "feedbackText",
                "geneScanLockedVisual", "geneScanUnlockedContent", "galaxyLockedVisual", "galaxyUnlockedContent",
                "galaxyEvidenceButton",
                "aiLockedVisual", "aiUnlockedContent", "aiPredictionText", "aiConfidenceText", "aiTopCandidatesText",
                "geneScanButton", "galaxyButton", "aiButton", "submitButton"
            };
            for (int index = 0; index < requiredBindings.Length; index++)
            {
                SerializedProperty property = bindings.FindProperty(requiredBindings[index]);
                if (property == null || property.objectReferenceValue == null)
                {
                    throw new InvalidOperationException("Missing controller reference: " + requiredBindings[index]);
                }
            }
            string[] arrayBindings =
            {
                "initialClueCardTexts", "geneScanClueTexts", "geneScanStateTexts", "geneScanValueTexts", "diagnosisButtons",
                "diagnosisSelectionOverlays", "diagnosisSelectionMarks"
            };
            int[] arrayLengths = { 2, 3, 3, 3, 8, 8, 8 };
            for (int index = 0; index < arrayBindings.Length; index++)
            {
                SerializedProperty property = bindings.FindProperty(arrayBindings[index]);
                if (property == null || property.arraySize != arrayLengths[index])
                {
                    throw new InvalidOperationException("Missing controller array: " + arrayBindings[index]);
                }
                for (int element = 0; element < property.arraySize; element++)
                {
                    if (property.GetArrayElementAtIndex(element).objectReferenceValue == null)
                    {
                        throw new InvalidOperationException("Missing controller array reference: " + arrayBindings[index] + "[" + element + "]");
                    }
                }
            }

            string[] requiredObjects =
            {
                "CaseAnalysisBackground", "ToolsArea", "EvidenceBoard", "DiagnosisArea", "InitialClueText0", "InitialClueText1",
                "GeneScanEvidenceSlot", "GeneScanLockedVisual",
                "CancerGalaxyEvidenceSlot", "GalaxyEvidenceButton", "AiAssistantEvidenceSlot",
                "GeneScan", "CancerGalaxy", "AiAssistant", "Submit",
                "Shift", "Case", "CaseId", "RP", "Score"
            };
            for (int index = 0; index < requiredObjects.Length; index++)
            {
                if (GameObject.Find(requiredObjects[index]) == null)
                {
                    throw new InvalidOperationException("Required UI object is missing: " + requiredObjects[index]);
                }
            }
            if (GameObject.Find("CaseNotebook") != null || GameObject.Find("InitialClues") != null ||
                GameObject.Find("EvidenceTitle") != null || GameObject.Find("DiagnosisTitle") != null ||
                GameObject.Find("TopBar") != null)
            {
                throw new InvalidOperationException("A duplicate legacy visual layer is still present.");
            }

            UnityEngine.UI.Image backgroundImage = GameObject.Find("CaseAnalysisBackground").GetComponent<UnityEngine.UI.Image>();
            if (backgroundImage == null || backgroundImage.sprite != LoadRequired<Sprite>(BackgroundPath) ||
                !backgroundImage.preserveAspect || backgroundImage.raycastTarget)
            {
                throw new InvalidOperationException("The CaseAnalysis background is not configured correctly.");
            }
            RectTransform backgroundRect = (RectTransform)backgroundImage.transform;
            if (backgroundRect.anchorMin != Vector2.zero || backgroundRect.anchorMax != Vector2.one ||
                backgroundRect.offsetMin != Vector2.zero || backgroundRect.offsetMax != Vector2.zero)
            {
                throw new InvalidOperationException("The CaseAnalysis background does not stretch full screen.");
            }

            TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                SerializedObject serializedText = new SerializedObject(texts[index]);
                SerializedProperty fontProperty = serializedText.FindProperty("m_fontAsset");
                if (fontProperty == null || fontProperty.objectReferenceValue != font)
                {
                    throw new InvalidOperationException("Non-formal TMP font on " + texts[index].name + ".");
                }
            }
            if (root.GetComponentsInChildren<UnityEngine.UI.Text>(true).Length != 0)
            {
                throw new InvalidOperationException("Legacy UGUI Text exists in CaseAnalysis.");
            }
            if (root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length < 13)
            {
                throw new InvalidOperationException("CaseAnalysis does not contain all tool, diagnosis, and submit buttons.");
            }

            GameObject geneScanSlot = GameObject.Find("GeneScanEvidenceSlot");
            Transform unlockedContent = geneScanSlot == null ? null : geneScanSlot.transform.Find("GeneScanContent");
            if (unlockedContent == null || unlockedContent.gameObject.activeSelf ||
                unlockedContent.GetComponent<UnityEngine.UI.Image>() != null)
            {
                throw new InvalidOperationException("Gene Scan locked/unlocked evidence states are invalid.");
            }
            for (int index = 0; index < 3; index++)
            {
                Transform row = unlockedContent.Find("GeneClue" + index);
                TMP_Text geneName = row == null ? null : row.Find("GeneName")?.GetComponent<TMP_Text>();
                TMP_Text state = row == null ? null : row.Find("State")?.GetComponent<TMP_Text>();
                TMP_Text value = row == null ? null : row.Find("Value")?.GetComponent<TMP_Text>();
                if (geneName == null || state == null || value == null)
                {
                    throw new InvalidOperationException("Gene Scan clue row is missing: " + index);
                }
                if (geneName.enableWordWrapping || geneName.overflowMode != TextOverflowModes.Overflow ||
                    !geneName.enableAutoSizing || state.enableWordWrapping || value.enableWordWrapping ||
                    state.overflowMode != TextOverflowModes.Overflow || value.overflowMode != TextOverflowModes.Overflow)
                {
                    throw new InvalidOperationException("Gene Scan clue text can truncate or wrap: " + index);
                }
                RectTransform geneRect = geneName.rectTransform;
                RectTransform stateRect = state.rectTransform;
                RectTransform valueRect = value.rectTransform;
                if (geneRect.anchorMin.y < stateRect.anchorMax.y || geneRect.anchorMin.y < valueRect.anchorMax.y ||
                    stateRect.anchorMax.x >= valueRect.anchorMin.x ||
                    geneRect.anchorMin.x < 0f || geneRect.anchorMax.x > 1f ||
                    stateRect.anchorMin.x < 0f || valueRect.anchorMax.x > 1f)
                {
                    throw new InvalidOperationException("Gene Scan clue text overlaps or exceeds its row: " + index);
                }
            }
            string[] slotNames = { "GeneScanEvidenceSlot", "CancerGalaxyEvidenceSlot", "AiAssistantEvidenceSlot" };
            string[] lockedNames = { "GeneScanLockedVisual", "GalaxyLockedVisual", "AiLockedVisual" };
            string[] contentNames = { "GeneScanContent", "GalaxyUnlockedContent", "AiUnlockedContent" };
            Vector2[] lockedMins =
            {
                new Vector2(0.14f, 0.27f), new Vector2(0.16f, 0.27f), new Vector2(0.16f, 0.27f)
            };
            Vector2[] lockedMaxs =
            {
                new Vector2(0.86f, 0.90f), new Vector2(0.84f, 0.90f), new Vector2(0.84f, 0.90f)
            };
            Vector2[] contentMins =
            {
                new Vector2(0.04f, 0.22f), new Vector2(0.08f, 0.25f), new Vector2(0.08f, 0.25f)
            };
            Vector2[] contentMaxs =
            {
                new Vector2(0.96f, 0.96f), new Vector2(0.92f, 0.90f), new Vector2(0.92f, 0.90f)
            };
            Sprite lockedSprite = LoadRequired<Sprite>(LockedEvidencePath);
            for (int index = 0; index < slotNames.Length; index++)
            {
                Transform slot = GameObject.Find(slotNames[index]).transform;
                Transform lockedTransform = slot.Find(lockedNames[index]);
                Transform contentTransform = slot.Find(contentNames[index]);
                UnityEngine.UI.Image lockedImage = lockedTransform == null
                    ? null
                    : lockedTransform.GetComponent<UnityEngine.UI.Image>();
                if (lockedImage == null || lockedImage.sprite != lockedSprite || !lockedImage.preserveAspect ||
                    lockedImage.raycastTarget || !lockedImage.gameObject.activeSelf ||
                    contentTransform == null || contentTransform.gameObject.activeSelf ||
                    contentTransform.GetComponent<UnityEngine.UI.Image>() != null)
                {
                    throw new InvalidOperationException("Evidence locked/unlocked visual is invalid: " + slotNames[index]);
                }
                RectTransform lockedRect = lockedTransform.GetComponent<RectTransform>();
                RectTransform contentRect = contentTransform.GetComponent<RectTransform>();
                if (lockedRect.anchorMin != lockedMins[index] || lockedRect.anchorMax != lockedMaxs[index] ||
                    contentRect.anchorMin != contentMins[index] || contentRect.anchorMax != contentMaxs[index])
                {
                    throw new InvalidOperationException("Evidence state layout changed: " + slotNames[index]);
                }
            }
            Transform aiContent = GameObject.Find("AiAssistantEvidenceSlot").transform.Find("AiUnlockedContent");
            TMP_Text predictionLabel = aiContent == null ? null : aiContent.Find("PredictionLabel")?.GetComponent<TMP_Text>();
            TMP_Text predictionValue = aiContent == null ? null : aiContent.Find("PredictionValue")?.GetComponent<TMP_Text>();
            TMP_Text confidenceLabel = aiContent == null ? null : aiContent.Find("ConfidenceLabel")?.GetComponent<TMP_Text>();
            TMP_Text confidenceValue = aiContent == null ? null : aiContent.Find("ConfidenceValue")?.GetComponent<TMP_Text>();
            TMP_Text top3Label = aiContent == null ? null : aiContent.Find("Top3Label")?.GetComponent<TMP_Text>();
            TMP_Text topCandidates = aiContent == null ? null : aiContent.Find("TopCandidates")?.GetComponent<TMP_Text>();
            if (predictionLabel == null || predictionLabel.text != "AI预测" || predictionValue == null ||
                confidenceLabel == null || confidenceLabel.text != "置信度" || confidenceValue == null ||
                top3Label == null || top3Label.text != "Top 3" || topCandidates == null)
            {
                throw new InvalidOperationException("AI evidence summary structure is incomplete.");
            }
            TMP_Text[] aiDynamicTexts = { predictionValue, confidenceValue, topCandidates };
            for (int index = 0; index < aiDynamicTexts.Length; index++)
            {
                TMP_Text value = aiDynamicTexts[index];
                RectTransform rect = value.rectTransform;
                if (!value.enableAutoSizing || value.enableWordWrapping || value.overflowMode != TextOverflowModes.Overflow ||
                    rect.anchorMin.x < 0f || rect.anchorMin.y < 0f || rect.anchorMax.x > 1f || rect.anchorMax.y > 1f)
                {
                    throw new InvalidOperationException("AI evidence text layout is invalid: " + value.name);
                }
            }
            Transform galaxySlot = GameObject.Find("CancerGalaxyEvidenceSlot").transform;
            Transform galaxyButtonTransform = galaxySlot.Find("GalaxyEvidenceButton");
            UnityEngine.UI.Button galaxyCardButton = galaxyButtonTransform == null
                ? null
                : galaxyButtonTransform.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Image galaxyButtonImage = galaxyButtonTransform == null
                ? null
                : galaxyButtonTransform.GetComponent<UnityEngine.UI.Image>();
            RectTransform galaxyButtonRect = galaxyButtonTransform == null
                ? null
                : galaxyButtonTransform.GetComponent<RectTransform>();
            if (galaxyCardButton == null || galaxyCardButton.interactable || galaxyButtonImage == null ||
                galaxyButtonImage.sprite != null || galaxyButtonImage.color.a != 0f || !galaxyButtonImage.raycastTarget ||
                galaxyButtonRect.anchorMin != Vector2.zero || galaxyButtonRect.anchorMax != Vector2.one ||
                galaxyButtonRect.offsetMin != Vector2.zero || galaxyButtonRect.offsetMax != Vector2.zero)
            {
                throw new InvalidOperationException("Galaxy evidence button is missing or has an invalid hit area.");
            }
            TMP_Text[] evidenceTexts = geneScanSlot.transform.parent.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < evidenceTexts.Length; index++)
            {
                string value = evidenceTexts[index].text;
                if (value.Contains("待解锁") || value.Contains("扫描证据") ||
                    value.Contains("星图证据") || value.Contains("AI辅助") || value.Contains("true class") ||
                    value.Contains("ai.correct") || value.Contains("ANOMALY") || value.Contains("Explanation"))
                {
                    throw new InvalidOperationException("Duplicate evidence title or locked TMP text exists: " + evidenceTexts[index].name);
                }
            }
            for (int index = 0; index < 8; index++)
            {
                GameObject diagnosis = GameObject.Find("Diagnosis" + index);
                UnityEngine.UI.Button button = diagnosis == null ? null : diagnosis.GetComponent<UnityEngine.UI.Button>();
                UnityEngine.UI.Image image = diagnosis == null ? null : diagnosis.GetComponent<UnityEngine.UI.Image>();
                if (button == null || image == null || image.sprite != null || image.color.a != 0f)
                {
                    throw new InvalidOperationException("Diagnosis hit area is missing or not transparent: " + index);
                }

                RectTransform diagnosisRect = diagnosis.GetComponent<RectTransform>();
                int row = index / 2;
                int column = index % 2;
                float expectedMinX = column == 0 ? 0.035f : 0.515f;
                float expectedMaxX = column == 0 ? 0.485f : 0.965f;
                float expectedMaxY = 0.76f - row * 0.15f;
                if (diagnosisRect.anchorMin != new Vector2(expectedMinX, expectedMaxY - 0.13f) ||
                    diagnosisRect.anchorMax != new Vector2(expectedMaxX, expectedMaxY))
                {
                    throw new InvalidOperationException("Diagnosis hit area moved: " + index);
                }

                Transform overlayTransform = diagnosis.transform.Find("SelectionOverlay");
                UnityEngine.UI.Image overlay = overlayTransform == null
                    ? null
                    : overlayTransform.GetComponent<UnityEngine.UI.Image>();
                UnityEngine.UI.Outline outline = overlayTransform == null
                    ? null
                    : overlayTransform.GetComponent<UnityEngine.UI.Outline>();
                Transform checkTransform = diagnosis.transform.Find("CheckMark");
                TMP_Text checkMark = checkTransform == null ? null : checkTransform.GetComponent<TMP_Text>();
                if (overlay == null || outline == null || overlay.raycastTarget ||
                    overlay.color.a < 0.15f || overlay.color.a > 0.25f || overlay.gameObject.activeSelf)
                {
                    throw new InvalidOperationException("Diagnosis selection overlay is invalid: " + index);
                }
                if (checkMark == null || checkMark.text != "✓" || checkMark.raycastTarget ||
                    checkMark.color.g <= checkMark.color.r || checkMark.color.g <= checkMark.color.b ||
                    checkMark.gameObject.activeSelf)
                {
                    throw new InvalidOperationException("Diagnosis check mark is invalid: " + index);
                }
            }
            string[] visualButtons = { "GeneScan", "CancerGalaxy", "AiAssistant", "Submit" };
            string[] visualButtonPaths = { GeneButtonPath, ObserveButtonPath, HintButtonPath, SubmitButtonPath };
            for (int index = 0; index < visualButtons.Length; index++)
            {
                GameObject item = GameObject.Find(visualButtons[index]);
                UnityEngine.UI.Image image = item == null ? null : item.GetComponent<UnityEngine.UI.Image>();
                if (item == null || item.GetComponent<UnityEngine.UI.Button>() == null || image == null ||
                    image.sprite != LoadRequired<Sprite>(visualButtonPaths[index]) || image.color.a <= 0f || !image.raycastTarget)
                {
                    throw new InvalidOperationException("Formal button visual is missing: " + visualButtons[index]);
                }
            }
            UnityEngine.UI.Image[] images = root.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                if (images[index].sprite != null && images[index] != backgroundImage &&
                    Array.IndexOf(visualButtons, images[index].name) < 0 &&
                    Array.IndexOf(lockedNames, images[index].name) < 0)
                {
                    throw new InvalidOperationException("Duplicate sprite layer: " + images[index].name);
                }
            }

            MonoBehaviour[] scripts = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int index = 0; index < scripts.Length; index++)
            {
                if (scripts[index] == null) throw new InvalidOperationException("Missing Script in CaseAnalysis.");
            }
            Debug.Log("CASE_ANALYSIS_BACKGROUND_VALIDATED: scene=" + scene.name + "; tmp=" + texts.Length + "; buttons=13");
        }

        private static void CreateMainCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(39, 48, 67, 255);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void ApplyFormalFont(GameObject root, TMP_FontAsset font)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                SerializedObject serializedText = new SerializedObject(texts[index]);
                SerializedProperty fontProperty = serializedText.FindProperty("m_fontAsset");
                SerializedProperty materialProperty = serializedText.FindProperty("m_sharedMaterial");
                if (fontProperty == null || materialProperty == null)
                {
                    throw new InvalidOperationException("TMP serialized font fields are unavailable on " + texts[index].name + ".");
                }
                fontProperty.objectReferenceValue = font;
                materialProperty.objectReferenceValue = font.material;
                serializedText.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(texts[index]);
            }
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required asset is missing: " + path);
            return asset;
        }
    }
}
