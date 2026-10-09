using System.Collections.Generic;
using CancerTrace.Data.Models;
using CancerTrace.Data.Repositories;

namespace CancerTrace.Gameplay.Flow
{
    /// <summary>
    /// Maps read-only scientific data to UI-safe views shared by formal gameplay and Tutorial.
    /// </summary>
    internal static class GameplayViewFactory
    {
        internal static List<ClueView> BuildClues(ClueData[] source)
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

        internal static CancerGalaxyView BuildCancerGalaxy(
            GameCaseData gameCase,
            GalaxyRepository galaxy,
            int remainingRp)
        {
            List<GalaxyReferenceNodeView> referenceNodes = new List<GalaxyReferenceNodeView>();
            IReadOnlyList<GalaxyNodeData> allNodes = galaxy.GetAll();
            for (int index = 0; index < allNodes.Count; index++)
            {
                GalaxyNodeData node = allNodes[index];
                if (!string.Equals(node.NodeType, "REFERENCE", System.StringComparison.Ordinal)) continue;
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
                remainingRp);
        }

        internal static AiAssistantView BuildAiAssistant(GameCaseData gameCase, int remainingRp)
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
                remainingRp);
        }
    }
}
