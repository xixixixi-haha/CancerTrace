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
            continueButton.onClick.AddListener(ContinueInvestigation);
            tutorialButton.onClick.AddListener(StartTutorial);
            SetButtons(false, false, false);
            statusText.text = "正在读取案件档案……";
        }

        private void Update()
        {
            if (app == null) app = CancerTraceApp.EnsureInstance();
            if (app.IsReady)
            {
                if (!readyShown)
                {
                    readyShown = true;
                    RefreshMainMenuState();
                }
            }
            else if (!string.IsNullOrEmpty(app.ErrorMessage))
            {
                SetButtons(false, false, false);
                statusText.text = "数据加载失败：" + app.ErrorMessage;
            }
        }

        private void RefreshMainMenuState()
        {
            GameplayActionResult<PlayerCaseView> current = app.Gameplay.GetCurrentCase();
            if (current.Success)
            {
                bool shiftComplete = current.Data.ShiftCompleted;
                SetButtons(shiftComplete, !shiftComplete, true);
                statusText.text = shiftComplete
                    ? "本轮调查已完成，可以开始下一轮。"
                    : "检测到进行中的调查，请继续调查。";
                return;
            }

            string saveError;
            FormalSaveStatus saveStatus = app.GetFormalSaveStatus(out saveError);
            switch (saveStatus)
            {
                case FormalSaveStatus.NoSave:
                    SetButtons(true, false, true);
                    statusText.text = "档案已就绪。开始今天的调查吧！";
                    return;
                case FormalSaveStatus.ValidSave:
                    SetButtons(false, true, true);
                    statusText.text = "检测到可恢复的调查记录，请继续调查。";
                    return;
                default:
                    SetButtons(false, false, true);
                    statusText.text = "调查记录无效。为保护原存档，已禁用开始与继续。";
                    return;
            }
        }

        private void StartNewShift()
        {
            SetButtons(false, false, false);
            app.Tutorial.EndTutorial();

            GameplayActionResult<PlayerCaseView> current = app.Gameplay.GetCurrentCase();
            GameplayActionResult<PlayerCaseView> result;
            if (current.Success)
            {
                if (!current.Data.ShiftCompleted)
                {
                    RefreshMainMenuState();
                    statusText.text = "当前调查尚未完成，请继续调查。";
                    return;
                }

                result = app.Gameplay.StartNextShift();
            }
            else
            {
                string saveError;
                FormalSaveStatus saveStatus = app.GetFormalSaveStatus(out saveError);
                if (saveStatus != FormalSaveStatus.NoSave)
                {
                    RefreshMainMenuState();
                    statusText.text = saveStatus == FormalSaveStatus.ValidSave
                        ? "已有调查记录，请使用继续调查。"
                        : "调查记录无效。为保护原存档，无法开始新调查。";
                    return;
                }

                result = app.Gameplay.StartNewShift();
            }

            if (!result.Success)
            {
                Debug.LogError("Could not start a formal Shift: " + result.Message);
                RefreshMainMenuState();
                statusText.text = "无法开始调查：" + result.Message;
                return;
            }

            SceneManager.LoadScene("CaseAnalysis");
        }

        private void ContinueInvestigation()
        {
            SetButtons(false, false, false);
            app.Tutorial.EndTutorial();

            GameplayActionResult<PlayerCaseView> current = app.Gameplay.GetCurrentCase();
            if (!current.Success)
            {
                string saveError;
                FormalSaveStatus saveStatus = app.GetFormalSaveStatus(out saveError);
                if (saveStatus != FormalSaveStatus.ValidSave)
                {
                    if (!string.IsNullOrEmpty(saveError))
                    {
                        Debug.LogError("Could not continue formal investigation: " + saveError);
                    }
                    RefreshMainMenuState();
                    statusText.text = "无法继续调查：没有可用的正式存档。";
                    return;
                }

                current = app.Gameplay.RestoreSavedShift();
                if (!current.Success)
                {
                    Debug.LogError("Could not restore formal investigation: " + current.Message);
                    RefreshMainMenuState();
                    statusText.text = "无法恢复调查：" + current.Message;
                    return;
                }
            }

            RouteFormalSession(current.Data);
        }

        private static void RouteFormalSession(PlayerCaseView current)
        {
            if (current.ShiftCompleted)
            {
                SceneManager.LoadScene("ShiftSummary");
                return;
            }
            if (current.DiagnosisSubmitted)
            {
                SceneManager.LoadScene("ResultSummary");
                return;
            }

            SceneManager.LoadScene("CaseAnalysis");
        }

        private void StartTutorial()
        {
            tutorialButton.interactable = false;
            string errorMessage;
            if (!app.Tutorial.TryStartTutorial(out errorMessage))
            {
                statusText.text = "无法开始教程：" + errorMessage;
                tutorialButton.interactable = true;
                return;
            }

            SceneManager.LoadScene("CaseAnalysis");
        }

        private void SetButtons(bool startEnabled, bool continueEnabled, bool tutorialEnabled)
        {
            newShiftButton.interactable = startEnabled;
            continueButton.interactable = continueEnabled;
            tutorialButton.interactable = tutorialEnabled;
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
