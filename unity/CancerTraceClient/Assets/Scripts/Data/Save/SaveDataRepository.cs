using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CancerTrace.Data.Models;
using CancerTrace.Data.Repositories;
using CancerTrace.Gameplay.Runtime;
using Newtonsoft.Json;
using UnityEngine;

namespace CancerTrace.Data.Save
{
    public sealed class SaveDataRepository
    {
        public const string SaveVersion = "1.0";
        public const string DefaultFileName = "cancertrace_save_v1.json";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Include,
            FloatParseHandling = FloatParseHandling.Double,
            DateParseHandling = DateParseHandling.None
        };

        private readonly CaseRepository cases;
        private readonly GameConfigRepository config;

        public SaveDataRepository(
            CaseRepository cases,
            GameConfigRepository config,
            string savePathOverride = null)
        {
            if (cases == null) throw new ArgumentNullException("cases");
            if (config == null) throw new ArgumentNullException("config");

            this.cases = cases;
            this.config = config;
            SavePath = string.IsNullOrWhiteSpace(savePathOverride)
                ? Path.Combine(Application.persistentDataPath, DefaultFileName)
                : savePathOverride;
        }

        public string SavePath { get; private set; }

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public SaveOperationResult Save(GameRuntimeState state)
        {
            if (state == null)
            {
                return FailOperation("Cannot save a null Runtime State.");
            }

            SaveDataV1 saveData = ToSaveData(state);
            SaveValidationResult validation = ValidateSave(saveData);
            if (!validation.IsValid)
            {
                return FailOperation(BuildValidationMessage("Save rejected", validation));
            }

            try
            {
                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented, SerializerSettings);
                WriteAtomically(json);
                return SaveOperationResult.Succeeded();
            }
            catch (Exception exception)
            {
                return FailOperation("Save failed at " + SavePath + Environment.NewLine + exception);
            }
        }

        public SaveLoadResult Load()
        {
            if (!HasSave())
            {
                return FailLoad("Save file does not exist: " + SavePath);
            }

            try
            {
                string json = File.ReadAllText(SavePath, Encoding.UTF8);
                SaveDataV1 saveData = JsonConvert.DeserializeObject<SaveDataV1>(json, SerializerSettings);
                if (saveData == null)
                {
                    return FailLoad("Save JSON produced a null root object: " + SavePath);
                }

                SaveValidationResult validation = ValidateSave(saveData);
                if (!validation.IsValid)
                {
                    return FailLoad(BuildValidationMessage("Load rejected", validation));
                }

                return SaveLoadResult.Succeeded(ToRuntimeState(saveData));
            }
            catch (JsonException exception)
            {
                return FailLoad("Save JSON parse/mapping failed: " + SavePath + Environment.NewLine + exception);
            }
            catch (Exception exception)
            {
                return FailLoad("Save load failed: " + SavePath + Environment.NewLine + exception);
            }
        }

        public SaveOperationResult DeleteSave()
        {
            try
            {
                DeleteIfPresent(SavePath);
                DeleteIfPresent(SavePath + ".tmp");
                DeleteIfPresent(SavePath + ".bak");
                return SaveOperationResult.Succeeded();
            }
            catch (Exception exception)
            {
                return FailOperation("Save deletion failed: " + SavePath + Environment.NewLine + exception);
            }
        }

        public SaveValidationResult ValidateSave(SaveDataV1 saveData)
        {
            SaveValidationResult result = new SaveValidationResult();
            if (saveData == null)
            {
                result.Add("$", "non-null SaveData v1", null);
                return result;
            }

            RequireEqual(result, "saveVersion", SaveVersion, saveData.SaveVersion);
            if (saveData.CurrentShift <= 0)
            {
                result.Add("currentShift", "positive integer", saveData.CurrentShift);
            }

            ValidateSelectedCaseIds(saveData, result);
            ValidateCounters(saveData, result);
            ValidateCurrentCase(saveData, result);
            ValidateCompletedResults(saveData, result);
            ValidateAggregateRelationships(saveData, result);
            return result;
        }

        private void ValidateSelectedCaseIds(SaveDataV1 saveData, SaveValidationResult result)
        {
            if (saveData.SelectedCaseIds == null)
            {
                result.Add("selectedCaseIds", "array with " + config.CasesPerShift + " entries", null);
                return;
            }

            RequireEqual(result, "selectedCaseIds.length", config.CasesPerShift, saveData.SelectedCaseIds.Length);
            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < saveData.SelectedCaseIds.Length; index++)
            {
                string caseId = saveData.SelectedCaseIds[index];
                string path = "selectedCaseIds[" + index + "]";
                if (string.IsNullOrWhiteSpace(caseId))
                {
                    result.Add(path, "non-empty caseId", caseId);
                    continue;
                }
                if (!unique.Add(caseId))
                {
                    result.Add(path, "unique within current Shift", caseId);
                }
                GameCaseData unused;
                if (!cases.TryGetByCaseId(caseId, out unused))
                {
                    result.Add(path, "caseId present in CaseRepository", caseId);
                }
            }

            if (saveData.CurrentCaseIndex < 0 || saveData.CurrentCaseIndex >= saveData.SelectedCaseIds.Length)
            {
                result.Add(
                    "currentCaseIndex",
                    "index in selectedCaseIds",
                    saveData.CurrentCaseIndex);
            }
        }

        private void ValidateCounters(SaveDataV1 saveData, SaveValidationResult result)
        {
            if (saveData.RemainingRp < config.MinimumRp || saveData.RemainingRp > config.StartingRp)
            {
                result.Add("remainingRP", "integer in [" + config.MinimumRp + "," + config.StartingRp + "]", saveData.RemainingRp);
            }
            if (saveData.CurrentScore < 0)
            {
                result.Add("currentScore", "non-negative integer", saveData.CurrentScore);
            }
            if (saveData.CompletedCaseCount < 0 || saveData.CompletedCaseCount > config.CasesPerShift)
            {
                result.Add("completedCaseCount", "integer in [0," + config.CasesPerShift + "]", saveData.CompletedCaseCount);
            }
            if (saveData.CorrectCaseCount < 0 || saveData.CorrectCaseCount > saveData.CompletedCaseCount)
            {
                result.Add("correctCaseCount", "integer in [0,completedCaseCount]", saveData.CorrectCaseCount);
            }
        }

        private void ValidateCurrentCase(SaveDataV1 saveData, SaveValidationResult result)
        {
            CurrentCaseSaveData current = saveData.CurrentCaseState;
            if (current == null)
            {
                result.Add("currentCaseState", "non-null object", null);
                return;
            }

            if (saveData.SelectedCaseIds != null &&
                saveData.CurrentCaseIndex >= 0 &&
                saveData.CurrentCaseIndex < saveData.SelectedCaseIds.Length)
            {
                RequireEqual(
                    result,
                    "currentCaseState.caseId",
                    saveData.SelectedCaseIds[saveData.CurrentCaseIndex],
                    current.CaseId);
            }

            if (current.SelectedDiagnosis != null)
            {
                ValidateCancerType(result, "currentCaseState.selectedDiagnosis", current.SelectedDiagnosis);
            }
            if (current.DiagnosisSubmitted && current.SelectedDiagnosis == null)
            {
                result.Add("currentCaseState.selectedDiagnosis", "legal CancerType when diagnosisSubmitted is true", null);
            }
            if (current.RpSpent < 0)
            {
                result.Add("currentCaseState.rpSpent", "non-negative integer", current.RpSpent);
            }

            int expectedRpSpent = CalculateToolSpend(
                current.GeneScanUsed,
                current.CancerGalaxyUsed,
                current.AiAssistantUsed);
            RequireEqual(result, "currentCaseState.rpSpent", expectedRpSpent, current.RpSpent);

            if (!current.DiagnosisSubmitted && current.ScoreEarned != 0)
            {
                result.Add("currentCaseState.scoreEarned", 0, current.ScoreEarned);
            }
            if (current.ScoreEarned != 0 && current.ScoreEarned != config.CorrectDiagnosisScore)
            {
                result.Add(
                    "currentCaseState.scoreEarned",
                    "0 or " + config.CorrectDiagnosisScore,
                    current.ScoreEarned);
            }
        }

        private void ValidateCompletedResults(SaveDataV1 saveData, SaveValidationResult result)
        {
            if (saveData.CompletedCaseResults == null)
            {
                result.Add("completedCaseResults", "non-null array", null);
                return;
            }

            RequireEqual(
                result,
                "completedCaseResults.length",
                saveData.CompletedCaseCount,
                saveData.CompletedCaseResults.Length);

            for (int index = 0; index < saveData.CompletedCaseResults.Length; index++)
            {
                CompletedCaseResultSaveData completed = saveData.CompletedCaseResults[index];
                string path = "completedCaseResults[" + index + "]";
                if (completed == null)
                {
                    result.Add(path, "non-null result", null);
                    continue;
                }

                if (saveData.SelectedCaseIds != null && index < saveData.SelectedCaseIds.Length)
                {
                    RequireEqual(result, path + ".caseId", saveData.SelectedCaseIds[index], completed.CaseId);
                }
                ValidateCancerType(result, path + ".playerDiagnosis", completed.PlayerDiagnosis);
                ValidateCancerType(result, path + ".trueDiagnosis", completed.TrueDiagnosis);

                GameCaseData staticCase;
                if (cases.TryGetByCaseId(completed.CaseId, out staticCase))
                {
                    RequireEqual(result, path + ".trueDiagnosis", staticCase.TrueClassId, completed.TrueDiagnosis);
                }
                else
                {
                    result.Add(path + ".caseId", "caseId present in CaseRepository", completed.CaseId);
                }

                bool expectedCorrect = string.Equals(
                    completed.PlayerDiagnosis,
                    completed.TrueDiagnosis,
                    StringComparison.Ordinal);
                RequireEqual(result, path + ".isCorrect", expectedCorrect, completed.IsCorrect);
                int expectedScore = expectedCorrect
                    ? config.CorrectDiagnosisScore
                    : config.WrongDiagnosisScore;
                RequireEqual(result, path + ".scoreEarned", expectedScore, completed.ScoreEarned);
                int expectedSpend = CalculateToolSpend(
                    completed.GeneScanUsed,
                    completed.CancerGalaxyUsed,
                    completed.AiAssistantUsed);
                RequireEqual(result, path + ".rpSpent", expectedSpend, completed.RpSpent);
            }
        }

        private void ValidateAggregateRelationships(SaveDataV1 saveData, SaveValidationResult result)
        {
            if (saveData.CompletedCaseResults == null || saveData.CurrentCaseState == null)
            {
                return;
            }

            int correctCount = 0;
            int score = 0;
            int completedSpend = 0;
            foreach (CompletedCaseResultSaveData completed in saveData.CompletedCaseResults)
            {
                if (completed == null) continue;
                if (completed.IsCorrect) correctCount++;
                score += completed.ScoreEarned;
                completedSpend += completed.RpSpent;
            }
            RequireEqual(result, "correctCaseCount", correctCount, saveData.CorrectCaseCount);
            RequireEqual(result, "currentScore", score, saveData.CurrentScore);

            int expectedIndex;
            if (saveData.CurrentCaseState.DiagnosisSubmitted)
            {
                expectedIndex = saveData.CompletedCaseCount - 1;
            }
            else
            {
                expectedIndex = saveData.CompletedCaseCount;
            }
            RequireEqual(result, "currentCaseIndex", expectedIndex, saveData.CurrentCaseIndex);

            if (saveData.CurrentCaseState.DiagnosisSubmitted && saveData.CompletedCaseResults.Length > 0)
            {
                CompletedCaseResultSaveData latest =
                    saveData.CompletedCaseResults[saveData.CompletedCaseResults.Length - 1];
                if (latest != null)
                {
                    RequireEqual(result, "currentCaseState.caseId", latest.CaseId, saveData.CurrentCaseState.CaseId);
                    RequireEqual(
                        result,
                        "currentCaseState.selectedDiagnosis",
                        latest.PlayerDiagnosis,
                        saveData.CurrentCaseState.SelectedDiagnosis);
                    RequireEqual(result, "currentCaseState.scoreEarned", latest.ScoreEarned, saveData.CurrentCaseState.ScoreEarned);
                    RequireEqual(result, "currentCaseState.rpSpent", latest.RpSpent, saveData.CurrentCaseState.RpSpent);
                    RequireEqual(result, "currentCaseState.geneScanUsed", latest.GeneScanUsed, saveData.CurrentCaseState.GeneScanUsed);
                    RequireEqual(result, "currentCaseState.cancerGalaxyUsed", latest.CancerGalaxyUsed, saveData.CurrentCaseState.CancerGalaxyUsed);
                    RequireEqual(result, "currentCaseState.aiAssistantUsed", latest.AiAssistantUsed, saveData.CurrentCaseState.AiAssistantUsed);
                }
            }

            int activeCaseSpend = saveData.CurrentCaseState.DiagnosisSubmitted
                ? 0
                : saveData.CurrentCaseState.RpSpent;
            int expectedRemainingRp = config.StartingRp - completedSpend - activeCaseSpend;
            RequireEqual(result, "remainingRP", expectedRemainingRp, saveData.RemainingRp);
        }

        private int CalculateToolSpend(bool geneScanUsed, bool cancerGalaxyUsed, bool aiAssistantUsed)
        {
            int total = 0;
            if (geneScanUsed) total += config.GeneScanCost;
            if (cancerGalaxyUsed) total += config.CancerGalaxyCost;
            if (aiAssistantUsed) total += config.AiAssistantCost;
            return total;
        }

        private static SaveDataV1 ToSaveData(GameRuntimeState state)
        {
            SaveDataV1 saveData = new SaveDataV1
            {
                SaveVersion = SaveVersion,
                CurrentShift = state.CurrentShift,
                CurrentCaseIndex = state.CurrentCaseIndex,
                SelectedCaseIds = CopyStrings(state.SelectedCaseIds),
                RemainingRp = state.RemainingRp,
                CurrentScore = state.CurrentScore,
                CompletedCaseCount = state.CompletedCaseCount,
                CorrectCaseCount = state.CorrectCaseCount,
                CurrentCaseState = ToSaveData(state.CurrentCaseState),
                CompletedCaseResults = new CompletedCaseResultSaveData[state.CompletedCaseResults.Count],
                TutorialCompleted = state.TutorialCompleted
            };

            for (int index = 0; index < state.CompletedCaseResults.Count; index++)
            {
                CompletedCaseResult result = state.CompletedCaseResults[index];
                saveData.CompletedCaseResults[index] = new CompletedCaseResultSaveData
                {
                    CaseId = result.CaseId,
                    PlayerDiagnosis = result.PlayerDiagnosis,
                    TrueDiagnosis = result.TrueDiagnosis,
                    IsCorrect = result.IsCorrect,
                    ScoreEarned = result.ScoreEarned,
                    RpSpent = result.RpSpent,
                    GeneScanUsed = result.GeneScanUsed,
                    CancerGalaxyUsed = result.CancerGalaxyUsed,
                    AiAssistantUsed = result.AiAssistantUsed
                };
            }

            return saveData;
        }

        private static CurrentCaseSaveData ToSaveData(CurrentCaseRuntimeState current)
        {
            if (current == null) return null;
            return new CurrentCaseSaveData
            {
                CaseId = current.CaseId,
                GeneScanUsed = current.GeneScanUsed,
                CancerGalaxyUsed = current.CancerGalaxyUsed,
                AiAssistantUsed = current.AiAssistantUsed,
                SelectedDiagnosis = current.SelectedDiagnosis,
                DiagnosisSubmitted = current.DiagnosisSubmitted,
                RpSpent = current.RpSpent,
                ScoreEarned = current.ScoreEarned
            };
        }

        private static GameRuntimeState ToRuntimeState(SaveDataV1 saveData)
        {
            List<CompletedCaseResult> completed = new List<CompletedCaseResult>();
            foreach (CompletedCaseResultSaveData savedResult in saveData.CompletedCaseResults)
            {
                completed.Add(new CompletedCaseResult(
                    savedResult.CaseId,
                    savedResult.PlayerDiagnosis,
                    savedResult.TrueDiagnosis,
                    savedResult.IsCorrect,
                    savedResult.ScoreEarned,
                    savedResult.RpSpent,
                    savedResult.GeneScanUsed,
                    savedResult.CancerGalaxyUsed,
                    savedResult.AiAssistantUsed));
            }

            CurrentCaseSaveData savedCurrent = saveData.CurrentCaseState;
            CurrentCaseRuntimeState current = new CurrentCaseRuntimeState(
                savedCurrent.CaseId,
                savedCurrent.GeneScanUsed,
                savedCurrent.CancerGalaxyUsed,
                savedCurrent.AiAssistantUsed,
                savedCurrent.SelectedDiagnosis,
                savedCurrent.DiagnosisSubmitted,
                savedCurrent.RpSpent,
                savedCurrent.ScoreEarned);

            return new GameRuntimeState(
                saveData.CurrentShift,
                saveData.CurrentCaseIndex,
                saveData.SelectedCaseIds,
                saveData.RemainingRp,
                saveData.CurrentScore,
                saveData.CompletedCaseCount,
                saveData.CorrectCaseCount,
                current,
                completed,
                saveData.TutorialCompleted);
        }

        private void WriteAtomically(string json)
        {
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = SavePath + ".tmp";
            string backupPath = SavePath + ".bak";
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

            try
            {
                if (File.Exists(SavePath))
                {
                    try
                    {
                        DeleteIfPresent(backupPath);
                        File.Replace(temporaryPath, SavePath, backupPath, true);
                        DeleteIfPresent(backupPath);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(temporaryPath, SavePath, true);
                        File.Delete(temporaryPath);
                    }
                    catch (NotSupportedException)
                    {
                        File.Copy(temporaryPath, SavePath, true);
                        File.Delete(temporaryPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, SavePath);
                }
            }
            finally
            {
                DeleteIfPresent(temporaryPath);
            }
        }

        private static string[] CopyStrings(IReadOnlyList<string> values)
        {
            string[] copy = new string[values.Count];
            for (int index = 0; index < values.Count; index++) copy[index] = values[index];
            return copy;
        }

        private static void ValidateCancerType(SaveValidationResult result, string path, string value)
        {
            CancerType unused;
            if (!GameDataValueParser.TryParseCancerType(value, out unused))
            {
                result.Add(path, "one of the 8 frozen CancerType IDs", value);
            }
        }

        private static void RequireEqual(SaveValidationResult result, string path, int expected, int actual)
        {
            if (expected != actual) result.Add(path, expected, actual);
        }

        private static void RequireEqual(SaveValidationResult result, string path, bool expected, bool actual)
        {
            if (expected != actual) result.Add(path, expected, actual);
        }

        private static void RequireEqual(SaveValidationResult result, string path, string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                result.Add(path, expected, actual);
            }
        }

        private static string BuildValidationMessage(string prefix, SaveValidationResult validation)
        {
            StringBuilder builder = new StringBuilder(prefix);
            builder.Append(" with ");
            builder.Append(validation.Errors.Count);
            builder.Append(" error(s):");
            foreach (string error in validation.Errors)
            {
                builder.AppendLine();
                builder.Append("- ");
                builder.Append(error);
            }
            return builder.ToString();
        }

        private static SaveOperationResult FailOperation(string error)
        {
            Debug.LogError(error);
            return SaveOperationResult.Failed(error);
        }

        private static SaveLoadResult FailLoad(string error)
        {
            Debug.LogError(error);
            return SaveLoadResult.Failed(error);
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public sealed class SaveValidationResult
    {
        private readonly List<string> errors = new List<string>();
        private ReadOnlyCollection<string> readOnlyErrors;

        public bool IsValid { get { return errors.Count == 0; } }

        public IReadOnlyList<string> Errors
        {
            get
            {
                if (readOnlyErrors == null) readOnlyErrors = errors.AsReadOnly();
                return readOnlyErrors;
            }
        }

        internal void Add(string path, object expected, object actual)
        {
            errors.Add(
                path + " | expected: " + (expected == null ? "null" : expected.ToString()) +
                " | actual: " + (actual == null ? "null" : actual.ToString()));
        }
    }

    public sealed class SaveOperationResult
    {
        private SaveOperationResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }

        internal static SaveOperationResult Succeeded()
        {
            return new SaveOperationResult(true, null);
        }

        internal static SaveOperationResult Failed(string error)
        {
            return new SaveOperationResult(false, error);
        }
    }

    public sealed class SaveLoadResult
    {
        private SaveLoadResult(bool success, GameRuntimeState state, string errorMessage)
        {
            Success = success;
            State = state;
            ErrorMessage = errorMessage;
        }

        public bool Success { get; private set; }
        public GameRuntimeState State { get; private set; }
        public string ErrorMessage { get; private set; }

        internal static SaveLoadResult Succeeded(GameRuntimeState state)
        {
            return new SaveLoadResult(true, state, null);
        }

        internal static SaveLoadResult Failed(string error)
        {
            return new SaveLoadResult(false, null, error);
        }
    }
}
