using System;
using Newtonsoft.Json;

namespace CancerTrace.Data.Save
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class SaveDataV1
    {
        [JsonProperty("saveVersion", Required = Required.Always)] public string SaveVersion;
        [JsonProperty("currentShift", Required = Required.Always)] public int CurrentShift;
        [JsonProperty("currentCaseIndex", Required = Required.Always)] public int CurrentCaseIndex;
        [JsonProperty("selectedCaseIds", Required = Required.Always)] public string[] SelectedCaseIds;
        [JsonProperty("remainingRP", Required = Required.Always)] public int RemainingRp;
        [JsonProperty("currentScore", Required = Required.Always)] public int CurrentScore;
        [JsonProperty("completedCaseCount", Required = Required.Always)] public int CompletedCaseCount;
        [JsonProperty("correctCaseCount", Required = Required.Always)] public int CorrectCaseCount;
        [JsonProperty("currentCaseState", Required = Required.Always)] public CurrentCaseSaveData CurrentCaseState;
        [JsonProperty("completedCaseResults", Required = Required.Always)] public CompletedCaseResultSaveData[] CompletedCaseResults;
        [JsonProperty("tutorialCompleted", Required = Required.Always)] public bool TutorialCompleted;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class CurrentCaseSaveData
    {
        [JsonProperty("caseId", Required = Required.Always)] public string CaseId;
        [JsonProperty("geneScanUsed", Required = Required.Always)] public bool GeneScanUsed;
        [JsonProperty("cancerGalaxyUsed", Required = Required.Always)] public bool CancerGalaxyUsed;
        [JsonProperty("aiAssistantUsed", Required = Required.Always)] public bool AiAssistantUsed;
        [JsonProperty("selectedDiagnosis", Required = Required.AllowNull)] public string SelectedDiagnosis;
        [JsonProperty("diagnosisSubmitted", Required = Required.Always)] public bool DiagnosisSubmitted;
        [JsonProperty("rpSpent", Required = Required.Always)] public int RpSpent;
        [JsonProperty("scoreEarned", Required = Required.Always)] public int ScoreEarned;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class CompletedCaseResultSaveData
    {
        [JsonProperty("caseId", Required = Required.Always)] public string CaseId;
        [JsonProperty("playerDiagnosis", Required = Required.Always)] public string PlayerDiagnosis;
        [JsonProperty("trueDiagnosis", Required = Required.Always)] public string TrueDiagnosis;
        [JsonProperty("isCorrect", Required = Required.Always)] public bool IsCorrect;
        [JsonProperty("scoreEarned", Required = Required.Always)] public int ScoreEarned;
        [JsonProperty("rpSpent", Required = Required.Always)] public int RpSpent;
        [JsonProperty("geneScanUsed", Required = Required.Always)] public bool GeneScanUsed;
        [JsonProperty("cancerGalaxyUsed", Required = Required.Always)] public bool CancerGalaxyUsed;
        [JsonProperty("aiAssistantUsed", Required = Required.Always)] public bool AiAssistantUsed;
    }
}
