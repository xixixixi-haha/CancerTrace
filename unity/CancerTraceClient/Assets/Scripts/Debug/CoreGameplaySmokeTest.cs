using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CancerTrace.Data;
using CancerTrace.Data.Models;
using CancerTrace.Data.Save;
using CancerTrace.Gameplay.Flow;
using CancerTrace.Gameplay.Runtime;
using UnityEngine;

namespace CancerTrace.Debugging
{
    public sealed class CoreGameplaySmokeTest : MonoBehaviour
    {
        private const string MainSmokeSaveFileName = "cancertrace_gameplay_flow_smoke_test.json";
        private const string AnomalySmokeSaveFileName = "cancertrace_anomaly_guard_smoke_test.json";
        private const string ZeroRpSmokeSaveFileName = "cancertrace_zero_rp_smoke_test.json";

        [ContextMenu("Run CancerTrace Core Gameplay Smoke Test")]
        public void RunSmokeTest()
        {
            StartCoroutine(RunSmokeTestCoroutine());
        }

        // Supports the same test in Unity Editor batch mode without a Scene or test GameObject.
        public static void RunBatchSmokeTest()
        {
            GameDataLoadResult loadResult = null;
            GameDataLoader loader = new GameDataLoader();
            Drain(loader.Load(value => loadResult = value));
            Require(loadResult != null && loadResult.Success,
                loadResult == null ? "Formal GameData load returned no result." : loadResult.ErrorMessage);
            ExecuteSmoke(loadResult.Data, true);
        }

        private IEnumerator RunSmokeTestCoroutine()
        {
            GameDataLoadResult loadResult = null;
            GameDataLoader loader = new GameDataLoader();
            yield return loader.Load(value => loadResult = value);

            if (loadResult == null || !loadResult.Success)
            {
                Debug.LogError(
                    "CancerTrace Core Gameplay smoke test could not load formal GameData." +
                    (loadResult == null ? string.Empty : "\n" + loadResult.ErrorMessage));
                yield break;
            }

            ExecuteSmoke(loadResult.Data, false);
        }

        private static void ExecuteSmoke(GameDataContext data, bool throwOnFailure)
        {
            string mainSavePath = Path.Combine(Application.persistentDataPath, MainSmokeSaveFileName);
            string anomalySavePath = Path.Combine(Application.persistentDataPath, AnomalySmokeSaveFileName);
            string zeroRpSavePath = Path.Combine(Application.persistentDataPath, ZeroRpSmokeSaveFileName);

            try
            {
                RunMainFlow(data, mainSavePath);
                RunAnomalyGuard(data, anomalySavePath);
                RunZeroRpSubmit(data, zeroRpSavePath);
                Debug.Log(
                    "CancerTrace Core Gameplay smoke test passed.\n" +
                    "Full 10-Case Shift: passed\n" +
                    "Initial/Gene/Galaxy/AI evidence boundaries: passed\n" +
                    "Diagnosis/Result/Next/Shift Summary: passed\n" +
                    "Mid-Shift save recovery: passed\n" +
                    "Zero-RP free diagnosis submission: passed\n" +
                    "ANOMALY pre-submit leak guard: passed\n" +
                    "ANOMALY post-submit reveal: passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("CancerTrace Core Gameplay smoke test failed.\n" + exception);
                if (throwOnFailure) throw;
            }
            finally
            {
                DeleteSmokeSave(data, mainSavePath);
                DeleteSmokeSave(data, anomalySavePath);
                DeleteSmokeSave(data, zeroRpSavePath);
            }
        }

        private static void RunMainFlow(GameDataContext data, string savePath)
        {
            SaveDataRepository saves = CreateSaveRepository(data, savePath);
            Require(saves.DeleteSave().Success, "Could not clear the main gameplay smoke save.");
            GameRuntimeService runtime = new GameRuntimeService(
                data.Cases,
                data.Config,
                saves,
                new System.Random(20261003));
            GameplayService gameplay = CreateGameplayService(data, runtime);

            Require(gameplay.GetDiagnosisOptions().Count == 8, "Diagnosis options did not contain 8 classes.");
            Require(!gameplay.GetResult().Success, "Result was available before a Shift/diagnosis existed.");

            GameplayActionResult<PlayerCaseView> started = gameplay.StartNewShift();
            RequireSuccess(started, "StartNewShift");
            Require(runtime.CurrentState.SelectedCaseIds.Count == data.Config.CasesPerShift,
                "New Shift did not contain the configured Case count.");
            Require(new HashSet<string>(runtime.CurrentState.SelectedCaseIds, StringComparer.Ordinal).Count ==
                    data.Config.CasesPerShift,
                "New Shift contained duplicate caseId values.");
            Require(started.Data.RemainingRp == data.Config.StartingRp, "New Shift RP was incorrect.");
            Require(started.Data.CurrentScore == 0, "New Shift Score was not zero.");
            Require(started.Data.InitialClues.Count == data.Config.InitialClueCount,
                "PlayerCaseView did not expose exactly the 2 Initial Clues.");
            RequireNoProperty(typeof(PlayerCaseView),
                "TrueDiagnosis", "TrueClassId", "Difficulty", "IsAnomaly", "AiCorrect", "GeneScanClues");

            GameplayActionResult<PlayerCaseView> prematureNext = gameplay.NextCase();
            Require(!prematureNext.Success && prematureNext.Status == GameplayActionStatus.InvalidState,
                "NextCase was accepted before diagnosis submission.");

            GameplayActionResult<EvidenceBoardView> initialEvidence = gameplay.GetEvidenceBoard();
            RequireSuccess(initialEvidence, "Initial Evidence Board");
            Require(initialEvidence.Data.InitialClues.Count == data.Config.InitialClueCount,
                "Evidence Board Initial Clues count was incorrect.");
            Require(initialEvidence.Data.GeneScanClues.Count == 0,
                "Evidence Board exposed Gene Scan clues before purchase.");
            Require(initialEvidence.Data.GalaxyEvidence == null,
                "Evidence Board exposed Galaxy evidence before purchase.");
            Require(initialEvidence.Data.AiEvidence == null,
                "Evidence Board exposed AI evidence before purchase.");

            GameplayActionResult<GeneScanView> gene = gameplay.UseGeneScan();
            RequireSuccess(gene, "UseGeneScan");
            Require(gene.Data.Clues.Count == data.Config.GeneScanClueCount,
                "Gene Scan did not return exactly 3 stored clues.");
            Require(gene.Data.RemainingRp == data.Config.StartingRp - data.Config.GeneScanCost,
                "Gene Scan RP deduction was incorrect.");
            Require(gameplay.UseGeneScan().Status == GameplayActionStatus.AlreadyUsed,
                "Second Gene Scan was not rejected as AlreadyUsed.");

            GameplayActionResult<CancerGalaxyView> galaxy = gameplay.UseCancerGalaxy();
            RequireSuccess(galaxy, "UseCancerGalaxy");
            Require(galaxy.Data.NearbyReferences.Count == 5,
                "Cancer Galaxy did not return 5 stored nearbyReferences.");
            Require(galaxy.Data.ReferenceNodes.Count == 645,
                "Cancer Galaxy did not return the 645 Reference Nodes.");
            Require(galaxy.Data.RemainingRp ==
                    data.Config.StartingRp - data.Config.GeneScanCost - data.Config.CancerGalaxyCost,
                "Cancer Galaxy RP deduction was incorrect.");
            Require(gameplay.UseCancerGalaxy().Status == GameplayActionStatus.AlreadyUsed,
                "Second Cancer Galaxy use was not rejected as AlreadyUsed.");
            RequireNoProperty(typeof(CancerGalaxyView),
                "TrueDiagnosis", "TrueClassId", "Difficulty", "IsAnomaly", "RecommendedDiagnosis");

            GameplayActionResult<AiAssistantView> ai = gameplay.UseAiAssistant();
            RequireSuccess(ai, "UseAiAssistant");
            Require(ai.Data.TopCandidates.Count == 3, "AI Assistant did not return Top 3 Candidates.");
            Require(ai.Data.RemainingRp ==
                    data.Config.StartingRp - data.Config.GeneScanCost -
                    data.Config.CancerGalaxyCost - data.Config.AiAssistantCost,
                "AI Assistant RP deduction was incorrect.");
            Require(gameplay.UseAiAssistant().Status == GameplayActionStatus.AlreadyUsed,
                "Second AI Assistant use was not rejected as AlreadyUsed.");
            RequireNoProperty(typeof(AiAssistantView),
                "Correct", "AiCorrect", "TrueDiagnosis", "TrueClassId", "IsAnomaly", "Explanation");

            GameplayActionResult<EvidenceBoardView> fullEvidence = gameplay.GetEvidenceBoard();
            RequireSuccess(fullEvidence, "Unlocked Evidence Board");
            Require(fullEvidence.Data.GeneScanClues.Count == 3,
                "Evidence Board did not include unlocked Gene Scan clues.");
            Require(fullEvidence.Data.GalaxyEvidence != null,
                "Evidence Board did not include unlocked Galaxy evidence.");
            Require(fullEvidence.Data.AiEvidence != null,
                "Evidence Board did not include unlocked AI evidence.");

            string[] selectedBeforeRestore = Copy(runtime.CurrentState.SelectedCaseIds);
            GameRuntimeService restoredRuntime = new GameRuntimeService(
                data.Cases,
                data.Config,
                saves,
                new System.Random(1));
            GameplayService restoredGameplay = CreateGameplayService(data, restoredRuntime);
            GameplayActionResult<PlayerCaseView> restored = restoredGameplay.RestoreSavedShift();
            RequireSuccess(restored, "RestoreSavedShift");
            Require(SequenceEqual(selectedBeforeRestore, restoredRuntime.CurrentState.SelectedCaseIds),
                "Save recovery changed selectedCaseIds.");
            Require(restored.Data.CaseId == started.Data.CaseId,
                "Save recovery changed the current Case.");
            Require(restored.Data.RemainingRp == ai.Data.RemainingRp,
                "Save recovery changed remainingRP.");
            Require(restored.Data.GeneScan.Used && restored.Data.CancerGalaxy.Used && restored.Data.AiAssistant.Used,
                "Save recovery lost tool state.");
            GameplayActionResult<EvidenceBoardView> restoredEvidence = restoredGameplay.GetEvidenceBoard();
            Require(restoredEvidence.Data.GeneScanClues.Count == 3 &&
                    restoredEvidence.Data.GalaxyEvidence != null &&
                    restoredEvidence.Data.AiEvidence != null,
                "Save recovery did not restore Evidence Board unlocks.");

            Require(restoredGameplay.SelectDiagnosis("not_a_cancer_type").Status ==
                    GameplayActionStatus.InvalidDiagnosis,
                "Invalid diagnosis selection was accepted.");
            GameCaseData firstCase = data.Cases.GetByCaseId(restored.Data.CaseId);
            RequireSuccess(restoredGameplay.SelectDiagnosis(firstCase.TrueClassId), "SelectDiagnosis");
            GameplayActionResult<ResultView> firstResult =
                restoredGameplay.SubmitDiagnosis(firstCase.TrueClassId);
            RequireSuccess(firstResult, "SubmitDiagnosis correct");
            Require(firstResult.Data.IsCorrect &&
                    firstResult.Data.ScoreEarned == data.Config.CorrectDiagnosisScore &&
                    firstResult.Data.CurrentScore == data.Config.CorrectDiagnosisScore,
                "Correct diagnosis did not award configured Score.");
            Require(firstResult.Data.AiReview != null && firstResult.Data.AiReview.TopCandidates.Count == 3,
                "Result did not include complete AI review.");
            Require(restoredGameplay.SubmitDiagnosis(firstCase.TrueClassId).Status ==
                    GameplayActionStatus.AlreadySubmitted,
                "Second diagnosis submission was not rejected.");

            int sharedRp = firstResult.Data.RemainingRp;
            GameplayActionResult<PlayerCaseView> secondCaseView = restoredGameplay.NextCase();
            RequireSuccess(secondCaseView, "NextCase after first result");
            Require(secondCaseView.Data.RemainingRp == sharedRp,
                "RP was not preserved across Cases.");
            Require(secondCaseView.Data.CurrentScore == data.Config.CorrectDiagnosisScore,
                "Score was not preserved across Cases.");
            Require(!secondCaseView.Data.GeneScan.Used &&
                    !secondCaseView.Data.CancerGalaxy.Used &&
                    !secondCaseView.Data.AiAssistant.Used,
                "New Case tool state was not reset.");
            Require(secondCaseView.Data.InitialClues.Count == 2,
                "New Case did not expose exactly 2 Initial Clues.");

            GameCaseData secondStatic = data.Cases.GetByCaseId(secondCaseView.Data.CaseId);
            string wrongDiagnosis = FindDifferentDiagnosis(
                restoredGameplay.GetDiagnosisOptions(),
                secondStatic.TrueClassId);
            GameplayActionResult<ResultView> wrongResult = restoredGameplay.SubmitDiagnosis(wrongDiagnosis);
            RequireSuccess(wrongResult, "SubmitDiagnosis wrong");
            Require(!wrongResult.Data.IsCorrect &&
                    wrongResult.Data.ScoreEarned == data.Config.WrongDiagnosisScore &&
                    wrongResult.Data.CurrentScore == data.Config.CorrectDiagnosisScore,
                "Wrong diagnosis did not use configured zero Score.");
            Require(wrongResult.Data.AiReview != null && wrongResult.Data.AiReview.TopCandidates.Count == 3,
                "Result did not provide AI review when AI Assistant was not purchased.");

            for (int completed = 2; completed < data.Config.CasesPerShift; completed++)
            {
                GameplayActionResult<PlayerCaseView> next = restoredGameplay.NextCase();
                RequireSuccess(next, "NextCase while completing Shift");
                GameCaseData current = data.Cases.GetByCaseId(next.Data.CaseId);
                GameplayActionResult<ResultView> result = restoredGameplay.SubmitDiagnosis(current.TrueClassId);
                RequireSuccess(result, "SubmitDiagnosis while completing Shift");
            }

            Require(restoredRuntime.CurrentState.IsShiftComplete, "Shift was not complete after 10 Cases.");
            Require(restoredGameplay.NextCase().Status == GameplayActionStatus.ShiftCompleted,
                "NextCase was not rejected with ShiftCompleted after Case 10.");
            GameplayActionResult<ShiftSummaryView> summary = restoredGameplay.GetShiftSummary();
            RequireSuccess(summary, "GetShiftSummary");
            Require(summary.Data.CasesCompleted == 10, "Shift Summary completed count was incorrect.");
            Require(summary.Data.CorrectCases == 9, "Shift Summary correct count was incorrect.");
            Require(Math.Abs(summary.Data.Accuracy - 0.9) < 0.000001,
                "Shift Summary accuracy was incorrect.");
            Require(summary.Data.Score == 9 * data.Config.CorrectDiagnosisScore,
                "Shift Summary Score was incorrect.");
            Require(summary.Data.RemainingRp == sharedRp, "Shift Summary remaining RP was incorrect.");
            Require(summary.Data.GeneScanUses == 1 &&
                    summary.Data.CancerGalaxyUses == 1 &&
                    summary.Data.AiAssistantUses == 1,
                "Shift Summary tool counts were incorrect.");

            GameplayActionResult<PlayerCaseView> nextShift = restoredGameplay.StartNextShift();
            RequireSuccess(nextShift, "StartNextShift");
            Require(restoredRuntime.CurrentState.CurrentShift == 2,
                "Next Shift number was not incremented.");
            Require(nextShift.Data.RemainingRp == data.Config.StartingRp && nextShift.Data.CurrentScore == 0,
                "Next Shift did not reset RP and Score.");
            Require(restoredRuntime.CurrentState.CompletedCaseCount == 0,
                "Next Shift did not clear completed Case statistics.");
        }

        private static void RunAnomalyGuard(GameDataContext data, string savePath)
        {
            IReadOnlyList<GameCaseData> allCases = data.Cases.GetAll();
            int anomalyIndex = -1;
            for (int index = 0; index < allCases.Count; index++)
            {
                if (string.Equals(allCases[index].Difficulty, "ANOMALY", StringComparison.Ordinal))
                {
                    anomalyIndex = index;
                    break;
                }
            }
            Require(anomalyIndex >= 0, "No ANOMALY Case exists for the leak-guard test.");

            SaveDataRepository saves = CreateSaveRepository(data, savePath);
            Require(saves.DeleteSave().Success, "Could not clear the ANOMALY smoke save.");
            GameRuntimeService runtime = new GameRuntimeService(
                data.Cases,
                data.Config,
                saves,
                new FirstPickRandom(anomalyIndex));
            GameplayService gameplay = CreateGameplayService(data, runtime);

            GameplayActionResult<PlayerCaseView> started = gameplay.StartNewShift();
            RequireSuccess(started, "Start ANOMALY guard Shift");
            Require(started.Data.CaseId == allCases[anomalyIndex].CaseId,
                "ANOMALY guard test did not select the intended Case.");
            Require(!ContainsAnomalyText(started.Data.SelectedDiagnosis),
                "PlayerCaseView leaked an ANOMALY marker.");

            GameplayActionResult<EvidenceBoardView> beforeTools = gameplay.GetEvidenceBoard();
            RequireSuccess(beforeTools, "ANOMALY initial Evidence Board");
            Require(beforeTools.Data.GeneScanClues.Count == 0 &&
                    beforeTools.Data.GalaxyEvidence == null &&
                    beforeTools.Data.AiEvidence == null,
                "ANOMALY Evidence Board exposed locked data.");

            GameplayActionResult<GeneScanView> gene = gameplay.UseGeneScan();
            GameplayActionResult<CancerGalaxyView> galaxy = gameplay.UseCancerGalaxy();
            GameplayActionResult<AiAssistantView> ai = gameplay.UseAiAssistant();
            RequireSuccess(gene, "ANOMALY Gene Scan");
            RequireSuccess(galaxy, "ANOMALY Cancer Galaxy");
            RequireSuccess(ai, "ANOMALY AI Assistant");
            RequireNoProperty(typeof(GeneScanView),
                "TrueDiagnosis", "TrueClassId", "Difficulty", "IsAnomaly", "AiCorrect");
            RequireNoProperty(typeof(CancerGalaxyView),
                "TrueDiagnosis", "TrueClassId", "Difficulty", "IsAnomaly", "AiCorrect");
            RequireNoProperty(typeof(AiAssistantView),
                "Correct", "AiCorrect", "TrueDiagnosis", "TrueClassId", "Difficulty", "IsAnomaly");
            RequireNoProperty(typeof(EvidenceBoardView),
                "TrueDiagnosis", "TrueClassId", "Difficulty", "IsAnomaly", "AiCorrect", "Result");

            GameplayActionResult<ResultView> beforeSubmit = gameplay.GetResult();
            Require(!beforeSubmit.Success && beforeSubmit.Data == null,
                "Result/ANOMALY data was available before submission.");
            Require(!ContainsAnomalyText(beforeSubmit.Message),
                "Pre-submit failure message leaked the ANOMALY marker.");

            GameCaseData anomaly = allCases[anomalyIndex];
            GameplayActionResult<ResultView> result = gameplay.SubmitDiagnosis(anomaly.TrueClassId);
            RequireSuccess(result, "Submit ANOMALY diagnosis");
            Require(result.Data.IsAnomaly, "Result did not reveal ANOMALY after submission.");
            Require(string.Equals(
                    result.Data.AnomalyDisplayLabel,
                    "异常案件 / Anomaly Case",
                    StringComparison.Ordinal),
                "Result ANOMALY display label was incorrect.");
            Require(result.Data.TrueDiagnosis == anomaly.TrueClassId,
                "Result did not reveal the true diagnosis after submission.");
            Require(result.Data.AiReview != null && result.Data.AiReview.TopCandidates.Count == 3,
                "ANOMALY Result did not contain AI review.");
        }

        private static void RunZeroRpSubmit(GameDataContext data, string savePath)
        {
            SaveDataRepository saves = CreateSaveRepository(data, savePath);
            Require(saves.DeleteSave().Success, "Could not clear the zero-RP smoke save.");
            GameRuntimeService runtime = new GameRuntimeService(
                data.Cases,
                data.Config,
                saves,
                new System.Random(40));
            GameplayService gameplay = CreateGameplayService(data, runtime);
            RequireSuccess(gameplay.StartNewShift(), "Start zero-RP Shift");

            for (int useIndex = 0; useIndex < 5; useIndex++)
            {
                RequireSuccess(gameplay.UseAiAssistant(), "Spend AI Assistant RP for zero-RP test");
                if (useIndex < 4)
                {
                    GameCaseData current = data.Cases.GetByCaseId(
                        runtime.CurrentState.CurrentCaseState.CaseId);
                    RequireSuccess(
                        gameplay.SubmitDiagnosis(current.TrueClassId),
                        "Submit while preparing zero-RP test");
                    RequireSuccess(gameplay.NextCase(), "Advance while preparing zero-RP test");
                }
            }

            Require(runtime.CurrentState.RemainingRp == data.Config.MinimumRp,
                "Zero-RP test did not reach the configured minimum RP.");
            Require(gameplay.UseGeneScan().Status == GameplayActionStatus.InsufficientRp,
                "Tool use at zero RP was not rejected as InsufficientRp.");
            GameCaseData zeroRpCase = data.Cases.GetByCaseId(
                runtime.CurrentState.CurrentCaseState.CaseId);
            GameplayActionResult<ResultView> zeroRpResult =
                gameplay.SubmitDiagnosis(zeroRpCase.TrueClassId);
            RequireSuccess(zeroRpResult, "SubmitDiagnosis at zero RP");
            Require(zeroRpResult.Data.RemainingRp == data.Config.MinimumRp,
                "Free diagnosis submission changed zero RP.");
        }

        private static GameplayService CreateGameplayService(
            GameDataContext data,
            GameRuntimeService runtime)
        {
            return new GameplayService(
                runtime,
                data.Cases,
                data.Galaxy,
                data.ClassProfiles,
                data.Config);
        }

        private static SaveDataRepository CreateSaveRepository(GameDataContext data, string path)
        {
            return new SaveDataRepository(data.Cases, data.Config, path);
        }

        private static void DeleteSmokeSave(GameDataContext data, string path)
        {
            CreateSaveRepository(data, path).DeleteSave();
        }

        private static string FindDifferentDiagnosis(
            IReadOnlyList<CancerTypeOptionView> options,
            string trueClassId)
        {
            for (int index = 0; index < options.Count; index++)
            {
                if (!string.Equals(options[index].ClassId, trueClassId, StringComparison.Ordinal))
                {
                    return options[index].ClassId;
                }
            }
            throw new InvalidOperationException("Could not find a diagnosis different from the true class.");
        }

        private static void RequireNoProperty(Type type, params string[] forbiddenPropertyNames)
        {
            for (int index = 0; index < forbiddenPropertyNames.Length; index++)
            {
                PropertyInfo property = type.GetProperty(
                    forbiddenPropertyNames[index],
                    BindingFlags.Instance | BindingFlags.Public);
                Require(property == null,
                    type.Name + " exposes forbidden property " + forbiddenPropertyNames[index] + ".");
            }
        }

        private static bool ContainsAnomalyText(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return value.IndexOf("ANOMALY", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("异常案件", StringComparison.Ordinal) >= 0;
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

        private static void RequireSuccess<T>(GameplayActionResult<T> result, string operation)
        {
            Require(result != null && result.Success,
                operation + " failed: " + (result == null ? "null result" : result.Message));
        }

        private static void Drain(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                IEnumerator nested = enumerator.Current as IEnumerator;
                if (nested != null) Drain(nested);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class FirstPickRandom : System.Random
        {
            private readonly int firstPick;
            private bool isFirst = true;

            public FirstPickRandom(int firstPick)
            {
                this.firstPick = firstPick;
            }

            public override int Next(int minValue, int maxValue)
            {
                if (isFirst)
                {
                    isFirst = false;
                    if (firstPick < minValue || firstPick >= maxValue)
                    {
                        throw new ArgumentOutOfRangeException("firstPick");
                    }
                    return firstPick;
                }
                return minValue;
            }
        }
    }
}
