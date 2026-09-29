"""准备 CancerTrace 八类癌症分类的完整基因表达数据集。"""

from pathlib import Path

import numpy as np
import pandas as pd


# 脚本位于 <项目根目录>/python/src/，据此自动定位项目根目录。
PROJECT_ROOT = Path(__file__).resolve().parents[2]
MODEL_PATH = PROJECT_ROOT / "data" / "raw" / "Model.csv"
RNA_PATH = (
    PROJECT_ROOT
    / "data"
    / "raw"
    / "OmicsExpressionTPMLogp1HumanProteinCodingGenesStranded.csv"
)
PROCESSED_DIR = PROJECT_ROOT / "data" / "processed"
TABLES_DIR = PROJECT_ROOT / "results" / "tables"

FULL_DATASET_PATH = PROCESSED_DIR / "cancertrace_classification_full.csv"
SAMPLE_MANIFEST_PATH = TABLES_DIR / "cancertrace_sample_manifest.csv"
CLASS_COUNTS_PATH = TABLES_DIR / "cancertrace_class_counts.csv"
ZERO_VARIANCE_PATH = TABLES_DIR / "zero_variance_genes_full_dataset.csv"

MODEL_COLUMNS = [
    "ModelID",
    "CellLineName",
    "OncotreeLineage",
    "OncotreePrimaryDisease",
    "OncotreeSubtype",
]

TARGET_LINEAGES = [
    "Lung",
    "Skin",
    "CNS/Brain",
    "Bowel",
    "Esophagus/Stomach",
    "Breast",
    "Bone",
    "Ovary/Fallopian Tube",
]


def print_table(table: pd.DataFrame) -> None:
    """以不显示行索引的形式打印小型统计表。"""
    print(table.to_string(index=False))


def main() -> None:
    """构建完整分类数据集并执行必要的数据质量检查。"""
    model_df = pd.read_csv(
        MODEL_PATH,
        usecols=MODEL_COLUMNS,
        dtype={column: "string" for column in MODEL_COLUMNS},
    )

    model_id_missing = int(model_df["ModelID"].isna().sum())
    model_id_duplicates = int(model_df["ModelID"].duplicated().sum())

    print("=== Model.csv 基本信息 ===")
    print(f"Model.csv 总记录数：{len(model_df):,}")
    print(f"ModelID 缺失数量：{model_id_missing:,}")
    print(f"ModelID 重复数量：{model_id_duplicates:,}（不含首次出现）")

    # 使用 isin 进行精确类别匹配，不使用 contains 等模糊规则。
    target_models = model_df.loc[
        model_df["OncotreeLineage"].isin(TARGET_LINEAGES), MODEL_COLUMNS
    ].copy()

    selected_counts = (
        target_models["OncotreeLineage"]
        .value_counts()
        .reindex(TARGET_LINEAGES, fill_value=0)
        .rename_axis("OncotreeLineage")
        .reset_index(name="SampleCount")
    )

    print("\n=== 8 个目标癌症类别 ===")
    print(f"筛选后的 Model 数量：{len(target_models):,}")
    print_table(selected_counts)

    print("\n正在读取 RNA-seq 表达矩阵……")
    rna_df = pd.read_csv(
        RNA_PATH,
        dtype={
            "ModelID": "string",
            "IsDefaultEntryForModel": "string",
        },
        low_memory=False,
    )
    rna_original_count = len(rna_df)

    if "Unnamed: 0" in rna_df.columns:
        rna_df = rna_df.drop(columns="Unnamed: 0")
        print('已删除无用列："Unnamed: 0"')

    # 基因列名形如 “EGFR (1956)”，同时含有左右括号。
    gene_columns = [
        column for column in rna_df.columns if "(" in column and ")" in column
    ]
    if not gene_columns:
        raise ValueError("未识别到符合命名格式的基因表达列。")

    default_rna = rna_df.loc[
        rna_df["IsDefaultEntryForModel"] == "Yes",
        ["ModelID", *gene_columns],
    ].copy()
    default_count = len(default_rna)
    default_id_missing = int(default_rna["ModelID"].isna().sum())
    default_id_duplicates = int(default_rna["ModelID"].duplicated().sum())

    print("\n=== RNA-seq 基本信息 ===")
    print(f"RNA-seq 原始记录数：{rna_original_count:,}")
    print(f"默认表达记录数：{default_count:,}")
    print(f"默认表达中 ModelID 缺失数量：{default_id_missing:,}")
    print(
        f"默认表达中 ModelID 重复数量：{default_id_duplicates:,}（不含首次出现）"
    )
    print(f"识别出的基因数量：{len(gene_columns):,}")

    if default_id_duplicates:
        print(
            "警告：默认表达记录中发现重复 ModelID；"
            "为保证每个模型仅出现一次，将保留每个 ModelID 的第一条记录。"
        )
        default_rna = default_rna.drop_duplicates(subset="ModelID", keep="first")

    # 缺失 ModelID 无法参与连接；若 Model 表存在重复，也显式提醒后保留第一条。
    default_rna = default_rna.dropna(subset=["ModelID"])
    target_duplicate_count = int(target_models["ModelID"].duplicated().sum())
    if target_duplicate_count:
        print(
            "警告：目标类别的 Model.csv 记录中发现重复 ModelID；"
            "连接前将保留每个 ModelID 的第一条记录。"
        )
        target_models = target_models.drop_duplicates(subset="ModelID", keep="first")
    target_models = target_models.dropna(subset=["ModelID"])

    final_df = target_models.merge(
        default_rna,
        on="ModelID",
        how="inner",
        validate="one_to_one",
    )
    final_df = final_df[[*MODEL_COLUMNS, *gene_columns]]

    final_sample_count = len(final_df)
    final_gene_count = len(gene_columns)
    final_model_duplicates = int(final_df["ModelID"].duplicated().sum())
    final_lineage_missing = int(final_df["OncotreeLineage"].isna().sum())

    gene_data = final_df[gene_columns]
    missing_mask = gene_data.isna()
    missing_value_count = int(missing_mask.to_numpy().sum())
    genes_with_missing = int(missing_mask.any(axis=0).sum())

    print("\n=== 最终数据质量检查 ===")
    print(f"最终样本总数：{final_sample_count:,}")
    print(f"最终基因数量：{final_gene_count:,}")
    print(f"最终数据维度：{final_df.shape}")
    print(f"ModelID 重复数量：{final_model_duplicates:,}")
    print(f"OncotreeLineage 缺失数量：{final_lineage_missing:,}")
    print(f"所有基因表达值的缺失值总数：{missing_value_count:,}")
    print(f"含有缺失值的基因数量：{genes_with_missing:,}")

    if final_sample_count != 807:
        print(
            f"提醒：最终样本数为 {final_sample_count:,}，"
            "与前一步估计的约 807 个样本不同，请以当前真实结果为准。"
        )

    final_counts = (
        final_df["OncotreeLineage"]
        .value_counts()
        .reindex(TARGET_LINEAGES, fill_value=0)
        .rename_axis("OncotreeLineage")
        .reset_index(name="SampleCount")
    )
    final_counts["SampleCount"] = final_counts["SampleCount"].astype(int)
    final_counts["Percentage"] = np.where(
        final_sample_count > 0,
        final_counts["SampleCount"] / final_sample_count * 100,
        0.0,
    )
    final_counts["Percentage"] = final_counts["Percentage"].round(2)

    print("\n=== 最终 8 个类别的样本数量与百分比 ===")
    print_table(final_counts)

    # 这里只检查零方差基因，不删除；正式过滤将在交叉验证 Pipeline 内完成。
    gene_variances = gene_data.var(axis=0)
    zero_variance_mask = gene_variances.eq(0)
    zero_variance_genes = gene_variances.loc[zero_variance_mask]
    zero_variance_table = (
        zero_variance_genes.rename_axis("Gene").reset_index(name="Variance")
    )

    print("\n=== 零方差基因检查 ===")
    print(f"零方差基因数量：{len(zero_variance_genes):,}")
    print(f"非零方差基因数量：{final_gene_count - len(zero_variance_genes):,}")
    if zero_variance_genes.empty:
        print("前 20 个零方差基因：无")
    else:
        print("前 20 个零方差基因名称：")
        for gene in zero_variance_genes.index[:20]:
            print(f"- {gene}")

    PROCESSED_DIR.mkdir(parents=True, exist_ok=True)
    TABLES_DIR.mkdir(parents=True, exist_ok=True)

    final_df.to_csv(FULL_DATASET_PATH, index=False)
    final_df[MODEL_COLUMNS].to_csv(SAMPLE_MANIFEST_PATH, index=False)
    final_counts.to_csv(CLASS_COUNTS_PATH, index=False, float_format="%.2f")
    zero_variance_table.to_csv(ZERO_VARIANCE_PATH, index=False)

    print("\nCancerTrace 8 类癌症分类数据集准备完成。")
    print(f"最终样本数：{final_sample_count:,}")
    print(f"最终类别数：{final_df['OncotreeLineage'].nunique():,}")
    print(f"最终基因数：{final_gene_count:,}")
    print(f"完整数据集保存位置：{FULL_DATASET_PATH}")
    print(f"样本清单保存位置：{SAMPLE_MANIFEST_PATH}")
    print(f"类别统计表保存位置：{CLASS_COUNTS_PATH}")
    print("本阶段尚未进行训练集/测试集划分、特征筛选或模型训练。")


if __name__ == "__main__":
    main()
