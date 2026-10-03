# 脚本架构说明

## 数据流

1. 启动时从 `StreamingAssets/GameData/` 读取版本化 JSON。
2. Data 层负责反序列化、结构校验和只读访问。
3. Core 与 Gameplay 层根据配置和病例数据维护游戏状态。
4. UI 层订阅或读取状态，并负责展示和用户交互。

核心原则是“先读 JSON，再驱动 UI”。病例、类别、概率、线索、证据、Galaxy 坐标和游戏规则都不应硬编码进 UI。

## 分层职责

- `Core`：应用启动、场景流程、Shift 与病例生命周期、全局服务。
- `Data`：JSON 模型、加载器、校验器和数据仓库。
- `UI`：视图组件、面板控制、中文文本与交互反馈。
- `Gameplay`：线索解锁、调查消费、诊断判定、Score 和调查评价。
- `Debug`：开发期数据浏览、测试入口和诊断日志。

各模块尽量通过清晰接口协作，避免单个脚本同时承担数据读取、规则计算和界面控制。

## v1.0 Data Layer 实现

数据层位于 `Assets/Scripts/Data/`，使用项目现有的 `com.unity.nuget.newtonsoft-json` 解析正式 JSON。

### 类与职责

- `GameDataModels`：声明四个正式 JSON 的可序列化模型、固定枚举及字符串映射。Galaxy Reference Node 的 `ClassId`、`ClassLabelEn`、`ClassLabelZh` 与 Case Node 的 `trueClassId`、`trueClassLabelEn`、`trueClassLabelZh` 分别显式映射，不修改源 Schema。
- `GameDataLoader`：从 `StreamingAssets/GameData/` 读取四个文件，严格反序列化并调用 `DataValidator`。文件缺失、解析失败或验证失败时返回失败并输出 `Debug.LogError`，不会创建默认数据。
- `DataValidator`：校验文件版本、数量、唯一 ID、枚举、线索数量、概率范围、Galaxy 坐标与近邻、跨文件引用以及冻结的 v1.0 配置。
- `GameDataContext`：只在四个文件全部加载并验证通过后创建，统一提供 Repository 访问入口。
- `CaseRepository`：按 `caseId` 查询 Case，并提供全部 Case 的只读集合和总数。
- `ClassProfileRepository`：按 `classId` 查询类别档案，并提供全部类别的只读集合。
- `GalaxyRepository`：按 `nodeId` 或 `caseId` 查询 Galaxy Node，并提供全部节点的只读集合。
- `GameConfigRepository`：通过只读属性提供 Shift、RP、调查次数、Score 与 Tutorial 配置。
- `GameDataSmokeTest`：开发期最小加载入口，仅验证正式 JSON、数量与核心配置，不包含 UI 或 Gameplay。

### 实际数据流

1. `GameDataLoader` 读取 `StreamingAssets/GameData/` 中的四个只读 JSON。
2. Newtonsoft.Json 按模型上的明确字段映射执行严格反序列化；必填字段缺失、字段为非法 null 或出现未声明字段时失败。
3. `DataValidator` 执行内容校验和跨文件一致性校验。
4. 仅在验证结果有效时创建 `GameDataContext` 与四个 Repository。
5. 后续 Core、Gameplay 与 UI 通过 Repository 读取静态数据，不直接读取或写回 JSON。

## v1.0 Runtime State 与 SaveData 实现

Runtime 与存档层建立在已验证的只读 `GameDataContext` 之上，不复制或改写正式科学数据。

### 实际类与职责

- `Gameplay/Runtime/GameRuntimeState`：保存当前 Shift、固定的 `selectedCaseIds`、RP、Score、计数、当前病例状态、已完成病例结果和 Tutorial 完成状态。
- `Gameplay/Runtime/CurrentCaseRuntimeState`：仅保存 `caseId`、三个工具使用标记、最终诊断提交状态、该病例 RP 消耗与得分；完整病例数据始终按 `caseId` 从 `CaseRepository` 查询。
- `Gameplay/Runtime/CompletedCaseResult`：保存病例结算所需的最小快照，包括玩家诊断、真实类别、正确性、得分、RP 消耗和工具使用状态。
- `Gameplay/Runtime/GameRuntimeService`：负责新 Shift 初始化、同一 Shift 内无重复的随机抽取、工具使用保护、RP 消费、诊断提交、病例推进、Score 累计及状态变更后的自动保存。
- `Data/Save/SaveDataV1`：Newtonsoft.Json 的严格存档 DTO，`saveVersion` 固定为 `1.0`。
- `Data/Save/SaveDataRepository`：负责 `Save`、`Load`、`HasSave`、`DeleteSave`、`ValidateSave`，并完成 Runtime State 与 SaveData 的双向映射。
- `Debug/RuntimeSaveSmokeTest`：可从组件 Context Menu 或 Unity Editor 批处理入口运行，不依赖正式 Scene 或 UI。

### Runtime 流程

1. `GameDataLoader` 成功创建只读 Repository 后，才能创建 `SaveDataRepository` 与 `GameRuntimeService`。
2. 新 Shift 从 `CaseRepository` 的全部病例中进行部分 Fisher-Yates 洗牌，抽取配置规定数量的唯一 `caseId`；顺序写入 Runtime State 后立即固定并自动保存。
3. 工具成本、Shift 初始 RP、每 Shift 病例数与诊断得分全部读取 `GameConfigRepository`，Runtime 层不重复硬编码正式配置值。
4. 工具使用、诊断提交、进入下一病例及 Shift 结束都会触发自动保存。恢复时直接采用存档中的 `selectedCaseIds`，不会重新随机。
5. Runtime State 不保存 `difficulty`，也不提供提交前的 ANOMALY 展示状态；需要结算展示时再通过 `caseId` 查询静态病例。

### Save / Load 流程

保存流程：`GameRuntimeState -> SaveDataV1 -> 严格校验 -> JSON -> 临时文件 -> 替换正式存档`。

加载流程：`JSON -> 严格反序列化 -> 字段及跨字段校验 -> GameRuntimeState`。非法或损坏存档返回失败和明确错误，不自动补值、不重新随机病例，也不伪造修复。

默认存档为 `Application.persistentDataPath/cancertrace_save_v1.json`。写入时优先使用同目录临时文件和平台原生文件替换；平台不支持替换 API 时使用覆盖复制作为兼容回退。正式 JSON 仍只从 `StreamingAssets/GameData/` 读取。

### 验证状态

- Unity 2021.3.45f2c1 C# 编译通过。
- Runtime/Save Smoke Test 通过：Shift 初始化与唯一抽取、RP/Score 初值、工具和诊断保护、保存/恢复一致性、完成结果恢复、RP 不足拒绝、损坏存档拒绝及删除存档均已验证。
