using System.Collections.Generic;
using CancerTrace.Gameplay.Flow;
using CancerTrace.Gameplay.Tutorial;
using CancerTrace.UI.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.UI.Controllers
{
    public sealed class CancerGalaxyController : MonoBehaviour
    {
        [SerializeField] private TMP_Text caseNodeText;
        [SerializeField] private TMP_Text rpText;
        [SerializeField] private TMP_Text nearbyText;
        [SerializeField] private TMP_Text legendText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private RectTransform plotArea;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text caseIdText;
        [SerializeField] private TMP_Text coordinatesText;
        [SerializeField] private GameObject[] nearbyItems;
        [SerializeField] private TMP_Text[] nearbyRankTexts;
        [SerializeField] private TMP_Text[] nearbySampleIdTexts;
        [SerializeField] private TMP_Text[] nearbyCancerTypeTexts;
        [SerializeField] private TMP_Text[] nearbyDistanceTexts;

        private CancerTraceApp app;
        private bool initialized;
        private TutorialGuideController tutorialGuide;
        private GalaxyTutorialStage tutorialStage;

        private enum GalaxyTutorialStage
        {
            None,
            GalaxyReview,
            GalaxyReturn
        }

        private static readonly Color[] NodeColors =
        {
            new Color32(229, 96, 113, 220),
            new Color32(242, 156, 89, 220),
            new Color32(239, 206, 91, 220),
            new Color32(89, 177, 151, 220),
            new Color32(91, 151, 211, 220),
            new Color32(139, 111, 196, 220),
            new Color32(213, 119, 190, 220),
            new Color32(112, 112, 125, 220)
        };

        private void Awake()
        {
            app = CancerTraceApp.EnsureInstance();
            backButton.onClick.AddListener(ReturnToCaseAnalysis);
            backButton.interactable = false;
            feedbackText.text = "正在读取癌症星图安全视图……";
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
            if (app.Tutorial.IsTutorialActive)
            {
                InitializeTutorialGalaxy();
                return;
            }

            GameplayActionResult<PlayerCaseView> caseResult = app.Gameplay.GetCurrentCase();
            GameplayActionResult<EvidenceBoardView> evidenceResult = app.Gameplay.GetEvidenceBoard();
            if (!caseResult.Success || !evidenceResult.Success || evidenceResult.Data.GalaxyEvidence == null)
            {
                feedbackText.text = "Cancer Galaxy 尚未解锁，请返回案件分析页。";
                backButton.interactable = true;
                return;
            }

            Render(caseResult.Data.CaseId, evidenceResult.Data.GalaxyEvidence);
            backButton.interactable = true;
        }

        private void OnDestroy()
        {
            if (tutorialGuide != null)
            {
                tutorialGuide.RuntimeNextRequested -= ShowTutorialReturnStage;
            }
        }

        private void InitializeTutorialGalaxy()
        {
            CancerGalaxyView galaxy;
            string errorMessage;
            if (!app.Tutorial.TryGetCancerGalaxyView(out galaxy, out errorMessage))
            {
                feedbackText.text = errorMessage;
                backButton.interactable = true;
                return;
            }

            Render(TutorialService.FixedTutorialCaseId, galaxy);
            tutorialGuide = FindObjectOfType<TutorialGuideController>(true);
            if (app.Tutorial.CurrentSession.CurrentStepIndex != TutorialService.CancerGalaxyReviewStepIndex)
            {
                if (tutorialGuide != null) tutorialGuide.HideRuntimeTutorial();
                backButton.interactable = true;
                return;
            }
            if (tutorialGuide == null)
            {
                feedbackText.text = "CancerGalaxy 教学引导层缺失，请先执行增量安装菜单。";
                backButton.interactable = false;
                return;
            }

            tutorialStage = GalaxyTutorialStage.GalaxyReview;
            tutorialGuide.RuntimeNextRequested -= ShowTutorialReturnStage;
            tutorialGuide.RuntimeNextRequested += ShowTutorialReturnStage;
            tutorialGuide.BeginRuntimeTutorial(0);
            backButton.interactable = false;
        }

        private void ShowTutorialReturnStage()
        {
            if (tutorialStage != GalaxyTutorialStage.GalaxyReview) return;
            tutorialStage = GalaxyTutorialStage.GalaxyReturn;
            tutorialGuide.BeginRuntimeTutorial(1);
            backButton.interactable = true;
        }

        private void ReturnToCaseAnalysis()
        {
            if (app != null && app.Tutorial.IsTutorialActive &&
                app.Tutorial.CurrentSession.CurrentStepIndex == TutorialService.CancerGalaxyReviewStepIndex)
            {
                if (tutorialStage != GalaxyTutorialStage.GalaxyReturn) return;

                string errorMessage;
                if (!app.Tutorial.TryCompleteCancerGalaxy(out errorMessage))
                {
                    feedbackText.text = errorMessage;
                    return;
                }

                Debug.Log("Tutorial Phase 2C completed. Ready for AI tutorial.");
            }

            SceneManager.LoadScene("CaseAnalysis");
        }

        private void Render(string caseId, CancerGalaxyView galaxy)
        {
            caseIdText.text = caseId;
            rpText.text = "Remaining RP\n" + galaxy.RemainingRp;
            coordinatesText.text =
                "(" + galaxy.UmapX.ToString("0.###") + ", " +
                galaxy.UmapY.ToString("0.###") + ")";

            for (int index = 0; index < nearbyItems.Length; index++)
            {
                bool hasItem = index < galaxy.NearbyReferences.Count;
                nearbyItems[index].SetActive(hasItem);
                if (!hasItem) continue;

                NearbyReferenceView item = galaxy.NearbyReferences[index];
                nearbyRankTexts[index].text = (index + 1).ToString();
                nearbySampleIdTexts[index].text = item.CellLineName;
                nearbyCancerTypeTexts[index].text = item.LabelZh;
                nearbyDistanceTexts[index].text = item.Distance.ToString("0.0000");
            }

            RenderNodes(galaxy);
            feedbackText.text = string.Empty;
        }

        private void RenderNodes(CancerGalaxyView galaxy)
        {
            CancerTraceUiFactory.ClearChildren(plotArea);
            if (galaxy.ReferenceNodes.Count == 0) return;

            double minX = galaxy.DisplayX;
            double maxX = galaxy.DisplayX;
            double minY = galaxy.DisplayY;
            double maxY = galaxy.DisplayY;
            Dictionary<string, int> classColors = new Dictionary<string, int>();
            for (int index = 0; index < galaxy.ReferenceNodes.Count; index++)
            {
                GalaxyReferenceNodeView node = galaxy.ReferenceNodes[index];
                minX = System.Math.Min(minX, node.DisplayX);
                maxX = System.Math.Max(maxX, node.DisplayX);
                minY = System.Math.Min(minY, node.DisplayY);
                maxY = System.Math.Max(maxY, node.DisplayY);
                if (!classColors.ContainsKey(node.ClassId)) classColors.Add(node.ClassId, classColors.Count);
            }

            double spanX = System.Math.Max(0.0001, maxX - minX);
            double spanY = System.Math.Max(0.0001, maxY - minY);
            for (int index = 0; index < galaxy.ReferenceNodes.Count; index++)
            {
                GalaxyReferenceNodeView node = galaxy.ReferenceNodes[index];
                float x = (float)((node.DisplayX - minX) / spanX);
                float y = (float)((node.DisplayY - minY) / spanY);
                GameObject dot = new GameObject("ReferenceNode", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(plotArea, false);
                RectTransform rect = (RectTransform)dot.transform;
                rect.anchorMin = new Vector2(x, y);
                rect.anchorMax = new Vector2(x, y);
                rect.sizeDelta = new Vector2(7f, 7f);
                Image image = dot.GetComponent<Image>();
                image.color = NodeColors[classColors[node.ClassId] % NodeColors.Length];
                image.raycastTarget = false;
            }

            float caseX = (float)((galaxy.DisplayX - minX) / spanX);
            float caseY = (float)((galaxy.DisplayY - minY) / spanY);
            GameObject current = new GameObject("CurrentCaseNode", typeof(RectTransform), typeof(Image));
            current.transform.SetParent(plotArea, false);
            RectTransform currentRect = (RectTransform)current.transform;
            currentRect.anchorMin = new Vector2(caseX, caseY);
            currentRect.anchorMax = new Vector2(caseX, caseY);
            currentRect.sizeDelta = new Vector2(25f, 25f);
            current.GetComponent<Image>().color = new Color32(255, 243, 129, 255);

        }

        public void BuildUi(
            TMP_FontAsset font,
            Sprite background,
            Sprite panelSprite,
            Sprite backButtonSprite)
        {
            Canvas canvas = CancerTraceUiFactory.CreateCanvas(transform);
            CancerTraceUiFactory.CreateBackground(canvas.transform, background);
            CancerTraceUiFactory.CreateText(
                "Title", canvas.transform, font, "Cancer Galaxy / 癌症星图", 44f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Ink,
                new Vector2(0.26f, 0.9f), new Vector2(0.74f, 0.98f), Vector2.zero, Vector2.zero);

            Image plotPanel = CancerTraceUiFactory.CreateImage(
                "GalaxyPanel", canvas.transform, panelSprite, Color.white,
                new Vector2(0.03f, 0.16f), new Vector2(0.69f, 0.89f), Vector2.zero, Vector2.zero);
            caseNodeText = CancerTraceUiFactory.CreateText(
                "CaseNodeInfo", plotPanel.transform, font, "Current Case Node", 19f,
                TextAlignmentOptions.TopLeft, CancerTraceUiFactory.Ink,
                new Vector2(0.04f, 0.78f), new Vector2(0.29f, 0.96f), Vector2.zero, Vector2.zero);
            rpText = CancerTraceUiFactory.CreateText(
                "RP", plotPanel.transform, font, "Remaining RP", 20f,
                TextAlignmentOptions.TopRight, CancerTraceUiFactory.Accent,
                new Vector2(0.7f, 0.87f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
            legendText = CancerTraceUiFactory.CreateText(
                "Legend", plotPanel.transform, font, "Reference Nodes", 17f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Muted,
                new Vector2(0.30f, 0.82f), new Vector2(0.70f, 0.94f), Vector2.zero, Vector2.zero);
            Image plotBackground = CancerTraceUiFactory.CreateImage(
                "PlotArea", plotPanel.transform, null, new Color32(48, 54, 83, 232),
                new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.77f), Vector2.zero, Vector2.zero);
            plotArea = plotBackground.rectTransform;

            Image nearbyPanel = CancerTraceUiFactory.CreateImage(
                "NearbyPanel", canvas.transform, panelSprite, Color.white,
                new Vector2(0.71f, 0.16f), new Vector2(0.97f, 0.89f), Vector2.zero, Vector2.zero);
            nearbyText = CancerTraceUiFactory.CreateText(
                "Nearby", nearbyPanel.transform, font, "最近 5 个 nearbyReferences", 20f,
                TextAlignmentOptions.TopLeft, CancerTraceUiFactory.Ink,
                new Vector2(0.07f, 0.11f), new Vector2(0.93f, 0.94f), Vector2.zero, Vector2.zero);

            backButton = CancerTraceUiFactory.CreateButton(
                "Back", canvas.transform, font, backButtonSprite, null,
                new Vector2(0.035f, 0.035f), new Vector2(0.20f, 0.125f), Vector2.zero, Vector2.zero);
            feedbackText = CancerTraceUiFactory.CreateText(
                "Feedback", canvas.transform, font, "", 18f, TextAlignmentOptions.Center,
                CancerTraceUiFactory.Ink, new Vector2(0.22f, 0.035f), new Vector2(0.96f, 0.125f), Vector2.zero, Vector2.zero);
        }
    }
}
