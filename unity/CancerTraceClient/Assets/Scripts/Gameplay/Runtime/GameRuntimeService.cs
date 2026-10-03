using System;
using System.Collections.Generic;
using CancerTrace.Data.Models;
using CancerTrace.Data.Repositories;
using CancerTrace.Data.Save;
using UnityEngine;

namespace CancerTrace.Gameplay.Runtime
{
    public sealed class GameRuntimeService
    {
        private readonly CaseRepository cases;
        private readonly GameConfigRepository config;
        private readonly SaveDataRepository saves;
        private readonly System.Random random;
        private bool tutorialCompletedBeforeShift;

        public GameRuntimeService(
            CaseRepository cases,
            GameConfigRepository config,
            SaveDataRepository saves,
            System.Random random = null)
        {
            if (cases == null) throw new ArgumentNullException("cases");
            if (config == null) throw new ArgumentNullException("config");
            if (saves == null) throw new ArgumentNullException("saves");

            this.cases = cases;
            this.config = config;
            this.saves = saves;
            this.random = random ?? new System.Random();
        }

        public GameRuntimeState CurrentState { get; private set; }

        public bool TryStartNewShift(out string errorMessage)
        {
            if (CurrentState != null && !CurrentState.IsShiftComplete)
            {
                errorMessage = "Cannot start a new Shift while the current Shift is incomplete.";
                return false;
            }
            if (cases.Count < config.CasesPerShift)
            {
                errorMessage =
                    "Cannot create Shift: CaseRepository contains " + cases.Count +
                    " cases but " + config.CasesPerShift + " are required.";
                return false;
            }

            int nextShift = CurrentState == null ? 1 : CurrentState.CurrentShift + 1;
            bool tutorialCompleted = CurrentState != null
                ? CurrentState.TutorialCompleted
                : tutorialCompletedBeforeShift;
            List<string> selectedCaseIds = SelectUniqueCases();
            CurrentCaseRuntimeState currentCase = CreateFreshCaseState(selectedCaseIds[0]);

            CurrentState = new GameRuntimeState(
                nextShift,
                0,
                selectedCaseIds,
                config.StartingRp,
                0,
                0,
                0,
                currentCase,
                new CompletedCaseResult[0],
                tutorialCompleted);

            errorMessage = null;
            AutoSave("new Shift creation");
            return true;
        }

        public SaveLoadResult RestoreFromSave()
        {
            SaveLoadResult result = saves.Load();
            if (result.Success)
            {
                CurrentState = result.State;
                tutorialCompletedBeforeShift = result.State.TutorialCompleted;
            }
            return result;
        }

        public SaveOperationResult SaveNow()
        {
            if (CurrentState == null)
            {
                return SaveOperationResult.Failed("Cannot save before Runtime State exists.");
            }
            return saves.Save(CurrentState);
        }

        public bool CanAfford(InvestigationTool tool)
        {
            return CurrentState != null && CurrentState.RemainingRp >= GetToolCost(tool);
        }

        public bool TryUseTool(InvestigationTool tool, out string errorMessage)
        {
            CurrentCaseRuntimeState current;
            if (!TryGetActiveCase(out current, out errorMessage))
            {
                return false;
            }
            if (current.DiagnosisSubmitted)
            {
                errorMessage = "Investigation tools cannot be used after diagnosis submission.";
                return false;
            }
            if (IsToolAlreadyUsed(current, tool))
            {
                errorMessage = tool + " has already been used for this Case.";
                return false;
            }

            int cost = GetToolCost(tool);
            if (CurrentState.RemainingRp < cost)
            {
                errorMessage =
                    "Insufficient RP for " + tool + ". Required: " + cost +
                    ", remaining: " + CurrentState.RemainingRp + ".";
                return false;
            }

            CurrentState.RemainingRp -= cost;
            current.RpSpent += cost;
            SetToolUsed(current, tool);
            errorMessage = null;
            AutoSave(tool + " usage");
            return true;
        }

        public bool TrySubmitDiagnosis(
            string diagnosis,
            out CompletedCaseResult completedResult,
            out string errorMessage)
        {
            completedResult = null;
            CurrentCaseRuntimeState current;
            if (!TryGetActiveCase(out current, out errorMessage))
            {
                return false;
            }
            if (current.DiagnosisSubmitted)
            {
                errorMessage = "Diagnosis has already been submitted for this Case.";
                return false;
            }

            CancerType unused;
            if (!GameDataValueParser.TryParseCancerType(diagnosis, out unused))
            {
                errorMessage = "Diagnosis must be one of the 8 frozen CancerType IDs.";
                return false;
            }

            GameCaseData staticCase;
            if (!cases.TryGetByCaseId(current.CaseId, out staticCase))
            {
                errorMessage = "Current caseId is missing from CaseRepository: " + current.CaseId;
                return false;
            }

            bool isCorrect = string.Equals(diagnosis, staticCase.TrueClassId, StringComparison.Ordinal);
            int scoreEarned = isCorrect
                ? config.CorrectDiagnosisScore
                : config.WrongDiagnosisScore;

            current.SelectedDiagnosis = diagnosis;
            current.DiagnosisSubmitted = true;
            current.ScoreEarned = scoreEarned;
            CurrentState.CurrentScore += scoreEarned;
            CurrentState.CompletedCaseCount += 1;
            if (isCorrect) CurrentState.CorrectCaseCount += 1;

            completedResult = new CompletedCaseResult(
                current.CaseId,
                diagnosis,
                staticCase.TrueClassId,
                isCorrect,
                scoreEarned,
                current.RpSpent,
                current.GeneScanUsed,
                current.CancerGalaxyUsed,
                current.AiAssistantUsed);
            CurrentState.AddCompletedResult(completedResult);

            errorMessage = null;
            AutoSave(CurrentState.IsShiftComplete ? "Shift completion" : "diagnosis submission");
            return true;
        }

        public bool TryAdvanceToNextCase(out bool shiftCompleted, out string errorMessage)
        {
            shiftCompleted = false;
            CurrentCaseRuntimeState current;
            if (!TryGetActiveCase(out current, out errorMessage))
            {
                return false;
            }
            if (!current.DiagnosisSubmitted)
            {
                errorMessage = "Cannot advance before diagnosis submission.";
                return false;
            }

            if (CurrentState.IsShiftComplete)
            {
                shiftCompleted = true;
                errorMessage = null;
                AutoSave("Shift end");
                return true;
            }

            CurrentState.CurrentCaseIndex += 1;
            CurrentState.CurrentCaseState = CreateFreshCaseState(
                CurrentState.SelectedCaseIds[CurrentState.CurrentCaseIndex]);
            errorMessage = null;
            AutoSave("advance to next Case");
            return true;
        }

        public void MarkTutorialCompleted()
        {
            if (CurrentState == null)
            {
                tutorialCompletedBeforeShift = true;
                return;
            }
            if (CurrentState.TutorialCompleted) return;

            CurrentState.TutorialCompleted = true;
            AutoSave("Tutorial completion");
        }

        private List<string> SelectUniqueCases()
        {
            IReadOnlyList<GameCaseData> allCases = cases.GetAll();
            List<string> pool = new List<string>(allCases.Count);
            for (int index = 0; index < allCases.Count; index++)
            {
                pool.Add(allCases[index].CaseId);
            }

            for (int index = 0; index < config.CasesPerShift; index++)
            {
                int swapIndex = random.Next(index, pool.Count);
                string temporary = pool[index];
                pool[index] = pool[swapIndex];
                pool[swapIndex] = temporary;
            }

            return pool.GetRange(0, config.CasesPerShift);
        }

        private static CurrentCaseRuntimeState CreateFreshCaseState(string caseId)
        {
            return new CurrentCaseRuntimeState(
                caseId,
                false,
                false,
                false,
                null,
                false,
                0,
                0);
        }

        private bool TryGetActiveCase(
            out CurrentCaseRuntimeState current,
            out string errorMessage)
        {
            current = CurrentState == null ? null : CurrentState.CurrentCaseState;
            if (CurrentState == null || current == null)
            {
                errorMessage = "No active Runtime State/Case.";
                return false;
            }
            errorMessage = null;
            return true;
        }

        private int GetToolCost(InvestigationTool tool)
        {
            switch (tool)
            {
                case InvestigationTool.GeneScan: return config.GeneScanCost;
                case InvestigationTool.CancerGalaxy: return config.CancerGalaxyCost;
                case InvestigationTool.AiAssistant: return config.AiAssistantCost;
                default: throw new ArgumentOutOfRangeException("tool", tool, "Unknown investigation tool.");
            }
        }

        private static bool IsToolAlreadyUsed(CurrentCaseRuntimeState current, InvestigationTool tool)
        {
            switch (tool)
            {
                case InvestigationTool.GeneScan: return current.GeneScanUsed;
                case InvestigationTool.CancerGalaxy: return current.CancerGalaxyUsed;
                case InvestigationTool.AiAssistant: return current.AiAssistantUsed;
                default: throw new ArgumentOutOfRangeException("tool", tool, "Unknown investigation tool.");
            }
        }

        private static void SetToolUsed(CurrentCaseRuntimeState current, InvestigationTool tool)
        {
            switch (tool)
            {
                case InvestigationTool.GeneScan: current.GeneScanUsed = true; return;
                case InvestigationTool.CancerGalaxy: current.CancerGalaxyUsed = true; return;
                case InvestigationTool.AiAssistant: current.AiAssistantUsed = true; return;
                default: throw new ArgumentOutOfRangeException("tool", tool, "Unknown investigation tool.");
            }
        }

        private void AutoSave(string reason)
        {
            SaveOperationResult result = saves.Save(CurrentState);
            if (!result.Success)
            {
                Debug.LogError("Automatic save failed after " + reason + ".\n" + result.ErrorMessage);
            }
        }
    }
}
