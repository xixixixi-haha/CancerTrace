# Case 系统

状态：v1.0规则已同步（实现细节待补充）  
规则来源：`../Game_Design_Document_v1.0.md` 第 5 至 8、11 节

## 已确认规则

- 病例来源于真实公开癌症组学数据。
- v1.0使用冻结的162个独立Test Set病例，不为了游戏平衡修改科学数据。
- 八类数量为：Lung 43、Skin 24、CNS/Brain 21、Bowel 17、Esophagus/Stomach 16、Breast 14、Bone 14、Ovary/Fallopian Tube 13。
- 每个Shift从162个病例中随机抽取10个Case；同一Shift内不得重复。
- 不同Shift之间允许重复抽到以前出现过的Case；v1.0不维护跨Shift全局去重池。
- Shift抽取不人为保证类别均衡，也不人为保证Difficulty均衡。
- v1.0 固定支持 GDD 中定义的 8 类癌症。
- Difficulty实际包含EASY 46、NORMAL 61、HARD 47、ANOMALY 8。
- ANOMALY不是普通难度等级，而是“AI预测错误且Confidence大于或等于0.80”的内部特殊病例标签。
- 玩家提交诊断前不得显示ANOMALY、异常案件或任何明确暗示AI当前预测错误的信息。
- 提交诊断后，Result Summary可以揭示“异常案件 / Anomaly Case”及其含义。
- 玩家从8类癌症中选择，不允许自由输入。
- 每个Case只能提交一次；玩家可随时提交，不要求完成所有调查。
- Tutorial Case不计入正式Shift，也不影响正式统计。

## Case数据结构

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

## 待补充

- 病例 JSON 字段类型、必填约束和版本信息。
- 异常数据、缺失字段和无效类别的处理方式。

本文档不得自行新增或修改 GDD 中的游戏规则。
