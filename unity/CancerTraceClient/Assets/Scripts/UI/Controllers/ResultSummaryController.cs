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
    public sealed class ResultSummaryController : MonoBehaviour
    {
        [SerializeField] private TMP_Text verdictText;
        [SerializeField] private TMP_Text diagnosisText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text aiReviewText;
        [SerializeField] private TMP_Text anomalyText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button nextButton;

        private CancerTraceApp app;
        private bool initialized;

        private void Awake()
        {
            app = CancerTraceApp.EnsureInstance();
            nextButton.onClick.AddListener(NextCase);
            nextButton.interactable = false;
            anomalyText.gameObject.SetActive(false);
            feedbackText.text = "正在整理调查结果……";
        }

        private void Update()
        {
            if (initialized) return;
            if (app == null) app = CancerTraceApp.EnsureInstance();
            if (!app.IsReady)
            {
                if (!string.IsNullOrEmpty(app.ErrorMessage)) feedbackText.text = app.ErrorMessage;
                return;
            }

            initialized = true;
            GameplayActionResult<ResultView> result = app.Gameplay.GetResult();
            if (!result.Success)
            {
                feedbackText.text = result.Message;
                return;
            }

            Render(result.Data);
            nextButton.interactable = true;
        }

        private void Render(ResultView result)
        {
            verdictText.text = result.IsCorrect ? "CORRECT / 诊断正确" : "WRONG / 诊断错误";
            verdictText.color = result.IsCorrect ? CancerTraceUiFactory.Teal : CancerTraceUiFactory.Accent;

            string playerLabel = FindDiagnosisLabel(result.PlayerDiagnosis);
            diagnosisText.text =
                "Case ID  " + result.CaseId +
                "\n\nPlayer Diagnosis\n" + playerLabel + " / " + result.PlayerDiagnosis +
                "\n\nTrue Diagnosis\n" + result.TrueDiagnosisLabelZh + " / " + result.TrueDiagnosis;
            scoreText.text =
                "Score Earned  " + result.ScoreEarned +
                "\nCurrent Shift Score  " + result.CurrentScore +
                "\nRemaining RP  " + result.RemainingRp;

            AiAssistantView ai = result.AiReview;
            StringBuilder review = new StringBuilder();
            review.AppendLine("AI Review / 教学复盘");
            review.AppendLine();
            review.AppendLine("Prediction  " + ai.PredictionLabelZh + " / " + ai.PredictionClassId);
            review.AppendLine("Confidence  " + ai.Confidence.ToString("P1"));
            review.AppendLine();
            review.AppendLine("Top 3 Candidates");
            for (int index = 0; index < ai.TopCandidates.Count; index++)
            {
                AiCandidateView candidate = ai.TopCandidates[index];
                review.Append(index + 1);
                review.Append(". ");
                review.Append(candidate.LabelZh);
                review.Append("  ");
                review.AppendLine(candidate.Probability.ToString("P1"));
            }
            aiReviewText.text = review.ToString();

            anomalyText.gameObject.SetActive(result.IsAnomaly);
            anomalyText.text = result.IsAnomaly ? result.AnomalyDisplayLabel : string.Empty;
            feedbackText.text = "Result 数据仅在提交诊断后显示。";
        }

        private string FindDiagnosisLabel(string classId)
        {
            IReadOnlyList<CancerTypeOptionView> options = app.Gameplay.GetDiagnosisOptions();
            for (int index = 0; index < options.Count; index++)
            {
                if (string.Equals(options[index].ClassId, classId, StringComparison.Ordinal))
                {
                    return options[index].LabelZh;
                }
            }
            return classId;
        }

        private void NextCase()
        {
            nextButton.interactable = false;
            GameplayActionResult<PlayerCaseView> result = app.Gameplay.NextCase();
            if (!result.Success)
            {
                feedbackText.text = result.Message;
                return;
            }

            SceneManager.LoadScene("CaseAnalysis");
        }

        public void BuildUi(
            TMP_FontAsset font,
            Sprite background,
            Sprite panelSprite,
            Sprite nextButtonSprite,
            Sprite characterSprite)
        {
            Canvas canvas = CancerTraceUiFactory.CreateCanvas(transform);
            CancerTraceUiFactory.CreateBackground(canvas.transform, background);

            Image panel = CancerTraceUiFactory.CreateImage(
                "ResultPanel", canvas.transform, panelSprite, Color.white,
                new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.92f), Vector2.zero, Vector2.zero);
            CancerTraceUiFactory.CreateText(
                "Title", panel.transform, font, "Result Summary / 调查结算", 45f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Ink,
                new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);
            verdictText = CancerTraceUiFactory.CreateText(
                "Verdict", panel.transform, font, "RESULT", 38f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Teal,
                new Vector2(0.25f, 0.78f), new Vector2(0.75f, 0.87f), Vector2.zero, Vector2.zero);

            diagnosisText = CancerTraceUiFactory.CreateText(
                "Diagnosis", panel.transform, font, "", 23f,
                TextAlignmentOptions.TopLeft, CancerTraceUiFactory.Ink,
                new Vector2(0.07f, 0.39f), new Vector2(0.42f, 0.76f), Vector2.zero, Vector2.zero);
            scoreText = CancerTraceUiFactory.CreateText(
                "Score", panel.transform, font, "", 25f,
                TextAlignmentOptions.TopLeft, CancerTraceUiFactory.Accent,
                new Vector2(0.07f, 0.20f), new Vector2(0.42f, 0.37f), Vector2.zero, Vector2.zero);
            aiReviewText = CancerTraceUiFactory.CreateText(
                "AiReview", panel.transform, font, "", 22f,
                TextAlignmentOptions.TopLeft, CancerTraceUiFactory.Ink,
                new Vector2(0.47f, 0.24f), new Vector2(0.86f, 0.76f), Vector2.zero, Vector2.zero);

            Image character = CancerTraceUiFactory.CreateImage(
                "Character", panel.transform, characterSprite, Color.white,
                new Vector2(0.05f, 0.02f), new Vector2(0.25f, 0.22f), Vector2.zero, Vector2.zero);
            character.type = Image.Type.Simple;
            character.preserveAspect = true;

            anomalyText = CancerTraceUiFactory.CreateText(
                "Anomaly", panel.transform, font, "", 26f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Accent,
                new Vector2(0.42f, 0.13f), new Vector2(0.88f, 0.22f), Vector2.zero, Vector2.zero);
            nextButton = CancerTraceUiFactory.CreateButton(
                "NextCase", panel.transform, font, nextButtonSprite, "Next Case / 下一个案件",
                new Vector2(0.52f, 0.03f), new Vector2(0.85f, 0.13f), Vector2.zero, Vector2.zero);
            feedbackText = CancerTraceUiFactory.CreateText(
                "Feedback", canvas.transform, font, "", 20f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Muted,
                new Vector2(0.2f, 0.025f), new Vector2(0.8f, 0.085f), Vector2.zero, Vector2.zero);
        }
    }
}
