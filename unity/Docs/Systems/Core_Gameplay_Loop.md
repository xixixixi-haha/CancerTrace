# 核心游戏循环

状态：v1.0纯逻辑流程已实现并通过 Smoke Test
规则来源：`../Game_Design_Document_v1.0.md` 第 3、17、18 节

## 已确认规则

1. 获得病例。
2. 查看免费线索。
3. 决定是否消耗科研点（RP）。
4. 可使用 Gene Scan、Cancer Galaxy、AI Assistant 获取额外信息。
5. 免费提交诊断并查看结果；即使RP为0也可提交。
6. 获得调查评价后进入下一病例。
7. 完成 10 个病例后进入 Shift Summary。

第一次启动游戏时先完成Tutorial Case，再进入正式Shift。Tutorial不消耗RP、不计入正式Shift，也不影响正式统计。

Evidence Board自动整理已获得的信息，不增加调查能力、不消耗RP、不影响评分，也不提供额外答案。

## 实际流程接口

核心流程由单一 `GameplayService` 提供：

1. `StartNewShift()`：调用 Runtime 层生成当前 Shift 固定的 10 个唯一 Case，并返回首个安全 `PlayerCaseView`。
2. `GetCurrentCase()`：返回当前病例允许展示的内容，不返回 Static Case。
3. `UseGeneScan()`、`UseCancerGalaxy()`、`UseAiAssistant()`：执行配置成本、次数和 RP 校验，成功后返回对应的已解锁证据。
4. `GetEvidenceBoard()`：按照三个 Runtime 工具标记聚合已解锁证据，不产生新数据或额外消耗。
5. `SelectDiagnosis()`：暂存一个合法的 8 类 CancerType 选择；正式提交后不可修改。
6. `SubmitDiagnosis()`：免费提交并完成 Score、计数、结果记录和自动保存。
7. `GetResult()`：只在提交后返回真实答案、AI 教学复盘与可选 ANOMALY 标记。
8. `NextCase()`：只在当前病例提交后推进 Case 1–9；Case 10 完成后返回 `ShiftCompleted`。
9. `GetShiftSummary()`：只在 10 个 Case 全部完成后生成当前 Shift 汇总。
10. `StartNextShift()`：仅允许从已完成 Shift 显式开始下一个 Shift。

## 操作结果

Gameplay 操作统一返回 `GameplayActionResult<T>`，状态包括：

- `Success`
- `InsufficientRp`
- `AlreadyUsed`
- `AlreadySubmitted`
- `InvalidDiagnosis`
- `ShiftCompleted`
- `InvalidState`

RP 不足、重复使用、重复提交和不合法阶段属于正常失败结果，不使用异常承担流程分支，也不会改变 Runtime State。

## 玩家安全视图

- `PlayerCaseView` 只包含 Case ID、受控展示信息、2 条 Initial Clues、当前 RP/Score 和工具可用状态。
- Gene Scan 成功后只返回正式 JSON 中已有的 3 条额外线索。
- Cancer Galaxy 只返回当前 Case 坐标、Reference Nodes 与 5 个正式 nearbyReferences，不计算推荐诊断。
- AI Assistant 只返回 Prediction、Confidence 与 Top 3，不返回 `ai.correct`。
- Evidence Board 只聚合已经解锁的模块；未购买 AI Assistant 时，即使提交后也不会将 Result AI 复盘倒灌为调查证据。
- Result 在提交前不可获取；真实类别、ANOMALY 和 AI 复盘只在提交后出现。

## Save 接入

流程调用现有 Runtime Service，因此新 Shift、成功使用工具、Diagnosis 提交、Next Case 与 Shift 结束继续自动保存。诊断暂存也会保存。恢复时沿用存档内固定的 `selectedCaseIds`、工具状态、RP、Score 和完成结果，不重新随机当前 Shift。

## 测试状态

- 已完整执行一个 10 Case Shift，并验证 RP/Score 跨 Case 保留、新 Case 工具状态重置和 Case 10 后 Shift 结束。
- 已验证 2 条 Initial Clues、3 条 Gene Scan Clues、5 个 nearbyReferences、AI Top 3 及各工具重复调用保护。
- 已验证不使用工具也可直接提交、RP 为 0 时仍可免费提交、正确 +100、错误 +0、重复提交拒绝。
- 已验证中途保存/恢复后的 Case 顺序、RP、Score、工具状态和 Evidence Board 一致。
- 已使用真实 ANOMALY Case 验证：提交前所有 Gameplay View 均不暴露 ANOMALY、真实类别或 `ai.correct`；提交后 Result 才揭示 ANOMALY。

## 后续 UI 范围

- Scene 页面之间的导航、返回和恢复表现。
- 调查与 Result 的正式 UI 布局、动画、音效和美术接入。
- 按钮根据 `GameplayActionStatus`、工具 `CanUse` 与当前阶段呈现反馈。

本文档不得自行新增或修改 GDD 中的游戏规则。
