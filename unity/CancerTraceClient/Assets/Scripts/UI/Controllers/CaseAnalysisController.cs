using System;
using System.Collections.Generic;
using System.Globalization;
using CancerTrace.Gameplay.Flow;
using CancerTrace.Gameplay.Tutorial;
using CancerTrace.UI.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.UI.Controllers
{
    public sealed class CaseAnalysisController : MonoBehaviour
    {
        [SerializeField] private TMP_Text shiftText;
        [SerializeField] private TMP_Text caseText;
        [SerializeField] private TMP_Text caseIdText;
        [SerializeField] private TMP_Text rpText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text[] initialClueCardTexts;
        [SerializeField] private Image geneScanLockedVisual;
        [SerializeField] private GameObject geneScanUnlockedContent;
        [SerializeField] private TMP_Text[] geneScanClueTexts;
        [SerializeField] private TMP_Text[] geneScanStateTexts;
        [SerializeField] private TMP_Text[] geneScanValueTexts;
        [SerializeField] private Image galaxyLockedVisual;
        [SerializeField] private GameObject galaxyUnlockedContent;
        [SerializeField] private Button galaxyEvidenceButton;
        [SerializeField] private Image aiLockedVisual;
        [SerializeField] private GameObject aiUnlockedContent;
        [SerializeField] private TMP_Text aiPredictionText;
        [SerializeField] private TMP_Text aiConfidenceText;
        [SerializeField] private TMP_Text aiTopCandidatesText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button geneScanButton;
        [SerializeField] private Button galaxyButton;
        [SerializeField] private Button aiButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button[] diagnosisButtons;
        [SerializeField] private GameObject[] diagnosisSelectionOverlays;
        [SerializeField] private GameObject[] diagnosisSelectionMarks;

        private CancerTraceApp app;
        private PlayerCaseView currentCase;
        private TutorialGuideController tutorialGuide;
        private static readonly string[] DiagnosisClassIds =
        {
            "lung", "skin", "cns_brain", "bowel", "esophagus_stomach",
            "breast", "bone", "ovary_fallopian_tube"
        };
        private bool initialized;

        private void Awake()
        {
            app = CancerTraceApp.EnsureInstance();
            geneScanButton.onClick.AddListener(UseGeneScan);
            galaxyButton.onClick.AddListener(UseGalaxy);
            galaxyEvidenceButton.onClick.AddListener(OpenGalaxy);
            aiButton.onClick.AddListener(UseAiAssistant);
            submitButton.onClick.AddListener(SubmitDiagnosis);
            SetInteractive(false);
            feedbackText.text = "正在恢复调查状态……";
        }

        private void Update()
        {
            if (initialized) return;
            if (app == null) app = CancerTraceApp.EnsureInstance();
            if (app.IsReady)
            {
                initialized = true;
                if (app.Tutorial.IsTutorialActive)
                {
                    RefreshTutorial();
                }
                else
                {
                    Refresh("案件档案已打开。请选择调查工具或直接提交诊断。");
                }
            }
            else if (!string.IsNullOrEmpty(app.ErrorMessage))
            {
                feedbackText.text = "数据加载失败：" + app.ErrorMessage;
            }
        }

        private void OnDestroy()
        {
            if (tutorialGuide != null)
            {
                tutorialGuide.RuntimeNextRequested -= AdvanceTutorialStep;
            }
        }

        private void SetInteractive(bool value)
        {
            geneScanButton.interactable = value;
            galaxyButton.interactable = value;
            galaxyEvidenceButton.interactable = value;
            aiButton.interactable = value;
            submitButton.interactable = value;
            if (diagnosisButtons == null) return;
            for (int index = 0; index < diagnosisButtons.Length; index++)
            {
                diagnosisButtons[index].interactable = value;
            }
        }

        private void Refresh(string feedback)
        {
            GameplayActionResult<PlayerCaseView> caseResult = app.Gameplay.GetCurrentCase();
            if (!caseResult.Success)
            {
                feedbackText.text = caseResult.Message;
                SetInteractive(false);
                return;
            }

            currentCase = caseResult.Data;
            shiftText.text = "Shift\n" + currentCase.ShiftNumber;
            caseText.text = "Case\n" + currentCase.CaseNumber + " / " + currentCase.CasesPerShift;
            caseIdText.text = "Case ID\n" + currentCase.CaseId;
            rpText.text = "Remaining RP\n" + currentCase.RemainingRp;
            scoreText.text = "Score\n" + currentCase.CurrentScore;
            GameplayActionResult<EvidenceBoardView> evidence = app.Gameplay.GetEvidenceBoard();
            if (evidence.Success) RenderEvidence(evidence.Data);

            RenderTools();
            RenderDiagnosisOptions();
            submitButton.interactable = !currentCase.DiagnosisSubmitted;
            feedbackText.text = feedback;
        }

        private void RefreshTutorial(string feedback = null)
        {
            TutorialCaseView tutorialCase;
            string errorMessage;
            if (!app.Tutorial.TryGetCurrentCase(out tutorialCase, out errorMessage))
            {
                feedbackText.text = errorMessage;
                SetInteractive(false);
                return;
            }

            currentCase = null;
            shiftText.text = "Tutorial";
            caseText.text = "Case\n1 / 1";
            caseIdText.text = "Case ID\n" + tutorialCase.CaseId;
            rpText.text = "Remaining RP\n" + tutorialCase.RemainingRp;
            scoreText.text = "Score\n0";
            RenderTutorialEvidence(tutorialCase);
            SetInteractive(false);
            int stepIndex = app.Tutorial.CurrentSession.CurrentStepIndex;
            geneScanButton.interactable =
                stepIndex == TutorialService.GeneScanStepIndex && !tutorialCase.GeneScanUsed;
            galaxyButton.interactable =
                stepIndex == TutorialService.CancerGalaxyStepIndex && !tutorialCase.GalaxyUsed;
            aiButton.interactable =
                stepIndex == TutorialService.AiReadyStepIndex && !tutorialCase.AiUsed;
            bool diagnosisActive =
                (stepIndex == TutorialService.DiagnosisStepIndex ||
                 stepIndex == TutorialService.SubmitStepIndex) &&
                !app.Tutorial.CurrentSession.Submitted;
            RenderTutorialDiagnosisOptions(diagnosisActive, app.Tutorial.CurrentSession.DraftDiagnosis);
            submitButton.interactable =
                stepIndex == TutorialService.SubmitStepIndex &&
                !string.IsNullOrEmpty(app.Tutorial.CurrentSession.DraftDiagnosis) &&
                !app.Tutorial.CurrentSession.Submitted;
            feedbackText.text = feedback ?? GetTutorialFeedback(stepIndex);

            tutorialGuide = FindObjectOfType<TutorialGuideController>(true);
            if (tutorialGuide == null)
            {
                feedbackText.text = "教学引导层缺失，无法显示教程。";
                return;
            }

            tutorialGuide.RuntimeNextRequested -= AdvanceTutorialStep;
            tutorialGuide.RuntimeNextRequested += AdvanceTutorialStep;
            if (stepIndex <= TutorialService.SubmitStepIndex)
            {
                int guideStepIndex = stepIndex >= TutorialService.AiReadyStepIndex
                    ? stepIndex - 1
                    : stepIndex;
                tutorialGuide.BeginRuntimeTutorial(guideStepIndex);
            }
            else
            {
                tutorialGuide.HideRuntimeTutorial();
            }
        }

        private void RenderTutorialDiagnosisOptions(bool interactable, string selectedDiagnosis)
        {
            for (int index = 0; index < diagnosisButtons.Length; index++)
            {
                Button button = diagnosisButtons[index];
                button.onClick.RemoveAllListeners();
                string classId = DiagnosisClassIds[index];
                button.onClick.AddListener(() => SelectDiagnosis(classId));
                button.interactable = interactable;
                bool isSelected = string.Equals(selectedDiagnosis, classId, StringComparison.Ordinal);
                diagnosisSelectionOverlays[index].SetActive(isSelected);
                diagnosisSelectionMarks[index].SetActive(isSelected);
            }
        }

        private void RenderTutorialEvidence(TutorialCaseView tutorialCase)
        {
            for (int index = 0; index < initialClueCardTexts.Length; index++)
            {
                TMP_Text cardText = initialClueCardTexts[index];
                if (cardText == null) continue;
                cardText.text = index < tutorialCase.InitialClues.Count
                    ? BuildClueCard(tutorialCase.InitialClues[index])
                    : "Initial Clue\n读取中…";
            }

            bool showGeneScanClues = tutorialCase.GeneScanUsed && tutorialCase.GeneScanClues.Count == 3;
            RenderGeneScanEvidence(tutorialCase.GeneScanClues, showGeneScanClues);
            bool showGalaxyEvidence = tutorialCase.GalaxyUsed && tutorialCase.GalaxyEvidence != null;
            galaxyLockedVisual.gameObject.SetActive(!showGalaxyEvidence);
            galaxyUnlockedContent.SetActive(showGalaxyEvidence);
            galaxyEvidenceButton.interactable = showGalaxyEvidence;
            bool showAiEvidence = tutorialCase.AiUsed && tutorialCase.AiEvidence != null;
            aiLockedVisual.gameObject.SetActive(!showAiEvidence);
            aiUnlockedContent.SetActive(showAiEvidence);
            if (showAiEvidence) RenderAiEvidence(tutorialCase.AiEvidence);

            for (int index = 0; index < diagnosisSelectionOverlays.Length; index++)
            {
                diagnosisSelectionOverlays[index].SetActive(false);
                diagnosisSelectionMarks[index].SetActive(false);
            }
        }

        private void AdvanceTutorialStep()
        {
            string errorMessage;
            if (!app.Tutorial.TryAdvanceNextStep(out errorMessage))
            {
                feedbackText.text = errorMessage;
                return;
            }

            RefreshTutorial();
        }

        private static string GetTutorialFeedback(int stepIndex)
        {
            if (stepIndex == TutorialService.GeneScanStepIndex)
            {
                return "教学模式：请点击左侧“扫描”按钮获取 3 条真实线索。";
            }
            if (stepIndex == TutorialService.GeneScanEvidenceStepIndex)
            {
                return "教学扫描完成：3 条新线索已加入证据板。";
            }
            if (stepIndex == TutorialService.CancerGalaxyStepIndex)
            {
                return "教学模式：请点击左侧“观察星图”按钮。";
            }
            if (stepIndex == TutorialService.AiReadyStepIndex)
            {
                return "教学模式：请点击左侧“AI辅助”按钮。";
            }
            if (stepIndex == TutorialService.AiEvidenceStepIndex)
            {
                return "AI 辅助结果已加入证据板。点击下一步选择诊断。";
            }
            if (stepIndex == TutorialService.DiagnosisStepIndex)
            {
                return "教学模式：请从右侧选择一个诊断结果。";
            }
            if (stepIndex == TutorialService.SubmitStepIndex)
            {
                return "已选择诊断；确认后请点击“提交判断”。";
            }
            return "教学模式：请按照引导认识调查界面。";
        }

        private void RenderTools()
        {
            RenderToolButton(geneScanButton, currentCase.GeneScan);
            galaxyButton.interactable = currentCase.CancerGalaxy.Used || currentCase.CancerGalaxy.CanUse;
            RenderToolButton(aiButton, currentCase.AiAssistant);
        }

        private static void RenderToolButton(Button button, ToolAvailabilityView tool)
        {
            button.interactable = tool.CanUse;
        }

        private void RenderDiagnosisOptions()
        {
            IReadOnlyList<CancerTypeOptionView> options = app.Gameplay.GetDiagnosisOptions();
            for (int index = 0; index < diagnosisButtons.Length; index++)
            {
                Button button = diagnosisButtons[index];
                button.onClick.RemoveAllListeners();
                string classId = DiagnosisClassIds[index];
                bool optionExists = false;
                for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
                {
                    if (string.Equals(options[optionIndex].ClassId, classId, StringComparison.Ordinal))
                    {
                        optionExists = true;
                        break;
                    }
                }
                button.interactable = optionExists && !currentCase.DiagnosisSubmitted;
                button.onClick.AddListener(() => SelectDiagnosis(classId));
                bool isSelected = string.Equals(currentCase.SelectedDiagnosis, classId, StringComparison.Ordinal);
                diagnosisSelectionOverlays[index].SetActive(isSelected);
                diagnosisSelectionMarks[index].SetActive(isSelected);
            }
        }

        private void RenderEvidence(EvidenceBoardView evidence)
        {
            if (initialClueCardTexts != null)
            {
                for (int index = 0; index < initialClueCardTexts.Length; index++)
                {
                    TMP_Text cardText = initialClueCardTexts[index];
                    if (cardText == null) continue;
                    cardText.text = index < evidence.InitialClues.Count
                        ? BuildClueCard(evidence.InitialClues[index])
                        : "Initial Clue\n读取中…";
                }
            }

            bool geneScanUsed = currentCase != null && currentCase.GeneScan != null && currentCase.GeneScan.Used;
            bool showGeneScanClues = geneScanUsed && evidence.GeneScanClues.Count == 3;
            RenderGeneScanEvidence(evidence.GeneScanClues, showGeneScanClues);

            bool showGalaxyEvidence = currentCase != null && currentCase.CancerGalaxy != null &&
                currentCase.CancerGalaxy.Used && evidence.GalaxyEvidence != null;
            galaxyLockedVisual.gameObject.SetActive(!showGalaxyEvidence);
            galaxyUnlockedContent.SetActive(showGalaxyEvidence);
            galaxyEvidenceButton.interactable = showGalaxyEvidence;

            bool showAiEvidence = currentCase != null && currentCase.AiAssistant != null &&
                currentCase.AiAssistant.Used && evidence.AiEvidence != null;
            aiLockedVisual.gameObject.SetActive(!showAiEvidence);
            aiUnlockedContent.SetActive(showAiEvidence);
            if (showAiEvidence) RenderAiEvidence(evidence.AiEvidence);
        }

        private void RenderGeneScanEvidence(IReadOnlyList<ClueView> clues, bool showGeneScanClues)
        {
            geneScanLockedVisual.gameObject.SetActive(!showGeneScanClues);
            geneScanUnlockedContent.SetActive(showGeneScanClues);
            if (showGeneScanClues)
            {
                for (int index = 0; index < geneScanClueTexts.Length; index++)
                {
                    ClueView clue = clues[index];
                    string state = clue.State == null ? string.Empty : clue.State.ToUpperInvariant();
                    string arrow = string.Equals(state, "HIGH", StringComparison.Ordinal) ? " ↑" :
                        string.Equals(state, "LOW", StringComparison.Ordinal) ? " ↓" : string.Empty;
                    geneScanClueTexts[index].text = clue.Gene;
                    geneScanStateTexts[index].text = state + arrow;
                    geneScanStateTexts[index].color = string.Equals(state, "HIGH", StringComparison.Ordinal)
                        ? new Color32(181, 108, 114, 255)
                        : new Color32(98, 127, 160, 255);
                    geneScanValueTexts[index].text = clue.ExpressionValue.ToString("0.###");
                }
            }
        }

        private void RenderAiEvidence(AiAssistantView evidence)
        {
            aiPredictionText.text = !string.IsNullOrEmpty(evidence.PredictionLabelZh)
                ? evidence.PredictionLabelZh
                : evidence.PredictionLabelEn;
            double confidencePercent = evidence.Confidence >= 0d && evidence.Confidence <= 1d
                ? evidence.Confidence * 100d
                : evidence.Confidence;
            aiConfidenceText.text = confidencePercent.ToString("0.#", CultureInfo.InvariantCulture) + "%";

            string candidates = string.Empty;
            string[] candidateMarkers = { "①", "②", "③" };
            int count = Math.Min(3, evidence.TopCandidates.Count);
            for (int index = 0; index < count; index++)
            {
                AiCandidateView candidate = evidence.TopCandidates[index];
                string label = !string.IsNullOrEmpty(candidate.LabelZh) ? candidate.LabelZh : candidate.LabelEn;
                if (index > 0) candidates += "\n";
                candidates += candidateMarkers[index] + " " + label;
            }
            aiTopCandidatesText.text = candidates;
        }

        private static string BuildClueCard(ClueView clue)
        {
            string state = clue.State == null ? string.Empty : clue.State.ToUpperInvariant();
            string arrow = string.Equals(state, "HIGH", StringComparison.Ordinal) ? " ↑" :
                string.Equals(state, "LOW", StringComparison.Ordinal) ? " ↓" : string.Empty;
            string color = string.Equals(state, "HIGH", StringComparison.Ordinal) ? "#C45063" : "#4D79B8";
            return "<size=30><b>" + clue.Gene + "</b></size>\n" +
                   "<size=23><color=" + color + "><b>" + state + arrow + "</b></color></size>\n" +
                   "<size=20>Expression  " + clue.ExpressionValue.ToString("0.###") + "</size>";
        }

        private void UseGeneScan()
        {
            if (app.Tutorial.IsTutorialActive)
            {
                string errorMessage;
                if (!app.Tutorial.TryUseGeneScan(out errorMessage))
                {
                    feedbackText.text = errorMessage;
                    return;
                }

                RefreshTutorial("教学扫描完成：3 条真实线索已加入证据板。");
                return;
            }

            GameplayActionResult<GeneScanView> result = app.Gameplay.UseGeneScan();
            Refresh(result.Success ? "Gene Scan 完成：3 条新线索已加入证据板。" : result.Message);
        }

        private void UseGalaxy()
        {
            if (app.Tutorial.IsTutorialActive)
            {
                CancerGalaxyView tutorialGalaxy;
                string errorMessage;
                if (!app.Tutorial.TryUseCancerGalaxy(out tutorialGalaxy, out errorMessage))
                {
                    feedbackText.text = errorMessage;
                    return;
                }

                OpenGalaxy();
                return;
            }

            if (currentCase != null && currentCase.CancerGalaxy != null && currentCase.CancerGalaxy.Used)
            {
                OpenGalaxy();
                return;
            }

            GameplayActionResult<CancerGalaxyView> result = app.Gameplay.UseCancerGalaxy();
            if (!result.Success)
            {
                Refresh(result.Message);
                return;
            }

            OpenGalaxy();
        }

        private static void OpenGalaxy()
        {
            SceneManager.LoadScene("CancerGalaxy");
        }

        private void UseAiAssistant()
        {
            if (app.Tutorial.IsTutorialActive)
            {
                AiAssistantView tutorialAi;
                string errorMessage;
                if (!app.Tutorial.TryUseAiAssistant(out tutorialAi, out errorMessage))
                {
                    feedbackText.text = errorMessage;
                    return;
                }

                RefreshTutorial("AI 辅助结果已加入证据板。");
                return;
            }

            GameplayActionResult<AiAssistantView> result = app.Gameplay.UseAiAssistant();
            Refresh(result.Success ? "AI Assistant 分析完成。" : result.Message);
        }

        private void SelectDiagnosis(string classId)
        {
            if (app.Tutorial.IsTutorialActive)
            {
                string errorMessage;
                if (!app.Tutorial.SelectDiagnosis(classId, out errorMessage))
                {
                    feedbackText.text = errorMessage;
                    return;
                }

                RefreshTutorial("已选择诊断：" + classId);
                return;
            }

            GameplayActionResult<PlayerCaseView> result = app.Gameplay.SelectDiagnosis(classId);
            Refresh(result.Success ? "已选择诊断：" + classId : result.Message);
        }

        private void SubmitDiagnosis()
        {
            if (app.Tutorial.IsTutorialActive)
            {
                ResultView tutorialResult;
                string errorMessage;
                if (!app.Tutorial.SubmitDiagnosis(out tutorialResult, out errorMessage))
                {
                    feedbackText.text = errorMessage;
                    return;
                }

                SceneManager.LoadScene("ResultSummary");
                return;
            }

            if (currentCase == null || string.IsNullOrEmpty(currentCase.SelectedDiagnosis))
            {
                feedbackText.text = "请先从 8 类癌症中选择一个最终诊断。";
                return;
            }

            GameplayActionResult<ResultView> result =
                app.Gameplay.SubmitDiagnosis(currentCase.SelectedDiagnosis);
            if (!result.Success)
            {
                Refresh(result.Message);
                return;
            }

            SceneManager.LoadScene("ResultSummary");
        }

        public void BuildUi(TMP_FontAsset font, Sprite background, Sprite casePanelSprite, Sprite evidencePanelSprite,
            Sprite notePanelSprite, Sprite geneButtonSprite, Sprite galaxyButtonSprite, Sprite aiButtonSprite,
            Sprite submitButtonSprite, Sprite lockedEvidenceSprite = null)
        {
            Canvas canvas = CancerTraceUiFactory.CreateCanvas(transform);
            Image backdrop = CancerTraceUiFactory.CreateBackground(canvas.transform, background);
            backdrop.gameObject.name = "CaseAnalysisBackground";
            backdrop.preserveAspect = true;
            backdrop.raycastTarget = false;

            // Normalized coordinates match the fixed paper and card spaces in bg_caseanalysis.
            shiftText = CreateOverlayText("Shift", canvas.transform, font, 22f, CancerTraceUiFactory.Ink, 0.295f, 0.845f, 0.361f, 0.925f);
            caseText = CreateOverlayText("Case", canvas.transform, font, 22f, CancerTraceUiFactory.Ink, 0.415f, 0.845f, 0.500f, 0.925f);
            caseIdText = CreateOverlayText("CaseId", canvas.transform, font, 19f, CancerTraceUiFactory.Ink, 0.535f, 0.845f, 0.665f, 0.925f);
            rpText = CreateOverlayText("RP", canvas.transform, font, 22f, CancerTraceUiFactory.Accent, 0.710f, 0.845f, 0.835f, 0.925f);
            scoreText = CreateOverlayText("Score", canvas.transform, font, 22f, new Color32(174, 116, 25, 255), 0.875f, 0.845f, 0.950f, 0.925f);

            RectTransform tools = CancerTraceUiFactory.CreateRect("ToolsArea", canvas.transform,
                new Vector2(0.04f, 0.14f), new Vector2(0.255f, 0.57f), Vector2.zero, Vector2.zero);
            geneScanButton = CancerTraceUiFactory.CreateButton("GeneScan", tools, font, geneButtonSprite, null,
                new Vector2(0.03f, 0.69f), new Vector2(0.97f, 0.94f), Vector2.zero, Vector2.zero);
            galaxyButton = CancerTraceUiFactory.CreateButton("CancerGalaxy", tools, font, galaxyButtonSprite, null,
                new Vector2(0.03f, 0.38f), new Vector2(0.97f, 0.63f), Vector2.zero, Vector2.zero);
            aiButton = CancerTraceUiFactory.CreateButton("AiAssistant", tools, font, aiButtonSprite, null,
                new Vector2(0.03f, 0.07f), new Vector2(0.97f, 0.32f), Vector2.zero, Vector2.zero);

            RectTransform board = CancerTraceUiFactory.CreateRect("EvidenceBoard", canvas.transform,
                new Vector2(0.285f, 0.18f), new Vector2(0.690f, 0.78f), Vector2.zero, Vector2.zero);
            initialClueCardTexts = new TMP_Text[2];
            initialClueCardTexts[0] = CreateOverlayText("InitialClueText0", board, font, 24f, CancerTraceUiFactory.Ink, 0.115f, 0.515f, 0.420f, 0.700f);
            initialClueCardTexts[1] = CreateOverlayText("InitialClueText1", board, font, 24f, CancerTraceUiFactory.Ink, 0.535f, 0.515f, 0.855f, 0.700f);
            CreateGeneScanEvidenceSlot(board, font, lockedEvidenceSprite);
            CreateGalaxyEvidenceSlot(board, font, lockedEvidenceSprite);
            CreateAiEvidenceSlot(board, font, lockedEvidenceSprite);

            RectTransform diagnosis = CancerTraceUiFactory.CreateRect("DiagnosisArea", canvas.transform,
                new Vector2(0.710f, 0.11f), new Vector2(0.985f, 0.77f), Vector2.zero, Vector2.zero);
            diagnosisButtons = new Button[8];
            diagnosisSelectionOverlays = new GameObject[8];
            diagnosisSelectionMarks = new GameObject[8];
            for (int index = 0; index < diagnosisButtons.Length; index++) CreateDiagnosisHitArea(index, diagnosis, font);
            submitButton = CancerTraceUiFactory.CreateButton("Submit", diagnosis, font, submitButtonSprite, null,
                new Vector2(0.12f, 0.03f), new Vector2(0.90f, 0.16f), Vector2.zero, Vector2.zero);
            feedbackText = CancerTraceUiFactory.CreateText("Feedback", canvas.transform, font, "", 23f, TextAlignmentOptions.Center,
                CancerTraceUiFactory.Ink, new Vector2(0.29f, 0.025f), new Vector2(0.69f, 0.08f), Vector2.zero, Vector2.zero);
        }

        private static TMP_Text CreateOverlayText(string name, Transform parent, TMP_FontAsset font, float size,
            Color color, float minX, float minY, float maxX, float maxY)
        {
            TMP_Text text = CancerTraceUiFactory.CreateText(name, parent, font, "", size, TextAlignmentOptions.Center,
                color, new Vector2(minX, minY), new Vector2(maxX, maxY), Vector2.zero, Vector2.zero);
            text.fontStyle = FontStyles.Bold;
            return text;
        }

        private static Image CreateLockedVisual(string name, Transform parent, Sprite sprite,
            Vector2 min, Vector2 max)
        {
            Image visual = CancerTraceUiFactory.CreateImage(name, parent, sprite, Color.white,
                min, max, Vector2.zero, Vector2.zero);
            visual.preserveAspect = true;
            visual.raycastTarget = false;
            return visual;
        }

        private void CreateGeneScanEvidenceSlot(Transform parent, TMP_FontAsset font, Sprite lockedSprite)
        {
            RectTransform slot = CancerTraceUiFactory.CreateRect("GeneScanEvidenceSlot", parent,
                new Vector2(0.030f, 0.045f), new Vector2(0.315f, 0.39f), Vector2.zero, Vector2.zero);
            geneScanLockedVisual = CreateLockedVisual("GeneScanLockedVisual", slot, lockedSprite,
                new Vector2(0.14f, 0.27f), new Vector2(0.86f, 0.90f));
            RectTransform content = CancerTraceUiFactory.CreateRect("GeneScanContent", slot,
                new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);

            geneScanClueTexts = new TMP_Text[3];
            geneScanStateTexts = new TMP_Text[3];
            geneScanValueTexts = new TMP_Text[3];
            for (int index = 0; index < geneScanClueTexts.Length; index++)
            {
                float maxY = 0.96f - index * 0.32f;
                RectTransform row = CancerTraceUiFactory.CreateRect("GeneClue" + index, content,
                    new Vector2(0.04f, maxY - 0.28f), new Vector2(0.96f, maxY), Vector2.zero, Vector2.zero);
                geneScanClueTexts[index] = CancerTraceUiFactory.CreateText("GeneName", row, font, "", 19f,
                    TextAlignmentOptions.Center, CancerTraceUiFactory.Ink,
                    new Vector2(0.02f, 0.46f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);
                geneScanClueTexts[index].fontStyle = FontStyles.Bold;
                ConfigureCompactText(geneScanClueTexts[index], 14f, 19f);

                geneScanStateTexts[index] = CancerTraceUiFactory.CreateText("State", row, font, "", 15f,
                    TextAlignmentOptions.MidlineLeft, new Color32(181, 108, 114, 255),
                    new Vector2(0.02f, 0.02f), new Vector2(0.58f, 0.45f), Vector2.zero, Vector2.zero);
                geneScanStateTexts[index].fontStyle = FontStyles.Bold;
                ConfigureCompactText(geneScanStateTexts[index], 12f, 15f);

                geneScanValueTexts[index] = CancerTraceUiFactory.CreateText("Value", row, font, "", 15f,
                    TextAlignmentOptions.MidlineRight, CancerTraceUiFactory.Ink,
                    new Vector2(0.60f, 0.02f), new Vector2(0.94f, 0.45f), Vector2.zero, Vector2.zero);
                ConfigureCompactText(geneScanValueTexts[index], 12f, 15f);
            }
            geneScanUnlockedContent = content.gameObject;
            geneScanUnlockedContent.SetActive(false);
        }

        private void CreateGalaxyEvidenceSlot(Transform parent, TMP_FontAsset font, Sprite lockedSprite)
        {
            RectTransform slot = CancerTraceUiFactory.CreateRect("CancerGalaxyEvidenceSlot", parent,
                new Vector2(0.345f, 0.045f), new Vector2(0.655f, 0.39f), Vector2.zero, Vector2.zero);
            galaxyLockedVisual = CreateLockedVisual("GalaxyLockedVisual", slot, lockedSprite,
                new Vector2(0.16f, 0.27f), new Vector2(0.84f, 0.90f));
            RectTransform content = CancerTraceUiFactory.CreateRect("GalaxyUnlockedContent", slot,
                new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);
            CancerTraceUiFactory.CreateText("UnlockedHint", content, font,
                "<size=22><b>已解锁</b></size>\n<size=16>点击查看</size>", 18f, TextAlignmentOptions.Center,
                CancerTraceUiFactory.Ink, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            galaxyEvidenceButton = CreateTransparentButton("GalaxyEvidenceButton", slot, Vector2.zero, Vector2.one);
            galaxyEvidenceButton.interactable = false;
            galaxyUnlockedContent = content.gameObject;
            galaxyUnlockedContent.SetActive(false);
        }

        private void CreateAiEvidenceSlot(Transform parent, TMP_FontAsset font, Sprite lockedSprite)
        {
            RectTransform slot = CancerTraceUiFactory.CreateRect("AiAssistantEvidenceSlot", parent,
                new Vector2(0.675f, 0.045f), new Vector2(0.975f, 0.39f), Vector2.zero, Vector2.zero);
            aiLockedVisual = CreateLockedVisual("AiLockedVisual", slot, lockedSprite,
                new Vector2(0.16f, 0.27f), new Vector2(0.84f, 0.90f));
            RectTransform content = CancerTraceUiFactory.CreateRect("AiUnlockedContent", slot,
                new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);
            CancerTraceUiFactory.CreateText("PredictionLabel", content, font, "AI预测", 12f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Ink,
                new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f), Vector2.zero, Vector2.zero);
            aiPredictionText = CancerTraceUiFactory.CreateText("PredictionValue", content, font, "", 21f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Ink,
                new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.86f), Vector2.zero, Vector2.zero);
            aiPredictionText.fontStyle = FontStyles.Bold;
            ConfigureCompactText(aiPredictionText, 15f, 21f);

            CancerTraceUiFactory.CreateText("ConfidenceLabel", content, font, "置信度", 11f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Ink,
                new Vector2(0.05f, 0.57f), new Vector2(0.95f, 0.67f), Vector2.zero, Vector2.zero);
            aiConfidenceText = CancerTraceUiFactory.CreateText("ConfidenceValue", content, font, "", 17f,
                TextAlignmentOptions.Center, new Color32(178, 116, 55, 255),
                new Vector2(0.05f, 0.43f), new Vector2(0.95f, 0.57f), Vector2.zero, Vector2.zero);
            aiConfidenceText.fontStyle = FontStyles.Bold;
            ConfigureCompactText(aiConfidenceText, 13f, 17f);

            CancerTraceUiFactory.CreateText("Top3Label", content, font, "Top 3", 11f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Ink,
                new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.42f), Vector2.zero, Vector2.zero);
            aiTopCandidatesText = CancerTraceUiFactory.CreateText("TopCandidates", content, font, "", 13f,
                TextAlignmentOptions.TopLeft, CancerTraceUiFactory.Ink,
                new Vector2(0.10f, 0.02f), new Vector2(0.90f, 0.32f), Vector2.zero, Vector2.zero);
            aiTopCandidatesText.lineSpacing = -8f;
            ConfigureCompactText(aiTopCandidatesText, 10.5f, 13f);
            aiUnlockedContent = content.gameObject;
            aiUnlockedContent.SetActive(false);
        }

        private static void ConfigureCompactText(TMP_Text text, float minimumSize, float maximumSize)
        {
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = minimumSize;
            text.fontSizeMax = maximumSize;
        }

        private void CreateDiagnosisHitArea(int index, Transform parent, TMP_FontAsset font)
        {
            int row = index / 2;
            int column = index % 2;
            float minX = column == 0 ? 0.035f : 0.515f;
            float maxX = column == 0 ? 0.485f : 0.965f;
            float maxY = 0.76f - row * 0.15f;
            Button hitArea = CreateTransparentButton("Diagnosis" + index, parent,
                new Vector2(minX, maxY - 0.13f), new Vector2(maxX, maxY));

            Image overlay = CancerTraceUiFactory.CreateImage("SelectionOverlay", hitArea.transform, null,
                new Color32(255, 207, 170, 48), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = false;
            Outline outline = overlay.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(221, 157, 111, 112);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
            overlay.gameObject.SetActive(false);

            TMP_Text checkMark = CancerTraceUiFactory.CreateText("CheckMark", hitArea.transform, font, "✓", 24f,
                TextAlignmentOptions.Center, new Color32(91, 153, 104, 255),
                new Vector2(0.82f, 0.57f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero);
            checkMark.fontStyle = FontStyles.Bold;
            checkMark.gameObject.SetActive(false);
            diagnosisButtons[index] = hitArea;
            diagnosisSelectionOverlays[index] = overlay.gameObject;
            diagnosisSelectionMarks[index] = checkMark.gameObject;
        }

        private static Button CreateTransparentButton(string name, Transform parent, Vector2 min, Vector2 max)
        {
            Image image = CancerTraceUiFactory.CreateImage(name, parent, null, Color.clear, min, max, Vector2.zero, Vector2.zero);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            return button;
        }
    }
}
