# 存档系统

状态：v1.0 Runtime State / SaveData 已实现并通过 Smoke Test
规则来源：`../Game_Design_Document_v1.0.md` 第 20 节

## 已确认规则

- v1.0采用本地存档。
- 不需要用户账号。
- 不需要云端同步。
- 保存当前Shift、当前Case、剩余RP、完成病例数量和当前Score。

## 存档位置与版本

- 默认路径：`Application.persistentDataPath/cancertrace_save_v1.json`。
- `saveVersion` 固定为 `"1.0"`。
- 存档不放入 `StreamingAssets`，也不写回四个正式 JSON。

## SaveData v1

根对象保存：

- `saveVersion`
- `currentShift`
- `currentCaseIndex`
- `selectedCaseIds`
- `remainingRP`
- `currentScore`
- `completedCaseCount`
- `correctCaseCount`
- `currentCaseState`
- `completedCaseResults`
- `tutorialCompleted`

`currentCaseState` 仅保存当前 `caseId`、三个工具使用状态、`selectedDiagnosis`、`diagnosisSubmitted`、`rpSpent` 与 `scoreEarned`。`completedCaseResults` 保存每个已结算病例的最小结果快照。完整病例、AI、基因与 Galaxy 科学数据不进入存档，均通过 `caseId` 从只读 Repository 查询。

## 保存与恢复

- 新 Shift 创建后自动保存。
- Gene Scan、Cancer Galaxy 或 AI Assistant 使用成功后自动保存。
- Diagnosis 正式提交后自动保存。
- 进入下一个 Case 后自动保存。
- Shift 结束时自动保存。
- 恢复时使用存档内已经固定的 `selectedCaseIds` 和顺序，不重新抽取病例，也不保存 Random Seed。
- Tutorial 完成状态随 SaveData 保存；正式 Shift 创建前设置的完成状态会在该 Shift 首次存档时写入。

## 校验与失败行为

加载前执行严格 JSON 映射与内容校验，包括：

- `saveVersion == "1.0"`。
- `selectedCaseIds` 数量等于配置值、Shift 内唯一，且全部存在于 `CaseRepository`。
- `currentCaseIndex`、当前 `caseId`、完成结果顺序与计数关系合法。
- `remainingRP`、当前 Score、完成数和正确数处于合法范围并与结果汇总一致。
- CancerType、真实类别、正确性、得分、工具状态和 RP 消耗均与静态数据及 `GameConfigRepository` 一致。
- `diagnosisSubmitted`、当前病例状态与 `completedCaseResults` 的关系一致。

非法、缺字段、多余字段、无法解析或关系不一致的存档会返回 `Load Failed` 和明确错误。系统不会自动伪造修复；后续 UI 可据此提供新游戏或删除损坏存档选项。

## 原子写入

保存时先在同目录写入 `.tmp` 文件。已有正式存档时优先使用平台文件替换 API，并在成功后清理备份；首次保存使用同目录移动。若运行平台不支持替换 API，则采用覆盖复制兼容回退（该回退不具备与原生替换相同的原子性保证）。流程结束时清理临时文件，异常会作为保存失败明确返回。

## 实现与测试

- `SaveDataRepository` 提供 `Save`、`Load`、`HasSave`、`DeleteSave` 与 `ValidateSave`。
- `GameRuntimeService` 在状态变更点调用保存，并从有效存档恢复完整 Runtime State。
- `RuntimeSaveSmokeTest` 已验证 10 个唯一 Case、初始 RP/Score、存取一致性、工具/诊断重复保护、RP 不足保护、完成结果恢复、损坏存档拒绝与删除存档。

本文档不得自行新增或修改GDD中的游戏规则。
