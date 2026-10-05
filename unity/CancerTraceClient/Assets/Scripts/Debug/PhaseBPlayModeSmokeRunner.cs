using System;
using System.Collections;
using System.IO;
using CancerTrace.Gameplay.Flow;
using CancerTrace.UI.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.Debugging
{
    public sealed class PhaseBPlayModeSmokeRunner : MonoBehaviour
    {
        private CancerTraceApp app;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            yield return RunGuarded();
        }

        private IEnumerator RunGuarded()
        {
            Exception failure = null;
            IEnumerator flow = RunFlow();
            while (true)
            {
                object current = null;
                try
                {
                    if (!flow.MoveNext()) break;
                    current = flow.Current;
                }
                catch (Exception exception)
                {
                    failure = exception;
                    break;
                }
                yield return current;
            }

            CleanupTemporarySave();
            if (failure == null)
            {
                Debug.Log("PHASE_B_PLAY_MODE_SMOKE_PASSED: MainMenu -> Case 1 -> Gene Scan -> " +
                          "Cancer Galaxy -> Back -> AI Assistant -> Diagnosis -> Result -> " +
                          "Next Case -> Case 2.");
                ExitEditor(0);
            }
            else
            {
                Debug.LogError("PHASE_B_PLAY_MODE_SMOKE_FAILED: " + failure);
                ExitEditor(1);
            }
        }

        private IEnumerator RunFlow()
        {
            app = CancerTraceApp.EnsureInstance();
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!app.IsReady && string.IsNullOrEmpty(app.ErrorMessage) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Require(app.IsReady, "CancerTraceApp did not become ready: " + app.ErrorMessage);

            InvokeButton("NewShiftButton");
            yield return WaitForScene("CaseAnalysis");
            yield return null;

            GameplayActionResult<PlayerCaseView> first = app.Gameplay.GetCurrentCase();
            RequireSuccess(first, "Read Case 1");
            Require(first.Data.ShiftNumber == 1 && first.Data.CaseNumber == 1 &&
                    first.Data.CasesPerShift == 10 && first.Data.RemainingRp == 200,
                "Case 1 header values are incorrect.");
            Require(first.Data.InitialClues.Count == 2, "Case 1 Initial Clues count is not 2.");
            string firstCaseId = first.Data.CaseId;
            RequireNoPreSubmitLeak();

            InvokeButton("GeneScan");
            yield return null;
            PlayerCaseView afterGene = RequireCurrentCase("Case 1 after Gene Scan");
            Require(afterGene.GeneScan.Used && afterGene.RemainingRp == 190,
                "Gene Scan button did not consume 10 RP.");

            InvokeButton("CancerGalaxy");
            yield return WaitForScene("CancerGalaxy");
            yield return null;
            yield return null;
            Require(GameObject.Find("CurrentCaseNode") != null,
                "Cancer Galaxy did not render the current Case Node.");
            Require(GameObject.Find("PlotArea").transform.childCount == 646,
                "Cancer Galaxy did not render 645 Reference Nodes plus the Case Node.");
            RequireNoPreSubmitLeak();

            InvokeButton("Back");
            yield return WaitForScene("CaseAnalysis");
            yield return null;
            PlayerCaseView afterBack = RequireCurrentCase("Case 1 after Galaxy Back");
            Require(afterBack.CancerGalaxy.Used && afterBack.GeneScan.Used &&
                    afterBack.RemainingRp == 165 && afterBack.CaseId == firstCaseId,
                "Galaxy Back did not preserve Case/RP/tool state.");

            InvokeButton("AiAssistant");
            yield return null;
            PlayerCaseView afterAi = RequireCurrentCase("Case 1 after AI");
            Require(afterAi.AiAssistant.Used && afterAi.RemainingRp == 125,
                "AI Assistant button did not consume 40 RP.");
            GameplayActionResult<EvidenceBoardView> evidence = app.Gameplay.GetEvidenceBoard();
            RequireSuccess(evidence, "Case 1 Evidence Board");
            Require(evidence.Data.InitialClues.Count == 2 &&
                    evidence.Data.GeneScanClues.Count == 3 &&
                    evidence.Data.GalaxyEvidence != null && evidence.Data.AiEvidence != null,
                "Case 1 Evidence Board did not refresh all evidence.");
            RequireNoPreSubmitLeak();

            InvokeButton("Diagnosis0");
            yield return null;
            Require(!string.IsNullOrEmpty(RequireCurrentCase("Selected diagnosis").SelectedDiagnosis),
                "Diagnosis button did not select a formal classId.");
            InvokeButton("Submit");
            yield return WaitForScene("ResultSummary");
            yield return null;
            GameplayActionResult<ResultView> firstResult = app.Gameplay.GetResult();
            RequireSuccess(firstResult, "Case 1 Result");
            Require(firstResult.Data.AiReview != null &&
                    firstResult.Data.AiReview.TopCandidates.Count == 3,
                "ResultSummary did not display AI review.");

            InvokeButton("NextCase");
            yield return WaitForScene("CaseAnalysis");
            yield return null;
            PlayerCaseView second = RequireCurrentCase("Case 2");
            Require(second.CaseNumber == 2 && second.CaseId != firstCaseId,
                "Next Case did not enter a distinct Case 2.");
            Require(second.RemainingRp == 125 &&
                    second.CurrentScore == firstResult.Data.CurrentScore,
                "Case 2 did not preserve RP/Score.");
            Require(!second.GeneScan.Used && !second.CancerGalaxy.Used &&
                    !second.AiAssistant.Used && second.InitialClues.Count == 2,
                "Case 2 did not reset its tools/Initial Clues.");
            Require(File.Exists(app.SavePath), "Play Mode autosave was not created.");
            RequireNoPreSubmitLeak();
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!string.Equals(SceneManager.GetActiveScene().name, sceneName, StringComparison.Ordinal) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Require(string.Equals(SceneManager.GetActiveScene().name, sceneName, StringComparison.Ordinal),
                "Timed out waiting for scene " + sceneName + ".");
        }

        private PlayerCaseView RequireCurrentCase(string operation)
        {
            GameplayActionResult<PlayerCaseView> result = app.Gameplay.GetCurrentCase();
            RequireSuccess(result, operation);
            return result.Data;
        }

        private static void InvokeButton(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            Require(target != null, "Button GameObject is missing: " + objectName);
            Button button = target.GetComponent<Button>();
            Require(button != null && button.interactable,
                "Button is missing or disabled: " + objectName);
            button.onClick.Invoke();
        }

        private static void RequireNoPreSubmitLeak()
        {
            Require(!ContainsForbiddenText("ANOMALY") && !ContainsForbiddenText("异常案件"),
                "A pre-submit scene leaked the ANOMALY marker.");
        }

        private static bool ContainsForbiddenText(string forbidden)
        {
            TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                string value = texts[index].text;
                if (!string.IsNullOrEmpty(value) &&
                    value.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void CleanupTemporarySave()
        {
            if (app == null || string.IsNullOrEmpty(app.SavePath)) return;
            try
            {
                if (File.Exists(app.SavePath)) File.Delete(app.SavePath);
                string temporaryPath = app.SavePath + ".tmp";
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not clean Phase B temporary save: " + exception.Message);
            }
        }

        private static void RequireSuccess<T>(GameplayActionResult<T> result, string operation)
        {
            Require(result != null && result.Success,
                operation + " failed: " + (result == null ? "null" : result.Message));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void ExitEditor(int exitCode)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(exitCode);
#else
            Application.Quit(exitCode);
#endif
        }
    }
}
