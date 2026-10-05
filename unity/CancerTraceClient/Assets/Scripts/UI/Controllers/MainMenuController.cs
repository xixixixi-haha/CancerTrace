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
            Sprite logo,
            Sprite startButtonSprite,
            Sprite continueButtonSprite,
            Sprite tutorialButtonSprite)
        {
            Canvas canvas = CancerTraceUiFactory.CreateCanvas(transform);
            Image backgroundImage = CancerTraceUiFactory.CreateBackground(canvas.transform, background);
            backgroundImage.preserveAspect = true;

            Image logoImage = CancerTraceUiFactory.CreateImage(
                "Logo", canvas.transform, logo, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-450f, 156f), new Vector2(450f, 484f));
            logoImage.type = Image.Type.Simple;
            logoImage.preserveAspect = true;

            RectTransform buttonGroup = CancerTraceUiFactory.CreateRect(
                "ButtonGroup", canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-180f, -393.5f), new Vector2(340f, 173.5f));

            newShiftButton = CancerTraceUiFactory.CreateButton(
                "NewShiftButton", buttonGroup, font, startButtonSprite, null,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-260f, 110.5f), new Vector2(260f, 283.5f));
            continueButton = CancerTraceUiFactory.CreateButton(
                "ContinueButton", buttonGroup, font, continueButtonSprite, null,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-260f, -86.5f), new Vector2(260f, 86.5f));
            tutorialButton = CancerTraceUiFactory.CreateButton(
                "TutorialButton", buttonGroup, font, tutorialButtonSprite, null,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-260f, -283.5f), new Vector2(260f, -110.5f));

            ConfigureArtButton(newShiftButton);
            ConfigureArtButton(continueButton);
            ConfigureArtButton(tutorialButton);
            continueButton.interactable = false;
            tutorialButton.interactable = false;

            statusText = CancerTraceUiFactory.CreateText(
                "Status", canvas.transform, font, "", 1f,
                TextAlignmentOptions.Center, CancerTraceUiFactory.Muted,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                Vector2.zero, Vector2.zero);
            statusText.gameObject.SetActive(false);
        }

        private static void ConfigureArtButton(Button button)
        {
            Image image = button.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
    }
}
