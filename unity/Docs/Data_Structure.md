# CancerTrace v1.0 数据结构设计

文档状态：正式数据契约  
GDD Version：1.0  
Data Schema Version：1.0  
最后核对日期：2026-10-03

---

## 1. 文档目的

本文档定义 CancerTrace 的正式数据契约，描述以下数据链路：

```text
Python 导出数据
    ↓
JSON
    ↓
Unity Data Layer
    ↓
Gameplay / UI
```

规则优先级：

1. `Game_Design_Document_v1.0.md` 是游戏规则最高依据。
2. `results/game_data/` 中的冻结 JSON 是科学数据字段与数值的事实来源。
3. `Systems/` 文档用于说明具体玩法如何消费数据。
4. 本文档不得改变冻结模型结果、基因线索、UMAP 坐标或真实病例分布。

本文档只定义数据职责，不包含 Unity C# 实现。

---

## 2. 数据层级与所有权

### 2.1 Static Game Data（静态游戏数据）

包括：

- Cases
- AI Prediction
- Gene Clues
- Galaxy coordinates
- Reference Nodes
- Class Profiles
- Game Config

静态游戏数据由正式 JSON 提供。游戏运行期间只读，不得将玩家操作写回这些文件。

### 2.2 Runtime State（游戏运行时状态）

包括当前 Shift、选中的 Case、剩余 RP、Score、工具使用状态和玩家诊断。Runtime State 由 Gameplay 层维护，不属于 Python 科学数据。

### 2.3 Save Data（本地存档数据）

Save Data 是 Runtime State 的可持久化快照。它通过 `caseId` 引用 Static Game Data，不保存完整科学数据副本。

---

## 3. 正式数据文件总览

当前正式源目录：

```text
results/game_data/
```

未来 Unity 读取目录：

```text
unity/CancerTraceClient/Assets/StreamingAssets/GameData/
```

v1.0 正式 JSON 已同步至 `StreamingAssets/GameData/`。`results/game_data/` 仍作为 Python 正式导出源。

|文件|当前内容|顶层结构|运行时职责|
|-|-|-|-|
|`game_cases.json`|162 个独立 Test Set 病例|`metadata`、`cases`|病例、线索、AI、Evidence 与 Case Galaxy 数据|
|`galaxy_nodes.json`|645 个 Reference Node 与 162 个 Case Node|`metadata`、`nodes`|Cancer Galaxy 全量节点|
|`class_profiles.json`|8 个癌症类别档案|`metadata`、`classes`|类别名称、模型指标与训练集表达线索|
|`game_config.json`|v1.0 游戏数值配置|`version` 及配置对象|Shift、RP、工具次数、Score 与 Tutorial 规则|

当前没有其他与 GDD 对应的正式游戏 JSON。

---

## 4. 版本策略

### 4.1 两类版本

- **GDD Version**：游戏设计规则版本，当前为 `1.0`。
- **Data Schema Version**：JSON 字段结构版本，当前为 `1.0`。

两者含义不同。即使字符串相同，也不得将 GDD 版本当作数据格式版本。

### 4.2 当前真实版本字段

|文件|真实版本字段|
|-|-|
|`game_cases.json`|`metadata.version`|
|`galaxy_nodes.json`|`metadata.version`|
|`class_profiles.json`|`metadata.version`|
|`game_config.json`|`version`|

Unity 必须读取现有字段，不为了形式统一而假设所有文件都使用相同路径。遇到不支持的 Data Schema Version 时应停止加载并报告错误。

---

## 5. CancerType 正式枚举

`classId` 是唯一机器存储值。Unity 逻辑不得依赖 `labelEn` 或 `labelZh` 判断类别。

|`classId`|`labelEn`|`labelZh`|
|-|-|-|
|`lung`|Lung|肺|
|`skin`|Skin|皮肤|
|`cns_brain`|CNS/Brain|中枢神经 / 脑|
|`bowel`|Bowel|肠道|
|`esophagus_stomach`|Esophagus/Stomach|食管 / 胃|
|`breast`|Breast|乳腺|
|`bone`|Bone|骨|
|`ovary_fallopian_tube`|Ovary/Fallopian Tube|卵巢 / 输卵管|

约束：

- 所有类别引用必须使用上述 8 个 `classId` 之一。
- `labelEn` 用于英文显示。
- `labelZh` 用于中文显示。
- 同一对象中的 ID 与显示名必须匹配本表。
- Case 的真实类别字段在提交诊断前不得暴露给 Gameplay/UI。

---

## 6. `game_cases.json` 数据契约

### 6.1 当前数据集事实

- 总病例数：162。
- 全部来自冻结的独立 Test Set。
- `caseId` 与 `modelId` 均为 162 个唯一值。
- 不为了游戏平衡而修改科学数据。

|`trueClassId`|数量|
|-|-:|
|`lung`|43|
|`skin`|24|
|`cns_brain`|21|
|`bowel`|17|
|`esophagus_stomach`|16|
|`breast`|14|
|`bone`|14|
|`ovary_fallopian_tube`|13|

### 6.2 `metadata`

|字段|类型|必填|含义与约束|
|-|-|-|-|
|`version`|string|是|Data Schema Version，当前为 `1.0`|
|`generatedBy`|string|是|生成脚本路径|
|`totalCases`|integer|是|必须与 `cases.length` 相等，当前为 162|
|`classCount`|integer|是|必须为 8|
|`dataSourceNote`|string|是|数据来源说明|
|`aiTestAccuracy`|number|是|冻结模型在 Test Set 上的准确率，范围 `[0,1]`|
|`scientificIntegrity`|array&lt;string&gt;|是|科学完整性声明|
|`scientificDisclaimer`|string|是|英文科学免责声明|
|`scientificDisclaimerZh`|string|是|中文科学免责声明|

### 6.3 Case 顶层字段

|字段|类型|必填|可见性/用途|约束与缺失处理|
|-|-|-|-|-|
|`caseId`|string|是|Case 唯一引用键|必须非空且全文件唯一；缺失时阻止加载|
|`modelId`|string|是|连接冻结科学数据与 Galaxy 节点|必须非空且唯一；缺失时阻止加载|
|`cellLineName`|string|是|数据来源标识或受控展示|必须非空|
|`trueClassId`|CancerType string|是|真实答案；结果揭晓前不可见|必须为合法 `classId`|
|`trueClassLabelEn`|string|是|真实答案英文显示|必须与 `trueClassId` 匹配|
|`trueClassLabelZh`|string|是|真实答案中文显示|必须与 `trueClassId` 匹配|
|`ai`|object|是|AI Assistant 的冻结输出|见第 9 节|
|`initialClues`|array&lt;Clue&gt;|是|免费 Initial Clues|长度必须为 2|
|`geneScanClues`|array&lt;Clue&gt;|是|Gene Scan 解锁内容|长度必须为 3|
|`evidence`|object|是|已导出的证据汇总|见第 8.4 节|
|`difficulty`|Difficulty string|是|病例难度或特殊标签|合法值见第 7 节|
|`difficultyScore`|number|是|导出阶段计算的歧义分数|有限数值，当前范围 `[0,1]`；Unity 不重算|
|`galaxy`|object|是|当前 Case 的 Galaxy 坐标与近邻|见第 10 节|

### 6.4 真实类别与 AI Prediction 分离

`trueClassId` 是真实答案，`ai.predictedClassId` 是冻结模型预测。两者允许不同。

Unity 不得：

- 根据 `trueClassId` 修正 AI Prediction；
- 在提交诊断前向玩家暴露真实类别；
- 假设 `ai.correct` 永远为 `true`；
- 重新计算或覆盖模型概率。

---

## 7. Difficulty 正式定义

|内部值|数量|类别|建议显示|
|-|-:|-|-|
|`EASY`|46|普通难度|Easy / 简单|
|`NORMAL`|61|普通难度|Normal / 普通|
|`HARD`|47|普通难度|Hard / 困难|
|`ANOMALY`|8|特殊病例标签|Anomaly Case / 异常案件|

`ANOMALY` 不是普通难度等级。其冻结数据生成条件为：

```text
AI Prediction 错误
且
AI Confidence >= 0.80
```

Unity 必须保留内部值 `ANOMALY`，不得把这 8 个 Case 重新标记、删除或修改。该标签用于体现 AI 即使置信度很高也可能判断错误。

---

## 8. Initial Clues 与 Gene Scan 数据契约

### 8.1 数量规则

- 每个 Case 固定 2 条 `initialClues`，Case 开始即可免费查看。
- 每个 Case 固定 3 条 `geneScanClues`，使用 Gene Scan 后解锁。
- Gene Scan 每个 Case 最多使用一次。
- 一个 Case 最多展示 `2 + 3 = 5` 条基因线索。
- Gene Scan 不是新增 5 条线索。

### 8.2 Clue 字段

`initialClues` 与 `geneScanClues` 使用相同结构。

|字段|类型|必填|含义|约束|
|-|-|-|-|-|
|`gene`|string|是|基因符号|非空|
|`geneColumn`|string|是|导出数据中的完整基因列名|非空；Unity 不解析或改写|
|`expressionValue`|number|是|冻结导出数据中的表达相关数值|必须为有限数值；Unity 只显示，不重新标准化|
|`state`|string|是|预计算表达状态|仅允许 `HIGH`、`LOW`|
|`support`|array&lt;Support&gt;|是|由冻结规则生成的辅助支持信息|当前每条 Clue 为 1–2 项|

`expressionValue` 的精确数学变换没有作为独立字段写入 JSON。Unity 不得猜测其变换底数，不得把它描述为原始 TPM，也不得据此重算 `state`。

### 8.3 Support 字段

|字段|类型|必填|约束|
|-|-|-|-|
|`classId`|CancerType string|是|合法 `classId`|
|`labelEn`|string|是|与 `classId` 匹配|
|`labelZh`|string|是|与 `classId` 匹配|
|`strength`|string|是|仅允许 `Weak`、`Moderate`、`Strong`|

Clue 与 Support 是辅助推理证据，不是直接证明某癌症的最终诊断答案。Unity 不得重算 `HIGH` / `LOW`、`support` 或 `strength`。

### 8.4 Evidence 汇总字段

`evidence` 的真实字段：

|字段|类型|必填|当前约束|
|-|-|-|-|
|`initialTop3`|array&lt;EvidenceEntry&gt;|是|固定 3 条|
|`afterGeneScanTop3`|array&lt;EvidenceEntry&gt;|是|固定 3 条|

每个 `EvidenceEntry` 包含：

|字段|类型|必填|约束|
|-|-|-|-|
|`classId`|CancerType string|是|合法 `classId`|
|`labelEn`|string|是|与 `classId` 匹配|
|`labelZh`|string|是|与 `classId` 匹配|
|`strength`|integer|是|当前范围 `0–4`|

这些值是冻结导出结果。Evidence Board 可以引用它们，但不得把引用行为描述为新增科学数据。

---

## 9. AI Assistant 数据契约

### 9.1 AI 字段

|字段|类型|必填|含义与约束|
|-|-|-|-|
|`predictedClassId`|CancerType string|是|冻结模型 Prediction|
|`predictedClassLabelEn`|string|是|Prediction 英文显示名|
|`predictedClassLabelZh`|string|是|Prediction 中文显示名|
|`confidence`|number|是|Prediction 置信度，范围 `[0,1]`|
|`correct`|boolean|是|Prediction 是否等于真实类别；结果揭晓前不可用于提示玩家|
|`top3`|array&lt;AICandidate&gt;|是|固定 3 条，按 `probability` 降序|
|`probabilities`|object|是|全部 8 类概率，键为正式 `classId`|

### 9.2 Top 3 Candidate

|字段|类型|必填|约束|
|-|-|-|-|
|`classId`|CancerType string|是|合法 `classId`|
|`labelEn`|string|是|与 `classId` 匹配|
|`labelZh`|string|是|与 `classId` 匹配|
|`probability`|number|是|范围 `[0,1]`|

约束：

- `top3.length` 必须为 3。
- `top3` 按 `probability` 非递增排列。
- `top3[0].classId` 必须等于 `predictedClassId`。
- `confidence` 必须与预测类别概率一致，允许仅由 JSON 小数序列化产生的微小误差。
- `probabilities` 必须包含全部 8 个 CancerType 键。
- 全部 8 类概率总和应在浮点容差内接近 1；当前导出脚本使用 `1e-5` 绝对容差。

### 9.3 v1.0 UI 输出

AI Assistant v1.0 固定使用：

1. Prediction
2. Confidence
3. Top 3 Candidates

当前正式 JSON 不存在 AI Explanation。Explanation 不属于 v1.0 必备字段，不得由 Unity 虚构。未来版本如需增加，必须通过新的 Data Schema Version 明确来源。

AI 结果来自冻结模型。Unity 不重新训练、不重新计算概率，也不根据真实标签修正 AI。

---

## 10. Case 内 Cancer Galaxy 数据契约

每个 Case 的 `galaxy` 实际包含：

|字段|类型|必填|含义与约束|
|-|-|-|-|
|`umapX`|number|是|冻结 UMAP 二维 X 坐标；必须为有限数值|
|`umapY`|number|是|冻结 UMAP 二维 Y 坐标；必须为有限数值|
|`displayX`|number|是|归一化显示 X 坐标，范围 `[-1,1]`|
|`displayY`|number|是|归一化显示 Y 坐标，范围 `[-1,1]`|
|`nearbyReferences`|array&lt;NearbyReference&gt;|是|固定 5 个最近 Reference Node，按距离升序|

每个 `NearbyReference` 包含：

|字段|类型|必填|含义与约束|
|-|-|-|-|
|`modelId`|string|是|Reference Node 对应模型 ID|
|`cellLineName`|string|是|Reference Node 细胞系名称|
|`classId`|CancerType string|是|该近邻所属癌症类别|
|`labelEn`|string|是|类别英文名称|
|`labelZh`|string|是|类别中文名称|
|`distance`|number|是|非负、有限的 UMAP 二维空间距离|

Unity 直接使用预计算的 5 个 `nearbyReferences`，不重新执行邻居搜索。

`distance` 表示 UMAP 二维降维空间中的邻近关系，不是 AI Prediction Probability。v1.0 不要求 Top 3 nearest cancer types，也不应从这 5 个节点虚构固定三类结果。

---

## 11. `galaxy_nodes.json` 数据契约

### 11.1 顶层与 metadata

顶层字段为 `metadata` 和 `nodes`。

|metadata 字段|类型|必填|当前值/含义|
|-|-|-|-|
|`version`|string|是|`1.0`|
|`trainReferenceNodes`|integer|是|645|
|`caseNodes`|integer|是|162|
|`totalNodes`|integer|是|807，必须等于前两项之和|
|`method`|string|是|Galaxy 数据处理方法说明|
|`displayCoordinateRange`|array&lt;number&gt;|是|当前为 `[-1,1]`|

### 11.2 Reference Node

Reference Node 数量为 645，`nodeType` 固定为 `REFERENCE`。

|字段|类型|必填|说明|
|-|-|-|-|
|`nodeId`|string|是|节点唯一 ID|
|`modelId`|string|是|科学数据模型 ID|
|`cellLineName`|string|是|细胞系名称|
|`nodeType`|string|是|固定 `REFERENCE`|
|`ClassId`|CancerType string|是|类别 ID；注意首字母大写|
|`ClassLabelEn`|string|是|英文类别名；注意首字母大写|
|`ClassLabelZh`|string|是|中文类别名；注意首字母大写|
|`umapX`|number|是|有限 UMAP X 坐标|
|`umapY`|number|是|有限 UMAP Y 坐标|
|`displayX`|number|是|范围 `[-1,1]`|
|`displayY`|number|是|范围 `[-1,1]`|
|`isDiscovered`|boolean|是|当前导出的初始发现状态|

`ClassId`、`ClassLabelEn`、`ClassLabelZh` 是当前真实 Schema。Data Layer 必须显式映射，不能假装它们已与其他对象的 camelCase 字段统一。

### 11.3 Case Node

Case Node 数量为 162，`nodeType` 固定为 `CASE`。

|字段|类型|必填|说明|
|-|-|-|-|
|`nodeId`|string|是|节点唯一 ID|
|`caseId`|string|是|必须引用 `game_cases.json` 中存在的 Case|
|`modelId`|string|是|必须与对应 Case 的 `modelId` 一致|
|`cellLineName`|string|是|细胞系名称|
|`nodeType`|string|是|固定 `CASE`|
|`trueClassId`|CancerType string|是|真实类别；结果揭晓前不得暴露|
|`trueClassLabelEn`|string|是|真实类别英文名|
|`trueClassLabelZh`|string|是|真实类别中文名|
|`umapX`|number|是|有限 UMAP X 坐标|
|`umapY`|number|是|有限 UMAP Y 坐标|
|`displayX`|number|是|范围 `[-1,1]`|
|`displayY`|number|是|范围 `[-1,1]`|
|`isDiscovered`|boolean|是|当前导出的初始发现状态|

### 11.4 明确不存在的字段

`galaxy_nodes.json` 自身没有：

- nearest neighbours
- Top 3 cancer types
- distance
- 类别统计

Case 对应的 5 个最近 Reference Nodes 存放于：

```text
game_cases.json → cases[] → galaxy → nearbyReferences
```

---

## 12. `class_profiles.json` 数据契约

### 12.1 顶层结构

顶层字段：

- `metadata`
- `classes`

### 12.2 metadata

|字段|类型|必填|含义|
|-|-|-|-|
|`version`|string|是|Data Schema Version，当前为 `1.0`|
|`classCount`|integer|是|必须为 8|
|`trainingExpressionCluesNote`|string|是|训练集表达线索的科学边界说明|

### 12.3 Class Profile

|字段|类型|必填|含义与约束|
|-|-|-|-|
|`classId`|CancerType string|是|类别唯一机器值|
|`labelEn`|string|是|英文显示名|
|`labelZh`|string|是|中文显示名|
|`trainSampleCount`|integer|是|训练集样本数，非负|
|`testSampleCount`|integer|是|测试集样本数，非负；8 类总和为 162|
|`ai`|object|是|该类冻结模型指标|
|`commonConfusions`|array&lt;CommonConfusion&gt;|是|常见错误预测类别，可为空|
|`trainingExpressionClues`|array&lt;TrainingExpressionClue&gt;|是|训练集代表性表达模式，当前每类 5 条|

`ai` 字段：

|字段|类型|必填|约束|
|-|-|-|-|
|`precision`|number|是|范围 `[0,1]`|
|`recall`|number|是|范围 `[0,1]`|
|`f1`|number|是|范围 `[0,1]`|

`commonConfusions` 条目：

|字段|类型|必填|约束|
|-|-|-|-|
|`classId`|CancerType string|是|被混淆到的类别|
|`labelEn`|string|是|与 `classId` 匹配|
|`labelZh`|string|是|与 `classId` 匹配|
|`errorCount`|integer|是|正整数|

`trainingExpressionClues` 条目：

|字段|类型|必填|约束|
|-|-|-|-|
|`gene`|string|是|基因符号|
|`direction`|string|是|`HIGH` 或 `LOW`|
|`strength`|string|是|`Weak`、`Moderate` 或 `Strong`|

### 12.4 表现层字段

当前科学 JSON 不存在：

- `iconKey`
- `color`
- `description`

这些属于 Unity 表现层配置。未来可建立 Visual Profile / UI Config 映射，但不得要求重跑科学数据，也不得将表现层占位值伪装为科学结论。

---

## 13. `game_config.json` 数据契约

### 13.1 当前对齐配置

|字段路径|类型|当前值|规则|
|-|-|-:|-|
|`version`|string|`1.0`|配置 Schema Version|
|`shift.casesPerShift`|integer|10|每个 Shift 选择 10 个 Case|
|`shift.startingRP`|integer|200|每个 Shift 初始 RP|
|`costs.geneScan`|integer|10|Gene Scan 消耗|
|`costs.cancerGalaxy`|integer|25|Cancer Galaxy 消耗|
|`costs.aiAssistant`|integer|40|AI Assistant 消耗|
|`investigation.initialClueCount`|integer|2|每 Case 免费线索数|
|`investigation.geneScanClueCount`|integer|3|Gene Scan 解锁线索数|
|`investigation.maxGeneScanUsesPerCase`|integer|1|每 Case 最大使用次数|
|`investigation.maxCancerGalaxyUsesPerCase`|integer|1|每 Case 最大使用次数|
|`investigation.maxAIAssistantUsesPerCase`|integer|1|每 Case 最大使用次数|
|`investigation.minimumRP`|integer|0|RP 不得低于此值|
|`rewards.correctDiagnosisScore`|integer|100|正确诊断 Score|
|`rewards.wrongDiagnosisScore`|integer|0|错误诊断 Score|
|`tutorial.investigationToolsCostRP`|integer|0|Tutorial 中三种工具均不消耗 RP|

所有 RP、Cost、Score 和次数字段必须为非负整数。`casesPerShift` 必须大于 0 且不超过 Case Pool 数量。

### 13.2 已废弃机制

当前 v1.0 配置不再包含：

- Preliminary Diagnosis
- Efficient Research Bonus
- RP Refund
- Emergency Grant
- Human Beats AI
- Rank thresholds
- Fixed difficulty mix
- Wildcard mix

Data Layer 不得为这些旧机制提供隐式默认值。

---

## 14. Runtime State 数据契约

Runtime State 不写回任何静态 JSON。

|字段|类型|必填|含义与约束|
|-|-|-|-|
|`currentShift`|integer|是|当前 Shift 编号，正整数|
|`currentCaseIndex`|integer|是|当前 Case 在 `selectedCaseIds` 中的零基索引，范围 `0–9`|
|`selectedCaseIds`|array&lt;string&gt;|是|当前 Shift 固定的 10 个唯一 `caseId`|
|`remainingRP`|integer|是|范围 `0–200`|
|`currentScore`|integer|是|非负；按正式 Score 规则累计|
|`completedCaseCount`|integer|是|范围 `0–10`|
|`correctCaseCount`|integer|是|范围 `0–completedCaseCount`|
|`currentCaseState`|object|是|当前病例的玩家进度|
|`currentEvidenceState`|object 或 null|否|仅记录模块解锁状态，不复制科学数据|

`currentCaseState`：

|字段|类型|必填|含义与约束|
|-|-|-|-|
|`caseId`|string|是|必须等于 `selectedCaseIds[currentCaseIndex]`|
|`geneScanUsed`|boolean|是|Gene Scan 是否已使用|
|`cancerGalaxyUsed`|boolean|是|Cancer Galaxy 是否已使用|
|`aiAssistantUsed`|boolean|是|AI Assistant 是否已使用|
|`selectedDiagnosis`|CancerType string 或 null|是|提交前可为 null|
|`diagnosisSubmitted`|boolean|是|每 Case 只允许由 false 变为 true 一次|

当 `diagnosisSubmitted` 为 true 时，`selectedDiagnosis` 必须为合法 CancerType。

---

## 15. Shift Runtime 规则

每个 Shift 从 162 个 Case 中随机选择 10 个。

约束：

- 同一 Shift 的 `selectedCaseIds` 不得重复。
- 每个 ID 必须存在于 `game_cases.json`。
- 不保证癌症类别均衡。
- 不保证 Difficulty 均衡。
- `selectedCaseIds` 在 Shift 创建时确定，并写入 Runtime State 与 Save Data。
- 加载存档时必须恢复原数组，不得重新随机，否则会改变玩家当前 Shift。

---

## 16. SaveData v1

SaveData 是本地存档，不包含完整科学数据副本。

### 16.1 顶层字段

|字段|类型|必填|含义与约束|
|-|-|-|-|
|`saveVersion`|string|是|Save Data 版本，v1 为 `1.0`|
|`currentShift`|integer|是|当前 Shift 编号|
|`currentCaseIndex`|integer|是|当前病例零基索引|
|`selectedCaseIds`|array&lt;string&gt;|是|当前 Shift 的 10 个唯一 Case ID|
|`remainingRP`|integer|是|不得小于 0|
|`currentScore`|integer|是|当前累计 Score|
|`completedCaseCount`|integer|是|已完成病例数量|
|`correctCaseCount`|integer|是|正确病例数量|
|`currentCaseState`|object|是|当前病例进度|
|`completedCaseResults`|array&lt;CompletedCaseResult&gt;|是|已完成病例的最小结果记录|
|`currentEvidenceState`|object 或 null|否|可选的解锁状态记录|

### 16.2 CompletedCaseResult

|字段|类型|必填|含义|
|-|-|-|-|
|`caseId`|string|是|引用 Static Case|
|`playerDiagnosis`|CancerType string|是|玩家提交的诊断|
|`trueDiagnosis`|CancerType string|是|结算时记录的真实类别|
|`isCorrect`|boolean|是|诊断是否正确|
|`scoreEarned`|integer|是|当前规则仅为 100 或 0|
|`rpSpent`|integer|是|该 Case 调查工具消耗的 RP，非负|

校验关系：

- `completedCaseResults.length == completedCaseCount`。
- `correctCaseCount` 等于结果数组中 `isCorrect == true` 的数量。
- `currentScore` 等于已完成结果的 `scoreEarned` 总和。
- 所有 Case 通过 `caseId` 引用静态数据。
- 不保存 AI 概率、基因线索、Galaxy 节点或 Class Profile 的副本。

---

## 17. Evidence Board 数据关系

Evidence Board 不创造新的科学数据，只引用当前 Case 中已经解锁的信息。

|模块|静态数据来源|Runtime 解锁条件|
|-|-|-|
|Initial Clues|`case.initialClues`|Case 开始即解锁|
|Gene Scan Results|`case.geneScanClues`|`geneScanUsed == true`|
|Cancer Galaxy Results|`case.galaxy`|`cancerGalaxyUsed == true`|
|AI Assistant Results|`case.ai`|`aiAssistantUsed == true`|

`currentEvidenceState` 如需保存，只记录哪些模块已解锁。实际内容应在加载后通过 `caseId` 从 Static Game Data 恢复，不复制、不重新计算科学数据。

---

## 18. 异常与校验规则

### 18.1 文件级校验

Unity Data Layer 必须检查四个文件均存在且 JSON 可解析：

- `game_cases.json`
- `galaxy_nodes.json`
- `class_profiles.json`
- `game_config.json`

### 18.2 内容级最低校验

- Case count 大于 0，且 `metadata.totalCases` 与实际数量一致。
- 所有 `caseId` 唯一。
- 所有 `modelId` 满足对应文件中的唯一性或引用关系。
- CancerType 必须属于正式 8 类。
- Difficulty 必须属于 `EASY`、`NORMAL`、`HARD`、`ANOMALY`。
- 每个 Case 的 `initialClues.length == 2`。
- 每个 Case 的 `geneScanClues.length == 3`。
- Clue `state` 与 Support `strength` 必须属于合法枚举。
- AI `confidence` 与全部 `probability` 必须在 `[0,1]`。
- AI `top3.length == 3` 且按概率降序。
- AI `probabilities` 必须覆盖全部 8 类。
- Case `nearbyReferences.length == 5` 且按 `distance` 升序。
- 所有坐标和距离必须是有限数值。
- `displayX`、`displayY` 必须在 `[-1,1]`。
- Galaxy 节点总数必须与 metadata 一致。
- Case Node 的 `caseId`、`modelId` 和坐标必须能与 Case 对应。
- Class Profile 必须恰好覆盖 8 个唯一 `classId`。
- RP、Cost、Score 与使用次数配置不得为负。
- `shift.casesPerShift <= cases.length`。

### 18.3 失败策略

如果校验失败，Data Layer 必须：

- 阻止进入正式游戏流程；
- 输出包含文件名、字段路径、期望值和实际值的明确错误日志；
- 不自动制造默认病例；
- 不伪造 AI Prediction；
- 不伪造基因线索；
- 不伪造 Galaxy 节点；
- 不用表现层占位值代替科学数据。

---

## 19. 字段缺失策略

### 19.1 Critical Field

Critical Field 缺失、为 null、类型错误或值非法时阻止加载。

包括但不限于：

- `caseId`
- 真实类别字段
- AI Prediction、Confidence 与 Top 3
- Initial Clues 与 Gene Scan Clues
- Difficulty
- Galaxy position 与 nearbyReferences
- Reference/Case Node 身份和坐标
- Class Profile 的 `classId`
- Game Config 核心数值

### 19.2 Optional Presentation Field

表现层字段可以缺失，UI 可使用明确的非科学占位表现：

- icon
- color
- description

占位内容不得伪造类别特征、模型结论或医学含义。当前科学 JSON 本身不包含这些字段。

---

## 20. 示例 JSON

以下内容均为“结构示例”，不是某个真实 Case，不得作为科学事实或正式病例导入。

### 20.1 简化 Case 结构示例

```json
{
  "caseId": "CASE_EXAMPLE",
  "modelId": "MODEL_EXAMPLE",
  "cellLineName": "Example Cell Line",
  "trueClassId": "lung",
  "trueClassLabelEn": "Lung",
  "trueClassLabelZh": "肺",
  "difficulty": "NORMAL",
  "difficultyScore": 0.5,
  "initialClues": [
    {
      "gene": "GENE_A",
      "geneColumn": "GENE_A (0001)",
      "expressionValue": 1.25,
      "state": "HIGH",
      "support": [
        { "classId": "lung", "labelEn": "Lung", "labelZh": "肺", "strength": "Moderate" }
      ]
    },
    {
      "gene": "GENE_B",
      "geneColumn": "GENE_B (0002)",
      "expressionValue": 0.75,
      "state": "LOW",
      "support": [
        { "classId": "breast", "labelEn": "Breast", "labelZh": "乳腺", "strength": "Weak" }
      ]
    }
  ],
  "geneScanClues": [
    {
      "gene": "GENE_C",
      "geneColumn": "GENE_C (0003)",
      "expressionValue": 2.1,
      "state": "HIGH",
      "support": [
        { "classId": "lung", "labelEn": "Lung", "labelZh": "肺", "strength": "Strong" }
      ]
    },
    {
      "gene": "GENE_D",
      "geneColumn": "GENE_D (0004)",
      "expressionValue": 0.4,
      "state": "LOW",
      "support": [
        { "classId": "cns_brain", "labelEn": "CNS/Brain", "labelZh": "中枢神经 / 脑", "strength": "Weak" }
      ]
    },
    {
      "gene": "GENE_E",
      "geneColumn": "GENE_E (0005)",
      "expressionValue": 3.0,
      "state": "HIGH",
      "support": [
        { "classId": "lung", "labelEn": "Lung", "labelZh": "肺", "strength": "Moderate" }
      ]
    }
  ]
}
```

该示例仅展示 Case 身份与 2+3 Clue 结构；正式 Case 还必须包含 `ai`、`evidence` 和 `galaxy`。

### 20.2 AI 结构示例

```json
{
  "predictedClassId": "lung",
  "predictedClassLabelEn": "Lung",
  "predictedClassLabelZh": "肺",
  "confidence": 0.78,
  "correct": false,
  "top3": [
    { "classId": "lung", "labelEn": "Lung", "labelZh": "肺", "probability": 0.78 },
    { "classId": "breast", "labelEn": "Breast", "labelZh": "乳腺", "probability": 0.15 },
    { "classId": "skin", "labelEn": "Skin", "labelZh": "皮肤", "probability": 0.07 }
  ],
  "probabilities": {
    "lung": 0.78,
    "skin": 0.07,
    "cns_brain": 0.0,
    "bowel": 0.0,
    "esophagus_stomach": 0.0,
    "breast": 0.15,
    "bone": 0.0,
    "ovary_fallopian_tube": 0.0
  }
}
```

### 20.3 NearbyReference 单条结构示例

```json
{
  "modelId": "REFERENCE_MODEL_EXAMPLE",
  "cellLineName": "Example Reference Cell Line",
  "classId": "lung",
  "labelEn": "Lung",
  "labelZh": "肺",
  "distance": 0.125
}
```

正式 `nearbyReferences` 数组必须包含 5 条，并按 `distance` 升序。

### 20.4 Runtime State 结构示例

```json
{
  "currentShift": 1,
  "currentCaseIndex": 2,
  "selectedCaseIds": [
    "CASE_001", "CASE_002", "CASE_003", "CASE_004", "CASE_005",
    "CASE_006", "CASE_007", "CASE_008", "CASE_009", "CASE_010"
  ],
  "remainingRP": 125,
  "currentScore": 200,
  "completedCaseCount": 2,
  "correctCaseCount": 2,
  "currentCaseState": {
    "caseId": "CASE_003",
    "geneScanUsed": true,
    "cancerGalaxyUsed": false,
    "aiAssistantUsed": false,
    "selectedDiagnosis": null,
    "diagnosisSubmitted": false
  },
  "currentEvidenceState": {
    "initialCluesUnlocked": true,
    "geneScanUnlocked": true,
    "cancerGalaxyUnlocked": false,
    "aiAssistantUnlocked": false
  }
}
```

### 20.5 SaveData v1 结构示例

```json
{
  "saveVersion": "1.0",
  "currentShift": 1,
  "currentCaseIndex": 2,
  "selectedCaseIds": [
    "CASE_001", "CASE_002", "CASE_003", "CASE_004", "CASE_005",
    "CASE_006", "CASE_007", "CASE_008", "CASE_009", "CASE_010"
  ],
  "remainingRP": 125,
  "currentScore": 200,
  "completedCaseCount": 2,
  "correctCaseCount": 2,
  "currentCaseState": {
    "caseId": "CASE_003",
    "geneScanUsed": true,
    "cancerGalaxyUsed": false,
    "aiAssistantUsed": false,
    "selectedDiagnosis": null,
    "diagnosisSubmitted": false
  },
  "completedCaseResults": [
    {
      "caseId": "CASE_001",
      "playerDiagnosis": "skin",
      "trueDiagnosis": "skin",
      "isCorrect": true,
      "scoreEarned": 100,
      "rpSpent": 10
    },
    {
      "caseId": "CASE_002",
      "playerDiagnosis": "lung",
      "trueDiagnosis": "lung",
      "isCorrect": true,
      "scoreEarned": 100,
      "rpSpent": 65
    }
  ],
  "currentEvidenceState": {
    "initialCluesUnlocked": true,
    "geneScanUnlocked": true,
    "cancerGalaxyUnlocked": false,
    "aiAssistantUnlocked": false
  }
}
```

---

## 21. Known Generator Risk

`python/src/08_export_game_data.py` 是正式游戏数据导出器，其 `game_config.json` 生成逻辑与 reload QA 已完成 v1.0 配置对齐。

Stage 08 当前生成的配置与以下文件的数据结构和规则一致：

```text
results/game_data/game_config.json
```

reload QA 会验证完整 v1.0 配置，包括 Shift、RP、三种调查工具、Score、使用次数上限与 Tutorial 调查费用，并拒绝旧版配置结构。

Stage 08 会写出四个正式游戏数据文件。未来执行时仍应作为受控的数据导出操作，在运行后复核冻结科学数据的哈希与 QA 结果；但旧版 `game_config.json` 覆盖风险已经解除。

本次配置对齐没有修改以下冻结科学数据的生成逻辑或当前文件：

- `game_cases.json`
- `galaxy_nodes.json`
- `class_profiles.json`

后续维护 Stage 08 时仍不得借配置变更重训模型、重跑 01–07.1、修改预测结果、重建基因线索或改变 UMAP 坐标。

---

## 22. Unity Data Layer 后续职责

以下仅描述未来组件职责，不规定具体 C# 实现。

### GameDataLoader

- 从 `StreamingAssets/GameData/` 统一加载四个正式 JSON。
- 处理文件读取、JSON 解析和版本检查。
- 不包含 Gameplay 或 UI 逻辑。

### CaseRepository

- 提供按 `caseId` 查询 Case 的只读接口。
- 维护 Case ID 唯一索引。
- 为 Shift 选择提供只读 Case 集合。

### ClassProfileRepository

- 按 `classId` 提供类别档案。
- 统一 CancerType 与中英文显示名映射。
- 不把显示文字作为逻辑键。

### GameConfigRepository

- 提供 Shift、RP、工具次数、Score 和 Tutorial 配置。
- 不提供任何已废弃旧机制的默认值。

### SaveDataRepository

- 保存和加载 SaveData v1。
- 根据 `saveVersion` 处理兼容性。
- 恢复固定的 `selectedCaseIds`，不重新抽取当前 Shift。
- 不复制 Static Game Data。

### DataValidator

- 执行第 18 节中的文件、字段、枚举、范围和跨文件引用校验。
- 在错误时阻止正式游戏流程并输出可定位日志。
- 不通过静默默认值掩盖科学数据错误。

### UI 与 Gameplay 边界

- UI 不直接读取 JSON 文件。
- Gameplay 不自行解析 JSON。
- 所有静态数据统一经过 Data Layer 与 Repository 提供。
- UI 只能读取当前流程允许展示的字段，尤其不得提前读取真实类别。
- Runtime State 与 Save Data 的修改不得反向污染 Static Game Data。
