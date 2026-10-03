using System;
using System.Collections.Generic;
using CancerTrace.Data.Models;

namespace CancerTrace.Data.Validation
{
    public static class DataValidator
    {
        private const string GameCasesFile = "game_cases.json";
        private const string GalaxyNodesFile = "galaxy_nodes.json";
        private const string ClassProfilesFile = "class_profiles.json";
        private const string GameConfigFile = "game_config.json";

        private const int ExpectedCaseCount = 162;
        private const int ExpectedGalaxyNodeCount = 807;
        private const int ExpectedReferenceNodeCount = 645;
        private const int ExpectedClassCount = 8;
        private const double ProbabilityTolerance = 0.00001d;
        private const double CoordinateTolerance = 0.000001d;

        public static DataValidationResult Validate(
            GameCasesDocument gameCases,
            GalaxyNodesDocument galaxyNodes,
            ClassProfilesDocument classProfiles,
            GameConfigData gameConfig)
        {
            DataValidationResult result = new DataValidationResult();
            Dictionary<string, ClassProfileData> profilesById = ValidateClassProfiles(classProfiles, result);
            Dictionary<string, GameCaseData> casesById = ValidateGameCases(gameCases, profilesById, result);
            ValidateGalaxyNodes(galaxyNodes, casesById, profilesById, result);
            ValidateGameConfig(gameConfig, gameCases, result);
            return result;
        }

        private static Dictionary<string, ClassProfileData> ValidateClassProfiles(
            ClassProfilesDocument document,
            DataValidationResult result)
        {
            Dictionary<string, ClassProfileData> profiles =
                new Dictionary<string, ClassProfileData>(StringComparer.Ordinal);

            if (document == null)
            {
                result.Add(ClassProfilesFile, "$", "non-null document", null);
                return profiles;
            }

            if (document.Metadata == null)
            {
                result.Add(ClassProfilesFile, "metadata", "non-null object", null);
            }
            else
            {
                RequireVersion(ClassProfilesFile, "metadata.version", document.Metadata.Version, result);
                RequireEqual(ClassProfilesFile, "metadata.classCount", ExpectedClassCount, document.Metadata.ClassCount, result);
                RequireNotBlank(
                    ClassProfilesFile,
                    "metadata.trainingExpressionCluesNote",
                    document.Metadata.TrainingExpressionCluesNote,
                    result);
            }

            if (document.Classes == null)
            {
                result.Add(ClassProfilesFile, "classes", "array with exactly 8 entries", null);
                return profiles;
            }

            RequireEqual(ClassProfilesFile, "classes.length", ExpectedClassCount, document.Classes.Length, result);

            for (int index = 0; index < document.Classes.Length; index++)
            {
                ClassProfileData profile = document.Classes[index];
                string path = "classes[" + index + "]";
                if (profile == null)
                {
                    result.Add(ClassProfilesFile, path, "non-null class profile", null);
                    continue;
                }

                if (!ValidateCancerType(ClassProfilesFile, path + ".classId", profile.ClassId, result))
                {
                    continue;
                }

                if (profiles.ContainsKey(profile.ClassId))
                {
                    result.Add(ClassProfilesFile, path + ".classId", "unique classId", profile.ClassId);
                }
                else
                {
                    profiles.Add(profile.ClassId, profile);
                }

                RequireNotBlank(ClassProfilesFile, path + ".labelEn", profile.LabelEn, result);
                RequireNotBlank(ClassProfilesFile, path + ".labelZh", profile.LabelZh, result);
                RequireNonNegative(ClassProfilesFile, path + ".trainSampleCount", profile.TrainSampleCount, result);
                RequireNonNegative(ClassProfilesFile, path + ".testSampleCount", profile.TestSampleCount, result);

                if (profile.Ai == null)
                {
                    result.Add(ClassProfilesFile, path + ".ai", "non-null object", null);
                }
                else
                {
                    RequireProbability(ClassProfilesFile, path + ".ai.precision", profile.Ai.Precision, result);
                    RequireProbability(ClassProfilesFile, path + ".ai.recall", profile.Ai.Recall, result);
                    RequireProbability(ClassProfilesFile, path + ".ai.f1", profile.Ai.F1, result);
                }

                if (profile.CommonConfusions == null)
                {
                    result.Add(ClassProfilesFile, path + ".commonConfusions", "non-null array", null);
                }
                else
                {
                    for (int confusionIndex = 0; confusionIndex < profile.CommonConfusions.Length; confusionIndex++)
                    {
                        CommonConfusionData confusion = profile.CommonConfusions[confusionIndex];
                        string confusionPath = path + ".commonConfusions[" + confusionIndex + "]";
                        if (confusion == null)
                        {
                            result.Add(ClassProfilesFile, confusionPath, "non-null entry", null);
                            continue;
                        }

                        ValidateCancerType(ClassProfilesFile, confusionPath + ".classId", confusion.ClassId, result);
                        RequireNotBlank(ClassProfilesFile, confusionPath + ".labelEn", confusion.LabelEn, result);
                        RequireNotBlank(ClassProfilesFile, confusionPath + ".labelZh", confusion.LabelZh, result);
                        if (confusion.ErrorCount <= 0)
                        {
                            result.Add(ClassProfilesFile, confusionPath + ".errorCount", "positive integer", confusion.ErrorCount);
                        }
                    }
                }

                if (profile.TrainingExpressionClues == null)
                {
                    result.Add(ClassProfilesFile, path + ".trainingExpressionClues", "array with 5 entries", null);
                }
                else
                {
                    RequireEqual(
                        ClassProfilesFile,
                        path + ".trainingExpressionClues.length",
                        5,
                        profile.TrainingExpressionClues.Length,
                        result);
                    for (int clueIndex = 0; clueIndex < profile.TrainingExpressionClues.Length; clueIndex++)
                    {
                        TrainingExpressionClueData clue = profile.TrainingExpressionClues[clueIndex];
                        string cluePath = path + ".trainingExpressionClues[" + clueIndex + "]";
                        if (clue == null)
                        {
                            result.Add(ClassProfilesFile, cluePath, "non-null entry", null);
                            continue;
                        }

                        RequireNotBlank(ClassProfilesFile, cluePath + ".gene", clue.Gene, result);
                        ValidateClueState(ClassProfilesFile, cluePath + ".direction", clue.Direction, result);
                        ValidateStrength(ClassProfilesFile, cluePath + ".strength", clue.Strength, result);
                    }
                }
            }

            foreach (string classId in GameDataValueParser.CancerTypeIds)
            {
                if (!profiles.ContainsKey(classId))
                {
                    result.Add(ClassProfilesFile, "classes", "profile for every CancerType", "missing " + classId);
                }
            }

            return profiles;
        }

        private static Dictionary<string, GameCaseData> ValidateGameCases(
            GameCasesDocument document,
            IDictionary<string, ClassProfileData> profiles,
            DataValidationResult result)
        {
            Dictionary<string, GameCaseData> cases =
                new Dictionary<string, GameCaseData>(StringComparer.Ordinal);
            HashSet<string> modelIds = new HashSet<string>(StringComparer.Ordinal);

            if (document == null)
            {
                result.Add(GameCasesFile, "$", "non-null document", null);
                return cases;
            }

            if (document.Metadata == null)
            {
                result.Add(GameCasesFile, "metadata", "non-null object", null);
            }
            else
            {
                RequireVersion(GameCasesFile, "metadata.version", document.Metadata.Version, result);
                RequireEqual(GameCasesFile, "metadata.totalCases", ExpectedCaseCount, document.Metadata.TotalCases, result);
                RequireEqual(GameCasesFile, "metadata.classCount", ExpectedClassCount, document.Metadata.ClassCount, result);
                RequireProbability(GameCasesFile, "metadata.aiTestAccuracy", document.Metadata.AiTestAccuracy, result);
                RequireNotBlank(GameCasesFile, "metadata.generatedBy", document.Metadata.GeneratedBy, result);
                RequireNotBlank(GameCasesFile, "metadata.dataSourceNote", document.Metadata.DataSourceNote, result);
                RequireNotBlank(GameCasesFile, "metadata.scientificDisclaimer", document.Metadata.ScientificDisclaimer, result);
                RequireNotBlank(GameCasesFile, "metadata.scientificDisclaimerZh", document.Metadata.ScientificDisclaimerZh, result);
                if (document.Metadata.ScientificIntegrity == null || document.Metadata.ScientificIntegrity.Length == 0)
                {
                    result.Add(GameCasesFile, "metadata.scientificIntegrity", "non-empty array", null);
                }
            }

            if (document.Cases == null)
            {
                result.Add(GameCasesFile, "cases", "non-empty array", null);
                return cases;
            }

            if (document.Cases.Length == 0)
            {
                result.Add(GameCasesFile, "cases.length", "greater than 0", 0);
            }
            RequireEqual(GameCasesFile, "cases.length", ExpectedCaseCount, document.Cases.Length, result);
            if (document.Metadata != null)
            {
                RequireEqual(
                    GameCasesFile,
                    "metadata.totalCases == cases.length",
                    document.Cases.Length,
                    document.Metadata.TotalCases,
                    result);
            }

            for (int index = 0; index < document.Cases.Length; index++)
            {
                GameCaseData gameCase = document.Cases[index];
                string path = "cases[" + index + "]";
                if (gameCase == null)
                {
                    result.Add(GameCasesFile, path, "non-null case", null);
                    continue;
                }

                if (RequireNotBlank(GameCasesFile, path + ".caseId", gameCase.CaseId, result))
                {
                    if (cases.ContainsKey(gameCase.CaseId))
                    {
                        result.Add(GameCasesFile, path + ".caseId", "unique caseId", gameCase.CaseId);
                    }
                    else
                    {
                        cases.Add(gameCase.CaseId, gameCase);
                    }
                }

                if (RequireNotBlank(GameCasesFile, path + ".modelId", gameCase.ModelId, result) &&
                    !modelIds.Add(gameCase.ModelId))
                {
                    result.Add(GameCasesFile, path + ".modelId", "unique modelId", gameCase.ModelId);
                }

                RequireNotBlank(GameCasesFile, path + ".cellLineName", gameCase.CellLineName, result);
                ValidateCancerType(GameCasesFile, path + ".trueClassId", gameCase.TrueClassId, result);
                ValidateClassLabels(
                    GameCasesFile,
                    path + ".trueClassId",
                    gameCase.TrueClassId,
                    gameCase.TrueClassLabelEn,
                    gameCase.TrueClassLabelZh,
                    profiles,
                    result);
                ValidateDifficulty(GameCasesFile, path + ".difficulty", gameCase.Difficulty, result);
                RequireProbability(GameCasesFile, path + ".difficultyScore", gameCase.DifficultyScore, result);
                ValidateAi(gameCase, path, profiles, result);
                ValidateClues(gameCase.InitialClues, 2, path + ".initialClues", profiles, result);
                ValidateClues(gameCase.GeneScanClues, 3, path + ".geneScanClues", profiles, result);
                ValidateEvidence(gameCase.Evidence, path + ".evidence", profiles, result);
                ValidateCaseGalaxy(gameCase.Galaxy, path + ".galaxy", profiles, result);

                if (gameCase.Ai != null)
                {
                    bool shouldBeAnomaly = !gameCase.Ai.Correct && gameCase.Ai.Confidence >= 0.80d;
                    bool isAnomaly = string.Equals(gameCase.Difficulty, "ANOMALY", StringComparison.Ordinal);
                    if (shouldBeAnomaly != isAnomaly)
                    {
                        result.Add(
                            GameCasesFile,
                            path + ".difficulty",
                            "ANOMALY iff AI is wrong and confidence >= 0.80",
                            gameCase.Difficulty);
                    }
                }
            }

            return cases;
        }

        private static void ValidateAi(
            GameCaseData gameCase,
            string casePath,
            IDictionary<string, ClassProfileData> profiles,
            DataValidationResult result)
        {
            string path = casePath + ".ai";
            CaseAiData ai = gameCase.Ai;
            if (ai == null)
            {
                result.Add(GameCasesFile, path, "non-null object", null);
                return;
            }

            ValidateCancerType(GameCasesFile, path + ".predictedClassId", ai.PredictedClassId, result);
            ValidateClassLabels(
                GameCasesFile,
                path + ".predictedClassId",
                ai.PredictedClassId,
                ai.PredictedClassLabelEn,
                ai.PredictedClassLabelZh,
                profiles,
                result);
            RequireProbability(GameCasesFile, path + ".confidence", ai.Confidence, result);

            bool expectedCorrect = string.Equals(gameCase.TrueClassId, ai.PredictedClassId, StringComparison.Ordinal);
            if (ai.Correct != expectedCorrect)
            {
                result.Add(GameCasesFile, path + ".correct", expectedCorrect, ai.Correct);
            }

            if (ai.Top3 == null)
            {
                result.Add(GameCasesFile, path + ".top3", "array with exactly 3 entries", null);
            }
            else
            {
                RequireEqual(GameCasesFile, path + ".top3.length", 3, ai.Top3.Length, result);
                double previous = double.PositiveInfinity;
                for (int index = 0; index < ai.Top3.Length; index++)
                {
                    AiCandidateData candidate = ai.Top3[index];
                    string candidatePath = path + ".top3[" + index + "]";
                    if (candidate == null)
                    {
                        result.Add(GameCasesFile, candidatePath, "non-null candidate", null);
                        continue;
                    }

                    ValidateCancerType(GameCasesFile, candidatePath + ".classId", candidate.ClassId, result);
                    ValidateClassLabels(
                        GameCasesFile,
                        candidatePath + ".classId",
                        candidate.ClassId,
                        candidate.LabelEn,
                        candidate.LabelZh,
                        profiles,
                        result);
                    RequireProbability(GameCasesFile, candidatePath + ".probability", candidate.Probability, result);
                    if (candidate.Probability > previous + ProbabilityTolerance)
                    {
                        result.Add(GameCasesFile, candidatePath + ".probability", "non-increasing order", candidate.Probability);
                    }
                    previous = candidate.Probability;
                }

                if (ai.Top3.Length > 0 && ai.Top3[0] != null &&
                    !string.Equals(ai.Top3[0].ClassId, ai.PredictedClassId, StringComparison.Ordinal))
                {
                    result.Add(GameCasesFile, path + ".top3[0].classId", ai.PredictedClassId, ai.Top3[0].ClassId);
                }
            }

            if (ai.Probabilities == null)
            {
                result.Add(GameCasesFile, path + ".probabilities", "all 8 CancerType probabilities", null);
                return;
            }

            double total = 0d;
            foreach (KeyValuePair<string, double> probability in ai.Probabilities.Enumerate())
            {
                RequireProbability(
                    GameCasesFile,
                    path + ".probabilities." + probability.Key,
                    probability.Value,
                    result);
                total += probability.Value;
            }

            if (Math.Abs(total - 1d) > ProbabilityTolerance)
            {
                result.Add(GameCasesFile, path + ".probabilities", "sum within 1e-5 of 1", total);
            }

            double predictedProbability;
            if (ai.Probabilities.TryGetValue(ai.PredictedClassId, out predictedProbability) &&
                Math.Abs(predictedProbability - ai.Confidence) > ProbabilityTolerance)
            {
                result.Add(GameCasesFile, path + ".confidence", predictedProbability, ai.Confidence);
            }
        }

        private static void ValidateClues(
            ClueData[] clues,
            int expectedCount,
            string path,
            IDictionary<string, ClassProfileData> profiles,
            DataValidationResult result)
        {
            if (clues == null)
            {
                result.Add(GameCasesFile, path, "array with exactly " + expectedCount + " entries", null);
                return;
            }

            RequireEqual(GameCasesFile, path + ".length", expectedCount, clues.Length, result);
            for (int index = 0; index < clues.Length; index++)
            {
                ClueData clue = clues[index];
                string cluePath = path + "[" + index + "]";
                if (clue == null)
                {
                    result.Add(GameCasesFile, cluePath, "non-null clue", null);
                    continue;
                }

                RequireNotBlank(GameCasesFile, cluePath + ".gene", clue.Gene, result);
                RequireNotBlank(GameCasesFile, cluePath + ".geneColumn", clue.GeneColumn, result);
                RequireFinite(GameCasesFile, cluePath + ".expressionValue", clue.ExpressionValue, result);
                ValidateClueState(GameCasesFile, cluePath + ".state", clue.State, result);
                if (clue.Support == null || clue.Support.Length < 1 || clue.Support.Length > 2)
                {
                    result.Add(
                        GameCasesFile,
                        cluePath + ".support",
                        "array with 1 or 2 entries",
                        clue.Support == null ? null : (object)clue.Support.Length);
                    continue;
                }

                for (int supportIndex = 0; supportIndex < clue.Support.Length; supportIndex++)
                {
                    ClueSupportData support = clue.Support[supportIndex];
                    string supportPath = cluePath + ".support[" + supportIndex + "]";
                    if (support == null)
                    {
                        result.Add(GameCasesFile, supportPath, "non-null support", null);
                        continue;
                    }

                    ValidateCancerType(GameCasesFile, supportPath + ".classId", support.ClassId, result);
                    ValidateClassLabels(
                        GameCasesFile,
                        supportPath + ".classId",
                        support.ClassId,
                        support.LabelEn,
                        support.LabelZh,
                        profiles,
                        result);
                    ValidateStrength(GameCasesFile, supportPath + ".strength", support.Strength, result);
                }
            }
        }

        private static void ValidateEvidence(
            CaseEvidenceData evidence,
            string path,
            IDictionary<string, ClassProfileData> profiles,
            DataValidationResult result)
        {
            if (evidence == null)
            {
                result.Add(GameCasesFile, path, "non-null object", null);
                return;
            }

            ValidateEvidenceEntries(evidence.InitialTop3, path + ".initialTop3", profiles, result);
            ValidateEvidenceEntries(evidence.AfterGeneScanTop3, path + ".afterGeneScanTop3", profiles, result);
        }

        private static void ValidateEvidenceEntries(
            EvidenceEntryData[] entries,
            string path,
            IDictionary<string, ClassProfileData> profiles,
            DataValidationResult result)
        {
            if (entries == null)
            {
                result.Add(GameCasesFile, path, "array with exactly 3 entries", null);
                return;
            }

            RequireEqual(GameCasesFile, path + ".length", 3, entries.Length, result);
            for (int index = 0; index < entries.Length; index++)
            {
                EvidenceEntryData entry = entries[index];
                string entryPath = path + "[" + index + "]";
                if (entry == null)
                {
                    result.Add(GameCasesFile, entryPath, "non-null entry", null);
                    continue;
                }

                ValidateCancerType(GameCasesFile, entryPath + ".classId", entry.ClassId, result);
                ValidateClassLabels(
                    GameCasesFile,
                    entryPath + ".classId",
                    entry.ClassId,
                    entry.LabelEn,
                    entry.LabelZh,
                    profiles,
                    result);
                if (entry.Strength < 0 || entry.Strength > 4)
                {
                    result.Add(GameCasesFile, entryPath + ".strength", "integer in [0,4]", entry.Strength);
                }
            }
        }

        private static void ValidateCaseGalaxy(
            CaseGalaxyData galaxy,
            string path,
            IDictionary<string, ClassProfileData> profiles,
            DataValidationResult result)
        {
            if (galaxy == null)
            {
                result.Add(GameCasesFile, path, "non-null object", null);
                return;
            }

            RequireFinite(GameCasesFile, path + ".umapX", galaxy.UmapX, result);
            RequireFinite(GameCasesFile, path + ".umapY", galaxy.UmapY, result);
            RequireDisplayCoordinate(GameCasesFile, path + ".displayX", galaxy.DisplayX, result);
            RequireDisplayCoordinate(GameCasesFile, path + ".displayY", galaxy.DisplayY, result);

            if (galaxy.NearbyReferences == null)
            {
                result.Add(GameCasesFile, path + ".nearbyReferences", "array with exactly 5 entries", null);
                return;
            }

            RequireEqual(GameCasesFile, path + ".nearbyReferences.length", 5, galaxy.NearbyReferences.Length, result);
            double previousDistance = double.NegativeInfinity;
            for (int index = 0; index < galaxy.NearbyReferences.Length; index++)
            {
                NearbyReferenceData reference = galaxy.NearbyReferences[index];
                string referencePath = path + ".nearbyReferences[" + index + "]";
                if (reference == null)
                {
                    result.Add(GameCasesFile, referencePath, "non-null reference", null);
                    continue;
                }

                RequireNotBlank(GameCasesFile, referencePath + ".modelId", reference.ModelId, result);
                RequireNotBlank(GameCasesFile, referencePath + ".cellLineName", reference.CellLineName, result);
                ValidateCancerType(GameCasesFile, referencePath + ".classId", reference.ClassId, result);
                ValidateClassLabels(
                    GameCasesFile,
                    referencePath + ".classId",
                    reference.ClassId,
                    reference.LabelEn,
                    reference.LabelZh,
                    profiles,
                    result);
                RequireFinite(GameCasesFile, referencePath + ".distance", reference.Distance, result);
                if (reference.Distance < 0d)
                {
                    result.Add(GameCasesFile, referencePath + ".distance", "non-negative", reference.Distance);
                }
                if (reference.Distance + CoordinateTolerance < previousDistance)
                {
                    result.Add(GameCasesFile, referencePath + ".distance", "non-decreasing order", reference.Distance);
                }
                previousDistance = reference.Distance;
            }
        }

        private static void ValidateGalaxyNodes(
            GalaxyNodesDocument document,
            IDictionary<string, GameCaseData> cases,
            IDictionary<string, ClassProfileData> profiles,
            DataValidationResult result)
        {
            if (document == null)
            {
                result.Add(GalaxyNodesFile, "$", "non-null document", null);
                return;
            }

            if (document.Metadata == null)
            {
                result.Add(GalaxyNodesFile, "metadata", "non-null object", null);
            }
            else
            {
                RequireVersion(GalaxyNodesFile, "metadata.version", document.Metadata.Version, result);
                RequireEqual(
                    GalaxyNodesFile,
                    "metadata.trainReferenceNodes",
                    ExpectedReferenceNodeCount,
                    document.Metadata.TrainReferenceNodes,
                    result);
                RequireEqual(GalaxyNodesFile, "metadata.caseNodes", ExpectedCaseCount, document.Metadata.CaseNodes, result);
                RequireEqual(
                    GalaxyNodesFile,
                    "metadata.totalNodes",
                    ExpectedGalaxyNodeCount,
                    document.Metadata.TotalNodes,
                    result);
                RequireNotBlank(GalaxyNodesFile, "metadata.method", document.Metadata.Method, result);
                if (document.Metadata.DisplayCoordinateRange == null ||
                    document.Metadata.DisplayCoordinateRange.Length != 2 ||
                    Math.Abs(document.Metadata.DisplayCoordinateRange[0] + 1d) > CoordinateTolerance ||
                    Math.Abs(document.Metadata.DisplayCoordinateRange[1] - 1d) > CoordinateTolerance)
                {
                    result.Add(GalaxyNodesFile, "metadata.displayCoordinateRange", "[-1,1]", "invalid range");
                }
            }

            if (document.Nodes == null)
            {
                result.Add(GalaxyNodesFile, "nodes", "non-empty array", null);
                return;
            }

            if (document.Nodes.Length == 0)
            {
                result.Add(GalaxyNodesFile, "nodes.length", "greater than 0", 0);
            }
            RequireEqual(GalaxyNodesFile, "nodes.length", ExpectedGalaxyNodeCount, document.Nodes.Length, result);
            if (document.Metadata != null)
            {
                RequireEqual(
                    GalaxyNodesFile,
                    "metadata.totalNodes == nodes.length",
                    document.Nodes.Length,
                    document.Metadata.TotalNodes,
                    result);
            }

            HashSet<string> nodeIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> caseNodeIds = new HashSet<string>(StringComparer.Ordinal);
            int referenceCount = 0;
            int caseCount = 0;

            for (int index = 0; index < document.Nodes.Length; index++)
            {
                GalaxyNodeData node = document.Nodes[index];
                string path = "nodes[" + index + "]";
                if (node == null)
                {
                    result.Add(GalaxyNodesFile, path, "non-null node", null);
                    continue;
                }

                if (RequireNotBlank(GalaxyNodesFile, path + ".nodeId", node.NodeId, result) &&
                    !nodeIds.Add(node.NodeId))
                {
                    result.Add(GalaxyNodesFile, path + ".nodeId", "unique nodeId", node.NodeId);
                }
                RequireNotBlank(GalaxyNodesFile, path + ".modelId", node.ModelId, result);
                RequireNotBlank(GalaxyNodesFile, path + ".cellLineName", node.CellLineName, result);
                RequireFinite(GalaxyNodesFile, path + ".umapX", node.UmapX, result);
                RequireFinite(GalaxyNodesFile, path + ".umapY", node.UmapY, result);
                RequireDisplayCoordinate(GalaxyNodesFile, path + ".displayX", node.DisplayX, result);
                RequireDisplayCoordinate(GalaxyNodesFile, path + ".displayY", node.DisplayY, result);

                GalaxyNodeType nodeType;
                if (!GameDataValueParser.TryParseNodeType(node.NodeType, out nodeType))
                {
                    result.Add(GalaxyNodesFile, path + ".nodeType", "REFERENCE or CASE", node.NodeType);
                    continue;
                }

                if (nodeType == GalaxyNodeType.Reference)
                {
                    referenceCount++;
                    ValidateCancerType(GalaxyNodesFile, path + ".ClassId", node.ReferenceClassId, result);
                    ValidateClassLabels(
                        GalaxyNodesFile,
                        path + ".ClassId",
                        node.ReferenceClassId,
                        node.ReferenceClassLabelEn,
                        node.ReferenceClassLabelZh,
                        profiles,
                        result);
                    RequireNull(GalaxyNodesFile, path + ".caseId", node.CaseId, result);
                    RequireNull(GalaxyNodesFile, path + ".trueClassId", node.TrueClassId, result);
                }
                else
                {
                    caseCount++;
                    if (RequireNotBlank(GalaxyNodesFile, path + ".caseId", node.CaseId, result) &&
                        !caseNodeIds.Add(node.CaseId))
                    {
                        result.Add(GalaxyNodesFile, path + ".caseId", "unique Case Node caseId", node.CaseId);
                    }
                    ValidateCancerType(GalaxyNodesFile, path + ".trueClassId", node.TrueClassId, result);
                    ValidateClassLabels(
                        GalaxyNodesFile,
                        path + ".trueClassId",
                        node.TrueClassId,
                        node.TrueClassLabelEn,
                        node.TrueClassLabelZh,
                        profiles,
                        result);
                    RequireNull(GalaxyNodesFile, path + ".ClassId", node.ReferenceClassId, result);

                    GameCaseData matchingCase;
                    if (!string.IsNullOrWhiteSpace(node.CaseId) && cases.TryGetValue(node.CaseId, out matchingCase))
                    {
                        RequireEqual(GalaxyNodesFile, path + ".modelId", matchingCase.ModelId, node.ModelId, result);
                        RequireEqual(GalaxyNodesFile, path + ".trueClassId", matchingCase.TrueClassId, node.TrueClassId, result);
                        if (matchingCase.Galaxy != null)
                        {
                            RequireNear(GalaxyNodesFile, path + ".umapX", matchingCase.Galaxy.UmapX, node.UmapX, result);
                            RequireNear(GalaxyNodesFile, path + ".umapY", matchingCase.Galaxy.UmapY, node.UmapY, result);
                            RequireNear(GalaxyNodesFile, path + ".displayX", matchingCase.Galaxy.DisplayX, node.DisplayX, result);
                            RequireNear(GalaxyNodesFile, path + ".displayY", matchingCase.Galaxy.DisplayY, node.DisplayY, result);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(node.CaseId))
                    {
                        result.Add(GalaxyNodesFile, path + ".caseId", "caseId present in game_cases.json", node.CaseId);
                    }
                }
            }

            RequireEqual(GalaxyNodesFile, "REFERENCE node count", ExpectedReferenceNodeCount, referenceCount, result);
            RequireEqual(GalaxyNodesFile, "CASE node count", ExpectedCaseCount, caseCount, result);
        }

        private static void ValidateGameConfig(
            GameConfigData config,
            GameCasesDocument gameCases,
            DataValidationResult result)
        {
            if (config == null)
            {
                result.Add(GameConfigFile, "$", "non-null document", null);
                return;
            }

            RequireVersion(GameConfigFile, "version", config.Version, result);
            if (config.Shift == null)
            {
                result.Add(GameConfigFile, "shift", "non-null object", null);
            }
            else
            {
                RequireEqual(GameConfigFile, "shift.casesPerShift", 10, config.Shift.CasesPerShift, result);
                RequireEqual(GameConfigFile, "shift.startingRP", 200, config.Shift.StartingRp, result);
                if (gameCases != null && gameCases.Cases != null && config.Shift.CasesPerShift > gameCases.Cases.Length)
                {
                    result.Add(
                        GameConfigFile,
                        "shift.casesPerShift",
                        "less than or equal to case count",
                        config.Shift.CasesPerShift);
                }
            }

            if (config.Costs == null)
            {
                result.Add(GameConfigFile, "costs", "non-null object", null);
            }
            else
            {
                RequireEqual(GameConfigFile, "costs.geneScan", 10, config.Costs.GeneScan, result);
                RequireEqual(GameConfigFile, "costs.cancerGalaxy", 25, config.Costs.CancerGalaxy, result);
                RequireEqual(GameConfigFile, "costs.aiAssistant", 40, config.Costs.AiAssistant, result);
                RequireNonNegative(GameConfigFile, "costs.geneScan", config.Costs.GeneScan, result);
                RequireNonNegative(GameConfigFile, "costs.cancerGalaxy", config.Costs.CancerGalaxy, result);
                RequireNonNegative(GameConfigFile, "costs.aiAssistant", config.Costs.AiAssistant, result);
            }

            if (config.Investigation == null)
            {
                result.Add(GameConfigFile, "investigation", "non-null object", null);
            }
            else
            {
                RequireEqual(GameConfigFile, "investigation.initialClueCount", 2, config.Investigation.InitialClueCount, result);
                RequireEqual(GameConfigFile, "investigation.geneScanClueCount", 3, config.Investigation.GeneScanClueCount, result);
                RequireEqual(GameConfigFile, "investigation.maxGeneScanUsesPerCase", 1, config.Investigation.MaxGeneScanUsesPerCase, result);
                RequireEqual(GameConfigFile, "investigation.maxCancerGalaxyUsesPerCase", 1, config.Investigation.MaxCancerGalaxyUsesPerCase, result);
                RequireEqual(GameConfigFile, "investigation.maxAIAssistantUsesPerCase", 1, config.Investigation.MaxAiAssistantUsesPerCase, result);
                RequireEqual(GameConfigFile, "investigation.minimumRP", 0, config.Investigation.MinimumRp, result);
            }

            if (config.Rewards == null)
            {
                result.Add(GameConfigFile, "rewards", "non-null object", null);
            }
            else
            {
                RequireEqual(GameConfigFile, "rewards.correctDiagnosisScore", 100, config.Rewards.CorrectDiagnosisScore, result);
                RequireEqual(GameConfigFile, "rewards.wrongDiagnosisScore", 0, config.Rewards.WrongDiagnosisScore, result);
            }

            if (config.Tutorial == null)
            {
                result.Add(GameConfigFile, "tutorial", "non-null object", null);
            }
            else
            {
                RequireEqual(GameConfigFile, "tutorial.investigationToolsCostRP", 0, config.Tutorial.InvestigationToolsCostRp, result);
            }
        }

        private static bool ValidateCancerType(string file, string path, string value, DataValidationResult result)
        {
            CancerType parsed;
            if (GameDataValueParser.TryParseCancerType(value, out parsed))
            {
                return true;
            }

            result.Add(file, path, "one of the 8 frozen CancerType IDs", value);
            return false;
        }

        private static void ValidateDifficulty(string file, string path, string value, DataValidationResult result)
        {
            CaseDifficulty parsed;
            if (!GameDataValueParser.TryParseDifficulty(value, out parsed))
            {
                result.Add(file, path, "EASY, NORMAL, HARD, or ANOMALY", value);
            }
        }

        private static void ValidateClueState(string file, string path, string value, DataValidationResult result)
        {
            ClueState parsed;
            if (!GameDataValueParser.TryParseClueState(value, out parsed))
            {
                result.Add(file, path, "HIGH or LOW", value);
            }
        }

        private static void ValidateStrength(string file, string path, string value, DataValidationResult result)
        {
            EvidenceStrength parsed;
            if (!GameDataValueParser.TryParseStrength(value, out parsed))
            {
                result.Add(file, path, "Weak, Moderate, or Strong", value);
            }
        }

        private static void ValidateClassLabels(
            string file,
            string path,
            string classId,
            string labelEn,
            string labelZh,
            IDictionary<string, ClassProfileData> profiles,
            DataValidationResult result)
        {
            RequireNotBlank(file, path.Replace("classId", "labelEn").Replace("ClassId", "ClassLabelEn"), labelEn, result);
            RequireNotBlank(file, path.Replace("classId", "labelZh").Replace("ClassId", "ClassLabelZh"), labelZh, result);

            ClassProfileData profile;
            if (!string.IsNullOrWhiteSpace(classId) && profiles.TryGetValue(classId, out profile))
            {
                RequireEqual(file, path + " labelEn", profile.LabelEn, labelEn, result);
                RequireEqual(file, path + " labelZh", profile.LabelZh, labelZh, result);
            }
        }

        private static bool RequireNotBlank(
            string file,
            string path,
            string value,
            DataValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            result.Add(file, path, "non-empty string", value);
            return false;
        }

        private static void RequireVersion(string file, string path, string value, DataValidationResult result)
        {
            RequireEqual(file, path, "1.0", value, result);
        }

        private static void RequireProbability(string file, string path, double value, DataValidationResult result)
        {
            if (!IsFinite(value) || value < 0d || value > 1d)
            {
                result.Add(file, path, "finite number in [0,1]", value);
            }
        }

        private static void RequireDisplayCoordinate(string file, string path, double value, DataValidationResult result)
        {
            if (!IsFinite(value) || value < -1d || value > 1d)
            {
                result.Add(file, path, "finite number in [-1,1]", value);
            }
        }

        private static void RequireFinite(string file, string path, double value, DataValidationResult result)
        {
            if (!IsFinite(value))
            {
                result.Add(file, path, "finite number", value);
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void RequireNonNegative(string file, string path, int value, DataValidationResult result)
        {
            if (value < 0)
            {
                result.Add(file, path, "non-negative integer", value);
            }
        }

        private static void RequireEqual(string file, string path, int expected, int actual, DataValidationResult result)
        {
            if (expected != actual)
            {
                result.Add(file, path, expected, actual);
            }
        }

        private static void RequireEqual(string file, string path, string expected, string actual, DataValidationResult result)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                result.Add(file, path, expected, actual);
            }
        }

        private static void RequireNear(string file, string path, double expected, double actual, DataValidationResult result)
        {
            if (!IsFinite(actual) || Math.Abs(expected - actual) > CoordinateTolerance)
            {
                result.Add(file, path, expected, actual);
            }
        }

        private static void RequireNull(string file, string path, string actual, DataValidationResult result)
        {
            if (actual != null)
            {
                result.Add(file, path, "field absent for this nodeType", actual);
            }
        }
    }
}
