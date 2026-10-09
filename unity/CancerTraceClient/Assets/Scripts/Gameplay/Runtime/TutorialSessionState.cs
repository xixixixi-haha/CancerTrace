using System;
using CancerTrace.Gameplay.Flow;

namespace CancerTrace.Gameplay.Tutorial
{
    /// <summary>
    /// In-memory Tutorial state. It is intentionally separate from GameRuntimeState and SaveData.
    /// </summary>
    public sealed class TutorialSessionState
    {
        internal TutorialSessionState(string caseId)
        {
            if (string.IsNullOrEmpty(caseId)) throw new ArgumentException("Tutorial Case ID is required.", "caseId");

            CaseId = caseId;
            CurrentStepIndex = 0;
            GeneScanUsed = false;
            GalaxyUsed = false;
            AiUsed = false;
            DraftDiagnosis = null;
            Submitted = false;
            Result = null;
            IsActive = true;
        }

        public string CaseId { get; private set; }
        public int CurrentStepIndex { get; internal set; }
        public bool GeneScanUsed { get; internal set; }
        public bool GalaxyUsed { get; internal set; }
        public bool AiUsed { get; internal set; }
        public string DraftDiagnosis { get; internal set; }
        public bool Submitted { get; internal set; }
        public ResultView Result { get; internal set; }
        public bool IsActive { get; internal set; }
    }
}
