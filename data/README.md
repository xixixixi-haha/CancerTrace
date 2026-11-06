# 数据来源

CancerTrace 的分析输入来自 Broad Institute 的 **DepMap Public 26Q1**。两份本地原始 CSV 的 Windows 下载元数据均记录了 DepMap 来源，下载对象标识同属 `public-26q1-5bbf`。版本也可对照 [DepMap 26Q1 发布说明](https://forum.depmap.org/t/announcing-the-26q1-release/4606)。

| 本地原始文件 | 项目用途 | 下载记录中的对象（省略签名） |
| --- | --- | --- |
| `raw/Model.csv` | 癌细胞系模型及谱系注释 | [public-26q1-5bbf.37](https://storage.googleapis.com/depmap-external-downloads/downloads-by-canonical-id/public-26q1-5bbf.37/Model.csv) |
| `raw/OmicsExpressionTPMLogp1HumanProteinCodingGenesStranded.csv` | 蛋白编码基因表达矩阵 | [public-26q1-5bbf.27](https://storage.googleapis.com/depmap-external-downloads/downloads-by-canonical-id/public-26q1-5bbf.27/OmicsExpressionTPMLogp1HumanProteinCodingGenesStranded.csv) |

原始来源为 [DepMap Portal](https://depmap.org/portal/)。通过 [DepMap 数据下载页](https://depmap.org/portal/data_page/?tab=allData) 选择 `DepMap Public 26Q1` 和对应文件名。下载记录中的临时签名参数不写入仓库。

## 引用

- 数据集：DepMap, Broad (2026). *DepMap Public 26Q1* [数据集]。[DepMap Portal](https://depmap.org/portal/) · [26Q1 发布说明](https://forum.depmap.org/t/announcing-the-26q1-release/4606)
- 项目参考文献：Arafeh, R., Shibue, T., Dempster, J. M., et al. (2025). The present and future of the Cancer Dependency Map. *Nature Reviews Cancer*, 25, 59–73. [DOI](https://doi.org/10.1038/s41568-024-00763-x) · [DepMap 引用指南](https://depmap.org/portal/data_page/)

**待核实**　26Q1 数据集的独立 DOI 或 Figshare 存档，以及两份文件对应的上游校验值。现有下载元数据支持来源和发布版本，未记录可用于逐字节比对的上游摘要。

## 目录与下游数据

- `raw/` 保存本地下载的原始数据，原始 CSV 不随仓库提交。
- `processed/` 保存本地处理后的中间数据。
- [Python 分析与导出流程](../python/README.md) 生成供游戏使用的派生数据，冻结版本与用途见 [游戏数据说明](../results/game_data/README.md)。
