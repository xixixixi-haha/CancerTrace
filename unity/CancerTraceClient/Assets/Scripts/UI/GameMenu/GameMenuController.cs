using System;
using CancerTrace.Data.Save;
using CancerTrace.UI.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.UI.GameMenu
{
    /// <summary>
    /// Owns the single cross-scene game menu and protects formal saves before leaving gameplay.
    /// </summary>
    public sealed class GameMenuController : MonoBehaviour
    {
        private static readonly string[] SupportedScenes =
        {
            "CaseAnalysis",
            "CancerGalaxy",
            "ResultSummary"
        };

        private static GameMenuController instance;

        [Header("Visibility")]
        [SerializeField] private GameObject menuEntryButton;
        [SerializeField] private GameObject gameMenuOverlay;

        [Header("Actions")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button returnMainMenuButton;
        [SerializeField] private Button quitButton;

        [Header("SFX preferences")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumePercentText;
        [SerializeField] private Button sfxCheckbox;
        [SerializeField] private Image sfxCheckboxImage;
        [SerializeField] private Sprite sfxCheckedSprite;
        [SerializeField] private Sprite sfxUncheckedSprite;

        [Header("Feedback")]
        [SerializeField] private TMP_Text errorText;

        public bool IsOpen
        {
            get { return gameMenuOverlay != null && gameMenuOverlay.activeSelf; }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            WireControls();
            RefreshPreferenceControls();
            CloseMenu();
            RefreshSceneVisibility(SceneManager.GetActiveScene().name);
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        private void OnDestroy()
        {
            if (instance != this) return;

            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            instance = null;
        }

        private void Update()
        {
            if (IsSupportedScene(SceneManager.GetActiveScene().name) && Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsOpen) CloseMenu();
                else OpenMenu();
            }
        }

        public void OpenMenu()
        {
            if (!IsSupportedScene(SceneManager.GetActiveScene().name)) return;

            ClearError();
            RefreshPreferenceControls();
            gameMenuOverlay.SetActive(true);
            gameMenuOverlay.transform.SetAsLastSibling();
        }

        public void CloseMenu()
        {
            if (gameMenuOverlay != null) gameMenuOverlay.SetActive(false);
            ClearError();
            SetActionButtonsInteractable(true);
        }

        private void WireControls()
        {
            Button entry = menuEntryButton.GetComponent<Button>();
            entry.onClick.AddListener(OpenMenu);
            closeButton.onClick.AddListener(CloseMenu);
            resumeButton.onClick.AddListener(CloseMenu);
            returnMainMenuButton.onClick.AddListener(ReturnToMainMenu);
            quitButton.onClick.AddListener(QuitGame);
            volumeSlider.onValueChanged.AddListener(SetSfxVolume);
            sfxCheckbox.onClick.AddListener(ToggleSfx);
        }

        private void HandleActiveSceneChanged(Scene previous, Scene next)
        {
            CloseMenu();
            RefreshSceneVisibility(next.name);
        }

        private void RefreshSceneVisibility(string sceneName)
        {
            menuEntryButton.SetActive(IsSupportedScene(sceneName));
        }

        private void RefreshPreferenceControls()
        {
            float volume = GameMenuPreferences.SfxVolume;
            volumeSlider.SetValueWithoutNotify(volume);
            volumePercentText.text = Mathf.RoundToInt(volume * 100f) + "%";
            sfxCheckboxImage.sprite = GameMenuPreferences.SfxEnabled
                ? sfxCheckedSprite
                : sfxUncheckedSprite;
        }

        private void SetSfxVolume(float value)
        {
            GameMenuPreferences.SfxVolume = value;
            volumePercentText.text = Mathf.RoundToInt(GameMenuPreferences.SfxVolume * 100f) + "%";
        }

        private void ToggleSfx()
        {
            GameMenuPreferences.SfxEnabled = !GameMenuPreferences.SfxEnabled;
            sfxCheckboxImage.sprite = GameMenuPreferences.SfxEnabled
                ? sfxCheckedSprite
                : sfxUncheckedSprite;
        }

        private void ReturnToMainMenu()
        {
            CancerTraceApp app = CancerTraceApp.Instance;
            if (!TryPrepareToLeave(app)) return;

            CloseMenu();
            SceneManager.LoadScene("MainMenu");
        }

        private void QuitGame()
        {
            CancerTraceApp app = CancerTraceApp.Instance;
            if (!TryPrepareToLeave(app)) return;

            CloseMenu();
            Application.Quit();
        }

        private bool TryPrepareToLeave(CancerTraceApp app)
        {
            ClearError();
            SetActionButtonsInteractable(false);

            if (app == null || !app.IsReady)
            {
                ShowError(app == null || string.IsNullOrEmpty(app.ErrorMessage)
                    ? "游戏服务尚未准备完成，请稍后再试。"
                    : "游戏服务加载失败，无法安全离开。\n" + app.ErrorMessage);
                SetActionButtonsInteractable(true);
                return false;
            }

            if (app.Tutorial != null && app.Tutorial.IsTutorialActive)
            {
                app.Tutorial.EndTutorial();
                return true;
            }

            if (app.Gameplay == null)
            {
                ShowError("正式游戏服务不可用，无法安全保存。");
                SetActionButtonsInteractable(true);
                return false;
            }

            SaveOperationResult save = app.Gameplay.SaveNow();
            if (save != null && save.Success) return true;

            ShowError(
                "保存失败，已留在当前调查。\n" +
                (save == null || string.IsNullOrEmpty(save.ErrorMessage)
                    ? "未返回可用的保存结果。"
                    : save.ErrorMessage));
            SetActionButtonsInteractable(true);
            return false;
        }

        private void SetActionButtonsInteractable(bool value)
        {
            if (returnMainMenuButton != null) returnMainMenuButton.interactable = value;
            if (quitButton != null) quitButton.interactable = value;
        }

        private void ShowError(string message)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }

        private void ClearError()
        {
            if (errorText == null) return;
            errorText.text = string.Empty;
            errorText.gameObject.SetActive(false);
        }

        private static bool IsSupportedScene(string sceneName)
        {
            for (int index = 0; index < SupportedScenes.Length; index++)
            {
                if (string.Equals(sceneName, SupportedScenes[index], StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
