"""使用锁定的 CancerTrace Pipeline 对封存测试集进行一次最终评估。"""

from pathlib import Path

import joblib
import matplotlib
import numpy as np
import pandas as pd
from sklearn.feature_selection import SelectKBest, VarianceThreshold, f_classif
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import (
    accuracy_score,
    balanced_accuracy_score,
    classification_report,
    confusion_matrix,
    f1_score,
    precision_score,
    recall_score,
)
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler


# 使用非交互式后端，确保脚本可直接保存高分辨率图片。
matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402


PROJECT_ROOT = Path(__file__).resolve().parents[2]
TRAIN_PATH = PROJECT_ROOT / "data" / "processed" / "cancertrace_train.csv"
TEST_PATH = PROJECT_ROOT / "data" / "processed" / "cancertrace_test.csv"
TABLES_DIR = PROJECT_ROOT / "results" / "tables"
FIGURES_DIR = PROJECT_ROOT / "results" / "figures"
MODELS_DIR = PROJECT_ROOT / "results" / "models"

CLASSIFICATION_REPORT_PATH = TABLES_DIR / "final_test_classification_report.csv"
CONFUSION_PATH = FIGURES_DIR / "final_test_confusion_matrix.png"
NORMALIZED_CONFUSION_PATH = (
    FIGURES_DIR / "final_test_confusion_matrix_normalized.png"
)
TOP_CONFUSIONS_PATH = TABLES_DIR / "top_test_confusions.csv"
PREDICTIONS_PATH = TABLES_DIR / "final_test_predictions.csv"
METRICS_PATH = TABLES_DIR / "final_test_metrics.csv"
MODEL_PATH = MODELS_DIR / "cancertrace_logistic_pipeline.joblib"
SELECTED_GENES_PATH = TABLES_DIR / "final_selected_genes.csv"

LABEL_COLUMN = "OncotreeLineage"
CLASS_LABELS = [
    "Lung",
    "Skin",
    "CNS/Brain",
    "Bowel",
    "Esophagus/Stomach",
    "Breast",
    "Bone",
    "Ovary/Fallopian Tube",
]
PROBABILITY_COLUMNS = {
    "Lung": "Prob_Lung",
    "Skin": "Prob_Skin",
    "CNS/Brain": "Prob_CNS_Brain",
    "Bowel": "Prob_Bowel",
    "Esophagus/Stomach": "Prob_Esophagus_Stomach",
    "Breast": "Prob_Breast",
    "Bone": "Prob_Bone",
    "Ovary/Fallopian Tube": "Prob_Ovary_Fallopian_Tube",
}


def save_confusion_figure(
    matrix: np.ndarray,
    output_path: Path,
    title: str,
    normalized: bool,
) -> None:
    """使用 matplotlib 保存带单元格数值的混淆矩阵。"""
    figure, axis = plt.subplots(figsize=(11, 9))
    image = axis.imshow(matrix, interpolation="nearest", cmap="Blues")
    figure.colorbar(image, ax=axis, fraction=0.046, pad=0.04)

    positions = np.arange(len(CLASS_LABELS))
    axis.set(
        xticks=positions,
        yticks=positions,
        xticklabels=CLASS_LABELS,
        yticklabels=CLASS_LABELS,
        xlabel="Predicted",
        ylabel="True",
        title=title,
    )
    axis.tick_params(axis="x", labelrotation=45)
    for label in axis.get_xticklabels():
        label.set_horizontalalignment("right")

    threshold = float(matrix.max()) / 2 if matrix.size else 0
    for row in range(matrix.shape[0]):
        for column in range(matrix.shape[1]):
            value = matrix[row, column]
            text = f"{value:.2f}" if normalized else f"{int(value)}"
            axis.text(
                column,
                row,
                text,
                ha="center",
                va="center",
                color="white" if value > threshold else "black",
                fontsize=9,
            )

    figure.tight_layout()
    figure.savefig(output_path, dpi=300, bbox_inches="tight")
    plt.close(figure)


def main() -> None:
    """拟合一次锁定模型，并对最终测试集执行一次预测与评估。"""
    train_df = pd.read_csv(TRAIN_PATH, low_memory=False)
    test_df = pd.read_csv(TEST_PATH, low_memory=False)

    required_columns = ["ModelID", "CellLineName", LABEL_COLUMN]
    for dataset_name, dataset in (("Train", train_df), ("Test", test_df)):
        missing = [column for column in required_columns if column not in dataset.columns]
        if missing:
            raise ValueError(f"{dataset_name} 数据缺少必要字段：{missing}")
        if dataset[LABEL_COLUMN].isna().any():
            raise ValueError(f"{dataset_name} 数据的 {LABEL_COLUMN} 存在缺失值。")

    train_gene_columns = [
        column for column in train_df.columns if "(" in column and ")" in column
    ]
    test_gene_columns = [
        column for column in test_df.columns if "(" in column and ")" in column
    ]
    if not train_gene_columns:
        raise ValueError("训练数据中未识别到符合命名格式的基因表达列。")
    if train_gene_columns != test_gene_columns:
        raise ValueError("Train/Test 的基因列名称或顺序不一致。")

    train_ids = set(train_df["ModelID"].dropna())
    test_ids = set(test_df["ModelID"].dropna())
    overlapping_ids = train_ids.intersection(test_ids)

    print("=== Train/Test 基本信息 ===")
    print(f"Train 样本数：{len(train_df):,}")
    print(f"Test 样本数：{len(test_df):,}")
    print(
        "癌症类别数："
        f"{len(set(train_df[LABEL_COLUMN]).union(test_df[LABEL_COLUMN])):,}"
    )
    print(f"Train/Test ModelID 重叠数量：{len(overlapping_ids):,}")
    print(f"基因数量：{len(train_gene_columns):,}")

    if overlapping_ids:
        preview = sorted(overlapping_ids)[:10]
        raise RuntimeError(
            "检测到 Train/Test ModelID 重叠，已停止最终评估。"
            f"前 10 个重叠 ModelID：{preview}"
        )

    X_train = train_df[train_gene_columns]
    y_train = train_df[LABEL_COLUMN]
    X_test = test_df[train_gene_columns]
    y_test = test_df[LABEL_COLUMN]

    # 模型方案已经锁定；禁止在此处搜索参数或根据测试结果调整 Pipeline。
    final_pipeline = Pipeline(
        steps=[
            ("variance", VarianceThreshold(threshold=0)),
            ("feature_selection", SelectKBest(score_func=f_classif, k=2000)),
            ("scaler", StandardScaler()),
            (
                "model",
                LogisticRegression(C=1.0, class_weight=None, max_iter=5000),
            ),
        ]
    )

    print("\n正在使用全部训练样本拟合锁定的最终 Pipeline……")
    final_pipeline.fit(X_train, y_train)

    # 最终测试集仅在此处预测一次，不参与任何 fit 或参数选择。
    y_pred = final_pipeline.predict(X_test)
    y_probability = final_pipeline.predict_proba(X_test)

    metrics = {
        "Accuracy": float(accuracy_score(y_test, y_pred)),
        "Macro_F1": float(f1_score(y_test, y_pred, average="macro")),
        "Balanced_Accuracy": float(balanced_accuracy_score(y_test, y_pred)),
        "Macro_Precision": float(
            precision_score(y_test, y_pred, average="macro", zero_division=0)
        ),
        "Macro_Recall": float(
            recall_score(y_test, y_pred, average="macro", zero_division=0)
        ),
    }

    report = classification_report(
        y_test,
        y_pred,
        labels=CLASS_LABELS,
        target_names=CLASS_LABELS,
        output_dict=True,
        zero_division=0,
    )
    report_table = pd.DataFrame(
        [
            {
                "OncotreeLineage": label,
                "Precision": report[label]["precision"],
                "Recall": report[label]["recall"],
                "F1-score": report[label]["f1-score"],
                "Support": int(report[label]["support"]),
            }
            for label in CLASS_LABELS
        ]
    )

    confusion = confusion_matrix(y_test, y_pred, labels=CLASS_LABELS)
    row_totals = confusion.sum(axis=1, keepdims=True)
    normalized_confusion = np.divide(
        confusion,
        row_totals,
        out=np.zeros_like(confusion, dtype=float),
        where=row_totals != 0,
    )

    confusion_rows = []
    for true_index, true_class in enumerate(CLASS_LABELS):
        for predicted_index, predicted_class in enumerate(CLASS_LABELS):
            if true_index == predicted_index:
                continue
            error_count = int(confusion[true_index, predicted_index])
            if error_count > 0:
                confusion_rows.append(
                    {
                        "TrueClass": true_class,
                        "PredictedClass": predicted_class,
                        "ErrorCount": error_count,
                    }
                )
    top_confusions = (
        pd.DataFrame(
            confusion_rows,
            columns=["TrueClass", "PredictedClass", "ErrorCount"],
        )
        .sort_values(
            ["ErrorCount", "TrueClass", "PredictedClass"],
            ascending=[False, True, True],
        )
        .head(10)
        .reset_index(drop=True)
    )

    predictions = pd.DataFrame(
        {
            "ModelID": test_df["ModelID"].to_numpy(),
            "CellLineName": test_df["CellLineName"].to_numpy(),
            "TrueClass": y_test.to_numpy(),
            "PredictedClass": y_pred,
            "Confidence": y_probability.max(axis=1),
            "Correct": y_pred == y_test.to_numpy(),
        }
    )
    probability_class_order = final_pipeline.named_steps["model"].classes_
    probability_indices = {
        label: index for index, label in enumerate(probability_class_order)
    }
    for label in CLASS_LABELS:
        predictions[PROBABILITY_COLUMNS[label]] = y_probability[
            :, probability_indices[label]
        ]

    metrics_table = pd.DataFrame(
        [{"Metric": name, "Value": value} for name, value in metrics.items()]
    )

    variance_step = final_pipeline.named_steps["variance"]
    selection_step = final_pipeline.named_steps["feature_selection"]
    genes_after_variance = np.asarray(train_gene_columns)[
        variance_step.get_support()
    ]
    selected_genes = genes_after_variance[selection_step.get_support()]
    selected_genes_table = pd.DataFrame({"Gene": selected_genes})

    TABLES_DIR.mkdir(parents=True, exist_ok=True)
    FIGURES_DIR.mkdir(parents=True, exist_ok=True)
    MODELS_DIR.mkdir(parents=True, exist_ok=True)

    report_table.to_csv(
        CLASSIFICATION_REPORT_PATH, index=False, float_format="%.6f"
    )
    top_confusions.to_csv(TOP_CONFUSIONS_PATH, index=False)
    predictions.to_csv(PREDICTIONS_PATH, index=False, float_format="%.6f")
    metrics_table.to_csv(METRICS_PATH, index=False, float_format="%.6f")
    selected_genes_table.to_csv(SELECTED_GENES_PATH, index=False)
    joblib.dump(final_pipeline, MODEL_PATH)

    save_confusion_figure(
        confusion,
        CONFUSION_PATH,
        "CancerTrace Final Test Confusion Matrix",
        normalized=False,
    )
    save_confusion_figure(
        normalized_confusion,
        NORMALIZED_CONFUSION_PATH,
        "CancerTrace Final Test Confusion Matrix (Row Normalized)",
        normalized=True,
    )

    print("\n=== 每个癌症类别的测试指标 ===")
    print(report_table.to_string(index=False, float_format=lambda value: f"{value:.4f}"))
    print("\n=== 错误数量最多的前 10 个混淆方向 ===")
    if top_confusions.empty:
        print("无错误预测。")
    else:
        print(top_confusions.to_string(index=False))

    error_examples = predictions.loc[
        ~predictions["Correct"],
        ["CellLineName", "TrueClass", "PredictedClass", "Confidence"],
    ].head(20)
    print("\n=== 前 20 个预测错误的测试样本 ===")
    if error_examples.empty:
        print("无错误预测。")
    else:
        print(
            error_examples.to_string(
                index=False, float_format=lambda value: f"{value:.4f}"
            )
        )

    correct_count = int(predictions["Correct"].sum())
    error_count = len(predictions) - correct_count

    print("\n==============================")
    print("CancerTrace 最终测试集评估")
    print("==============================")
    print(f"Test Accuracy:\n{metrics['Accuracy']:.4f}")
    print(f"Test Macro F1:\n{metrics['Macro_F1']:.4f}")
    print(f"Test Balanced Accuracy:\n{metrics['Balanced_Accuracy']:.4f}")
    print(f"Macro Precision:\n{metrics['Macro_Precision']:.4f}")
    print(f"Macro Recall:\n{metrics['Macro_Recall']:.4f}")
    print(f"测试样本数：\n{len(test_df):,}")
    print(f"预测正确数量：\n{correct_count:,}")
    print(f"预测错误数量：\n{error_count:,}")
    print(
        "162个最终测试样本已完成一次性评估。"
        "从此不能再使用该测试集进行模型选择、参数调优或特征筛选。"
    )
    print(
        "后续可以使用这些预测结果构建 CancerTrace 游戏内容，"
        "但不得根据测试结果重新优化模型。"
    )


if __name__ == "__main__":
    main()
