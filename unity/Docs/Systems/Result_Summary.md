# Result Summary

状态：v1.0规则已同步（实现细节待补充）  
规则来源：`../Game_Design_Document_v1.0.md` 第 12 至 14、18 节

## 单病例结果：已确认规则

- 揭晓顺序为：玩家判断 → AI 分析结果 → 真实答案。
- 展示玩家预测、AI 预测、AI 置信度、真实癌症类型和调查评价。
- 无论提交前是否使用AI Assistant，结果页都可展示AI Prediction、AI Confidence和AI Top 3用于教学复盘；这不视为免费使用AI Assistant。
- 若Case为ANOMALY，只有在提交诊断后才可显示“异常案件 / Anomaly Case”，并说明其为AI高置信度但预测错误的特殊情况。
- 判断正确获得 100 Score，判断错误获得 0 Score。
- Score是调查评价指标，不是货币，也不是RP。
- 调查评价考虑证据利用程度、RP 使用效率和调查完整度，但不影响核心分。
- 不设计RP奖励、金币、商店或刷资源机制。
- Tutorial结果不计入正式统计。

## Shift Summary：已确认规则

- 完成 10 个病例后生成调查报告。
- 展示完成病例数、准确率、三种调查工具使用次数、剩余RP和当前Shift最终Score。
- 每个新Shift开始时`currentScore = 0`；历史成绩属于未来Statistics / Profile扩展。

## 待补充

- 单病例结果与 Shift Summary 的页面边界和导航关系。
- 调查评价的具体计算与文案规则。
- 汇总指标的计算口径和异常数据处理。

本文档不得自行新增或修改 GDD 中的游戏规则。
