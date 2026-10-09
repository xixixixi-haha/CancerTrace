using System;
using System.Collections.Generic;
using System.Text;
using CancerTrace.Gameplay.Flow;
using CancerTrace.UI.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.UI.Controllers
{
    public sealed class ResultSummaryController : MonoBehaviour
    {
        private enum TutorialResultStage
        {
            ResultReview = 0,
            TutorialComplete = 1
        }

        [SerializeField] private Image correctBanner;
        [SerializeField] private Image wrongBanner;
        [SerializeField] private Image playerDiagnosisIcon;
        [SerializeField] private Image trueDiagnosisIcon;
        [SerializeField] private Image aiPredictionIcon;
        [SerializeField] private TMP_Text playerDiagnosisText;
        [SerializeField] private TMP_Text trueDiagnosisText;
        [SerializeField] private TMP_Text scoreEarnedText;
        [SerializeField] private TMP_Text aiPredictionText;
        [SerializeField] private TMP_Text confidenceText;
        [SerializeField] private TMP_Text topCandidatesText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Sprite lungIcon;
        [SerializeField] private Sprite skinIcon;
        [SerializeField] private Sprite cnsBrainIcon;
        [SerializeField] private Sprite bowelIcon;
        [SerializeField] private Sprite esophagusStomachIcon;
        [SerializeField] private Sprite breastIcon;
        [SerializeField] private Sprite boneIcon;
        [SerializeField] private Sprite ovaryFallopianTubeIcon;

        private CancerTraceApp app;
        private TutorialGuideController tutorialGuide;
        private bool initialized;
        private bool tutorialCompletionPending;
        private TutorialResultStage tutorialStage;

        private void Awake()
        {
            app = CancerTraceApp.EnsureInstance();
            nextButton.onClick.AddListener(NextCase);
            nextButton.interactable = false;
            correctBanner.gameObject.SetActive(false);
            wrongBanner.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (initialized) return;
            if (app == null) app = CancerTraceApp.EnsureInstance();
            if (!app.IsReady)
            {
                return;
            }

            initialized = true;
            if (app.Tutorial.IsTutorialActive)
            {
                nextButton.gameObject.SetActive(false);
                ResultView tutorialResult;
                string tutorialError;
                if (!app.Tutorial.TryGetResult(out tutorialResult, out tutorialError))
                {
                    Debug.LogWarning("Tutorial Result Summary could not be displayed: " + tutorialError);
                    return;
                }

                Render(tutorialResult);
                nextButton.interactable = false;
                BeginTutorialResultReview();
                return;
            }

            nextButton.gameObject.SetActive(true);
            GameplayActionResult<ResultView> result = app.Gameplay.GetResult();
            if (!result.Success)
            {
                Debug.LogWarning("Result Summary could not be displayed: " + result.Message);
                return;
            }

            Render(result.Data);
            nextButton.interactable = true;
        }

        private void OnDestroy()
        {
            if (tutorialGuide != null)
            {
                tutorialGuide.RuntimeNextRequested -= AdvanceTutorialResult;
            }
        }

        private void BeginTutorialResultReview()
        {
            tutorialGuide = FindObjectOfType<TutorialGuideController>(true);
            if (tutorialGuide == null)
            {
                Debug.LogError("ResultSummary TutorialGuideLayer is missing.");
                return;
            }

            tutorialStage = TutorialResultStage.ResultReview;
            tutorialGuide.RuntimeNextRequested -= AdvanceTutorialResult;
            tutorialGuide.RuntimeNextRequested += AdvanceTutorialResult;
            tutorialGuide.BeginRuntimeTutorial((int)tutorialStage);
        }

        private void AdvanceTutorialResult()
        {
            if (tutorialCompletionPending || app == null || !app.Tutorial.IsTutorialActive) return;

            if (tutorialStage == TutorialResultStage.ResultReview)
            {
                tutorialStage = TutorialResultStage.TutorialComplete;
                tutorialGuide.BeginRuntimeTutorial((int)tutorialStage);
                return;
            }

            if (tutorialStage != TutorialResultStage.TutorialComplete) return;

            tutorialCompletionPending = true;
            string errorMessage;
            if (!app.Tutorial.CompleteTutorial(out errorMessage))
            {
                tutorialCompletionPending = false;
                Debug.LogError("Tutorial completion failed: " + errorMessage);
                return;
            }

            tutorialGuide.HideRuntimeTutorial();
            SceneManager.LoadScene("MainMenu");
        }

        private void Render(ResultView result)
        {
            correctBanner.gameObject.SetActive(result.IsCorrect);
            wrongBanner.gameObject.SetActive(!result.IsCorrect);

            playerDiagnosisText.text = FindDiagnosisLabel(result.PlayerDiagnosis);
            trueDiagnosisText.text = result.TrueDiagnosisLabelZh;
            scoreEarnedText.text = result.ScoreEarned > 0
                ? "+" + result.ScoreEarned
                : result.ScoreEarned.ToString();

            playerDiagnosisIcon.sprite = FindCancerTypeIcon(result.PlayerDiagnosis);
            trueDiagnosisIcon.sprite = FindCancerTypeIcon(result.TrueDiagnosis);

            AiAssistantView ai = result.AiReview;
            aiPredictionText.text = ai.PredictionLabelZh;
            confidenceText.text = ai.Confidence.ToString("P1");
            aiPredictionIcon.sprite = FindCancerTypeIcon(ai.PredictionClassId);

            StringBuilder candidates = new StringBuilder();
            int candidateCount = Mathf.Min(3, ai.TopCandidates.Count);
            for (int index = 0; index < candidateCount; index++)
            {
                if (index > 0) candidates.AppendLine();
                candidates.Append(index + 1);
                candidates.Append(".  ");
                candidates.Append(ai.TopCandidates[index].LabelZh);
            }
            topCandidatesText.text = candidates.ToString();
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
            return "未知";
        }

        private Sprite FindCancerTypeIcon(string classId)
        {
            switch (classId)
            {
                case "lung": return lungIcon;
                case "skin": return skinIcon;
                case "cns_brain": return cnsBrainIcon;
                case "bowel": return bowelIcon;
                case "esophagus_stomach": return esophagusStomachIcon;
                case "breast": return breastIcon;
                case "bone": return boneIcon;
                case "ovary_fallopian_tube": return ovaryFallopianTubeIcon;
                default: return null;
            }
        }

        private void NextCase()
        {
            if (app != null && app.Tutorial.IsTutorialActive)
            {
                Debug.LogWarning("Formal NextCase was ignored while Tutorial is active.");
                return;
            }

            nextButton.interactable = false;
            GameplayActionResult<PlayerCaseView> result = app.Gameplay.NextCase();
            if (!result.Success)
            {
                if (result.Status == GameplayActionStatus.ShiftCompleted)
                {
                    SceneManager.LoadScene("ShiftSummary");
                    return;
                }

                Debug.LogWarning("Could not advance to the next case: " + result.Message);
                nextButton.interactable = true;
                return;
            }

            SceneManager.LoadScene("CaseAnalysis");
        }

        public void BuildUi(
            TMP_FontAsset font,
            Sprite background,
            Sprite titleBannerSprite,
            Sprite correctBannerSprite,
            Sprite wrongBannerSprite,
            Sprite[] cancerTypeIconSprites,
            Sprite scoreStarSprite,
            Sprite aiReviewSprite,
            Sprite nextButtonSprite)
        {
            if (cancerTypeIconSprites == null || cancerTypeIconSprites.Length != 8)
            {
                throw new ArgumentException("ResultSummary requires exactly eight cancer-type icons.");
            }

            lungIcon = cancerTypeIconSprites[0];
            skinIcon = cancerTypeIconSprites[1];
            cnsBrainIcon = cancerTypeIconSprites[2];
            bowelIcon = cancerTypeIconSprites[3];
            esophagusStomachIcon = cancerTypeIconSprites[4];
            breastIcon = cancerTypeIconSprites[5];
            boneIcon = cancerTypeIconSprites[6];
            ovaryFallopianTubeIcon = cancerTypeIconSprites[7];

            Canvas canvas = CancerTraceUiFactory.CreateCanvas(transform);
            Image backgroundImage = CancerTraceUiFactory.CreateBackground(canvas.transform, background);
            backgroundImage.raycastTarget = false;

            RectTransform content = CancerTraceUiFactory.CreateRect(
                "ResultContentArea", canvas.transform,
                new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.94f), Vector2.zero, Vector2.zero);

            CreateArtImage(
                "ResultTitleBanner", content, titleBannerSprite,
                new Vector2(0.15f, 0.82f), new Vector2(0.85f, 0.99f));
            correctBanner = CreateArtImage(
                "CorrectBanner", content, correctBannerSprite,
                new Vector2(0.30f, 0.70f), new Vector2(0.70f, 0.83f));
            wrongBanner = CreateArtImage(
                "WrongBanner", content, wrongBannerSprite,
                new Vector2(0.30f, 0.70f), new Vector2(0.70f, 0.83f));

            CancerTraceUiFactory.CreateText(
                "PlayerDiagnosisLabel", content, font, "你的诊断", 30f,
                TextAlignmentOptions.Left, CancerTraceUiFactory.Ink,
                new Vector2(0.07f, 0.59f), new Vector2(0.43f, 0.68f), Vector2.zero, Vector2.zero);
            playerDiagnosisIcon = CreateArtImage(
                "PlayerDiagnosisIcon", content, lungIcon,
                new Vector2(0.08f, 0.43f), new Vector2(0.20f, 0.58f));
            playerDiagnosisText = CreateResultValueText(
                "PlayerDiagnosisValue", content, font,
                new Vector2(0.22f, 0.44f), new Vector2(0.47f, 0.57f));

            CancerTraceUiFactory.CreateText(
                "TrueDiagnosisLabel", content, font, "正确诊断", 30f,
                TextAlignmentOptions.Left, CancerTraceUiFactory.Ink,
                new Vector2(0.07f, 0.33f), new Vector2(0.43f, 0.42f), Vector2.zero, Vector2.zero);
            trueDiagnosisIcon = CreateArtImage(
                "TrueDiagnosisIcon", content, lungIcon,
                new Vector2(0.08f, 0.17f), new Vector2(0.20f, 0.32f));
            trueDiagnosisText = CreateResultValueText(
                "TrueDiagnosisValue", content, font,
                new Vector2(0.22f, 0.18f), new Vector2(0.47f, 0.31f));

            CreateArtImage(
                "ScoreStar", content, scoreStarSprite,
                new Vector2(0.08f, 0.035f), new Vector2(0.17f, 0.145f));
            CancerTraceUiFactory.CreateText(
                "ScoreEarnedLabel", content, font, "本病例得分", 28f,
                TextAlignmentOptions.Left, CancerTraceUiFactory.Ink,
                new Vector2(0.18f, 0.095f), new Vector2(0.38f, 0.155f), Vector2.zero, Vector2.zero);
            scoreEarnedText = CancerTraceUiFactory.CreateText(
                "ScoreEarnedValue", content, font, "", 45f,
                TextAlignmentOptions.Left, CancerTraceUiFactory.Accent,
                new Vector2(0.18f, 0.025f), new Vector2(0.43f, 0.10f), Vector2.zero, Vector2.zero);

            CreateArtImage(
                "AiReviewIcon", content, aiReviewSprite,
                new Vector2(0.54f, 0.59f), new Vector2(0.62f, 0.69f));
            CancerTraceUiFactory.CreateText(
                "AiReviewTitle", content, font, "AI教学复盘", 34f,
                TextAlignmentOptions.Left, CancerTraceUiFactory.Ink,
                new Vector2(0.63f, 0.59f), new Vector2(0.94f, 0.69f), Vector2.zero, Vector2.zero);
            CancerTraceUiFactory.CreateText(
                "AiPredictionLabel", content, font, "AI判断", 28f,
                TextAlignmentOptions.Left, CancerTraceUiFactory.Ink,
                new Vector2(0.54f, 0.50f), new Vector2(0.93f, 0.57f), Vector2.zero, Vector2.zero);
            aiPredictionIcon = CreateArtImage(
                "AiPredictionIcon", content, lungIcon,
                new Vector2(0.55f, 0.35f), new Vector2(0.67f, 0.49f));
            aiPredictionText = CreateResultValueText(
                "AiPredictionValue", content, font,
                new Vector2(0.69f, 0.36f), new Vector2(0.94f, 0.48f));

            CancerTraceUiFactory.CreateText(
                "ConfidenceLabel", content, font, "置信度", 27f,
                TextAlignmentOptions.Left, CancerTraceUiFactory.Ink,
                new Vector2(0.54f, 0.28f), new Vector2(0.70f, 0.35f), Vector2.zero, Vector2.zero);
            confidenceText = CancerTraceUiFactory.CreateText(
                "ConfidenceValue", content, font, "", 38f,
                TextAlignmentOptions.Left, CancerTraceUiFactory.Teal,
                new Vector2(0.70f, 0.27f), new Vector2(0.93f, 0.35f), Vector2.zero, Vector2.zero);

            CancerTraceUiFactory.CreateText(
                "TopCandidatesLabel", content, font, "前三候选", 27f,
                TextAlignmentOptions.Left, CancerTraceUiFactory.Ink,
                new Vector2(0.54f, 0.19f), new Vector2(0.93f, 0.27f), Vector2.zero, Vector2.zero);
            topCandidatesText = CancerTraceUiFactory.CreateText(
                "TopCandidatesValue", content, font, "", 29f,
                TextAlignmentOptions.TopLeft, CancerTraceUiFactory.Ink,
                new Vector2(0.56f, 0.03f), new Vector2(0.92f, 0.20f), Vector2.zero, Vector2.zero);
            topCandidatesText.overflowMode = TextOverflowModes.Overflow;
            topCandidatesText.lineSpacing = 10f;

            nextButton = CancerTraceUiFactory.CreateButton(
                "NextCase", content, font, nextButtonSprite, null,
                new Vector2(0.70f, -0.075f), new Vector2(0.94f, 0.045f), Vector2.zero, Vector2.zero);
        }

        private static Image CreateArtImage(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Image image = CancerTraceUiFactory.CreateImage(
                name, parent, sprite, Color.white,
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateResultValueText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            TMP_Text text = CancerTraceUiFactory.CreateText(
                name, parent, font, "", 38f,
                TextAlignmentOptions.Left, CancerTraceUiFactory.Ink,
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableWordWrapping = false;
            return text;
        }
    }
}
