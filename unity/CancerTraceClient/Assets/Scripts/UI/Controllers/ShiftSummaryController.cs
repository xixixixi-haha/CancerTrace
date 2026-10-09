using CancerTrace.Gameplay.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.UI.Controllers
{
    public sealed class ShiftSummaryController : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreValueText;
        [SerializeField] private TMP_Text correctDiagnosisValueText;
        [SerializeField] private Button returnMainMenuButton;
        [SerializeField] private Button startNewShiftButton;

        private CancerTraceApp app;
        private bool initialized;

        private void Awake()
        {
            app = CancerTraceApp.EnsureInstance();
            returnMainMenuButton.onClick.AddListener(ReturnToMainMenu);
            startNewShiftButton.onClick.AddListener(StartNewShift);
            returnMainMenuButton.interactable = false;
            startNewShiftButton.interactable = false;
        }

        private void Update()
        {
            if (initialized) return;
            if (app == null) app = CancerTraceApp.EnsureInstance();
            if (!app.IsReady) return;

            initialized = true;
            GameplayActionResult<ShiftSummaryView> result = app.Gameplay.GetShiftSummary();
            if (!result.Success)
            {
                Debug.LogWarning("Shift Summary could not be displayed: " + result.Message);
                return;
            }

            scoreValueText.text = result.Data.Score.ToString();
            correctDiagnosisValueText.text = result.Data.CorrectCases + " / " + result.Data.CasesCompleted;
            returnMainMenuButton.interactable = true;
            startNewShiftButton.interactable = true;
        }

        private void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void StartNewShift()
        {
            startNewShiftButton.interactable = false;
            GameplayActionResult<PlayerCaseView> result = app.Gameplay.StartNextShift();
            if (!result.Success)
            {
                Debug.LogWarning("Could not start the next Shift: " + result.Message);
                startNewShiftButton.interactable = true;
                return;
            }

            SceneManager.LoadScene("CaseAnalysis");
        }
    }
}
