using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CancerTrace.Gameplay.Runtime
{
    public enum InvestigationTool
    {
        GeneScan,
        CancerGalaxy,
        AiAssistant
    }

    public sealed class GameRuntimeState
    {
        private readonly List<string> selectedCaseIds;
        private readonly ReadOnlyCollection<string> readOnlySelectedCaseIds;
        private readonly List<CompletedCaseResult> completedCaseResults;
        private readonly ReadOnlyCollection<CompletedCaseResult> readOnlyCompletedCaseResults;

        internal GameRuntimeState(
            int currentShift,
            int currentCaseIndex,
            IEnumerable<string> selectedCaseIds,
            int remainingRp,
            int currentScore,
            int completedCaseCount,
            int correctCaseCount,
            CurrentCaseRuntimeState currentCaseState,
            IEnumerable<CompletedCaseResult> completedCaseResults,
            bool tutorialCompleted)
        {
            CurrentShift = currentShift;
            CurrentCaseIndex = currentCaseIndex;
            this.selectedCaseIds = new List<string>(selectedCaseIds);
            readOnlySelectedCaseIds = this.selectedCaseIds.AsReadOnly();
            RemainingRp = remainingRp;
            CurrentScore = currentScore;
            CompletedCaseCount = completedCaseCount;
            CorrectCaseCount = correctCaseCount;
            CurrentCaseState = currentCaseState;
            this.completedCaseResults = new List<CompletedCaseResult>(completedCaseResults);
            readOnlyCompletedCaseResults = this.completedCaseResults.AsReadOnly();
            TutorialCompleted = tutorialCompleted;
        }

        public int CurrentShift { get; internal set; }
        public int CurrentCaseIndex { get; internal set; }
        public IReadOnlyList<string> SelectedCaseIds { get { return readOnlySelectedCaseIds; } }
        public int RemainingRp { get; internal set; }
        public int CurrentScore { get; internal set; }
        public int CompletedCaseCount { get; internal set; }
        public int CorrectCaseCount { get; internal set; }
        public CurrentCaseRuntimeState CurrentCaseState { get; internal set; }
        public IReadOnlyList<CompletedCaseResult> CompletedCaseResults { get { return readOnlyCompletedCaseResults; } }
        public bool TutorialCompleted { get; internal set; }

        public bool IsShiftComplete
        {
            get { return CompletedCaseCount == selectedCaseIds.Count; }
        }

        internal void AddCompletedResult(CompletedCaseResult result)
        {
            completedCaseResults.Add(result);
        }
    }

    public sealed class CurrentCaseRuntimeState
    {
        internal CurrentCaseRuntimeState(
            string caseId,
            bool geneScanUsed,
            bool cancerGalaxyUsed,
            bool aiAssistantUsed,
            string selectedDiagnosis,
            bool diagnosisSubmitted,
            int rpSpent,
            int scoreEarned)
        {
            CaseId = caseId;
            GeneScanUsed = geneScanUsed;
            CancerGalaxyUsed = cancerGalaxyUsed;
            AiAssistantUsed = aiAssistantUsed;
            SelectedDiagnosis = selectedDiagnosis;
            DiagnosisSubmitted = diagnosisSubmitted;
            RpSpent = rpSpent;
            ScoreEarned = scoreEarned;
        }

        public string CaseId { get; private set; }
        public bool GeneScanUsed { get; internal set; }
        public bool CancerGalaxyUsed { get; internal set; }
        public bool AiAssistantUsed { get; internal set; }
        public string SelectedDiagnosis { get; internal set; }
        public bool DiagnosisSubmitted { get; internal set; }
        public int RpSpent { get; internal set; }
        public int ScoreEarned { get; internal set; }
    }

    public sealed class CompletedCaseResult
    {
        internal CompletedCaseResult(
            string caseId,
            string playerDiagnosis,
            string trueDiagnosis,
            bool isCorrect,
            int scoreEarned,
            int rpSpent,
            bool geneScanUsed,
            bool cancerGalaxyUsed,
            bool aiAssistantUsed)
        {
            CaseId = caseId;
            PlayerDiagnosis = playerDiagnosis;
            TrueDiagnosis = trueDiagnosis;
            IsCorrect = isCorrect;
            ScoreEarned = scoreEarned;
            RpSpent = rpSpent;
            GeneScanUsed = geneScanUsed;
            CancerGalaxyUsed = cancerGalaxyUsed;
            AiAssistantUsed = aiAssistantUsed;
        }

        public string CaseId { get; private set; }
        public string PlayerDiagnosis { get; private set; }
        public string TrueDiagnosis { get; private set; }
        public bool IsCorrect { get; private set; }
        public int ScoreEarned { get; private set; }
        public int RpSpent { get; private set; }
        public bool GeneScanUsed { get; private set; }
        public bool CancerGalaxyUsed { get; private set; }
        public bool AiAssistantUsed { get; private set; }
    }
}
