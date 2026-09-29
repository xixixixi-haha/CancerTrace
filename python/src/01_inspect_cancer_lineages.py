"""统计具有默认 RNA-seq 数据的癌细胞模型之 OncotreeLineage 分布。"""

from pathlib import Path

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
OUTPUT_DIR = PROJECT_ROOT / "results" / "tables"


def main() -> None:
    """读取元数据、统计癌症组织来源，并导出完整及候选类别表。"""
    model_df = pd.read_csv(
        MODEL_PATH,
        usecols=["ModelID", "OncotreeLineage"],
        dtype={"ModelID": "string", "OncotreeLineage": "string"},
    )

    # RNA-seq 文件很宽，只读取分析所需的两个元数据字段。
    rna_metadata = pd.read_csv(
        RNA_PATH,
        usecols=["ModelID", "IsDefaultEntryForModel"],
        dtype={"ModelID": "string", "IsDefaultEntryForModel": "string"},
    )
    default_rna = rna_metadata.loc[
        rna_metadata["IsDefaultEntryForModel"] == "Yes"
    ].copy()

    default_rna_duplicate_count = int(default_rna["ModelID"].duplicated().sum())
    model_duplicate_count = int(model_df["ModelID"].duplicated().sum())

    print("=== 基本信息 ===")
    print(f"Model.csv 总记录数：{len(model_df):,}")
    print(f"RNA-seq 元数据记录数：{len(rna_metadata):,}")
    print(f"默认 RNA-seq 记录数：{len(default_rna):,}")
    print(
        "默认 RNA-seq 中 ModelID 重复数量："
        f"{default_rna_duplicate_count:,}（不含首次出现）"
    )
    print(
        f"Model.csv 中 ModelID 重复数量：{model_duplicate_count:,}（不含首次出现）"
    )

    # 每个 ModelID 代表一个细胞模型；报告重复数后再去重，避免连接放大计数。
    default_models = default_rna.dropna(subset=["ModelID"]).drop_duplicates("ModelID")
    model_lineages = model_df.dropna(subset=["ModelID"]).drop_duplicates("ModelID")
    matched = default_models.merge(
        model_lineages[["ModelID", "OncotreeLineage"]],
        on="ModelID",
        how="inner",
        validate="one_to_one",
    )

    # 将纯空白组织来源也视为缺失值。
    matched["OncotreeLineage"] = matched["OncotreeLineage"].str.strip()
    matched.loc[matched["OncotreeLineage"] == "", "OncotreeLineage"] = pd.NA

    missing_lineage_count = int(matched["OncotreeLineage"].isna().sum())
    valid_lineages = matched["OncotreeLineage"].dropna()
    valid_sample_count = len(valid_lineages)

    lineage_counts = (
        valid_lineages.value_counts()
        .rename_axis("OncotreeLineage")
        .reset_index(name="SampleCount")
    )
    lineage_counts["Percentage"] = (
        lineage_counts["SampleCount"] / valid_sample_count * 100
    ).round(2)
    lineage_counts = lineage_counts.sort_values(
        ["SampleCount", "OncotreeLineage"], ascending=[False, True]
    ).reset_index(drop=True)

    print("\n=== 匹配结果 ===")
    print(f"总细胞模型数：{len(matched):,}")
    print(f"OncotreeLineage 缺失数量：{missing_lineage_count:,}")
    print(f"有效 OncotreeLineage 类别数量：{len(lineage_counts):,}")

    print("\n=== 样本数最多的前 20 个 OncotreeLineage ===")
    print(lineage_counts.head(20).to_string(index=False))

    for threshold in (30, 50, 80):
        category_count = int((lineage_counts["SampleCount"] >= threshold).sum())
        print(f"样本数 >= {threshold} 的类别数量：{category_count:,}")

    candidates = lineage_counts.loc[lineage_counts["SampleCount"] >= 50].copy()

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    lineage_counts.to_csv(
        OUTPUT_DIR / "cancer_lineage_counts.csv", index=False, float_format="%.2f"
    )
    candidates.to_csv(
        OUTPUT_DIR / "cancer_lineage_candidates.csv", index=False, float_format="%.2f"
    )

    print("\n=== 候选类别（SampleCount >= 50）===")
    print(candidates.to_string(index=False))
    print("\n本脚本仅用于统计癌症类别分布，不进行模型训练，也不决定最终分类类别。")


if __name__ == "__main__":
    main()
