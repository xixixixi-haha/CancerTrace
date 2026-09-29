"""仅使用训练集生成 CancerTrace 二维 Cancer Galaxy 星图。"""

from pathlib import Path

import matplotlib
import numpy as np
import pandas as pd
import umap
from sklearn.decomposition import PCA
from sklearn.feature_selection import VarianceThreshold
from sklearn.metrics import silhouette_score
from sklearn.preprocessing import StandardScaler


# 使用非交互式后端，确保脚本可在命令行环境中直接保存图片。
matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402


# 本脚本只定义训练集路径，不读取或引用最终测试集文件。
PROJECT_ROOT = Path(__file__).resolve().parents[2]
TRAIN_PATH = PROJECT_ROOT / "data" / "processed" / "cancertrace_train.csv"
TABLES_DIR = PROJECT_ROOT / "results" / "tables"
FIGURES_DIR = PROJECT_ROOT / "results" / "figures"

COORDINATES_PATH = TABLES_DIR / "cancer_galaxy_train_coordinates.csv"
SUMMARY_PATH = TABLES_DIR / "cancer_galaxy_summary.csv"
FIGURE_PATH = FIGURES_DIR / "cancer_galaxy_train.png"

LABEL_COLUMN = "OncotreeLineage"
METADATA_COLUMNS = [
    "ModelID",
    "CellLineName",
    "OncotreeLineage",
    "OncotreePrimaryDisease",
    "OncotreeSubtype",
]


def main() -> None:
    """完成无监督预处理、PCA、UMAP、结构评价及结果保存。"""
    train_df = pd.read_csv(TRAIN_PATH, low_memory=False)

    missing_columns = [
        column for column in METADATA_COLUMNS if column not in train_df.columns
    ]
    if missing_columns:
        raise ValueError(f"训练数据缺少必要字段：{missing_columns}")
    if train_df[LABEL_COLUMN].isna().any():
        raise ValueError("训练数据的 OncotreeLineage 存在缺失值。")

    # 基因列名形如 “EGFR (1956)”。标签不参与任何特征筛选或降维拟合。
    gene_columns = [
        column for column in train_df.columns if "(" in column and ")" in column
    ]
    if not gene_columns:
        raise ValueError("未识别到符合命名格式的基因表达列。")

    class_counts = train_df[LABEL_COLUMN].value_counts()
    print("=== 训练集基本信息 ===")
    print(f"训练样本数：{len(train_df):,}")
    print(f"癌症类别数：{train_df[LABEL_COLUMN].nunique():,}")
    print(f"基因数量：{len(gene_columns):,}")
    print("每个类别样本数：")
    print(class_counts.rename("SampleCount").to_string())

    expression = train_df[gene_columns]

    # 预处理仅基于当前训练集表达值，不使用 OncotreeLineage。
    variance_filter = VarianceThreshold(threshold=0)
    expression_nonzero = variance_filter.fit_transform(expression)
    genes_after_filtering = int(expression_nonzero.shape[1])
    print(f"\n零方差过滤后基因数量：{genes_after_filtering:,}")

    scaler = StandardScaler()
    expression_scaled = scaler.fit_transform(expression_nonzero)

    pca = PCA(n_components=50, svd_solver="randomized", random_state=42)
    pca_coordinates = pca.fit_transform(expression_scaled)
    pca_explained_percent = float(pca.explained_variance_ratio_.sum() * 100)

    print(f"PCA 后维度：{pca_coordinates.shape}")
    print(f"前 50 个主成分累计解释方差比例：{pca_explained_percent:.2f}%")

    reducer = umap.UMAP(
        n_components=2,
        n_neighbors=15,
        min_dist=0.1,
        metric="euclidean",
        random_state=42,
    )
    umap_coordinates = reducer.fit_transform(pca_coordinates)

    # 标签仅在无监督嵌入完成后用于评价既有结构。
    labels = train_df[LABEL_COLUMN]
    pca_silhouette = float(silhouette_score(pca_coordinates, labels))
    umap_silhouette = float(silhouette_score(umap_coordinates, labels))

    print(f"PCA 50 维 silhouette score：{pca_silhouette:.4f}")
    print(f"UMAP 2 维 silhouette score：{umap_silhouette:.4f}")

    coordinates = train_df[METADATA_COLUMNS].copy()
    coordinates["UMAP1"] = umap_coordinates[:, 0]
    coordinates["UMAP2"] = umap_coordinates[:, 1]

    summary = pd.DataFrame(
        [
            {
                "TrainSamples": len(train_df),
                "GenesBeforeFiltering": len(gene_columns),
                "GenesAfterVarianceFilter": genes_after_filtering,
                "PCAComponents": pca_coordinates.shape[1],
                "PCAExplainedVariancePercent": pca_explained_percent,
                "PCASilhouette": pca_silhouette,
                "UMAPSilhouette": umap_silhouette,
            }
        ]
    )

    TABLES_DIR.mkdir(parents=True, exist_ok=True)
    FIGURES_DIR.mkdir(parents=True, exist_ok=True)
    coordinates.to_csv(COORDINATES_PATH, index=False, float_format="%.6f")
    summary.to_csv(SUMMARY_PATH, index=False, float_format="%.6f")

    # 使用 matplotlib 默认颜色循环，为八个类别绘制清晰且不过度装饰的散点图。
    figure, axis = plt.subplots(figsize=(11, 8))
    for lineage in class_counts.index:
        mask = labels == lineage
        axis.scatter(
            umap_coordinates[mask, 0],
            umap_coordinates[mask, 1],
            s=30,
            alpha=0.72,
            edgecolors="none",
            label=lineage,
        )

    axis.set_title("CancerTrace - Cancer Galaxy", fontsize=16)
    axis.set_xlabel("UMAP 1")
    axis.set_ylabel("UMAP 2")
    axis.grid(alpha=0.18, linewidth=0.6)
    axis.legend(
        title="OncotreeLineage",
        loc="center left",
        bbox_to_anchor=(1.02, 0.5),
        frameon=True,
    )
    figure.tight_layout()
    figure.savefig(FIGURE_PATH, dpi=300, bbox_inches="tight")
    plt.close(figure)

    print("\nCancerTrace Cancer Galaxy 训练集星图生成完成。")
    print(f"样本数：{len(train_df):,}")
    print(f"零方差过滤后基因数：{genes_after_filtering:,}")
    print(f"PCA 累计解释方差：{pca_explained_percent:.2f}%")
    print(f"PCA silhouette：{pca_silhouette:.4f}")
    print(f"UMAP silhouette：{umap_silhouette:.4f}")
    print(f"坐标文件位置：{COORDINATES_PATH}")
    print(f"星图图片位置：{FIGURE_PATH}")
    print(
        "本次降维仅使用645个训练样本，162个最终测试样本仍未使用。"
        "OncotreeLineage仅用于可视化着色和结构评价，没有参与PCA或UMAP拟合。"
    )


if __name__ == "__main__":
    main()
