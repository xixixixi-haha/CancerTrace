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
using CancerTrace.UI.Controllers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.EditorTools
{
    public static class PhaseBIntegrationSmokeTest
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/CaseAnalysis.unity",
            "Assets/Scenes/CancerGalaxy.unity",
            "Assets/Scenes/ResultSummary.unity"
        };

        [MenuItem("CancerTrace/Phase B/Run Integration Smoke Test")]
        public static void RunBatchSmokeTest()
        {
            PhaseBSceneBuilder.BuildFormalScenes();
            ValidateScenesAndBuildSettings();
            RunTwoCaseFlow();
            Debug.Log("PHASE_B_UI_SMOKE_TEST_PASSED: formal scenes, TMP font, no missing scripts, " +
                      "Case 1 tools/evidence/result, save restore, and Case 2 reset all verified.");
        }

        private static void ValidateScenesAndBuildSettings()
        {
            Require(EditorBuildSettings.scenes.Length == ScenePaths.Length,
                "Build Settings scene count is not 4.");
            for (int index = 0; index < ScenePaths.Length; index++)
            {
                EditorBuildSettingsScene buildScene = EditorBuildSettings.scenes[index];
                Require(buildScene.enabled, "Build scene is disabled: " + buildScene.path);
                Require(string.Equals(buildScene.path, ScenePaths[index], StringComparison.Ordinal),
                    "Build scene order is incorrect at index " + index + ".");
                Require(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ScenePaths[index])),
                    "Scene file is missing: " + ScenePaths[index]);
            }

            TMP_FontAsset formalFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset");
            Require(formalFont != null, "Formal Chinese TMP font is missing.");

            Type[] expectedControllers =
            {
                typeof(MainMenuController),
                typeof(CaseAnalysisController),
                typeof(CancerGalaxyController),
                typeof(ResultSummaryController)
            };
            for (int sceneIndex = 0; sceneIndex < ScenePaths.Length; sceneIndex++)
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePaths[sceneIndex], OpenSceneMode.Single);
                Require(scene.IsValid(), "Could not open scene: " + ScenePaths[sceneIndex]);
                Require(UnityEngine.Object.FindObjectOfType(expectedControllers[sceneIndex]) != null,
                    "Expected controller is missing from " + ScenePaths[sceneIndex]);
                Require(UnityEngine.Object.FindObjectsOfType<Text>(true).Length == 0,
                    "Legacy UGUI Text exists in " + ScenePaths[sceneIndex]);

                TMP_Text[] texts = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
                Require(texts.Length > 0, "No TMP text exists in " + ScenePaths[sceneIndex]);
                for (int textIndex = 0; textIndex < texts.Length; textIndex++)
                {
                    Require(texts[textIndex].font == formalFont,
                        "TMP text does not use the formal Chinese font: " + texts[textIndex].name);
                }
                RequireNoMissingScripts(scene);
            }
        }

        private static void RequireNoMissingScripts(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    Component[] components = transforms[transformIndex].GetComponents<Component>();
                    for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                    {
                        Require(components[componentIndex] != null,
                            "Missing Script found on " + transforms[transformIndex].name +
                            " in " + scene.path);
                    }
                }
            }
        }

        private static void RunTwoCaseFlow()
        {
            GameDataLoadResult load = null;
            GameDataLoader loader = new GameDataLoader();
            Drain(loader.Load(value => load = value));
            Require(load != null && load.Success, "Formal GameData failed to load.");

            string savePath = Path.Combine(
                Application.temporaryCachePath,
                "cancertrace_phase_b_ui_smoke_save.json");
            SaveDataRepository saves = new SaveDataRepository(load.Data.Cases, load.Data.Config, savePath);
            saves.DeleteSave();

            try
            {
                GameRuntimeService runtime = new GameRuntimeService(
                    load.Data.Cases,
                    load.Data.Config,
                    saves,
                    new System.Random(20261005));
                GameplayService gameplay = CreateGameplay(load, runtime);

                GameplayActionResult<PlayerCaseView> first = gameplay.StartNewShift();
                RequireSuccess(first, "StartNewShift");
                Require(first.Data.ShiftNumber == 1, "Shift number is not 1.");
                Require(first.Data.CaseNumber == 1 && first.Data.CasesPerShift == 10,
                    "Case indicator is not 1 / 10.");
                Require(first.Data.RemainingRp == 200, "Starting RP is not 200.");
                Require(first.Data.InitialClues.Count == 2, "Initial Clues count is not 2.");
                Require(first.Data.GeneScan.Cost == 10 && first.Data.CancerGalaxy.Cost == 25 &&
                        first.Data.AiAssistant.Cost == 40,
                    "Tool costs do not match formal config.");
                RequireNoProperty(typeof(PlayerCaseView), "TrueDiagnosis", "Difficulty", "IsAnomaly");
                Require(!gameplay.GetResult().Success, "Result was available before submit.");

                string firstCaseId = first.Data.CaseId;
                string[] selectedCaseIds = Copy(runtime.CurrentState.SelectedCaseIds);

                GameplayActionResult<GeneScanView> gene = gameplay.UseGeneScan();
                RequireSuccess(gene, "UseGeneScan");
                Require(gene.Data.Clues.Count == 3 && gene.Data.RemainingRp == 190,
                    "Gene Scan output or RP deduction is incorrect.");
                GameplayActionResult<CancerGalaxyView> galaxy = gameplay.UseCancerGalaxy();
                RequireSuccess(galaxy, "UseCancerGalaxy");
                Require(galaxy.Data.NearbyReferences.Count == 5 &&
                        galaxy.Data.ReferenceNodes.Count == 645 &&
                        galaxy.Data.RemainingRp == 165,
                    "Cancer Galaxy output or RP deduction is incorrect.");

                GameplayActionResult<EvidenceBoardView> afterGalaxy = gameplay.GetEvidenceBoard();
                RequireSuccess(afterGalaxy, "Evidence after Galaxy");
                Require(afterGalaxy.Data.GalaxyEvidence != null,
                    "Galaxy evidence was not retained for the Back flow.");

                GameplayActionResult<AiAssistantView> ai = gameplay.UseAiAssistant();
                RequireSuccess(ai, "UseAiAssistant");
                Require(ai.Data.TopCandidates.Count == 3 && ai.Data.RemainingRp == 125,
                    "AI output or RP deduction is incorrect.");
                RequireNoProperty(typeof(AiAssistantView),
                    "Correct", "AiCorrect", "TrueDiagnosis", "IsAnomaly", "Explanation");

                GameplayActionResult<EvidenceBoardView> evidence = gameplay.GetEvidenceBoard();
                RequireSuccess(evidence, "Full Evidence Board");
                Require(evidence.Data.InitialClues.Count == 2 &&
                        evidence.Data.GeneScanClues.Count == 3 &&
                        evidence.Data.GalaxyEvidence != null &&
                        evidence.Data.AiEvidence != null,
                    "Evidence Board did not refresh all unlocked evidence.");

                GameRuntimeService restoredRuntime = new GameRuntimeService(
                    load.Data.Cases,
                    load.Data.Config,
                    saves,
                    new System.Random(1));
                GameplayService restoredGameplay = CreateGameplay(load, restoredRuntime);
                GameplayActionResult<PlayerCaseView> restored = restoredGameplay.RestoreSavedShift();
                RequireSuccess(restored, "RestoreSavedShift");
                Require(restored.Data.CaseId == firstCaseId && restored.Data.RemainingRp == 125,
                    "Save restore changed the active Case or RP.");
                Require(SequenceEqual(selectedCaseIds, restoredRuntime.CurrentState.SelectedCaseIds),
                    "Save restore re-randomized selectedCaseIds.");
                Require(restored.Data.GeneScan.Used && restored.Data.CancerGalaxy.Used &&
                        restored.Data.AiAssistant.Used,
                    "Save restore lost tool state.");

                GameCaseData firstStatic = load.Data.Cases.GetByCaseId(firstCaseId);
                RequireSuccess(restoredGameplay.SelectDiagnosis(firstStatic.TrueClassId),
                    "Select first diagnosis");
                GameplayActionResult<ResultView> firstResult =
                    restoredGameplay.SubmitDiagnosis(firstStatic.TrueClassId);
                RequireSuccess(firstResult, "Submit first diagnosis");
                Require(firstResult.Data.IsCorrect && firstResult.Data.RemainingRp == 125,
                    "First Result is incorrect.");
                Require(firstResult.Data.AiReview != null &&
                        firstResult.Data.AiReview.TopCandidates.Count == 3,
                    "First Result is missing AI review.");

                GameplayActionResult<PlayerCaseView> second = restoredGameplay.NextCase();
                RequireSuccess(second, "NextCase");
                Require(second.Data.CaseNumber == 2 && second.Data.CaseId != firstCaseId,
                    "Case 2 was not entered with a new Case ID.");
                Require(second.Data.RemainingRp == 125 &&
                        second.Data.CurrentScore == firstResult.Data.CurrentScore,
                    "RP or Score did not carry into Case 2.");
                Require(!second.Data.GeneScan.Used && !second.Data.CancerGalaxy.Used &&
                        !second.Data.AiAssistant.Used,
                    "Case 2 tool state was not reset.");
                Require(second.Data.InitialClues.Count == 2,
                    "Case 2 Initial Clues count is not 2.");
                Require(SequenceEqual(selectedCaseIds, restoredRuntime.CurrentState.SelectedCaseIds),
                    "Next Case changed selectedCaseIds.");

                string secondDiagnosis = restoredGameplay.GetDiagnosisOptions()[0].ClassId;
                RequireSuccess(restoredGameplay.SelectDiagnosis(secondDiagnosis),
                    "Select second diagnosis without tools");
                GameplayActionResult<ResultView> secondResult =
                    restoredGameplay.SubmitDiagnosis(secondDiagnosis);
                RequireSuccess(secondResult, "Submit second diagnosis without tools");
                Require(secondResult.Data.AiReview != null &&
                        secondResult.Data.AiReview.TopCandidates.Count == 3,
                    "Case 2 Result is missing AI review when AI was not purchased.");
                Require(secondResult.Data.RemainingRp == 125,
                    "Free Case 2 submit changed RP.");
                Require(saves.HasSave(), "Autosave file was not maintained.");
            }
            finally
            {
                saves.DeleteSave();
            }
        }

        private static GameplayService CreateGameplay(
            GameDataLoadResult load,
            GameRuntimeService runtime)
        {
            return new GameplayService(
                runtime,
                load.Data.Cases,
                load.Data.Galaxy,
                load.Data.ClassProfiles,
                load.Data.Config);
        }

        private static void RequireNoProperty(Type type, params string[] names)
        {
            for (int index = 0; index < names.Length; index++)
            {
                Require(type.GetProperty(names[index], BindingFlags.Public | BindingFlags.Instance) == null,
                    type.Name + " exposes forbidden property " + names[index] + ".");
            }
        }

        private static string[] Copy(IReadOnlyList<string> values)
        {
            string[] result = new string[values.Count];
            for (int index = 0; index < values.Count; index++) result[index] = values[index];
            return result;
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

        private static void Drain(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                IEnumerator nested = enumerator.Current as IEnumerator;
                if (nested != null) Drain(nested);
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
    }
}
