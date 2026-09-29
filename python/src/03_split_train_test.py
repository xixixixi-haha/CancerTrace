"""将 CancerTrace 完整分类数据集固定划分为训练集和最终测试集。"""

from pathlib import Path

import pandas as pd
from sklearn.model_selection import train_test_split


# 脚本位于 <项目根目录>/python/src/，据此自动定位项目根目录。
PROJECT_ROOT = Path(__file__).resolve().parents[2]
INPUT_PATH = PROJECT_ROOT / "data" / "processed" / "cancertrace_classification_full.csv"
PROCESSED_DIR = PROJECT_ROOT / "data" / "processed"
TABLES_DIR = PROJECT_ROOT / "results" / "tables"

TRAIN_PATH = PROCESSED_DIR / "cancertrace_train.csv"
TEST_PATH = PROCESSED_DIR / "cancertrace_test.csv"
MANIFEST_PATH = TABLES_DIR / "train_test_manifest.csv"
TRAIN_DISTRIBUTION_PATH = TABLES_DIR / "train_class_distribution.csv"
TEST_DISTRIBUTION_PATH = TABLES_DIR / "test_class_distribution.csv"

METADATA_COLUMNS = [
    "ModelID",
    "CellLineName",
    "OncotreeLineage",
    "OncotreePrimaryDisease",
    "OncotreeSubtype",
]


def build_class_distribution(data: pd.DataFrame) -> pd.DataFrame:
    """生成按样本数降序排列的类别数量和百分比表。"""
    distribution = (
        data["OncotreeLineage"]
        .value_counts()
        .rename_axis("OncotreeLineage")
        .reset_index(name="SampleCount")
    )
    distribution["Percentage"] = (
        distribution["SampleCount"] / len(data) * 100
    ).round(2)
    return distribution.sort_values(
        ["SampleCount", "OncotreeLineage"], ascending=[False, True]
    ).reset_index(drop=True)


def print_distribution(title: str, distribution: pd.DataFrame) -> None:
    """打印一张小型类别分布表。"""
    print(f"\n=== {title} ===")
    print(distribution.to_string(index=False))


def main() -> None:
    """执行固定分层划分、泄漏检查并保存数据与清单。"""
    df = pd.read_csv(INPUT_PATH, low_memory=False)

    missing_columns = [column for column in METADATA_COLUMNS if column not in df.columns]
    if missing_columns:
        raise ValueError(f"输入数据缺少必要字段：{missing_columns}")
    if df["OncotreeLineage"].isna().any():
        raise ValueError("OncotreeLineage 存在缺失值，无法执行可靠的分层划分。")

    class_counts = df["OncotreeLineage"].value_counts()
    print("=== 完整数据集基本信息 ===")
    print(f"总样本数：{len(df):,}")
    print(f"总类别数：{df['OncotreeLineage'].nunique():,}")
    print("每个类别的样本数量：")
    print(class_counts.rename("SampleCount").to_string())

    # 基因列名形如 “EGFR (1956)”，这里只识别和计数，不删除任何基因。
    gene_columns = [
        column for column in df.columns if "(" in column and ")" in column
    ]
    if not gene_columns:
        raise ValueError("未识别到符合命名格式的基因表达列。")
    print(f"\n基因数量：{len(gene_columns):,}")

    # 固定随机种子并按癌症类别分层，最终测试集从此保持封存。
    train_df, test_df = train_test_split(
        df,
        test_size=0.20,
        random_state=42,
        stratify=df["OncotreeLineage"],
    )
    train_df = train_df.reset_index(drop=True)
    test_df = test_df.reset_index(drop=True)

    train_ids = set(train_df["ModelID"].dropna())
    test_ids = set(test_df["ModelID"].dropna())
    overlapping_ids = train_ids.intersection(test_ids)

    print("\n=== Train/Test 数据泄漏检查 ===")
    print(f"Training Set 样本数：{len(train_df):,}")
    print(f"Test Set 样本数：{len(test_df):,}")
    print(f"Train ModelID 唯一数量：{train_df['ModelID'].nunique():,}")
    print(f"Test ModelID 唯一数量：{test_df['ModelID'].nunique():,}")
    print(f"Train / Test ModelID 重叠数量：{len(overlapping_ids):,}")

    if overlapping_ids:
        preview = sorted(overlapping_ids)[:10]
        raise RuntimeError(
            "检测到 Train/Test ModelID 重叠，已停止保存。"
            f"前 10 个重叠 ModelID：{preview}"
        )

    full_distribution = build_class_distribution(df)
    train_distribution = build_class_distribution(train_df)
    test_distribution = build_class_distribution(test_df)

    print_distribution("全部数据类别分布", full_distribution)
    print_distribution("Training Set 类别分布", train_distribution)
    print_distribution("Test Set 类别分布", test_distribution)

    PROCESSED_DIR.mkdir(parents=True, exist_ok=True)
    TABLES_DIR.mkdir(parents=True, exist_ok=True)

    # 完整保存全部元数据与基因，不做标准化、特征筛选或任何降维。
    train_df.to_csv(TRAIN_PATH, index=False)
    test_df.to_csv(TEST_PATH, index=False)

    train_manifest = train_df[
        ["ModelID", "CellLineName", "OncotreeLineage"]
    ].copy()
    train_manifest["Set"] = "Train"
    test_manifest = test_df[["ModelID", "CellLineName", "OncotreeLineage"]].copy()
    test_manifest["Set"] = "Test"
    manifest = pd.concat([train_manifest, test_manifest], ignore_index=True)
    manifest.to_csv(MANIFEST_PATH, index=False)

    train_distribution.to_csv(
        TRAIN_DISTRIBUTION_PATH, index=False, float_format="%.2f"
    )
    test_distribution.to_csv(
        TEST_DISTRIBUTION_PATH, index=False, float_format="%.2f"
    )

    print("\nCancerTrace Train/Test 固定划分完成。")
    print(f"总样本数：{len(df):,}")
    print(f"Train 样本数：{len(train_df):,}")
    print(f"Test 样本数：{len(test_df):,}")
    print(f"Train/Test 重叠数量：{len(overlapping_ids):,}")
    print(f"训练集保存位置：{TRAIN_PATH}")
    print(f"测试集保存位置：{TEST_PATH}")
    print(f"manifest 保存位置：{MANIFEST_PATH}")
    print(
        "从此步骤开始，最终测试集必须保持封存，"
        "不参与任何模型选择、特征筛选或参数调优。"
    )


if __name__ == "__main__":
    main()
