using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CancerTrace.Gameplay.Flow
{
    public sealed class CancerTypeOptionView
    {
        internal CancerTypeOptionView(string classId, string labelEn, string labelZh)
        {
            ClassId = classId;
            LabelEn = labelEn;
            LabelZh = labelZh;
        }

        public string ClassId { get; private set; }
        public string LabelEn { get; private set; }
        public string LabelZh { get; private set; }
    }

    public sealed class ClueSupportView
    {
        internal ClueSupportView(string classId, string labelEn, string labelZh, string strength)
        {
            ClassId = classId;
            LabelEn = labelEn;
            LabelZh = labelZh;
            Strength = strength;
        }

        public string ClassId { get; private set; }
        public string LabelEn { get; private set; }
        public string LabelZh { get; private set; }
        public string Strength { get; private set; }
    }

    public sealed class ClueView
    {
        internal ClueView(
            string gene,
            double expressionValue,
            string state,
            IList<ClueSupportView> support)
        {
            Gene = gene;
            ExpressionValue = expressionValue;
            State = state;
            Support = new ReadOnlyCollection<ClueSupportView>(support);
        }

        public string Gene { get; private set; }
        public double ExpressionValue { get; private set; }
        public string State { get; private set; }
        public IReadOnlyList<ClueSupportView> Support { get; private set; }
    }

    public sealed class ToolAvailabilityView
    {
        internal ToolAvailabilityView(int cost, bool used, bool canUse)
        {
            Cost = cost;
            Used = used;
            CanUse = canUse;
        }

        public int Cost { get; private set; }
        public bool Used { get; private set; }
        public bool CanUse { get; private set; }
    }

    public sealed class PlayerCaseView
    {
        internal PlayerCaseView(
            string caseId,
            string cellLineName,
            int shiftNumber,
            int caseNumber,
            int casesPerShift,
            int remainingRp,
            int currentScore,
            IList<ClueView> initialClues,
            ToolAvailabilityView geneScan,
            ToolAvailabilityView cancerGalaxy,
            ToolAvailabilityView aiAssistant,
            string selectedDiagnosis,
            bool diagnosisSubmitted,
            bool shiftCompleted)
        {
            CaseId = caseId;
            CellLineName = cellLineName;
            ShiftNumber = shiftNumber;
            CaseNumber = caseNumber;
            CasesPerShift = casesPerShift;
            RemainingRp = remainingRp;
            CurrentScore = currentScore;
            InitialClues = new ReadOnlyCollection<ClueView>(initialClues);
            GeneScan = geneScan;
            CancerGalaxy = cancerGalaxy;
            AiAssistant = aiAssistant;
            SelectedDiagnosis = selectedDiagnosis;
            DiagnosisSubmitted = diagnosisSubmitted;
            ShiftCompleted = shiftCompleted;
        }

        public string CaseId { get; private set; }
        public string CellLineName { get; private set; }
        public int ShiftNumber { get; private set; }
        public int CaseNumber { get; private set; }
        public int CasesPerShift { get; private set; }
        public int RemainingRp { get; private set; }
        public int CurrentScore { get; private set; }
        public IReadOnlyList<ClueView> InitialClues { get; private set; }
        public ToolAvailabilityView GeneScan { get; private set; }
        public ToolAvailabilityView CancerGalaxy { get; private set; }
        public ToolAvailabilityView AiAssistant { get; private set; }
        public string SelectedDiagnosis { get; private set; }
        public bool DiagnosisSubmitted { get; private set; }
        public bool ShiftCompleted { get; private set; }
    }

    public sealed class GeneScanView
    {
        internal GeneScanView(IList<ClueView> clues, int remainingRp)
        {
            Clues = new ReadOnlyCollection<ClueView>(clues);
            RemainingRp = remainingRp;
        }

        public IReadOnlyList<ClueView> Clues { get; private set; }
        public int RemainingRp { get; private set; }
    }

    public sealed class GalaxyReferenceNodeView
    {
        internal GalaxyReferenceNodeView(
            string cellLineName,
            string classId,
            string labelEn,
            string labelZh,
            double umapX,
            double umapY,
            double displayX,
            double displayY)
        {
            CellLineName = cellLineName;
            ClassId = classId;
            LabelEn = labelEn;
            LabelZh = labelZh;
            UmapX = umapX;
            UmapY = umapY;
            DisplayX = displayX;
            DisplayY = displayY;
        }

        public string CellLineName { get; private set; }
        public string ClassId { get; private set; }
        public string LabelEn { get; private set; }
        public string LabelZh { get; private set; }
        public double UmapX { get; private set; }
        public double UmapY { get; private set; }
        public double DisplayX { get; private set; }
        public double DisplayY { get; private set; }
    }

    public sealed class NearbyReferenceView
    {
        internal NearbyReferenceView(
            string cellLineName,
            string classId,
            string labelEn,
            string labelZh,
            double distance)
        {
            CellLineName = cellLineName;
            ClassId = classId;
            LabelEn = labelEn;
            LabelZh = labelZh;
            Distance = distance;
        }

        public string CellLineName { get; private set; }
        public string ClassId { get; private set; }
        public string LabelEn { get; private set; }
        public string LabelZh { get; private set; }
        public double Distance { get; private set; }
    }

    public sealed class CancerGalaxyView
    {
        internal CancerGalaxyView(
            double umapX,
            double umapY,
            double displayX,
            double displayY,
            IList<GalaxyReferenceNodeView> referenceNodes,
            IList<NearbyReferenceView> nearbyReferences,
            int remainingRp)
        {
            UmapX = umapX;
            UmapY = umapY;
            DisplayX = displayX;
            DisplayY = displayY;
            ReferenceNodes = new ReadOnlyCollection<GalaxyReferenceNodeView>(referenceNodes);
            NearbyReferences = new ReadOnlyCollection<NearbyReferenceView>(nearbyReferences);
            RemainingRp = remainingRp;
        }

        public double UmapX { get; private set; }
        public double UmapY { get; private set; }
        public double DisplayX { get; private set; }
        public double DisplayY { get; private set; }
        public IReadOnlyList<GalaxyReferenceNodeView> ReferenceNodes { get; private set; }
        public IReadOnlyList<NearbyReferenceView> NearbyReferences { get; private set; }
        public int RemainingRp { get; private set; }
    }

    public sealed class AiCandidateView
    {
        internal AiCandidateView(string classId, string labelEn, string labelZh, double probability)
        {
            ClassId = classId;
            LabelEn = labelEn;
            LabelZh = labelZh;
            Probability = probability;
        }

        public string ClassId { get; private set; }
        public string LabelEn { get; private set; }
        public string LabelZh { get; private set; }
        public double Probability { get; private set; }
    }

    public sealed class AiAssistantView
    {
        internal AiAssistantView(
            string predictionClassId,
            string predictionLabelEn,
            string predictionLabelZh,
            double confidence,
            IList<AiCandidateView> topCandidates,
            int remainingRp)
        {
            PredictionClassId = predictionClassId;
            PredictionLabelEn = predictionLabelEn;
            PredictionLabelZh = predictionLabelZh;
            Confidence = confidence;
            TopCandidates = new ReadOnlyCollection<AiCandidateView>(topCandidates);
            RemainingRp = remainingRp;
        }

        public string PredictionClassId { get; private set; }
        public string PredictionLabelEn { get; private set; }
        public string PredictionLabelZh { get; private set; }
        public double Confidence { get; private set; }
        public IReadOnlyList<AiCandidateView> TopCandidates { get; private set; }
        public int RemainingRp { get; private set; }
    }

    public sealed class EvidenceBoardView
    {
        internal EvidenceBoardView(
            IList<ClueView> initialClues,
            IList<ClueView> geneScanClues,
            CancerGalaxyView galaxyEvidence,
            AiAssistantView aiEvidence)
        {
            InitialClues = new ReadOnlyCollection<ClueView>(initialClues);
            GeneScanClues = new ReadOnlyCollection<ClueView>(geneScanClues);
            GalaxyEvidence = galaxyEvidence;
            AiEvidence = aiEvidence;
        }

        public IReadOnlyList<ClueView> InitialClues { get; private set; }
        public IReadOnlyList<ClueView> GeneScanClues { get; private set; }
        public CancerGalaxyView GalaxyEvidence { get; private set; }
        public AiAssistantView AiEvidence { get; private set; }
    }

    public sealed class ResultView
    {
        internal ResultView(
            string caseId,
            string playerDiagnosis,
            string trueDiagnosis,
            string trueDiagnosisLabelEn,
            string trueDiagnosisLabelZh,
            bool isCorrect,
            int scoreEarned,
            int currentScore,
            int remainingRp,
            AiAssistantView aiReview,
            bool isAnomaly,
            string anomalyDisplayLabel)
        {
            CaseId = caseId;
            PlayerDiagnosis = playerDiagnosis;
            TrueDiagnosis = trueDiagnosis;
            TrueDiagnosisLabelEn = trueDiagnosisLabelEn;
            TrueDiagnosisLabelZh = trueDiagnosisLabelZh;
            IsCorrect = isCorrect;
            ScoreEarned = scoreEarned;
            CurrentScore = currentScore;
            RemainingRp = remainingRp;
            AiReview = aiReview;
            IsAnomaly = isAnomaly;
            AnomalyDisplayLabel = anomalyDisplayLabel;
        }

        public string CaseId { get; private set; }
        public string PlayerDiagnosis { get; private set; }
        public string TrueDiagnosis { get; private set; }
        public string TrueDiagnosisLabelEn { get; private set; }
        public string TrueDiagnosisLabelZh { get; private set; }
        public bool IsCorrect { get; private set; }
        public int ScoreEarned { get; private set; }
        public int CurrentScore { get; private set; }
        public int RemainingRp { get; private set; }
        public AiAssistantView AiReview { get; private set; }
        public bool IsAnomaly { get; private set; }
        public string AnomalyDisplayLabel { get; private set; }
    }

    public sealed class ShiftSummaryView
    {
        internal ShiftSummaryView(
            int shiftNumber,
            int casesCompleted,
            int correctCases,
            double accuracy,
            int score,
            int remainingRp,
            int geneScanUses,
            int cancerGalaxyUses,
            int aiAssistantUses)
        {
            ShiftNumber = shiftNumber;
            CasesCompleted = casesCompleted;
            CorrectCases = correctCases;
            Accuracy = accuracy;
            Score = score;
            RemainingRp = remainingRp;
            GeneScanUses = geneScanUses;
            CancerGalaxyUses = cancerGalaxyUses;
            AiAssistantUses = aiAssistantUses;
        }

        public int ShiftNumber { get; private set; }
        public int CasesCompleted { get; private set; }
        public int CorrectCases { get; private set; }
        public double Accuracy { get; private set; }
        public int Score { get; private set; }
        public int RemainingRp { get; private set; }
        public int GeneScanUses { get; private set; }
        public int CancerGalaxyUses { get; private set; }
        public int AiAssistantUses { get; private set; }
    }
}
