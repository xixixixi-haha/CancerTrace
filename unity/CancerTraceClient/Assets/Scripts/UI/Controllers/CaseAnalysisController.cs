using System;
using System.Collections.Generic;
using System.Text;
using CancerTrace.Gameplay.Flow;
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
        [SerializeField] private TMP_Text initialCluesText;
        [SerializeField] private TMP_Text evidenceText;
        [SerializeField] private TMP_Text aiText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button geneScanButton;
        [SerializeField] private Button galaxyButton;
        [SerializeField] private Button aiButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button[] diagnosisButtons;

        private CancerTraceApp app;
        private PlayerCaseView currentCase;
        private bool initialized;

        private void Awake()
        {
            app = CancerTraceApp.EnsureInstance();
            geneScanButton.onClick.AddListener(UseGeneScan);
            galaxyButton.onClick.AddListener(UseGalaxy);
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
                Refresh("案件档案已打开。请选择调查工具或直接提交诊断。");
            }
            else if (!string.IsNullOrEmpty(app.ErrorMessage))
            {
                feedbackText.text = "数据加载失败：" + app.ErrorMessage;
            }
        }

        private void SetInteractive(bool value)
        {
            geneScanButton.interactable = value;
            galaxyButton.interactable = value;
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
            shiftText.text = "Shift " + currentCase.ShiftNumber;
            caseText.text = "Case " + currentCase.CaseNumber + " / " + currentCase.CasesPerShift;
            caseIdText.text = "Case ID  " + currentCase.CaseId + "\nCell Line  " + currentCase.CellLineName;
            rpText.text = "Remaining RP  " + currentCase.RemainingRp;
            scoreText.text = "Score  " + currentCase.CurrentScore;
            initialCluesText.text = BuildClueList(currentCase.InitialClues);

            GameplayActionResult<EvidenceBoardView> evidence = app.Gameplay.GetEvidenceBoard();
            if (evidence.Success) RenderEvidence(evidence.Data);

            RenderTools();
            RenderDiagnosisOptions();
            submitButton.interactable = !currentCase.DiagnosisSubmitted;
            feedbackText.text = feedback;
        }

        private void RenderTools()
        {
            RenderToolButton(geneScanButton, "Gene Scan", currentCase.GeneScan);
            RenderToolButton(galaxyButton, "Cancer Galaxy", currentCase.CancerGalaxy);
            RenderToolButton(aiButton, "AI Assistant", currentCase.AiAssistant);
        }

        private static void RenderToolButton(Button button, string title, ToolAvailabilityView tool)
        {
            CancerTraceUiFactory.SetButtonLabel(
                button,
                tool.Used ? title + "\nUsed" : title + "\n" + tool.Cost + " RP");
            button.interactable = tool.CanUse;
        }

        private void RenderDiagnosisOptions()
        {
            IReadOnlyList<CancerTypeOptionView> options = app.Gameplay.GetDiagnosisOptions();
            int count = Math.Min(options.Count, diagnosisButtons.Length);
            for (int index = 0; index < diagnosisButtons.Length; index++)
            {
                Button button = diagnosisButtons[index];
                button.onClick.RemoveAllListeners();
                if (index >= count)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                button.gameObject.SetActive(true);
                CancerTypeOptionView option = options[index];
                CancerTraceUiFactory.SetButtonLabel(button, option.LabelZh + "\n" + option.ClassId);
                string classId = option.ClassId;
                button.onClick.AddListener(() => SelectDiagnosis(classId));
                Image image = button.GetComponent<Image>();
                image.color = string.Equals(currentCase.SelectedDiagnosis, classId, StringComparison.Ordinal)
                    ? new Color32(255, 218, 126, 255)
                    : Color.white;
                button.interactable = !currentCase.DiagnosisSubmitted;
            }
        }

        private void RenderEvidence(EvidenceBoardView evidence)
        {
            StringBuilder board = new StringBuilder();
            board.AppendLine("INITIAL CLUES");
            board.Append(BuildClueList(evidence.InitialClues));

            if (evidence.GeneScanClues.Count > 0)
            {
                board.AppendLine();
                board.AppendLine("GENE SCAN");
                board.Append(BuildClueList(evidence.GeneScanClues));
            }

            if (evidence.GalaxyEvidence != null)
            {
                board.AppendLine();
                board.AppendLine("CANCER GALAXY");
                board.Append("已记录 5 个附近参考节点；RP ");
                board.Append(evidence.GalaxyEvidence.RemainingRp);
                board.AppendLine();
            }

            evidenceText.text = board.ToString();

            if (evidence.AiEvidence == null)
            {
                aiText.text = "AI Assistant 尚未使用。\n不会提前展示预测。";
                return;
            }

            AiAssistantView ai = evidence.AiEvidence;
            StringBuilder aiBuilder = new StringBuilder();
            aiBuilder.AppendLine("Prediction");
            aiBuilder.AppendLine(ai.PredictionLabelZh + " / " + ai.PredictionClassId);
            aiBuilder.AppendLine("Confidence  " + ai.Confidence.ToString("P1"));
            aiBuilder.AppendLine("Top 3 Candidates");
            for (int index = 0; index < ai.TopCandidates.Count; index++)
            {
                AiCandidateView candidate = ai.TopCandidates[index];
                aiBuilder.Append(index + 1);
                aiBuilder.Append(". ");
                aiBuilder.Append(candidate.LabelZh);
                aiBuilder.Append("  ");
                aiBuilder.AppendLine(candidate.Probability.ToString("P1"));
            }
            aiText.text = aiBuilder.ToString();
        }

        private static string BuildClueList(IReadOnlyList<ClueView> clues)
        {
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < clues.Count; index++)
            {
                ClueView clue = clues[index];
                builder.Append("• ");
                builder.Append(clue.Gene);
                builder.Append("  ");
                builder.Append(clue.State);
                builder.Append("  ");
                builder.AppendLine(clue.ExpressionValue.ToString("0.###"));
            }
            return builder.ToString();
        }

        private void UseGeneScan()
        {
            GameplayActionResult<GeneScanView> result = app.Gameplay.UseGeneScan();
            Refresh(result.Success ? "Gene Scan 完成：3 条新线索已加入证据板。" : result.Message);
        }

        private void UseGalaxy()
        {
            GameplayActionResult<CancerGalaxyView> result = app.Gameplay.UseCancerGalaxy();
            if (!result.Success)
            {
                Refresh(result.Message);
                return;
            }

            SceneManager.LoadScene("CancerGalaxy");
        }

        private void UseAiAssistant()
        {
            GameplayActionResult<AiAssistantView> result = app.Gameplay.UseAiAssistant();
            Refresh(result.Success ? "AI Assistant 分析完成。" : result.Message);
        }

        private void SelectDiagnosis(string classId)
        {
            GameplayActionResult<PlayerCaseView> result = app.Gameplay.SelectDiagnosis(classId);
            Refresh(result.Success ? "已选择诊断：" + classId : result.Message);
        }

        private void SubmitDiagnosis()
        {
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

        public void BuildUi(
            TMP_FontAsset font,
            Sprite background,
            Sprite casePanelSprite,
            Sprite evidencePanelSprite,
            Sprite notePanelSprite,
            Sprite geneButtonSprite,
            Sprite galaxyButtonSprite,
            Sprite aiButtonSprite,
            Sprite submitButtonSprite)
        {
            Canvas canvas = CancerTraceUiFactory.CreateCanvas(transform);
            CancerTraceUiFactory.CreateBackground(canvas.transform, background);

            Image top = CancerTraceUiFactory.CreateImage(
                "TopBar", canvas.transform, casePanelSprite, Color.white,
                new Vector2(0.025f, 0.87f), new Vector2(0.975f, 0.98f), Vector2.zero, Vector2.zero);
            shiftText = CancerTraceUiFactory.CreateText(
                "Shift", top.transform, font, "Shift -", 27f, TextAlignmentOptions.Left,
                CancerTraceUiFactory.Ink, new Vector2(0.03f, 0.1f), new Vector2(0.18f, 0.9f), Vector2.zero, Vector2.zero);
            caseText = CancerTraceUiFactory.CreateText(
                "Case", top.transform, font, "Case - / -", 27f, TextAlignmentOptions.Left,
                CancerTraceUiFactory.Ink, new Vector2(0.18f, 0.1f), new Vector2(0.35f, 0.9f), Vector2.zero, Vector2.zero);
            caseIdText = CancerTraceUiFactory.CreateText(
                "CaseId", top.transform, font, "Case ID", 22f, TextAlignmentOptions.Center,
                CancerTraceUiFactory.Ink, new Vector2(0.36f, 0.06f), new Vector2(0.68f, 0.94f), Vector2.zero, Vector2.zero);
            rpText = CancerTraceUiFactory.CreateText(
                "RP", top.transform, font, "Remaining RP", 27f, TextAlignmentOptions.Center,
                CancerTraceUiFactory.Accent, new Vector2(0.68f, 0.1f), new Vector2(0.84f, 0.9f), Vector2.zero, Vector2.zero);
            scoreText = CancerTraceUiFactory.CreateText(
                "Score", top.transform, font, "Score", 27f, TextAlignmentOptions.Center,
                CancerTraceUiFactory.Teal, new Vector2(0.84f, 0.1f), new Vector2(0.98f, 0.9f), Vector2.zero, Vector2.zero);

            Image left = CancerTraceUiFactory.CreateImage(
                "CaseNotebook", canvas.transform, notePanelSprite, Color.white,
                new Vector2(0.025f, 0.15f), new Vector2(0.33f, 0.85f), Vector2.zero, Vector2.zero);
            CancerTraceUiFactory.CreateText(
                "InitialTitle", left.transform, font, "Initial Clues / 初始线索", 30f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Accent,
                new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.95f), Vector2.zero, Vector2.zero);
            initialCluesText = CancerTraceUiFactory.CreateText(
                "InitialClues", left.transform, font, "", 25f, TextAlignmentOptions.TopLeft,
                CancerTraceUiFactory.Ink, new Vector2(0.09f, 0.55f), new Vector2(0.91f, 0.81f), Vector2.zero, Vector2.zero);
            geneScanButton = CancerTraceUiFactory.CreateButton(
                "GeneScan", left.transform, font, geneButtonSprite, "Gene Scan",
                new Vector2(0.08f, 0.39f), new Vector2(0.92f, 0.52f), Vector2.zero, Vector2.zero);
            galaxyButton = CancerTraceUiFactory.CreateButton(
                "CancerGalaxy", left.transform, font, galaxyButtonSprite, "Cancer Galaxy",
                new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero);
            aiButton = CancerTraceUiFactory.CreateButton(
                "AiAssistant", left.transform, font, aiButtonSprite, "AI Assistant",
                new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.20f), Vector2.zero, Vector2.zero);

            Image middle = CancerTraceUiFactory.CreateImage(
                "EvidenceBoard", canvas.transform, evidencePanelSprite, Color.white,
                new Vector2(0.345f, 0.15f), new Vector2(0.665f, 0.85f), Vector2.zero, Vector2.zero);
            CancerTraceUiFactory.CreateText(
                "EvidenceTitle", middle.transform, font, "Evidence Board / 证据板", 30f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Teal,
                new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
            evidenceText = CancerTraceUiFactory.CreateText(
                "Evidence", middle.transform, font, "", 20f, TextAlignmentOptions.TopLeft,
                CancerTraceUiFactory.Ink, new Vector2(0.07f, 0.34f), new Vector2(0.93f, 0.83f), Vector2.zero, Vector2.zero);
            aiText = CancerTraceUiFactory.CreateText(
                "AiEvidence", middle.transform, font, "", 20f, TextAlignmentOptions.TopLeft,
                CancerTraceUiFactory.Ink, new Vector2(0.07f, 0.05f), new Vector2(0.93f, 0.32f), Vector2.zero, Vector2.zero);

            Image right = CancerTraceUiFactory.CreateImage(
                "DiagnosisPanel", canvas.transform, casePanelSprite, Color.white,
                new Vector2(0.68f, 0.15f), new Vector2(0.975f, 0.85f), Vector2.zero, Vector2.zero);
            CancerTraceUiFactory.CreateText(
                "DiagnosisTitle", right.transform, font, "Diagnosis / 最终诊断", 30f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Accent,
                new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);

            diagnosisButtons = new Button[8];
            for (int index = 0; index < diagnosisButtons.Length; index++)
            {
                int row = index / 2;
                int column = index % 2;
                float xMin = column == 0 ? 0.06f : 0.52f;
                float xMax = column == 0 ? 0.48f : 0.94f;
                float yMax = 0.83f - row * 0.145f;
                float yMin = yMax - 0.12f;
                diagnosisButtons[index] = CancerTraceUiFactory.CreateButton(
                    "Diagnosis" + index, right.transform, font, null, "Cancer Type",
                    new Vector2(xMin, yMin), new Vector2(xMax, yMax), Vector2.zero, Vector2.zero);
                TMP_Text buttonLabel = CancerTraceUiFactory.GetButtonLabel(diagnosisButtons[index]);
                buttonLabel.fontSize = 18f;
            }

            submitButton = CancerTraceUiFactory.CreateButton(
                "Submit", right.transform, font, submitButtonSprite, "Submit Diagnosis / 提交诊断",
                new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.20f), Vector2.zero, Vector2.zero);

            feedbackText = CancerTraceUiFactory.CreateText(
                "Feedback", canvas.transform, font, "", 23f, TextAlignmentOptions.Center,
                CancerTraceUiFactory.Ink, new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.12f), Vector2.zero, Vector2.zero);
        }
    }
}
