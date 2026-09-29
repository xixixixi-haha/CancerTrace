"""在封存测试集的前提下建立 CancerTrace 首个分类 baseline。"""

from pathlib import Path

import numpy as np
import pandas as pd
from sklearn.dummy import DummyClassifier
from sklearn.feature_selection import SelectKBest, VarianceThreshold, f_classif
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import GridSearchCV, StratifiedKFold, cross_validate
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler


# 本脚本只定义训练集路径，不读取或引用最终测试集文件。
PROJECT_ROOT = Path(__file__).resolve().parents[2]
TRAIN_PATH = PROJECT_ROOT / "data" / "processed" / "cancertrace_train.csv"
TABLES_DIR = PROJECT_ROOT / "results" / "tables"
COMPARISON_PATH = TABLES_DIR / "baseline_classification_comparison.csv"
CV_RESULTS_PATH = TABLES_DIR / "logistic_regression_cv_results.csv"

LABEL_COLUMN = "OncotreeLineage"
SCORING = {
    "accuracy": "accuracy",
    "f1_macro": "f1_macro",
    "balanced_accuracy": "balanced_accuracy",
}


def mean_cv_scores(cv_result: dict[str, np.ndarray]) -> dict[str, float]:
    """从 cross_validate 结果中提取三个评价指标的平均值。"""
    return {
        "CV_Accuracy": float(np.mean(cv_result["test_accuracy"])),
        "CV_Macro_F1": float(np.mean(cv_result["test_f1_macro"])),
        "CV_Balanced_Accuracy": float(
            np.mean(cv_result["test_balanced_accuracy"])
        ),
    }


def main() -> None:
    """仅使用训练集完成 Dummy 与逻辑回归的五折交叉验证比较。"""
    train_df = pd.read_csv(TRAIN_PATH, low_memory=False)
    if LABEL_COLUMN not in train_df.columns:
        raise ValueError(f"训练数据缺少目标标签列：{LABEL_COLUMN}")
    if train_df[LABEL_COLUMN].isna().any():
        raise ValueError("训练数据的 OncotreeLineage 存在缺失值。")

    # 基因列名形如 “EGFR (1956)”。本阶段不在 Pipeline 外删除任何基因。
    gene_columns = [
        column for column in train_df.columns if "(" in column and ")" in column
    ]
    if not gene_columns:
        raise ValueError("未识别到符合命名格式的基因表达列。")

    class_counts = train_df[LABEL_COLUMN].value_counts()
    print("=== 训练集基本信息 ===")
    print(f"训练样本数：{len(train_df):,}")
    print(f"癌症类别数：{train_df[LABEL_COLUMN].nunique():,}")
    print("每类样本数量：")
    print(class_counts.rename("SampleCount").to_string())
    print(f"基因数量：{len(gene_columns):,}")

    X = train_df[gene_columns]
    y = train_df[LABEL_COLUMN]

    cv = StratifiedKFold(n_splits=5, shuffle=True, random_state=42)

    print("\n=== DummyClassifier 五折交叉验证 ===")
    dummy = DummyClassifier(strategy="most_frequent")
    dummy_cv = cross_validate(
        dummy,
        X,
        y,
        cv=cv,
        scoring=SCORING,
        return_train_score=False,
        n_jobs=1,
    )
    dummy_scores = mean_cv_scores(dummy_cv)
    print(f"平均 CV Accuracy：{dummy_scores['CV_Accuracy']:.4f}")
    print(f"平均 CV Macro F1：{dummy_scores['CV_Macro_F1']:.4f}")
    print(
        "平均 CV Balanced Accuracy："
        f"{dummy_scores['CV_Balanced_Accuracy']:.4f}"
    )

    # 所有会从数据中学习信息的步骤都位于 Pipeline 内，避免折间数据泄漏。
    pipeline = Pipeline(
        steps=[
            ("variance", VarianceThreshold(threshold=0)),
            ("feature_selection", SelectKBest(score_func=f_classif)),
            ("scaler", StandardScaler()),
            ("model", LogisticRegression(max_iter=5000)),
        ]
    )
    parameter_grid = {
        "feature_selection__k": [100, 500, 1000, 2000],
        "model__C": [0.01, 0.1, 1.0, 10.0],
        "model__class_weight": [None, "balanced"],
    }

    print("\n=== LogisticRegression GridSearchCV ===")
    print("开始评估 32 组参数组合，共 160 次折内拟合……")
    search = GridSearchCV(
        estimator=pipeline,
        param_grid=parameter_grid,
        scoring=SCORING,
        refit="f1_macro",
        cv=cv,
        n_jobs=-1,
        pre_dispatch=2,
        return_train_score=False,
        error_score="raise",
        verbose=1,
    )
    search.fit(X, y)

    best_index = search.best_index_
    logistic_scores = {
        "CV_Accuracy": float(search.cv_results_["mean_test_accuracy"][best_index]),
        "CV_Macro_F1": float(search.cv_results_["mean_test_f1_macro"][best_index]),
        "CV_Balanced_Accuracy": float(
            search.cv_results_["mean_test_balanced_accuracy"][best_index]
        ),
    }

    print("\n最佳参数：")
    for name, value in search.best_params_.items():
        print(f"- {name}: {value}")
    print(f"最佳 CV Accuracy：{logistic_scores['CV_Accuracy']:.4f}")
    print(f"最佳 CV Macro F1：{logistic_scores['CV_Macro_F1']:.4f}")
    print(
        "最佳 CV Balanced Accuracy："
        f"{logistic_scores['CV_Balanced_Accuracy']:.4f}"
    )

    comparison = pd.DataFrame(
        [
            {"Model": "DummyClassifier", **dummy_scores},
            {"Model": "LogisticRegression", **logistic_scores},
        ]
    )
    print("\n=== Baseline 模型比较 ===")
    print(comparison.to_string(index=False, float_format=lambda value: f"{value:.4f}"))

    accuracy_gain = (
        logistic_scores["CV_Accuracy"] - dummy_scores["CV_Accuracy"]
    ) * 100
    macro_f1_gain = (
        logistic_scores["CV_Macro_F1"] - dummy_scores["CV_Macro_F1"]
    ) * 100
    print(f"Logistic Regression Accuracy 提升：{accuracy_gain:.2f} 个百分点")
    print(f"Logistic Regression Macro F1 提升：{macro_f1_gain:.2f} 个百分点")

    # GridSearchCV 已按最佳参数在完整训练集上重新拟合 best_estimator_。
    best_pipeline = search.best_estimator_
    variance_step = best_pipeline.named_steps["variance"]
    selection_step = best_pipeline.named_steps["feature_selection"]

    genes_after_variance = np.asarray(gene_columns)[variance_step.get_support()]
    selected_indices = selection_step.get_support(indices=True)
    selected_genes = genes_after_variance[selected_indices]

    # “前 30 个”按完整训练集重新拟合后的单变量 F 分数由高到低排列。
    selected_scores = selection_step.scores_[selected_indices]
    ranked_indices = np.argsort(selected_scores)[::-1]
    top_genes = selected_genes[ranked_indices[:30]]

    print("\n=== 最佳 Pipeline 在完整训练集上的特征情况 ===")
    print(f"零方差过滤后剩余基因数量：{len(genes_after_variance):,}")
    print(f"SelectKBest 最终保留基因数量：{len(selected_genes):,}")
    print("最终被选中的前 30 个基因名称：")
    for gene in top_genes:
        print(f"- {gene}")
    print(
        "提醒：这些基因只是当前最佳分类模型在完整训练集重新拟合后"
        "选中的特征，暂时不能直接称为癌症标志基因。"
    )

    TABLES_DIR.mkdir(parents=True, exist_ok=True)
    comparison.to_csv(COMPARISON_PATH, index=False, float_format="%.6f")

    cv_results = pd.DataFrame(search.cv_results_).sort_values(
        "rank_test_f1_macro"
    )
    cv_results.to_csv(CV_RESULTS_PATH, index=False)

    print("\nCancerTrace baseline 分类实验完成。")
    print("本实验仅使用645个训练样本进行5折交叉验证，162个最终测试样本仍未使用。")


if __name__ == "__main__":
    main()
