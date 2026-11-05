using CancerTrace.Data.Save;
using CancerTrace.Gameplay.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.UI.Controllers
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private enum RecoveryTarget
        {
            None,
            FormalSave,
            TutorialProgress
        }

        [SerializeField] private Button newShiftButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private TMP_Text statusText;

        private CancerTraceApp app;
        private bool readyShown;
        private GameObject recoveryPanel;
        private TMP_Text recoveryTitleText;
        private TMP_Text recoveryMessageText;
        private Button recoveryPrimaryButton;
        private Button recoveryCancelButton;
        private RecoveryTarget recoveryTarget;
        private string recoveryErrorDetails;

        private void Awake()
        {
            app = CancerTraceApp.EnsureInstance();
            newShiftButton.onClick.AddListener(StartNewShift);
            continueButton.onClick.AddListener(ContinueInvestigation);
            tutorialButton.onClick.AddListener(StartTutorial);
            ConfigureStatusText();
            CreateRecoveryPanel();
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
                ShowFatalStartupError(app.ErrorMessage);
            }
        }

        private void RefreshMainMenuState()
        {
            HideRecoveryPanel();
            if (app.TutorialProgressRecoveryRequired)
            {
                ShowRecoveryChoice(
                    RecoveryTarget.TutorialProgress,
                    app.TutorialProgressRecoveryError);
                return;
            }

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
                    ShowRecoveryChoice(RecoveryTarget.FormalSave, saveError);
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

        private void ConfigureStatusText()
        {
            RectTransform rect = statusText.rectTransform;
            rect.anchorMin = new Vector2(0.20f, 0.025f);
            rect.anchorMax = new Vector2(0.80f, 0.105f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            statusText.fontSize = 22f;
            statusText.color = CancerTraceUiFactory.Muted;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.enableWordWrapping = true;
            statusText.overflowMode = TextOverflowModes.Ellipsis;
            statusText.gameObject.SetActive(true);
        }

        private void CreateRecoveryPanel()
        {
            Transform canvas = statusText.transform.parent;
            Image paper = CancerTraceUiFactory.CreateImage(
                "RecoveryPanel",
                canvas,
                null,
                new Color32(255, 249, 225, 250),
                new Vector2(0.19f, 0.12f),
                new Vector2(0.81f, 0.60f),
                Vector2.zero,
                Vector2.zero);
            recoveryPanel = paper.gameObject;
            Outline outline = recoveryPanel.AddComponent<Outline>();
            outline.effectColor = new Color32(153, 112, 105, 190);
            outline.effectDistance = new Vector2(4f, -4f);
            outline.useGraphicAlpha = false;

            recoveryTitleText = CancerTraceUiFactory.CreateText(
                "RecoveryTitle",
                paper.transform,
                statusText.font,
                "档案需要处理",
                32f,
                TextAlignmentOptions.Center,
                CancerTraceUiFactory.Accent,
                new Vector2(0.08f, 0.78f),
                new Vector2(0.92f, 0.94f),
                Vector2.zero,
                Vector2.zero);
            recoveryTitleText.fontStyle = FontStyles.Bold;

            recoveryMessageText = CancerTraceUiFactory.CreateText(
                "RecoveryMessage",
                paper.transform,
                statusText.font,
                string.Empty,
                23f,
                TextAlignmentOptions.TopLeft,
                CancerTraceUiFactory.Ink,
                new Vector2(0.08f, 0.30f),
                new Vector2(0.92f, 0.77f),
                Vector2.zero,
                Vector2.zero);
            recoveryMessageText.enableAutoSizing = true;
            recoveryMessageText.richText = false;
            recoveryMessageText.fontSizeMin = 17f;
            recoveryMessageText.fontSizeMax = 23f;
            recoveryMessageText.overflowMode = TextOverflowModes.Ellipsis;

            recoveryPrimaryButton = CancerTraceUiFactory.CreateButton(
                "RecoveryPrimaryButton",
                paper.transform,
                statusText.font,
                null,
                "查看恢复说明",
                new Vector2(0.12f, 0.08f),
                new Vector2(0.60f, 0.24f),
                Vector2.zero,
                Vector2.zero);
            recoveryCancelButton = CancerTraceUiFactory.CreateButton(
                "RecoveryCancelButton",
                paper.transform,
                statusText.font,
                null,
                "取消",
                new Vector2(0.64f, 0.08f),
                new Vector2(0.88f, 0.24f),
                Vector2.zero,
                Vector2.zero);
            recoveryCancelButton.gameObject.SetActive(false);
            recoveryPanel.SetActive(false);
        }

        private void ShowRecoveryChoice(RecoveryTarget target, string details)
        {
            recoveryTarget = target;
            recoveryErrorDetails = details;
            SetButtons(false, false, false);
            recoveryPanel.SetActive(true);
            recoveryPanel.transform.SetAsLastSibling();
            recoveryCancelButton.gameObject.SetActive(false);
            recoveryPrimaryButton.gameObject.SetActive(true);
            recoveryPrimaryButton.interactable = true;
            recoveryPrimaryButton.onClick.RemoveAllListeners();
            recoveryPrimaryButton.onClick.AddListener(ShowRecoveryConfirmation);
            CancerTraceUiFactory.SetButtonLabel(recoveryPrimaryButton, "查看恢复说明");

            if (target == RecoveryTarget.FormalSave)
            {
                recoveryTitleText.text = "调查存档无法读取";
                recoveryMessageText.text =
                    "检测到损坏或不兼容的正式调查存档。为保护原文件，开始调查与继续游戏已暂时停用。\n\n" +
                    "存档位置：" + app.SavePath + BuildShortError(details);
                statusText.text = "正式调查存档需要由你确认处理。";
            }
            else
            {
                recoveryTitleText.text = "教程进度无法读取";
                recoveryMessageText.text =
                    "检测到损坏或不兼容的 Tutorial Progress。原文件尚未被覆盖或删除；在你确认处理前，游戏入口已暂时停用。\n\n" +
                    "文件位置：" + app.TutorialProgressPath + BuildShortError(details);
                statusText.text = "教程进度需要由你确认处理。";
            }
        }

        private void ShowRecoveryConfirmation()
        {
            recoveryCancelButton.gameObject.SetActive(true);
            recoveryCancelButton.interactable = true;
            recoveryCancelButton.onClick.RemoveAllListeners();
            recoveryCancelButton.onClick.AddListener(RefreshMainMenuState);
            recoveryPrimaryButton.onClick.RemoveAllListeners();
            recoveryPrimaryButton.onClick.AddListener(ConfirmRecovery);
            CancerTraceUiFactory.SetButtonLabel(recoveryPrimaryButton, "确认备份并重置");

            if (recoveryTarget == RecoveryTarget.FormalSave)
            {
                recoveryMessageText.text =
                    "确认后，原损坏存档会被移动到同一目录下带时间戳的 .corrupt-*.bak 文件，不会被修复或覆盖。\n\n" +
                    "原存档位置将变为空档位，你可以开始新的调查；旧调查无法再通过“继续游戏”恢复。";
            }
            else
            {
                recoveryMessageText.text =
                    "确认后，原损坏教程进度会被移动到同一目录下带时间戳的 .corrupt-*.bak 文件，不会被修复或覆盖。\n\n" +
                    "若有效正式存档保存了教程完成标记，会沿用该标记；否则教程进度回到未完成状态。正式调查存档不会被改动。";
            }
        }

        private void ConfirmRecovery()
        {
            recoveryPrimaryButton.interactable = false;
            recoveryCancelButton.interactable = false;
            CorruptFileRecoveryResult result = recoveryTarget == RecoveryTarget.FormalSave
                ? app.PreserveInvalidFormalSave()
                : app.RecoverTutorialProgress();

            if (!result.Success)
            {
                recoveryTitleText.text = "未能完成恢复操作";
                recoveryMessageText.text =
                    "原文件未被静默覆盖。请检查文件权限或磁盘状态后重试。" +
                    (string.IsNullOrEmpty(result.PreservedPath) ? string.Empty :
                        "\n原损坏文件已保留为：" + result.PreservedPath) +
                    BuildShortError(result.ErrorMessage);
                recoveryPrimaryButton.interactable = true;
                recoveryCancelButton.interactable = true;
                CancerTraceUiFactory.SetButtonLabel(recoveryPrimaryButton, "重试");
                return;
            }

            recoveryTitleText.text = "损坏文件已保留";
            recoveryMessageText.text = string.IsNullOrEmpty(result.PreservedPath)
                ? "待处理文件已经不存在，当前档位已恢复为可用状态。"
                : "原文件已保留为：\n" + result.PreservedPath +
                  "\n\n可以在需要排查时查看该备份。";
            recoveryCancelButton.gameObject.SetActive(false);
            recoveryPrimaryButton.interactable = true;
            recoveryPrimaryButton.onClick.RemoveAllListeners();
            recoveryPrimaryButton.onClick.AddListener(AcknowledgeRecovery);
            CancerTraceUiFactory.SetButtonLabel(recoveryPrimaryButton, "知道了");
        }

        private void AcknowledgeRecovery()
        {
            recoveryTarget = RecoveryTarget.None;
            recoveryErrorDetails = null;
            recoveryPanel.SetActive(false);
            readyShown = true;
            RefreshMainMenuState();
        }

        private void ShowFatalStartupError(string details)
        {
            recoveryTarget = RecoveryTarget.None;
            recoveryTitleText.text = "游戏数据加载失败";
            recoveryMessageText.text =
                "应用无法安全初始化，未对存档执行任何修改。请保留 Player.log 并检查游戏文件。" +
                BuildShortError(details);
            recoveryPrimaryButton.gameObject.SetActive(false);
            recoveryCancelButton.gameObject.SetActive(false);
            recoveryPanel.SetActive(true);
            recoveryPanel.transform.SetAsLastSibling();
            statusText.text = "游戏数据加载失败。";
        }

        private void HideRecoveryPanel()
        {
            if (recoveryPanel != null) recoveryPanel.SetActive(false);
        }

        private static string BuildShortError(string details)
        {
            if (string.IsNullOrEmpty(details)) return string.Empty;
            const int maximumLength = 320;
            string compact = details.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (compact.Length > maximumLength)
            {
                compact = compact.Substring(0, maximumLength) + "…";
            }
            return "\n\n错误详情：" + compact;
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
