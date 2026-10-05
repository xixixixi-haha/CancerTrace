using CancerTrace.Gameplay.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.UI.Controllers
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button newShiftButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private TMP_Text statusText;

        private CancerTraceApp app;
        private bool readyShown;

        private void Awake()
        {
            app = CancerTraceApp.EnsureInstance();
            newShiftButton.onClick.AddListener(StartNewShift);
            newShiftButton.interactable = false;
            continueButton.interactable = false;
            tutorialButton.interactable = false;
            statusText.text = "正在读取案件档案……";
        }

        private void Update()
        {
            if (app == null) app = CancerTraceApp.EnsureInstance();
            if (app.IsReady)
            {
                newShiftButton.interactable = true;
                if (!readyShown)
                {
                    statusText.text = "档案已就绪。开始今天的调查吧！";
                    readyShown = true;
                }
            }
            else if (!string.IsNullOrEmpty(app.ErrorMessage))
            {
                newShiftButton.interactable = false;
                statusText.text = "数据加载失败：" + app.ErrorMessage;
            }
        }

        private void StartNewShift()
        {
            newShiftButton.interactable = false;
            GameplayActionResult<PlayerCaseView> result = app.Gameplay.StartNewShift();
            if (!result.Success)
            {
                statusText.text = "无法开始调查：" + result.Message;
                newShiftButton.interactable = true;
                return;
            }

            SceneManager.LoadScene("CaseAnalysis");
        }

        public void BuildUi(
            TMP_FontAsset font,
            Sprite background,
            Sprite panelSprite,
            Sprite startButtonSprite,
            Sprite detectiveSprite)
        {
            Canvas canvas = CancerTraceUiFactory.CreateCanvas(transform);
            CancerTraceUiFactory.CreateBackground(canvas.transform, background);

            Image panel = CancerTraceUiFactory.CreateImage(
                "MenuPanel", canvas.transform, panelSprite, Color.white,
                new Vector2(0.24f, 0.13f), new Vector2(0.76f, 0.88f),
                Vector2.zero, Vector2.zero);

            CancerTraceUiFactory.CreateText(
                "Title", panel.transform, font, "CancerTrace", 82f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Ink,
                new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.94f),
                Vector2.zero, Vector2.zero);
            CancerTraceUiFactory.CreateText(
                "Subtitle", panel.transform, font, "癌迹追踪 · 基因侦探事务所", 32f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Accent,
                new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.76f),
                Vector2.zero, Vector2.zero);

            Image character = CancerTraceUiFactory.CreateImage(
                "Detective", panel.transform, detectiveSprite, Color.white,
                new Vector2(0.07f, 0.23f), new Vector2(0.37f, 0.66f),
                Vector2.zero, Vector2.zero);
            character.type = Image.Type.Simple;
            character.preserveAspect = true;

            newShiftButton = CancerTraceUiFactory.CreateButton(
                "NewShiftButton", panel.transform, font, startButtonSprite,
                "开始调查 / New Shift",
                new Vector2(0.39f, 0.45f), new Vector2(0.88f, 0.59f),
                Vector2.zero, Vector2.zero);
            continueButton = CancerTraceUiFactory.CreateButton(
                "ContinueButton", panel.transform, font, null,
                "继续调查（Phase C）",
                new Vector2(0.39f, 0.30f), new Vector2(0.88f, 0.41f),
                Vector2.zero, Vector2.zero);
            tutorialButton = CancerTraceUiFactory.CreateButton(
                "TutorialButton", panel.transform, font, null,
                "教程（本轮未实现）",
                new Vector2(0.39f, 0.17f), new Vector2(0.88f, 0.27f),
                Vector2.zero, Vector2.zero);
            statusText = CancerTraceUiFactory.CreateText(
                "Status", panel.transform, font, "正在读取案件档案……", 23f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Muted,
                new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.14f),
                Vector2.zero, Vector2.zero);
        }
    }
}
