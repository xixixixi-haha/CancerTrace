using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CancerTrace.Data.Models
{
    public enum CancerType
    {
        Lung,
        Skin,
        CnsBrain,
        Bowel,
        EsophagusStomach,
        Breast,
        Bone,
        OvaryFallopianTube
    }

    public enum CaseDifficulty
    {
        Easy,
        Normal,
        Hard,
        Anomaly
    }

    public enum ClueState
    {
        High,
        Low
    }

    public enum EvidenceStrength
    {
        Weak,
        Moderate,
        Strong
    }

    public enum GalaxyNodeType
    {
        Reference,
        Case
    }

    public static class GameDataValueParser
    {
        public static readonly string[] CancerTypeIds =
        {
            "lung",
            "skin",
            "cns_brain",
            "bowel",
            "esophagus_stomach",
            "breast",
            "bone",
            "ovary_fallopian_tube"
        };

        public static bool TryParseCancerType(string value, out CancerType result)
        {
            switch (value)
            {
                case "lung": result = CancerType.Lung; return true;
                case "skin": result = CancerType.Skin; return true;
                case "cns_brain": result = CancerType.CnsBrain; return true;
                case "bowel": result = CancerType.Bowel; return true;
                case "esophagus_stomach": result = CancerType.EsophagusStomach; return true;
                case "breast": result = CancerType.Breast; return true;
                case "bone": result = CancerType.Bone; return true;
                case "ovary_fallopian_tube": result = CancerType.OvaryFallopianTube; return true;
                default: result = default(CancerType); return false;
            }
        }

        public static bool TryParseDifficulty(string value, out CaseDifficulty result)
        {
            switch (value)
            {
                case "EASY": result = CaseDifficulty.Easy; return true;
                case "NORMAL": result = CaseDifficulty.Normal; return true;
                case "HARD": result = CaseDifficulty.Hard; return true;
                case "ANOMALY": result = CaseDifficulty.Anomaly; return true;
                default: result = default(CaseDifficulty); return false;
            }
        }

        public static bool TryParseClueState(string value, out ClueState result)
        {
            switch (value)
            {
                case "HIGH": result = ClueState.High; return true;
                case "LOW": result = ClueState.Low; return true;
                default: result = default(ClueState); return false;
            }
        }

        public static bool TryParseStrength(string value, out EvidenceStrength result)
        {
            switch (value)
            {
                case "Weak": result = EvidenceStrength.Weak; return true;
                case "Moderate": result = EvidenceStrength.Moderate; return true;
                case "Strong": result = EvidenceStrength.Strong; return true;
                default: result = default(EvidenceStrength); return false;
            }
        }

        public static bool TryParseNodeType(string value, out GalaxyNodeType result)
        {
            switch (value)
            {
                case "REFERENCE": result = GalaxyNodeType.Reference; return true;
                case "CASE": result = GalaxyNodeType.Case; return true;
                default: result = default(GalaxyNodeType); return false;
            }
        }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class GameCasesDocument
    {
        [JsonProperty("metadata", Required = Required.Always)] public GameCasesMetadata Metadata;
        [JsonProperty("cases", Required = Required.Always)] public GameCaseData[] Cases;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class GameCasesMetadata
    {
        [JsonProperty("version", Required = Required.Always)] public string Version;
        [JsonProperty("generatedBy", Required = Required.Always)] public string GeneratedBy;
        [JsonProperty("totalCases", Required = Required.Always)] public int TotalCases;
        [JsonProperty("classCount", Required = Required.Always)] public int ClassCount;
        [JsonProperty("dataSourceNote", Required = Required.Always)] public string DataSourceNote;
        [JsonProperty("aiTestAccuracy", Required = Required.Always)] public double AiTestAccuracy;
        [JsonProperty("scientificIntegrity", Required = Required.Always)] public string[] ScientificIntegrity;
        [JsonProperty("scientificDisclaimer", Required = Required.Always)] public string ScientificDisclaimer;
        [JsonProperty("scientificDisclaimerZh", Required = Required.Always)] public string ScientificDisclaimerZh;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class GameCaseData
    {
        [JsonProperty("caseId", Required = Required.Always)] public string CaseId;
        [JsonProperty("modelId", Required = Required.Always)] public string ModelId;
        [JsonProperty("cellLineName", Required = Required.Always)] public string CellLineName;
        [JsonProperty("trueClassId", Required = Required.Always)] public string TrueClassId;
        [JsonProperty("trueClassLabelEn", Required = Required.Always)] public string TrueClassLabelEn;
        [JsonProperty("trueClassLabelZh", Required = Required.Always)] public string TrueClassLabelZh;
        [JsonProperty("ai", Required = Required.Always)] public CaseAiData Ai;
        [JsonProperty("initialClues", Required = Required.Always)] public ClueData[] InitialClues;
        [JsonProperty("geneScanClues", Required = Required.Always)] public ClueData[] GeneScanClues;
        [JsonProperty("evidence", Required = Required.Always)] public CaseEvidenceData Evidence;
        [JsonProperty("difficulty", Required = Required.Always)] public string Difficulty;
        [JsonProperty("difficultyScore", Required = Required.Always)] public double DifficultyScore;
        [JsonProperty("galaxy", Required = Required.Always)] public CaseGalaxyData Galaxy;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class CaseAiData
    {
        [JsonProperty("predictedClassId", Required = Required.Always)] public string PredictedClassId;
        [JsonProperty("predictedClassLabelEn", Required = Required.Always)] public string PredictedClassLabelEn;
        [JsonProperty("predictedClassLabelZh", Required = Required.Always)] public string PredictedClassLabelZh;
        [JsonProperty("confidence", Required = Required.Always)] public double Confidence;
        [JsonProperty("correct", Required = Required.Always)] public bool Correct;
        [JsonProperty("top3", Required = Required.Always)] public AiCandidateData[] Top3;
        [JsonProperty("probabilities", Required = Required.Always)] public CancerProbabilityData Probabilities;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class AiCandidateData
    {
        [JsonProperty("classId", Required = Required.Always)] public string ClassId;
        [JsonProperty("labelEn", Required = Required.Always)] public string LabelEn;
        [JsonProperty("labelZh", Required = Required.Always)] public string LabelZh;
        [JsonProperty("probability", Required = Required.Always)] public double Probability;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class CancerProbabilityData
    {
        [JsonProperty("lung", Required = Required.Always)] public double Lung;
        [JsonProperty("skin", Required = Required.Always)] public double Skin;
        [JsonProperty("cns_brain", Required = Required.Always)] public double CnsBrain;
        [JsonProperty("bowel", Required = Required.Always)] public double Bowel;
        [JsonProperty("esophagus_stomach", Required = Required.Always)] public double EsophagusStomach;
        [JsonProperty("breast", Required = Required.Always)] public double Breast;
        [JsonProperty("bone", Required = Required.Always)] public double Bone;
        [JsonProperty("ovary_fallopian_tube", Required = Required.Always)] public double OvaryFallopianTube;

        public IEnumerable<KeyValuePair<string, double>> Enumerate()
        {
            yield return new KeyValuePair<string, double>("lung", Lung);
            yield return new KeyValuePair<string, double>("skin", Skin);
            yield return new KeyValuePair<string, double>("cns_brain", CnsBrain);
            yield return new KeyValuePair<string, double>("bowel", Bowel);
            yield return new KeyValuePair<string, double>("esophagus_stomach", EsophagusStomach);
            yield return new KeyValuePair<string, double>("breast", Breast);
            yield return new KeyValuePair<string, double>("bone", Bone);
            yield return new KeyValuePair<string, double>("ovary_fallopian_tube", OvaryFallopianTube);
        }

        public bool TryGetValue(string classId, out double value)
        {
            switch (classId)
            {
                case "lung": value = Lung; return true;
                case "skin": value = Skin; return true;
                case "cns_brain": value = CnsBrain; return true;
                case "bowel": value = Bowel; return true;
                case "esophagus_stomach": value = EsophagusStomach; return true;
                case "breast": value = Breast; return true;
                case "bone": value = Bone; return true;
                case "ovary_fallopian_tube": value = OvaryFallopianTube; return true;
                default: value = 0d; return false;
            }
        }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ClueData
    {
        [JsonProperty("gene", Required = Required.Always)] public string Gene;
        [JsonProperty("geneColumn", Required = Required.Always)] public string GeneColumn;
        [JsonProperty("expressionValue", Required = Required.Always)] public double ExpressionValue;
        [JsonProperty("state", Required = Required.Always)] public string State;
        [JsonProperty("support", Required = Required.Always)] public ClueSupportData[] Support;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ClueSupportData
    {
        [JsonProperty("classId", Required = Required.Always)] public string ClassId;
        [JsonProperty("labelEn", Required = Required.Always)] public string LabelEn;
        [JsonProperty("labelZh", Required = Required.Always)] public string LabelZh;
        [JsonProperty("strength", Required = Required.Always)] public string Strength;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class CaseEvidenceData
    {
        [JsonProperty("initialTop3", Required = Required.Always)] public EvidenceEntryData[] InitialTop3;
        [JsonProperty("afterGeneScanTop3", Required = Required.Always)] public EvidenceEntryData[] AfterGeneScanTop3;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class EvidenceEntryData
    {
        [JsonProperty("classId", Required = Required.Always)] public string ClassId;
        [JsonProperty("labelEn", Required = Required.Always)] public string LabelEn;
        [JsonProperty("labelZh", Required = Required.Always)] public string LabelZh;
        [JsonProperty("strength", Required = Required.Always)] public int Strength;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class CaseGalaxyData
    {
        [JsonProperty("umapX", Required = Required.Always)] public double UmapX;
        [JsonProperty("umapY", Required = Required.Always)] public double UmapY;
        [JsonProperty("displayX", Required = Required.Always)] public double DisplayX;
        [JsonProperty("displayY", Required = Required.Always)] public double DisplayY;
        [JsonProperty("nearbyReferences", Required = Required.Always)] public NearbyReferenceData[] NearbyReferences;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class NearbyReferenceData
    {
        [JsonProperty("modelId", Required = Required.Always)] public string ModelId;
        [JsonProperty("cellLineName", Required = Required.Always)] public string CellLineName;
        [JsonProperty("classId", Required = Required.Always)] public string ClassId;
        [JsonProperty("labelEn", Required = Required.Always)] public string LabelEn;
        [JsonProperty("labelZh", Required = Required.Always)] public string LabelZh;
        [JsonProperty("distance", Required = Required.Always)] public double Distance;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class GalaxyNodesDocument
    {
        [JsonProperty("metadata", Required = Required.Always)] public GalaxyMetadata Metadata;
        [JsonProperty("nodes", Required = Required.Always)] public GalaxyNodeData[] Nodes;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class GalaxyMetadata
    {
        [JsonProperty("version", Required = Required.Always)] public string Version;
        [JsonProperty("trainReferenceNodes", Required = Required.Always)] public int TrainReferenceNodes;
        [JsonProperty("caseNodes", Required = Required.Always)] public int CaseNodes;
        [JsonProperty("totalNodes", Required = Required.Always)] public int TotalNodes;
        [JsonProperty("method", Required = Required.Always)] public string Method;
        [JsonProperty("displayCoordinateRange", Required = Required.Always)] public double[] DisplayCoordinateRange;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class GalaxyNodeData
    {
        [JsonProperty("nodeId", Required = Required.Always)] public string NodeId;
        [JsonProperty("modelId", Required = Required.Always)] public string ModelId;
        [JsonProperty("cellLineName", Required = Required.Always)] public string CellLineName;
        [JsonProperty("nodeType", Required = Required.Always)] public string NodeType;

        // Reference Nodes use these exact leading-capital JSON field names.
        [JsonProperty("ClassId")] public string ReferenceClassId;
        [JsonProperty("ClassLabelEn")] public string ReferenceClassLabelEn;
        [JsonProperty("ClassLabelZh")] public string ReferenceClassLabelZh;

        // Case Nodes use the camelCase true-class fields below.
        [JsonProperty("caseId")] public string CaseId;
        [JsonProperty("trueClassId")] public string TrueClassId;
        [JsonProperty("trueClassLabelEn")] public string TrueClassLabelEn;
        [JsonProperty("trueClassLabelZh")] public string TrueClassLabelZh;

        [JsonProperty("umapX", Required = Required.Always)] public double UmapX;
        [JsonProperty("umapY", Required = Required.Always)] public double UmapY;
        [JsonProperty("displayX", Required = Required.Always)] public double DisplayX;
        [JsonProperty("displayY", Required = Required.Always)] public double DisplayY;
        [JsonProperty("isDiscovered", Required = Required.Always)] public bool IsDiscovered;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ClassProfilesDocument
    {
        [JsonProperty("metadata", Required = Required.Always)] public ClassProfilesMetadata Metadata;
        [JsonProperty("classes", Required = Required.Always)] public ClassProfileData[] Classes;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ClassProfilesMetadata
    {
        [JsonProperty("version", Required = Required.Always)] public string Version;
        [JsonProperty("classCount", Required = Required.Always)] public int ClassCount;
        [JsonProperty("trainingExpressionCluesNote", Required = Required.Always)] public string TrainingExpressionCluesNote;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ClassProfileData
    {
        [JsonProperty("classId", Required = Required.Always)] public string ClassId;
        [JsonProperty("labelEn", Required = Required.Always)] public string LabelEn;
        [JsonProperty("labelZh", Required = Required.Always)] public string LabelZh;
        [JsonProperty("trainSampleCount", Required = Required.Always)] public int TrainSampleCount;
        [JsonProperty("testSampleCount", Required = Required.Always)] public int TestSampleCount;
        [JsonProperty("ai", Required = Required.Always)] public ClassAiMetricsData Ai;
        [JsonProperty("commonConfusions", Required = Required.Always)] public CommonConfusionData[] CommonConfusions;
        [JsonProperty("trainingExpressionClues", Required = Required.Always)] public TrainingExpressionClueData[] TrainingExpressionClues;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ClassAiMetricsData
    {
        [JsonProperty("precision", Required = Required.Always)] public double Precision;
        [JsonProperty("recall", Required = Required.Always)] public double Recall;
        [JsonProperty("f1", Required = Required.Always)] public double F1;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class CommonConfusionData
    {
        [JsonProperty("classId", Required = Required.Always)] public string ClassId;
        [JsonProperty("labelEn", Required = Required.Always)] public string LabelEn;
        [JsonProperty("labelZh", Required = Required.Always)] public string LabelZh;
        [JsonProperty("errorCount", Required = Required.Always)] public int ErrorCount;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class TrainingExpressionClueData
    {
        [JsonProperty("gene", Required = Required.Always)] public string Gene;
        [JsonProperty("direction", Required = Required.Always)] public string Direction;
        [JsonProperty("strength", Required = Required.Always)] public string Strength;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class GameConfigData
    {
        [JsonProperty("version", Required = Required.Always)] public string Version;
        [JsonProperty("shift", Required = Required.Always)] public ShiftConfigData Shift;
        [JsonProperty("costs", Required = Required.Always)] public ToolCostsConfigData Costs;
        [JsonProperty("investigation", Required = Required.Always)] public InvestigationConfigData Investigation;
        [JsonProperty("rewards", Required = Required.Always)] public RewardsConfigData Rewards;
        [JsonProperty("tutorial", Required = Required.Always)] public TutorialConfigData Tutorial;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ShiftConfigData
    {
        [JsonProperty("casesPerShift", Required = Required.Always)] public int CasesPerShift;
        [JsonProperty("startingRP", Required = Required.Always)] public int StartingRp;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ToolCostsConfigData
    {
        [JsonProperty("geneScan", Required = Required.Always)] public int GeneScan;
        [JsonProperty("cancerGalaxy", Required = Required.Always)] public int CancerGalaxy;
        [JsonProperty("aiAssistant", Required = Required.Always)] public int AiAssistant;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class InvestigationConfigData
    {
        [JsonProperty("initialClueCount", Required = Required.Always)] public int InitialClueCount;
        [JsonProperty("geneScanClueCount", Required = Required.Always)] public int GeneScanClueCount;
        [JsonProperty("maxGeneScanUsesPerCase", Required = Required.Always)] public int MaxGeneScanUsesPerCase;
        [JsonProperty("maxCancerGalaxyUsesPerCase", Required = Required.Always)] public int MaxCancerGalaxyUsesPerCase;
        [JsonProperty("maxAIAssistantUsesPerCase", Required = Required.Always)] public int MaxAiAssistantUsesPerCase;
        [JsonProperty("minimumRP", Required = Required.Always)] public int MinimumRp;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class RewardsConfigData
    {
        [JsonProperty("correctDiagnosisScore", Required = Required.Always)] public int CorrectDiagnosisScore;
        [JsonProperty("wrongDiagnosisScore", Required = Required.Always)] public int WrongDiagnosisScore;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class TutorialConfigData
    {
        [JsonProperty("investigationToolsCostRP", Required = Required.Always)] public int InvestigationToolsCostRp;
    }
}
