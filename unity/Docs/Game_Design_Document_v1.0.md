# CancerTrace v1.0
# Game Design Document (GDD)

版本：v1.0  
项目名称：CancerTrace  
项目类型：生物信息学 × AI × 推理探索游戏  

---

# 1. 项目概述（Overview）

## 1.1 项目简介

CancerTrace 是一款融合生物信息学、人工智能和游戏化推理机制的探索类游戏。

玩家将在游戏中扮演一名“细胞侦探”，通过分析未知癌细胞样本，利用基因表达线索、癌症空间关系以及 AI 辅助分析，逐步推理未知样本所属癌症类别。

游戏将真实生物信息学数据转化为可理解、可交互的探索体验，让玩家在调查过程中理解：

- 基因表达差异
- 癌症类别之间的关系
- AI模型预测过程

---

# 2. 核心定位（Core Concept）

## 2.1 玩家身份

玩家：

> 细胞侦探（Cell Detective）

任务：

> 调查未知癌细胞样本，并判断其所属癌症类型。

---

## 2.2 核心理念

CancerTrace 不希望 AI 直接替代玩家。

AI 的定位：

> AI科研助手。

玩家负责：

- 收集证据
- 分析线索
- 做出判断

AI负责：

- 提供预测
- 提供概率

最终决定权属于玩家。

---

# 3. 核心游戏循环（Gameplay Loop）

完整流程：

```
获得病例
    ↓
查看免费线索
    ↓
决定是否消耗科研点
    ↓
Gene Scan / Cancer Galaxy / AI Assistant
    ↓
提交诊断
    ↓
结果揭晓
    ↓
获得调查评价
    ↓
进入下一病例
    ↓
Shift总结
```

---

# 4. 游戏周期设计（Shift System）

## 4.1 Shift定义

一个完整调查周期：

> Shift = 10个病例（Case）

---

## 4.2 Shift流程

开始Shift：

```
生成10个随机病例

↓

初始化科研点 RP = 200

↓

开始Case 1
```

完成：

```
Case 1
Case 2
...
Case 10
```

进入：

Shift Summary（调查总结）

---

# 5. 病例系统（Case System）

## 5.1 病例来源

病例来自真实公开癌症组学数据。

核心数据：

- RNA-seq基因表达数据
- 癌症类别标签
- 模型预测结果

---

## 5.2 Case数据结构

每个病例包含：

```
{
    caseId,
    modelId,
    cellLineName,
    trueClassId,
    trueClassLabelEn,
    trueClassLabelZh,
    ai,
    initialClues,
    geneScanClues,
    evidence,
    difficulty,
    difficultyScore,
    galaxy
}
```

---

## 5.3 随机机制

每个Shift：

从冻结的162个独立Test Set病例中随机抽取10个病例。

规则：

- 同一Shift内不得出现重复Case
- 不人为保证类别均衡
- 不人为保证Difficulty均衡
- 不固定剧情顺序
- 支持重复游玩
- 不同Shift之间允许再次抽到以前出现过的Case
- v1.0不维护跨Shift的全局病例去重池

---

# 6. 癌症类别系统（Cancer Categories）

v1.0固定支持8类癌症：

| ID | Cancer Type |
|----|-------------|
| C01 | Lung Cancer |
| C02 | Breast Cancer |
| C03 | Skin Cancer |
| C04 | CNS Cancer |
| C05 | Bone Cancer |
| C06 | Ovary/Fallopian Tube Cancer |
| C07 | Esophagus/Stomach Cancer |
| C08 | Bowel Cancer |

---

# 7. 病例池设计（Case Pool）

## 7.1 v1.0病例池规模

CancerTrace v1.0使用已经导出的：

> 162个独立Test Set病例

八类真实数量：

|Cancer Type|Cases|
|-|-:|
|Lung|43|
|Skin|24|
|CNS/Brain|21|
|Bowel|17|
|Esophagus/Stomach|16|
|Breast|14|
|Bone|14|
|Ovary/Fallopian Tube|13|

这些病例来自冻结的独立测试集，不为了游戏平衡而修改科学数据。

病例池本身不保证类别均衡。每个Shift从162个病例中随机抽取10个不重复Case，也不人为保证Difficulty均衡。

---

# 8. 难度系统（Difficulty System）

## 8.1 难度来源

采用：

> 数据驱动难度（Data-driven Difficulty）

不采用：

```
第一关简单
最后一关困难
```

而根据真实数据决定。

---

## 8.2 难度因素

冻结数据已经为每个Case保存`difficulty`与`difficultyScore`。

普通病例的Difficulty由导出阶段根据预计算的证据歧义程度划分。Unity直接读取该结果，不重新计算或修改科学数据。

---

## 8.3 Difficulty值与数量

|值|数量|说明|
|-|-:|-|
|EASY|46|普通难度等级|
|NORMAL|61|普通难度等级|
|HARD|47|普通难度等级|
|ANOMALY|8|特殊病例标签|

ANOMALY不是普通难度等级。

定义为：

```
AI预测错误
且
AI Confidence >= 0.80
```

这8个病例的数据保持不变。

### ANOMALY信息边界

玩家提交诊断之前，不得显示：

- `ANOMALY`
- 异常案件
- 任何明确暗示AI当前预测错误的信息

玩家提交诊断之后，Result Summary可以揭示：

> 异常案件 / Anomaly Case

并解释该病例属于AI高置信度但预测错误的特殊情况。该边界用于避免在玩家决策前泄漏答案信息。

---

# 9. 科研点系统（Research Point System）

## 9.1 定位

科研点（RP）代表：

> 玩家在一次调查周期内可以调用的科研资源。

不是金币。

不是奖励货币。

---

## 9.2 RP规则

每个Shift开始：

```
remainingRP = 200
```

整个Shift的10个Case共享这200 RP。

Shift中途不自动刷新，也不补充RP。

进入下一个Shift时：

```text
remainingRP = 200
```

不存在：

- 每5个Case自动刷新
- RP奖励
- Emergency Grant
- 借贷
- 负RP

---

## 9.3 RP消耗

|功能|消耗|
|-|-|
|Gene Scan|10 RP|
|Cancer Galaxy|25 RP|
|AI Assistant|40 RP|

---

## 9.4 RP限制

当：

```text
remainingRP < Tool Cost
```

对应工具：

- 按钮禁用
- 显示RP不足提示
- 不允许执行

不得出现：

- `remainingRP < 0`
- 欠费使用
- 自动补充
- Emergency Grant
- 临时贷款

Submit Diagnosis始终免费，即使`remainingRP = 0`仍可提交。

---

# 10. 调查系统（Investigation System）

玩家通过三个核心工具获取额外信息。

---

# 10.1 Gene Scan

## 定位

基础基因分析工具。

---

## 消耗

```
10 RP
```

---

## 使用次数

每个病例最多一次。

---

## 输出内容

每个Case开始时免费展示固定2条Initial Clues。

使用Gene Scan后：

> 解锁额外3条基因线索

因此每个Case最多看到：

```
2 Initial Clues
+
3 Gene Scan Clues
=
5条基因线索
```

每条线索包括：

- 基因名称
- 表达变化
- 支持信息

---

# 10.2 Cancer Galaxy

## 定位

癌症表达空间探索系统。

---

## 消耗

```
25 RP
```

---

## 使用次数

每个病例最多一次。

---

## 数据来源

RNA-seq表达数据降维。

流程：

```
高维基因表达

↓

降维算法

↓

二维癌症空间

↓

Cancer Galaxy
```

---

## 玩家获得信息

包括：

- 当前未知Case在Cancer Galaxy中的位置
- Reference Nodes
- 当前Case最近的5个Reference Nodes
- 每个近邻节点所属癌症类别
- 必要的邻近距离信息

不直接显示答案。

UMAP二维空间距离表示降维后的空间邻近关系，不是AI Prediction Probability。

v1.0不新增Top 3 nearest cancer types算法，也不修改Galaxy科学数据。

Cancer Galaxy不得自动输出：

- 最终答案
- 最可能癌症类型
- 推荐诊断

Galaxy只提供空间证据，最终诊断仍由玩家完成。

---

# 10.3 AI Assistant

## 定位

AI科研助手。

---

## 消耗

```
40 RP
```

---

## 使用次数

每个病例最多一次。

---

## 输出内容

固定包括：

### 1. Prediction

预测类别。

例如：

```
Lung Cancer
```

---

### 2. Confidence

预测置信度。

例如：

```
Confidence:
78%
```

---

### 3. Top 3 Candidates

前三候选类别。

例如：

```
Lung Cancer 78%

Breast Cancer 15%

Skin Cancer 7%
```

---

AI Explanation属于未来可扩展功能，不属于当前v1.0必备功能。v1.0不生成虚构解释，也不修改冻结的Python数据。

## 提交前后展示规则

提交诊断前，只有支付40 RP并使用AI Assistant后，玩家才能看到：

- Prediction
- Confidence
- Top 3 Candidates

AI Assistant每个Case最多使用一次。

提交诊断后，无论玩家此前是否使用AI Assistant，Result Summary都允许展示：

- AI Prediction
- AI Confidence
- AI Top 3

结果页展示用于病例结束后的教学复盘，不视为免费使用AI Assistant，因为此时已经无法影响该Case的玩家决策。

---

# 10.4 Evidence Board

## 定位

Evidence Board是证据整理展示区域。

不是新的调查功能。

不是独立消耗RP的系统。

作用：

> 帮助玩家整理已经获得的信息。

---

## 展示内容

1. Initial Clues（初始线索）
2. Gene Scan Results（基因扫描结果）
3. Cancer Galaxy Results（癌症星图结果）
4. AI Assistant Results（AI分析结果）

---

## 规则

- 自动记录玩家已经获得的信息
- 不消耗RP
- 不影响评分
- 不提供额外答案

---

# 11. 诊断提交系统（Diagnosis System）

## 11.1 提交规则

- 玩家从8类癌症中选择
- 不允许自由输入
- 每个Case只能提交一次
- 玩家可以随时提交
- 不要求完成所有调查
- Submit Diagnosis始终免费，即使RP为0仍可提交

---

## 11.2 玩家选择

从8类癌症中选择：

最终判断。

---

# 12. 结果系统（Result System）

## 12.1 揭晓流程

顺序：

```
玩家判断

↓

AI分析结果

↓

真实答案
```

---

## 12.2 展示内容

包括：

- 玩家预测
- AI预测
- AI置信度
- AI Top 3
- 真实癌症类型
- 调查评价

无论玩家在提交前是否使用AI Assistant，提交后的Result Summary都可以展示完整AI复盘信息。

若当前Case的内部Difficulty为`ANOMALY`，Result Summary可以在此时显示“异常案件 / Anomaly Case”，并说明该病例属于AI高置信度但预测错误的特殊情况。

---

# 13. 评分与评价系统

## 13.1 Score定位

Score是调查评价指标。

不是货币。

不是RP。

---

## 13.2 核心评分

答对：

```
+100 Score
```

答错：

```
+0 Score
```

---

## 13.3 Score生命周期

Score是当前Shift的评价指标。

每个新Shift开始：

```text
currentScore = 0
```

完成Case后，正确诊断增加100 Score，错误诊断增加0 Score。

Shift结束时显示该Shift的最终Score。

历史成绩如果未来需要保存，属于Statistics / Profile扩展，不影响v1.0当前Shift Score。

---

## 13.4 调查评价

不影响核心分。

评价：

- 证据利用程度
- RP使用效率
- 调查完整度

---

## 13.5 奖励边界

v1.0不设计：

- RP奖励
- 金币
- 商店
- 刷资源机制

---

# 14. Shift总结系统

完成10个病例后：

生成调查报告。

展示：

```
Cases Completed:

10/10


Accuracy:

80%


Gene Scan:

7 times


Cancer Galaxy:

4 times


AI Assistant:

5 times


Remaining RP:

35


Score:

700
```

---

# 15. AI系统设计

## AI定位

统一定位为：

> AI Assistant（AI科研助手）

AI作用：

- 提供预测
- 提供置信度
- 提供候选类别

玩家负责最终判断。

---

## AI特点

- 预测不是100%准确
- 根据病例难度变化
- 输出概率而非绝对答案

---

# 16. 数据与技术架构

整体流程：

```
真实癌症组学数据

↓

数据处理

↓

机器学习模型

↓

生成病例

↓

玩家调查

↓

AI辅助

↓

诊断结果
```

---

# 17. v1.0开发范围（MVP）

## 必须实现

- 主菜单
- Tutorial Case
- 病例系统
- 8类癌症
- 随机病例
- RP系统
- Gene Scan
- Cancer Galaxy
- AI Assistant
- Evidence Board
- 提交判断
- 结果反馈
- Shift总结
- 本地存档


---

## 暂缓功能

以下不属于v1.0：

- 在线排行榜
- 多人模式
- 实时AI聊天
- 实时模型训练
- 复杂剧情系统
- 成就系统（可选扩展）

---

# 18. 教学流程（Tutorial System）

第一次进入游戏时，不直接进入正式Shift。

新增Tutorial Case（教学病例）。

## 18.1 教学流程

```
第一次启动游戏
    ↓
角色介绍
    ↓
进入教学病例
    ↓
病例档案介绍
    ↓
学习免费线索
    ↓
学习Gene Scan
    ↓
学习Cancer Galaxy
    ↓
学习AI Assistant
    ↓
提交诊断
    ↓
展示结果
    ↓
进入正式Shift
```

## 18.2 教学规则

- Tutorial不消耗RP
- Tutorial不计入正式Shift
- Tutorial不影响正式统计

---

# 19. AI使用边界声明（AI Disclaimer）

CancerTrace不是医疗诊断系统。

定位：

> 生物信息学教学与AI探索游戏。

AI Assistant：

- 输出模型预测概率
- 存在预测错误可能
- 置信度不代表医学结论

---

# 20. 存档系统（Save System）

v1.0采用本地存档。

不需要：

- 用户账号
- 云端同步

保存：

- 当前Shift
- 当前Case
- 剩余RP
- 完成病例数量
- 当前Score

---

# 21. 成就系统（Achievement System）

成就系统为可选扩展。

不是排行榜。

用于增强探索反馈。

示例：

- 初次调查
- 基因探索者
- AI协作者
- 完美调查

---

# 22. 项目最终目标

CancerTrace希望实现：

> 将真实生物信息学数据转化为一个具有探索感的AI辅助癌症推理游戏，让玩家通过收集证据、分析基因、探索癌症空间，理解人工智能如何参与生命科学研究。

---

# 23. 规则冻结

## v1.0 Rule Freeze

CancerTrace v1.0核心规则至此冻结。

后续允许调整：

- UI布局
- 动画
- 美术
- 音效
- 按钮反馈
- 场景过渡
- C#实现方式
- 日志和异常处理
- 性能优化

未经明确版本升级，不再调整：

- Shift = 10
- Starting RP = 200
- 工具成本10 / 25 / 40
- Score 100 / 0
- 162 Case Pool
- 2 Initial + 3 Gene Scan
- Cancer Galaxy规则
- AI Assistant规则
- ANOMALY定义
- Submit规则
- Case抽取规则
- RP生命周期

若以后确实需要修改核心玩法，必须升级为GDD v1.1或更高版本，不得直接修改v1.0冻结规则。
