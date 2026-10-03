# Evidence Board

状态：v1.0规则已同步（实现细节待补充）  
规则来源：`../Game_Design_Document_v1.0.md` 第 10.4 节

## 定位

Evidence Board是证据整理展示区域，不是新的调查功能，也不是独立消耗RP的系统。

## 展示内容

1. Initial Clues（初始线索）。
2. Gene Scan Results（额外3条基因线索）。
3. Cancer Galaxy Results（Case位置与5个最近Reference Nodes）。
4. AI Assistant Results（Prediction、Confidence与Top 3 Candidates）。

## 已确认规则

- 自动记录玩家已经获得的信息。
- 提交诊断前，只有实际使用AI Assistant后才记录其Prediction、Confidence和Top 3 Candidates。
- 未使用AI Assistant时，提交后的AI复盘仅属于Result Summary，不视为Evidence Board免费解锁调查工具。
- 不消耗RP。
- 不影响评分。
- 不提供额外答案。

## 待补充

- 信息卡片的排序、折叠和空状态表现。
- 各调查结果写入Evidence Board的触发时机。

本文档不得自行新增或修改GDD中的游戏规则。
