# Cancer Galaxy

状态：v1.0规则已同步（实现细节待补充）  
规则来源：`../Game_Design_Document_v1.0.md` 第 8.2、10.2 节

## 已确认规则

- Cancer Galaxy 是癌症表达空间探索系统。
- 每次使用消耗 25 RP。
- 每个病例最多使用一次。
- Tutorial中使用不消耗RP，且不影响正式统计。
- 二维空间来源于 RNA-seq 高维基因表达数据的降维结果。
- 使用冻结数据中的Case坐标、Reference Nodes和每个Case预先保存的5个最近Reference Nodes。
- 不展示距离计算过程或复杂数学公式。
- UMAP二维空间距离只表示降维后的空间邻近关系，不是AI Prediction Probability。
- v1.0不新增Top 3 nearest cancer types算法，也不修改Galaxy科学数据。
- Cancer Galaxy不得自动输出最终答案、最可能癌症类型或推荐诊断。
- Galaxy只提供空间证据，最终诊断由玩家完成。

## 固定输出

1. 当前未知Case在Cancer Galaxy中的位置。
2. Reference Nodes。
3. 当前Case最近的5个Reference Nodes。
4. 每个近邻节点所属癌症类别。
5. 必要的邻近距离信息。

## 待补充

- 坐标、参考样本和类别标识的数据契约。
- 缩放、平移、选点和提示信息等交互约定。

本文档不得自行新增或修改 GDD 中的游戏规则。
