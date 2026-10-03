# 场景规划

## MainMenu.unity — 主菜单

负责游戏入口、开始 Shift、新手教学入口、基础设置和项目免责声明展示。

## CaseAnalysis.unity — 病例分析主界面

承载单个病例的主要流程：查看初始线索、使用 Gene Scan / Cancer Galaxy / AI Assistant、通过 Evidence Board 整理已获得的信息，并提交一次最终诊断。

## CancerGalaxy.unity — 癌症星图

展示已知Train Reference Nodes、当前Test Case Node及该Case预先保存的5个最近Reference Nodes，并可查看近邻节点类别与必要的距离信息。UMAP空间邻近关系不是AI Prediction Probability。

## ResultSummary.unity — 结算与结果页

展示病例揭晓、玩家诊断、AI Assistant 结果、Score、调查评价、剩余 RP 和 Shift 总结。

## Tutorial.unity — 新手教学

首次启动时通过 Tutorial Case 分步骤介绍角色、病例档案、免费线索、Gene Scan、Cancer Galaxy、AI Assistant、诊断提交、结果展示和科学免责声明。Tutorial 不消耗 RP、不计入正式 Shift，也不影响正式统计。

场景之间应通过统一流程控制传递状态，避免在场景内重复维护全局数据。
