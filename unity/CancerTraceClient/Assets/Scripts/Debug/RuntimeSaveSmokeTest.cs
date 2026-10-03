using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CancerTrace.Data;
using CancerTrace.Data.Models;
using CancerTrace.Data.Save;
using CancerTrace.Gameplay.Runtime;
using UnityEngine;

namespace CancerTrace.Debugging
{
    public sealed class RuntimeSaveSmokeTest : MonoBehaviour
    {
        private const string SmokeSaveFileName = "cancertrace_save_v1_smoke_test.json";

        [ContextMenu("Run CancerTrace Runtime/Save Smoke Test")]
        public void RunSmokeTest()
        {
            StartCoroutine(RunSmokeTestCoroutine());
        }

        // Allows the same smoke test to run through Unity Editor batch mode without a Scene.
        public static void RunBatchSmokeTest()
        {
            GameDataLoadResult loadResult = null;
            GameDataLoader loader = new GameDataLoader();
            Drain(loader.Load(value => loadResult = value));
            Require(loadResult != null && loadResult.Success,
                loadResult == null ? "Formal GameData load returned no result." : loadResult.ErrorMessage);

            ExecuteSmoke(
                loadResult.Data,
                Path.Combine(Application.persistentDataPath, SmokeSaveFileName),
                true);
        }

        private IEnumerator RunSmokeTestCoroutine()
        {
            GameDataLoadResult loadResult = null;
            GameDataLoader loader = new GameDataLoader();
            yield return loader.Load(value => loadResult = value);

            if (loadResult == null || !loadResult.Success)
            {
                Debug.LogError(
                    "CancerTrace Runtime/Save smoke test could not load formal GameData." +
                    (loadResult == null ? string.Empty : "\n" + loadResult.ErrorMessage));
                yield break;
            }

            ExecuteSmoke(
                loadResult.Data,
                Path.Combine(Application.persistentDataPath, SmokeSaveFileName),
                false);
        }

        private static void ExecuteSmoke(
            GameDataContext data,
            string savePath,
            bool throwOnFailure)
        {
            SaveDataRepository saveRepository = new SaveDataRepository(
                data.Cases,
                data.Config,
                savePath);

            try
            {
                SaveOperationResult cleanupBeforeTest = saveRepository.DeleteSave();
                Require(cleanupBeforeTest.Success, cleanupBeforeTest.ErrorMessage);

                GameRuntimeService runtime = new GameRuntimeService(
                    data.Cases,
                    data.Config,
                    saveRepository,
                    new System.Random(20261003));

                string error;
                Require(runtime.TryStartNewShift(out error), error);
                GameRuntimeState initial = runtime.CurrentState;
                Require(initial.SelectedCaseIds.Count == data.Config.CasesPerShift,
                    "New Shift did not select the configured Case count.");
                Require(new HashSet<string>(initial.SelectedCaseIds, StringComparer.Ordinal).Count ==
                        initial.SelectedCaseIds.Count,
                    "New Shift contains duplicate caseId values.");
                Require(initial.RemainingRp == data.Config.StartingRp,
                    "New Shift did not use the configured starting RP.");
                Require(initial.CurrentScore == 0, "New Shift Score was not reset to zero.");
                Require(saveRepository.HasSave(), "New Shift creation did not auto-save.");

                Require(runtime.TryUseTool(InvestigationTool.GeneScan, out error), error);
                Require(!runtime.TryUseTool(InvestigationTool.GeneScan, out error),
                    "The same tool was accepted twice for one Case.");

                SaveOperationResult explicitSave = runtime.SaveNow();
                Require(explicitSave.Success, explicitSave.ErrorMessage);

                string[] selectedBeforeLoad = Copy(initial.SelectedCaseIds);
                int caseIndexBeforeLoad = initial.CurrentCaseIndex;
                int remainingRpBeforeLoad = initial.RemainingRp;
                int scoreBeforeLoad = initial.CurrentScore;
                bool geneScanBeforeLoad = initial.CurrentCaseState.GeneScanUsed;
                int completedBeforeLoad = initial.CompletedCaseResults.Count;

                GameRuntimeService restoredRuntime = new GameRuntimeService(
                    data.Cases,
                    data.Config,
                    saveRepository,
                    new System.Random(1));
                SaveLoadResult restored = restoredRuntime.RestoreFromSave();
                Require(restored.Success, restored.ErrorMessage);
                Require(SequenceEqual(selectedBeforeLoad, restored.State.SelectedCaseIds),
                    "selectedCaseIds changed during save/load recovery.");
                Require(restored.State.CurrentCaseIndex == caseIndexBeforeLoad,
                    "currentCaseIndex changed during save/load recovery.");
                Require(restored.State.RemainingRp == remainingRpBeforeLoad,
                    "remainingRP changed during save/load recovery.");
                Require(restored.State.CurrentScore == scoreBeforeLoad,
                    "currentScore changed during save/load recovery.");
                Require(restored.State.CurrentCaseState.GeneScanUsed == geneScanBeforeLoad,
                    "Tool state changed during save/load recovery.");
                Require(restored.State.CompletedCaseResults.Count == completedBeforeLoad,
                    "Completed results changed during save/load recovery.");

                GameCaseData firstStaticCase = data.Cases.GetByCaseId(
                    restoredRuntime.CurrentState.CurrentCaseState.CaseId);
                CompletedCaseResult completedResult;
                Require(restoredRuntime.TrySubmitDiagnosis(
                    firstStaticCase.TrueClassId,
                    out completedResult,
                    out error), error);
                Require(!restoredRuntime.TrySubmitDiagnosis(
                    firstStaticCase.TrueClassId,
                    out completedResult,
                    out error), "A Case accepted diagnosis submission twice.");
                Require(restoredRuntime.CurrentState.CompletedCaseResults.Count == 1,
                    "Diagnosis submission did not create one completed result.");

                SaveLoadResult afterDiagnosisLoad = saveRepository.Load();
                Require(afterDiagnosisLoad.Success, afterDiagnosisLoad.ErrorMessage);
                Require(afterDiagnosisLoad.State.CompletedCaseResults.Count == 1,
                    "Completed result was not restored from save.");
                Require(afterDiagnosisLoad.State.CurrentCaseState.DiagnosisSubmitted,
                    "Submitted diagnosis state was not restored from save.");

                SpendRpUntilInsufficient(restoredRuntime, data, out error);
                Require(!restoredRuntime.TryUseTool(InvestigationTool.GeneScan, out error),
                    "A tool was allowed when remainingRP was below its configured cost.");
                Require(restoredRuntime.CurrentState.RemainingRp >= data.Config.MinimumRp,
                    "remainingRP fell below the configured minimum.");

                File.WriteAllText(savePath, "{ damaged save", new System.Text.UTF8Encoding(false));
                SaveLoadResult damagedLoad = saveRepository.Load();
                Require(!damagedLoad.Success, "Malformed save JSON was accepted.");

                SaveOperationResult deleteResult = saveRepository.DeleteSave();
                Require(deleteResult.Success, deleteResult.ErrorMessage);
                Require(!saveRepository.HasSave(), "DeleteSave left the smoke-test save in place.");

                Debug.Log(
                    "CancerTrace Runtime/Save smoke test passed.\n" +
                    "Selected Cases: " + selectedBeforeLoad.Length + " unique\n" +
                    "Save/load recovery: passed\n" +
                    "Tool/RP/diagnosis guards: passed\n" +
                    "Damaged-save rejection: passed\n" +
                    "DeleteSave: passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("CancerTrace Runtime/Save smoke test failed.\n" + exception);
                if (throwOnFailure) throw;
            }
            finally
            {
                saveRepository.DeleteSave();
            }
        }

        private static void SpendRpUntilInsufficient(
            GameRuntimeService runtime,
            GameDataContext data,
            out string error)
        {
            bool shiftCompleted;
            Require(runtime.TryAdvanceToNextCase(out shiftCompleted, out error), error);
            Require(!shiftCompleted, "Shift completed unexpectedly during RP guard test.");

            UseAllTools(runtime, out error);
            SubmitCurrentCorrectly(runtime, data, out error);
            Require(runtime.TryAdvanceToNextCase(out shiftCompleted, out error), error);

            UseAllTools(runtime, out error);
            SubmitCurrentCorrectly(runtime, data, out error);
            Require(runtime.TryAdvanceToNextCase(out shiftCompleted, out error), error);

            Require(runtime.TryUseTool(InvestigationTool.AiAssistant, out error), error);
            Require(runtime.CurrentState.RemainingRp == data.Config.MinimumRp,
                "RP setup for insufficient-funds test did not reach the configured minimum.");
        }

        private static void UseAllTools(GameRuntimeService runtime, out string error)
        {
            Require(runtime.TryUseTool(InvestigationTool.GeneScan, out error), error);
            Require(runtime.TryUseTool(InvestigationTool.CancerGalaxy, out error), error);
            Require(runtime.TryUseTool(InvestigationTool.AiAssistant, out error), error);
        }

        private static void SubmitCurrentCorrectly(
            GameRuntimeService runtime,
            GameDataContext data,
            out string error)
        {
            GameCaseData gameCase = data.Cases.GetByCaseId(runtime.CurrentState.CurrentCaseState.CaseId);
            CompletedCaseResult unused;
            Require(runtime.TrySubmitDiagnosis(gameCase.TrueClassId, out unused, out error), error);
        }

        private static void Drain(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                IEnumerator nested = enumerator.Current as IEnumerator;
                if (nested != null) Drain(nested);
            }
        }

        private static string[] Copy(IReadOnlyList<string> values)
        {
            string[] copy = new string[values.Count];
            for (int index = 0; index < values.Count; index++) copy[index] = values[index];
            return copy;
        }

        private static bool SequenceEqual(string[] expected, IReadOnlyList<string> actual)
        {
            if (expected.Length != actual.Count) return false;
            for (int index = 0; index < expected.Length; index++)
            {
                if (!string.Equals(expected[index], actual[index], StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(string.IsNullOrEmpty(message)
                    ? "Smoke-test assertion failed."
                    : message);
            }
        }
    }
}
