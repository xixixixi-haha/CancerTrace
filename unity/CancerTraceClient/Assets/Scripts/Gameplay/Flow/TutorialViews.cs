using System.Collections.Generic;
using System.Collections.ObjectModel;
using CancerTrace.Gameplay.Flow;

namespace CancerTrace.Gameplay.Tutorial
{
    /// <summary>
    /// The pre-investigation data that Tutorial UI may display without exposing GameCaseData.
    /// </summary>
    public sealed class TutorialCaseView
    {
        internal TutorialCaseView(
            string caseId,
            string cellLineName,
            int remainingRp,
            IList<ClueView> initialClues,
            bool geneScanUsed,
            IList<ClueView> geneScanClues,
            bool galaxyUsed,
            CancerGalaxyView galaxyEvidence,
            bool aiUsed,
            AiAssistantView aiEvidence)
        {
            CaseId = caseId;
            CellLineName = cellLineName;
            RemainingRp = remainingRp;
            InitialClues = new ReadOnlyCollection<ClueView>(initialClues);
            GeneScanUsed = geneScanUsed;
            GeneScanClues = new ReadOnlyCollection<ClueView>(geneScanClues);
            GalaxyUsed = galaxyUsed;
            GalaxyEvidence = galaxyEvidence;
            AiUsed = aiUsed;
            AiEvidence = aiEvidence;
        }

        public string CaseId { get; private set; }
        public string CellLineName { get; private set; }
        public int RemainingRp { get; private set; }
        public IReadOnlyList<ClueView> InitialClues { get; private set; }
        public bool GeneScanUsed { get; private set; }
        public IReadOnlyList<ClueView> GeneScanClues { get; private set; }
        public bool GalaxyUsed { get; private set; }
        public CancerGalaxyView GalaxyEvidence { get; private set; }
        public bool AiUsed { get; private set; }
        public AiAssistantView AiEvidence { get; private set; }
    }
}
