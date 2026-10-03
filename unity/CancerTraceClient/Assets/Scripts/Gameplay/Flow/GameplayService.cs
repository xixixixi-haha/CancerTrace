using System;
using System.Collections.Generic;
using CancerTrace.Data.Models;
using CancerTrace.Data.Repositories;
using CancerTrace.Data.Save;
using CancerTrace.Gameplay.Runtime;

namespace CancerTrace.Gameplay.Flow
{
    public sealed class GameplayService
    {
        private const string AnomalyDisplayLabel = "异常案件 / Anomaly Case";

        private readonly GameRuntimeService runtime;
        private readonly CaseRepository cases;
        private readonly GalaxyRepository galaxy;
        private readonly ClassProfileRepository classProfiles;
        private readonly GameConfigRepository config;
        private readonly IReadOnlyList<CancerTypeOptionView> diagnosisOptions;

        public GameplayService(
            GameRuntimeService runtime,
            CaseRepository cases,
            GalaxyRepository galaxy,
            ClassProfileRepository classProfiles,
            GameConfigRepository config)
        {
            if (runtime == null) throw new ArgumentNullException("runtime");
            if (cases == null) throw new ArgumentNullException("cases");
            if (galaxy == null) throw new ArgumentNullException("galaxy");
            if (classProfiles == null) throw new ArgumentNullException("classProfiles");
            if (config == null) throw new ArgumentNullException("config");

            this.runtime = runtime;
            this.cases = cases;
            this.galaxy = galaxy;
            this.classProfiles = classProfiles;
            this.config = config;
            diagnosisOptions = BuildDiagnosisOptions();
        }

        public IReadOnlyList<CancerTypeOptionView> GetDiagnosisOptions()
        {
            return diagnosisOptions;
        }

        public GameplayActionResult<PlayerCaseView> StartNewShift()
        {
            if (runtime.CurrentState != null)
            {
                return Failure<PlayerCaseView>(
                    GameplayActionStatus.InvalidState,
                    "A Runtime State already exists. Resume it or explicitly start the next Shift after completion.");
            }

            return StartShiftInternal();
        }

        public GameplayActionResult<PlayerCaseView> StartNextShift()
        {
            if (runtime.CurrentState == null || !runtime.CurrentState.IsShiftComplete)
            {
                return Failure<PlayerCaseView>(
                    GameplayActionStatus.InvalidState,
                    "The current Shift must be complete before starting the next Shift.");
            }

            return StartShiftInternal();
        }

        public GameplayActionResult<PlayerCaseView> RestoreSavedShift()
        {
            SaveLoadResult load = runtime.RestoreFromSave();
            if (!load.Success)
            {
                return Failure<PlayerCaseView>(GameplayActionStatus.InvalidState, load.ErrorMessage);
            }

            return GameplayActionResult<PlayerCaseView>.Succeeded(BuildPlayerCaseView());
        }

        public GameplayActionResult<PlayerCaseView> GetCurrentCase()
        {
            if (!HasActiveCase())
            {
                return Failure<PlayerCaseView>(GameplayActionStatus.InvalidState, "No active Case exists.");
            }

            return GameplayActionResult<PlayerCaseView>.Succeeded(BuildPlayerCaseView());
        }

        public GameplayActionResult<PlayerCaseView> SelectDiagnosis(string classId)
        {
            CurrentCaseRuntimeState current;
            GameplayActionResult<PlayerCaseView> stateFailure;
            if (!TryGetMutableCase(out current, out stateFailure)) return stateFailure;
            if (current.DiagnosisSubmitted)
            {
                return Failure<PlayerCaseView>(
                    GameplayActionStatus.AlreadySubmitted,
                    "The final diagnosis has already been submitted.");
            }
            if (!IsCancerType(classId))
            {
                return Failure<PlayerCaseView>(
                    GameplayActionStatus.InvalidDiagnosis,
                    "Diagnosis must be one of the 8 frozen CancerType IDs.");
            }

            string runtimeError;
            if (!runtime.TrySelectDiagnosis(classId, out runtimeError))
            {
                return Failure<PlayerCaseView>(GameplayActionStatus.InvalidState, runtimeError);
            }

            return GameplayActionResult<PlayerCaseView>.Succeeded(BuildPlayerCaseView());
        }

        public GameplayActionResult<GeneScanView> UseGeneScan()
        {
            GameplayActionResult<GeneScanView> failure;
            if (!CanUseTool(
                    InvestigationTool.GeneScan,
                    runtime.CurrentState == null ? false : runtime.CurrentState.CurrentCaseState.GeneScanUsed,
                    config.GeneScanCost,
                    out failure))
            {
                return failure;
            }

            string error;
            if (!runtime.TryUseTool(InvestigationTool.GeneScan, out error))
            {
                return Failure<GeneScanView>(GameplayActionStatus.InvalidState, error);
            }

            GameCaseData gameCase = GetCurrentStaticCase();
            return GameplayActionResult<GeneScanView>.Succeeded(
                new GeneScanView(
                    BuildClues(gameCase.GeneScanClues),
                    runtime.CurrentState.RemainingRp));
        }

        public GameplayActionResult<CancerGalaxyView> UseCancerGalaxy()
        {
            GameplayActionResult<CancerGalaxyView> failure;
            if (!CanUseTool(
                    InvestigationTool.CancerGalaxy,
                    runtime.CurrentState == null ? false : runtime.CurrentState.CurrentCaseState.CancerGalaxyUsed,
                    config.CancerGalaxyCost,
                    out failure))
            {
                return failure;
            }

            string error;
            if (!runtime.TryUseTool(InvestigationTool.CancerGalaxy, out error))
            {
                return Failure<CancerGalaxyView>(GameplayActionStatus.InvalidState, error);
            }

            return GameplayActionResult<CancerGalaxyView>.Succeeded(
                BuildCancerGalaxyView(GetCurrentStaticCase()));
        }

        public GameplayActionResult<AiAssistantView> UseAiAssistant()
        {
            GameplayActionResult<AiAssistantView> failure;
            if (!CanUseTool(
                    InvestigationTool.AiAssistant,
                    runtime.CurrentState == null ? false : runtime.CurrentState.CurrentCaseState.AiAssistantUsed,
                    config.AiAssistantCost,
                    out failure))
            {
                return failure;
            }

            string error;
            if (!runtime.TryUseTool(InvestigationTool.AiAssistant, out error))
            {
                return Failure<AiAssistantView>(GameplayActionStatus.InvalidState, error);
            }

            return GameplayActionResult<AiAssistantView>.Succeeded(
                BuildAiAssistantView(GetCurrentStaticCase()));
        }

        public GameplayActionResult<EvidenceBoardView> GetEvidenceBoard()
        {
            if (!HasActiveCase())
            {
                return Failure<EvidenceBoardView>(GameplayActionStatus.InvalidState, "No active Case exists.");
            }

            GameCaseData gameCase = GetCurrentStaticCase();
            CurrentCaseRuntimeState current = runtime.CurrentState.CurrentCaseState;
            IList<ClueView> geneScanClues = current.GeneScanUsed
                ? BuildClues(gameCase.GeneScanClues)
                : new List<ClueView>();
            CancerGalaxyView galaxyEvidence = current.CancerGalaxyUsed
                ? BuildCancerGalaxyView(gameCase)
                : null;
            AiAssistantView aiEvidence = current.AiAssistantUsed
                ? BuildAiAssistantView(gameCase)
                : null;

            return GameplayActionResult<EvidenceBoardView>.Succeeded(
                new EvidenceBoardView(
                    BuildClues(gameCase.InitialClues),
                    geneScanClues,
                    galaxyEvidence,
                    aiEvidence));
        }

        public GameplayActionResult<ResultView> SubmitDiagnosis(string classId)
        {
            if (!HasActiveCase())
            {
                return Failure<ResultView>(GameplayActionStatus.InvalidState, "No active Case exists.");
            }
            if (runtime.CurrentState.CurrentCaseState.DiagnosisSubmitted)
            {
                return Failure<ResultView>(
                    GameplayActionStatus.AlreadySubmitted,
                    "The final diagnosis has already been submitted.");
            }
            if (!IsCancerType(classId))
            {
                return Failure<ResultView>(
                    GameplayActionStatus.InvalidDiagnosis,
                    "Diagnosis must be one of the 8 frozen CancerType IDs.");
            }

            CompletedCaseResult unused;
            string error;
            if (!runtime.TrySubmitDiagnosis(classId, out unused, out error))
            {
                return Failure<ResultView>(GameplayActionStatus.InvalidState, error);
            }

            return GameplayActionResult<ResultView>.Succeeded(BuildResultView());
        }

        public GameplayActionResult<ResultView> GetResult()
        {
            if (!HasActiveCase() || !runtime.CurrentState.CurrentCaseState.DiagnosisSubmitted)
            {
                return Failure<ResultView>(
                    GameplayActionStatus.InvalidState,
                    "Result is unavailable before diagnosis submission.");
            }

            return GameplayActionResult<ResultView>.Succeeded(BuildResultView());
        }

        public GameplayActionResult<PlayerCaseView> NextCase()
        {
            if (!HasActiveCase())
            {
                return Failure<PlayerCaseView>(GameplayActionStatus.InvalidState, "No active Case exists.");
            }
            if (runtime.CurrentState.IsShiftComplete)
            {
                return Failure<PlayerCaseView>(
                    GameplayActionStatus.ShiftCompleted,
                    "The Shift is complete. No next Case is available.");
            }
            if (!runtime.CurrentState.CurrentCaseState.DiagnosisSubmitted)
            {
                return Failure<PlayerCaseView>(
                    GameplayActionStatus.InvalidState,
                    "Diagnosis must be submitted before entering the next Case.");
            }

            bool shiftCompleted;
            string error;
            if (!runtime.TryAdvanceToNextCase(out shiftCompleted, out error))
            {
                return Failure<PlayerCaseView>(GameplayActionStatus.InvalidState, error);
            }
            if (shiftCompleted)
            {
                return Failure<PlayerCaseView>(GameplayActionStatus.ShiftCompleted, "The Shift is complete.");
            }

            return GameplayActionResult<PlayerCaseView>.Succeeded(BuildPlayerCaseView());
        }

        public GameplayActionResult<ShiftSummaryView> GetShiftSummary()
        {
            if (runtime.CurrentState == null || !runtime.CurrentState.IsShiftComplete)
            {
                return Failure<ShiftSummaryView>(
                    GameplayActionStatus.InvalidState,
                    "Shift Summary is unavailable until all Cases are complete.");
            }

            GameRuntimeState state = runtime.CurrentState;
            int geneScanUses = 0;
            int cancerGalaxyUses = 0;
            int aiAssistantUses = 0;
            for (int index = 0; index < state.CompletedCaseResults.Count; index++)
            {
                CompletedCaseResult result = state.CompletedCaseResults[index];
                if (result.GeneScanUsed) geneScanUses++;
                if (result.CancerGalaxyUsed) cancerGalaxyUses++;
                if (result.AiAssistantUsed) aiAssistantUses++;
            }

            double accuracy = state.CompletedCaseCount == 0
                ? 0.0
                : (double)state.CorrectCaseCount / state.CompletedCaseCount;
            return GameplayActionResult<ShiftSummaryView>.Succeeded(
                new ShiftSummaryView(
                    state.CurrentShift,
                    state.CompletedCaseCount,
                    state.CorrectCaseCount,
                    accuracy,
                    state.CurrentScore,
                    state.RemainingRp,
                    geneScanUses,
                    cancerGalaxyUses,
                    aiAssistantUses));
        }

        private GameplayActionResult<PlayerCaseView> StartShiftInternal()
        {
            string error;
            if (!runtime.TryStartNewShift(out error))
            {
                return Failure<PlayerCaseView>(GameplayActionStatus.InvalidState, error);
            }

            return GameplayActionResult<PlayerCaseView>.Succeeded(BuildPlayerCaseView());
        }

        private bool CanUseTool<T>(
            InvestigationTool tool,
            bool alreadyUsed,
            int cost,
            out GameplayActionResult<T> failure)
        {
            if (!HasActiveCase())
            {
                failure = Failure<T>(GameplayActionStatus.InvalidState, "No active Case exists.");
                return false;
            }
            if (runtime.CurrentState.CurrentCaseState.DiagnosisSubmitted)
            {
                failure = Failure<T>(
                    GameplayActionStatus.AlreadySubmitted,
                    "Investigation is closed after diagnosis submission.");
                return false;
            }
            if (alreadyUsed)
            {
                failure = Failure<T>(
                    GameplayActionStatus.AlreadyUsed,
                    tool + " has already been used for this Case.");
                return false;
            }
            if (runtime.CurrentState.RemainingRp < cost)
            {
                failure = Failure<T>(
                    GameplayActionStatus.InsufficientRp,
                    "Insufficient RP for " + tool + ".");
                return false;
            }

            failure = null;
            return true;
        }

        private bool TryGetMutableCase(
            out CurrentCaseRuntimeState current,
            out GameplayActionResult<PlayerCaseView> failure)
        {
            if (!HasActiveCase())
            {
                current = null;
                failure = Failure<PlayerCaseView>(GameplayActionStatus.InvalidState, "No active Case exists.");
                return false;
            }

            current = runtime.CurrentState.CurrentCaseState;
            failure = null;
            return true;
        }

        private bool HasActiveCase()
        {
            return runtime.CurrentState != null && runtime.CurrentState.CurrentCaseState != null;
        }

        private GameCaseData GetCurrentStaticCase()
        {
            return cases.GetByCaseId(runtime.CurrentState.CurrentCaseState.CaseId);
        }

        private PlayerCaseView BuildPlayerCaseView()
        {
            GameRuntimeState state = runtime.CurrentState;
            CurrentCaseRuntimeState current = state.CurrentCaseState;
            GameCaseData gameCase = GetCurrentStaticCase();
            bool investigationOpen = !current.DiagnosisSubmitted;

            return new PlayerCaseView(
                gameCase.CaseId,
                gameCase.CellLineName,
                state.CurrentCaseIndex + 1,
                state.SelectedCaseIds.Count,
                state.RemainingRp,
                state.CurrentScore,
                BuildClues(gameCase.InitialClues),
                new ToolAvailabilityView(
                    config.GeneScanCost,
                    current.GeneScanUsed,
                    investigationOpen && !current.GeneScanUsed && state.RemainingRp >= config.GeneScanCost),
                new ToolAvailabilityView(
                    config.CancerGalaxyCost,
                    current.CancerGalaxyUsed,
                    investigationOpen && !current.CancerGalaxyUsed && state.RemainingRp >= config.CancerGalaxyCost),
                new ToolAvailabilityView(
                    config.AiAssistantCost,
                    current.AiAssistantUsed,
                    investigationOpen && !current.AiAssistantUsed && state.RemainingRp >= config.AiAssistantCost),
                current.SelectedDiagnosis,
                current.DiagnosisSubmitted,
                state.IsShiftComplete);
        }

        private ResultView BuildResultView()
        {
            GameRuntimeState state = runtime.CurrentState;
            GameCaseData gameCase = GetCurrentStaticCase();
            CompletedCaseResult completed =
                state.CompletedCaseResults[state.CompletedCaseResults.Count - 1];
            bool isAnomaly = string.Equals(
                gameCase.Difficulty,
                CaseDifficulty.Anomaly.ToString().ToUpperInvariant(),
                StringComparison.Ordinal);

            return new ResultView(
                completed.CaseId,
                completed.PlayerDiagnosis,
                completed.TrueDiagnosis,
                gameCase.TrueClassLabelEn,
                gameCase.TrueClassLabelZh,
                completed.IsCorrect,
                completed.ScoreEarned,
                state.CurrentScore,
                state.RemainingRp,
                BuildAiAssistantView(gameCase),
                isAnomaly,
                isAnomaly ? AnomalyDisplayLabel : null);
        }

        private CancerGalaxyView BuildCancerGalaxyView(GameCaseData gameCase)
        {
            List<GalaxyReferenceNodeView> referenceNodes = new List<GalaxyReferenceNodeView>();
            IReadOnlyList<GalaxyNodeData> allNodes = galaxy.GetAll();
            for (int index = 0; index < allNodes.Count; index++)
            {
                GalaxyNodeData node = allNodes[index];
                if (!string.Equals(node.NodeType, "REFERENCE", StringComparison.Ordinal)) continue;
                referenceNodes.Add(new GalaxyReferenceNodeView(
                    node.CellLineName,
                    node.ReferenceClassId,
                    node.ReferenceClassLabelEn,
                    node.ReferenceClassLabelZh,
                    node.UmapX,
                    node.UmapY,
                    node.DisplayX,
                    node.DisplayY));
            }

            List<NearbyReferenceView> nearby = new List<NearbyReferenceView>();
            for (int index = 0; index < gameCase.Galaxy.NearbyReferences.Length; index++)
            {
                NearbyReferenceData item = gameCase.Galaxy.NearbyReferences[index];
                nearby.Add(new NearbyReferenceView(
                    item.CellLineName,
                    item.ClassId,
                    item.LabelEn,
                    item.LabelZh,
                    item.Distance));
            }

            return new CancerGalaxyView(
                gameCase.Galaxy.UmapX,
                gameCase.Galaxy.UmapY,
                gameCase.Galaxy.DisplayX,
                gameCase.Galaxy.DisplayY,
                referenceNodes,
                nearby,
                runtime.CurrentState.RemainingRp);
        }

        private AiAssistantView BuildAiAssistantView(GameCaseData gameCase)
        {
            List<AiCandidateView> candidates = new List<AiCandidateView>();
            for (int index = 0; index < gameCase.Ai.Top3.Length; index++)
            {
                AiCandidateData candidate = gameCase.Ai.Top3[index];
                candidates.Add(new AiCandidateView(
                    candidate.ClassId,
                    candidate.LabelEn,
                    candidate.LabelZh,
                    candidate.Probability));
            }

            return new AiAssistantView(
                gameCase.Ai.PredictedClassId,
                gameCase.Ai.PredictedClassLabelEn,
                gameCase.Ai.PredictedClassLabelZh,
                gameCase.Ai.Confidence,
                candidates,
                runtime.CurrentState.RemainingRp);
        }

        private static List<ClueView> BuildClues(ClueData[] source)
        {
            List<ClueView> clues = new List<ClueView>(source.Length);
            for (int clueIndex = 0; clueIndex < source.Length; clueIndex++)
            {
                ClueData clue = source[clueIndex];
                List<ClueSupportView> support = new List<ClueSupportView>(clue.Support.Length);
                for (int supportIndex = 0; supportIndex < clue.Support.Length; supportIndex++)
                {
                    ClueSupportData item = clue.Support[supportIndex];
                    support.Add(new ClueSupportView(
                        item.ClassId,
                        item.LabelEn,
                        item.LabelZh,
                        item.Strength));
                }
                clues.Add(new ClueView(clue.Gene, clue.ExpressionValue, clue.State, support));
            }
            return clues;
        }

        private IReadOnlyList<CancerTypeOptionView> BuildDiagnosisOptions()
        {
            IReadOnlyList<ClassProfileData> profiles = classProfiles.GetAll();
            List<CancerTypeOptionView> options = new List<CancerTypeOptionView>(profiles.Count);
            for (int index = 0; index < profiles.Count; index++)
            {
                ClassProfileData profile = profiles[index];
                options.Add(new CancerTypeOptionView(
                    profile.ClassId,
                    profile.LabelEn,
                    profile.LabelZh));
            }
            return options.AsReadOnly();
        }

        private static bool IsCancerType(string classId)
        {
            CancerType unused;
            return GameDataValueParser.TryParseCancerType(classId, out unused);
        }

        private static GameplayActionResult<T> Failure<T>(
            GameplayActionStatus status,
            string message)
        {
            return GameplayActionResult<T>.Failed(status, message);
        }
    }
}
