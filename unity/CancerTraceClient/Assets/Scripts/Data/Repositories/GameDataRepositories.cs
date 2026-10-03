using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CancerTrace.Data.Models;

namespace CancerTrace.Data.Repositories
{
    public sealed class CaseRepository
    {
        private readonly Dictionary<string, GameCaseData> byCaseId;
        private readonly ReadOnlyCollection<GameCaseData> allCases;

        internal CaseRepository(GameCaseData[] cases)
        {
            GameCaseData[] snapshot = (GameCaseData[])cases.Clone();
            allCases = Array.AsReadOnly(snapshot);
            byCaseId = new Dictionary<string, GameCaseData>(snapshot.Length, StringComparer.Ordinal);
            foreach (GameCaseData gameCase in snapshot)
            {
                byCaseId.Add(gameCase.CaseId, gameCase);
            }
        }

        public int Count
        {
            get { return allCases.Count; }
        }

        public IReadOnlyList<GameCaseData> GetAll()
        {
            return allCases;
        }

        public bool TryGetByCaseId(string caseId, out GameCaseData gameCase)
        {
            return byCaseId.TryGetValue(caseId, out gameCase);
        }

        public GameCaseData GetByCaseId(string caseId)
        {
            GameCaseData gameCase;
            return byCaseId.TryGetValue(caseId, out gameCase) ? gameCase : null;
        }
    }

    public sealed class ClassProfileRepository
    {
        private readonly Dictionary<string, ClassProfileData> byClassId;
        private readonly ReadOnlyCollection<ClassProfileData> allProfiles;

        internal ClassProfileRepository(ClassProfileData[] profiles)
        {
            ClassProfileData[] snapshot = (ClassProfileData[])profiles.Clone();
            allProfiles = Array.AsReadOnly(snapshot);
            byClassId = new Dictionary<string, ClassProfileData>(snapshot.Length, StringComparer.Ordinal);
            foreach (ClassProfileData profile in snapshot)
            {
                byClassId.Add(profile.ClassId, profile);
            }
        }

        public int Count
        {
            get { return allProfiles.Count; }
        }

        public IReadOnlyList<ClassProfileData> GetAll()
        {
            return allProfiles;
        }

        public bool TryGetByClassId(string classId, out ClassProfileData profile)
        {
            return byClassId.TryGetValue(classId, out profile);
        }

        public ClassProfileData GetByClassId(string classId)
        {
            ClassProfileData profile;
            return byClassId.TryGetValue(classId, out profile) ? profile : null;
        }
    }

    public sealed class GalaxyRepository
    {
        private readonly Dictionary<string, GalaxyNodeData> byNodeId;
        private readonly Dictionary<string, GalaxyNodeData> byCaseId;
        private readonly ReadOnlyCollection<GalaxyNodeData> allNodes;

        internal GalaxyRepository(GalaxyNodeData[] nodes)
        {
            GalaxyNodeData[] snapshot = (GalaxyNodeData[])nodes.Clone();
            allNodes = Array.AsReadOnly(snapshot);
            byNodeId = new Dictionary<string, GalaxyNodeData>(snapshot.Length, StringComparer.Ordinal);
            byCaseId = new Dictionary<string, GalaxyNodeData>(StringComparer.Ordinal);
            foreach (GalaxyNodeData node in snapshot)
            {
                byNodeId.Add(node.NodeId, node);
                if (string.Equals(node.NodeType, "CASE", StringComparison.Ordinal))
                {
                    byCaseId.Add(node.CaseId, node);
                }
            }
        }

        public int Count
        {
            get { return allNodes.Count; }
        }

        public IReadOnlyList<GalaxyNodeData> GetAll()
        {
            return allNodes;
        }

        public bool TryGetByNodeId(string nodeId, out GalaxyNodeData node)
        {
            return byNodeId.TryGetValue(nodeId, out node);
        }

        public bool TryGetCaseNode(string caseId, out GalaxyNodeData node)
        {
            return byCaseId.TryGetValue(caseId, out node);
        }

        public GalaxyNodeData GetByNodeId(string nodeId)
        {
            GalaxyNodeData node;
            return byNodeId.TryGetValue(nodeId, out node) ? node : null;
        }

        public GalaxyNodeData GetCaseNode(string caseId)
        {
            GalaxyNodeData node;
            return byCaseId.TryGetValue(caseId, out node) ? node : null;
        }
    }

    public sealed class GameConfigRepository
    {
        private readonly GameConfigData config;

        internal GameConfigRepository(GameConfigData config)
        {
            this.config = config;
        }

        public string Version { get { return config.Version; } }
        public int CasesPerShift { get { return config.Shift.CasesPerShift; } }
        public int StartingRp { get { return config.Shift.StartingRp; } }
        public int GeneScanCost { get { return config.Costs.GeneScan; } }
        public int CancerGalaxyCost { get { return config.Costs.CancerGalaxy; } }
        public int AiAssistantCost { get { return config.Costs.AiAssistant; } }
        public int InitialClueCount { get { return config.Investigation.InitialClueCount; } }
        public int GeneScanClueCount { get { return config.Investigation.GeneScanClueCount; } }
        public int MaxGeneScanUsesPerCase { get { return config.Investigation.MaxGeneScanUsesPerCase; } }
        public int MaxCancerGalaxyUsesPerCase { get { return config.Investigation.MaxCancerGalaxyUsesPerCase; } }
        public int MaxAiAssistantUsesPerCase { get { return config.Investigation.MaxAiAssistantUsesPerCase; } }
        public int MinimumRp { get { return config.Investigation.MinimumRp; } }
        public int CorrectDiagnosisScore { get { return config.Rewards.CorrectDiagnosisScore; } }
        public int WrongDiagnosisScore { get { return config.Rewards.WrongDiagnosisScore; } }
        public int TutorialInvestigationToolsCostRp { get { return config.Tutorial.InvestigationToolsCostRp; } }
    }
}
