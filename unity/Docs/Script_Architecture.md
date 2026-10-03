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
