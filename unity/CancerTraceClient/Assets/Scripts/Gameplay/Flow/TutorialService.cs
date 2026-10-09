using System;
using System.Collections.Generic;
using CancerTrace.Data.Models;
using CancerTrace.Data.Repositories;
using CancerTrace.Data.Save;
using CancerTrace.Gameplay.Flow;

namespace CancerTrace.Gameplay.Tutorial
{
    /// <summary>
    /// Owns the temporary Tutorial session and resolves its fixed Case through read-only repositories.
    /// </summary>
    public sealed class TutorialService
    {
        public const string FixedTutorialCaseId = "CASE_147";
        public const int GeneScanStepIndex = 2;
        public const int GeneScanEvidenceStepIndex = 3;
        public const int CancerGalaxyStepIndex = 4;
        public const int CancerGalaxyReviewStepIndex = 5;
        public const int AiReadyStepIndex = 6;
        public const int AiEvidenceStepIndex = 7;
        public const int DiagnosisStepIndex = 8;
        public const int SubmitStepIndex = 9;
        public const int ResultSummaryStepIndex = 10;

        private static readonly string[] DiagnosisClassIds =
        {
            "lung", "skin", "cns_brain", "bowel", "esophagus_stomach",
            "breast", "bone", "ovary_fallopian_tube"
        };

        private readonly CaseRepository cases;
        private readonly GalaxyRepository galaxy;
        private readonly GameConfigRepository config;
        private readonly TutorialProgressRepository progress;

        public TutorialService(
            CaseRepository cases,
            GalaxyRepository galaxy,
            GameConfigRepository config,
            TutorialProgressRepository progress)
        {
            if (cases == null) throw new ArgumentNullException("cases");
            if (galaxy == null) throw new ArgumentNullException("galaxy");
            if (config == null) throw new ArgumentNullException("config");
            if (progress == null) throw new ArgumentNullException("progress");

            this.cases = cases;
            this.galaxy = galaxy;
            this.config = config;
            this.progress = progress;
        }

        public TutorialSessionState CurrentSession { get; private set; }

        public bool IsTutorialActive
        {
            get { return CurrentSession != null && CurrentSession.IsActive; }
        }

        public bool TutorialCompleted
        {
            get { return progress.TutorialCompleted; }
        }

        public bool TryStartTutorial(out string errorMessage)
        {
            GameCaseData gameCase = cases.GetByCaseId(FixedTutorialCaseId);
            if (gameCase == null)
            {
                errorMessage = "无法读取固定教学病例：" + FixedTutorialCaseId;
                return false;
            }

            CurrentSession = new TutorialSessionState(FixedTutorialCaseId);
            errorMessage = null;
            return true;
        }

        public bool TryGetCurrentCase(out TutorialCaseView view, out string errorMessage)
        {
            view = null;
            if (!IsTutorialActive)
            {
                errorMessage = "当前没有活动中的教学会话。";
                return false;
            }

            GameCaseData gameCase = cases.GetByCaseId(CurrentSession.CaseId);
            if (gameCase == null)
            {
                errorMessage = "教学病例不存在：" + CurrentSession.CaseId;
                return false;
            }

            IList<ClueView> geneScanClues = CurrentSession.GeneScanUsed
                ? GameplayViewFactory.BuildClues(gameCase.GeneScanClues)
                : new List<ClueView>();
            CancerGalaxyView galaxyEvidence = CurrentSession.GalaxyUsed
                ? GameplayViewFactory.BuildCancerGalaxy(gameCase, galaxy, config.StartingRp)
                : null;
            AiAssistantView aiEvidence = CurrentSession.AiUsed
                ? GameplayViewFactory.BuildAiAssistant(gameCase, config.StartingRp)
                : null;
            view = new TutorialCaseView(
                gameCase.CaseId,
                gameCase.CellLineName,
                config.StartingRp,
                GameplayViewFactory.BuildClues(gameCase.InitialClues),
                CurrentSession.GeneScanUsed,
                geneScanClues,
                CurrentSession.GalaxyUsed,
                galaxyEvidence,
                CurrentSession.AiUsed,
                aiEvidence);
            errorMessage = null;
            return true;
        }

        public bool TryAdvanceNextStep(out string errorMessage)
        {
            if (!IsTutorialActive)
            {
                errorMessage = "当前没有活动中的教学会话。";
                return false;
            }
            int stepIndex = CurrentSession.CurrentStepIndex;
            bool canAdvance = stepIndex == 0 || stepIndex == 1 ||
                              stepIndex == GeneScanEvidenceStepIndex ||
                              stepIndex == AiEvidenceStepIndex;
            if (!canAdvance)
            {
                errorMessage = "当前教学步骤不能通过下一步按钮推进。";
                return false;
            }

            CurrentSession.CurrentStepIndex += 1;
            errorMessage = null;
            return true;
        }

        public bool TryUseCancerGalaxy(out CancerGalaxyView view, out string errorMessage)
        {
            view = null;
            GameCaseData gameCase;
            if (!TryGetGalaxyCaseForStep(CancerGalaxyStepIndex, out gameCase, out errorMessage))
            {
                return false;
            }
            if (CurrentSession.GalaxyUsed)
            {
                errorMessage = "教学星图已经查看过了。";
                return false;
            }

            view = GameplayViewFactory.BuildCancerGalaxy(gameCase, galaxy, config.StartingRp);
            CurrentSession.GalaxyUsed = true;
            CurrentSession.CurrentStepIndex = CancerGalaxyReviewStepIndex;
            errorMessage = null;
            return true;
        }

        public bool TryGetCancerGalaxyView(out CancerGalaxyView view, out string errorMessage)
        {
            view = null;
            GameCaseData gameCase;
            if (!TryGetGalaxyCaseForStep(-1, out gameCase, out errorMessage)) return false;
            if (!CurrentSession.GalaxyUsed)
            {
                errorMessage = "教学星图尚未解锁。";
                return false;
            }

            view = GameplayViewFactory.BuildCancerGalaxy(gameCase, galaxy, config.StartingRp);
            errorMessage = null;
            return true;
        }

        public bool TryCompleteCancerGalaxy(out string errorMessage)
        {
            if (!IsTutorialActive || !CurrentSession.GalaxyUsed ||
                CurrentSession.CurrentStepIndex != CancerGalaxyReviewStepIndex)
            {
                errorMessage = "当前教学状态不能完成星图讲解。";
                return false;
            }

            CurrentSession.CurrentStepIndex = AiReadyStepIndex;
            errorMessage = null;
            return true;
        }

        public bool TryUseAiAssistant(out AiAssistantView view, out string errorMessage)
        {
            view = null;
            if (!IsTutorialActive)
            {
                errorMessage = "当前没有活动中的教学会话。";
                return false;
            }
            if (!string.Equals(CurrentSession.CaseId, FixedTutorialCaseId, StringComparison.Ordinal))
            {
                errorMessage = "教学病例与固定病例不一致。";
                return false;
            }
            if (CurrentSession.CurrentStepIndex != AiReadyStepIndex ||
                !CurrentSession.GeneScanUsed || !CurrentSession.GalaxyUsed)
            {
                errorMessage = "请先完成当前教学步骤。";
                return false;
            }
            if (CurrentSession.AiUsed)
            {
                errorMessage = "教学 AI 辅助已经使用过了。";
                return false;
            }

            GameCaseData gameCase = cases.GetByCaseId(CurrentSession.CaseId);
            if (gameCase == null || gameCase.Ai == null || gameCase.Ai.Top3 == null ||
                gameCase.Ai.Top3.Length != 3)
            {
                errorMessage = "教学病例没有可用的 AI 辅助结果。";
                return false;
            }

            view = GameplayViewFactory.BuildAiAssistant(gameCase, config.StartingRp);
            CurrentSession.AiUsed = true;
            CurrentSession.CurrentStepIndex = AiEvidenceStepIndex;
            errorMessage = null;
            return true;
        }

        public bool SelectDiagnosis(string classId, out string errorMessage)
        {
            if (!IsTutorialActive)
            {
                errorMessage = "当前没有活动中的教学会话。";
                return false;
            }
            if (CurrentSession.Submitted)
            {
                errorMessage = "教学病例已经提交。";
                return false;
            }
            if (CurrentSession.CurrentStepIndex != DiagnosisStepIndex &&
                CurrentSession.CurrentStepIndex != SubmitStepIndex)
            {
                errorMessage = "请先完成当前教学步骤。";
                return false;
            }
            if (!IsDiagnosisClass(classId))
            {
                errorMessage = "请选择有效的癌症来源。";
                return false;
            }

            CurrentSession.DraftDiagnosis = classId;
            if (CurrentSession.CurrentStepIndex == DiagnosisStepIndex)
            {
                CurrentSession.CurrentStepIndex = SubmitStepIndex;
            }
            errorMessage = null;
            return true;
        }

        public bool SubmitDiagnosis(out ResultView view, out string errorMessage)
        {
            view = null;
            if (!IsTutorialActive)
            {
                errorMessage = "当前没有活动中的教学会话。";
                return false;
            }
            if (CurrentSession.Submitted)
            {
                errorMessage = "教学病例已经提交。";
                return false;
            }
            if (CurrentSession.CurrentStepIndex != SubmitStepIndex ||
                !IsDiagnosisClass(CurrentSession.DraftDiagnosis))
            {
                errorMessage = "请先选择一个有效的诊断结果。";
                return false;
            }

            GameCaseData gameCase = cases.GetByCaseId(CurrentSession.CaseId);
            if (gameCase == null || gameCase.Ai == null || gameCase.Ai.Top3 == null)
            {
                errorMessage = "教学病例结果数据不可用。";
                return false;
            }

            bool isCorrect = string.Equals(
                CurrentSession.DraftDiagnosis,
                gameCase.TrueClassId,
                StringComparison.Ordinal);
            int scoreEarned = isCorrect ? config.CorrectDiagnosisScore : config.WrongDiagnosisScore;
            view = new ResultView(
                gameCase.CaseId,
                CurrentSession.DraftDiagnosis,
                gameCase.TrueClassId,
                gameCase.TrueClassLabelEn,
                gameCase.TrueClassLabelZh,
                isCorrect,
                scoreEarned,
                0,
                config.StartingRp,
                GameplayViewFactory.BuildAiAssistant(gameCase, config.StartingRp),
                false,
                null);

            CurrentSession.Submitted = true;
            CurrentSession.Result = view;
            CurrentSession.CurrentStepIndex = ResultSummaryStepIndex;
            errorMessage = null;
            return true;
        }

        public bool TryGetResult(out ResultView view, out string errorMessage)
        {
            view = null;
            if (!IsTutorialActive || !CurrentSession.Submitted || CurrentSession.Result == null)
            {
                errorMessage = "当前没有可显示的教学结果。";
                return false;
            }

            view = CurrentSession.Result;
            errorMessage = null;
            return true;
        }

        private static bool IsDiagnosisClass(string classId)
        {
            for (int index = 0; index < DiagnosisClassIds.Length; index++)
            {
                if (string.Equals(DiagnosisClassIds[index], classId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private bool TryGetGalaxyCaseForStep(
            int requiredStepIndex,
            out GameCaseData gameCase,
            out string errorMessage)
        {
            gameCase = null;
            if (!IsTutorialActive)
            {
                errorMessage = "当前没有活动中的教学会话。";
                return false;
            }
            if (!string.Equals(CurrentSession.CaseId, FixedTutorialCaseId, StringComparison.Ordinal))
            {
                errorMessage = "教学病例与固定病例不一致。";
                return false;
            }
            if (requiredStepIndex >= 0 && CurrentSession.CurrentStepIndex != requiredStepIndex)
            {
                errorMessage = "请先完成当前教学步骤。";
                return false;
            }

            gameCase = cases.GetByCaseId(CurrentSession.CaseId);
            if (gameCase == null || gameCase.Galaxy == null ||
                gameCase.Galaxy.NearbyReferences == null)
            {
                errorMessage = "教学病例没有可用的癌症星图数据。";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool TryUseGeneScan(out string errorMessage)
        {
            if (!IsTutorialActive)
            {
                errorMessage = "当前没有活动中的教学会话。";
                return false;
            }
            if (CurrentSession.CurrentStepIndex != GeneScanStepIndex)
            {
                errorMessage = "请先完成当前教学步骤。";
                return false;
            }
            if (CurrentSession.GeneScanUsed)
            {
                errorMessage = "教学扫描已经完成。";
                return false;
            }

            GameCaseData gameCase = cases.GetByCaseId(CurrentSession.CaseId);
            if (gameCase == null || gameCase.GeneScanClues == null || gameCase.GeneScanClues.Length != 3)
            {
                errorMessage = "教学病例没有可用的 3 条扫描线索。";
                return false;
            }

            CurrentSession.GeneScanUsed = true;
            CurrentSession.CurrentStepIndex = GeneScanEvidenceStepIndex;
            errorMessage = null;
            return true;
        }

        public bool CompleteTutorial(out string errorMessage)
        {
            if (!IsTutorialActive || !CurrentSession.Submitted || CurrentSession.Result == null ||
                CurrentSession.CurrentStepIndex != ResultSummaryStepIndex)
            {
                errorMessage = "当前教学状态不能标记为完成。";
                return false;
            }
            if (!progress.TryMarkCompleted(out errorMessage)) return false;

            ClearSession();
            return true;
        }

        public void CancelTutorial()
        {
            ClearSession();
        }

        public void EndTutorial()
        {
            CancelTutorial();
        }

        private void ClearSession()
        {
            if (CurrentSession != null) CurrentSession.IsActive = false;
            CurrentSession = null;
        }
    }
}
