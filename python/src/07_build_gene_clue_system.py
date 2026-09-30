"""基于冻结模型与 Train 统计构建 CancerTrace Gene Clue System。"""

from __future__ import annotations

import re
from collections import defaultdict
from pathlib import Path

import joblib
import numpy as np
import pandas as pd


PROJECT_ROOT = Path(__file__).resolve().parents[2]
TRAIN_PATH = PROJECT_ROOT / "data" / "processed" / "cancertrace_train.csv"
TEST_PATH = PROJECT_ROOT / "data" / "processed" / "cancertrace_test.csv"
MODEL_PATH = PROJECT_ROOT / "results" / "models" / "cancertrace_logistic_pipeline.joblib"
TABLES_DIR = PROJECT_ROOT / "results" / "tables"

GENE_POOL_PATH = TABLES_DIR / "gene_clue_pool.csv"
GENE_PROFILES_PATH = TABLES_DIR / "gene_class_profiles.csv"
TEST_CLUES_PATH = TABLES_DIR / "test_case_gene_clues.csv"
EXAMPLES_PATH = TABLES_DIR / "gene_clue_examples.csv"
EVIDENCE_BOARD_PATH = TABLES_DIR / "test_case_evidence_board.csv"
SUMMARY_PATH = TABLES_DIR / "gene_clue_system_summary.csv"

LABEL_COLUMN = "OncotreeLineage"
TEST_METADATA_COLUMNS = ["ModelID", "CellLineName"]
TOP_GENES_PER_CLASS = 15
CLUES_PER_CASE = 5
EPSILON = 1e-8


def parse_gene_column(gene_column: str) -> tuple[str, str]:
    """从 “SOX10 (6663)” 中安全解析基因符号和 Entrez ID。"""
    match = re.match(r"^\s*(.*?)\s*\(([^()]*)\)\s*$", gene_column)
    if not match:
        return "", ""
    gene_symbol = match.group(1).strip()
    entrez_id = match.group(2).strip()
    return gene_symbol, entrez_id


def safe_metric_name(value: str) -> str:
    """将类别名转换为适合摘要 Metric 字段的名称。"""
    return re.sub(r"[^A-Za-z0-9]+", "_", value).strip("_")


def strength_label(support: float, weak_cutoff: float, strong_cutoff: float) -> str:
    """使用仅由 Train support 分布确定的阈值转换证据强度。"""
    if support <= 0:
        return ""
    if support >= strong_cutoff:
        return "Strong"
    if support >= weak_cutoff:
        return "Moderate"
    return "Weak"


def support_vector(expression_state: str, class_z_values: np.ndarray) -> np.ndarray:
    """根据表达状态将 Train 类别偏移转换为八类支持强度。"""
    if expression_state == "HIGH":
        return np.maximum(class_z_values, 0.0)
    if expression_state == "LOW":
        return np.maximum(-class_z_values, 0.0)
    return np.zeros_like(class_z_values, dtype=float)


def generate_case_clues(
    test_features: pd.DataFrame,
    test_metadata: pd.DataFrame,
    candidate_genes: list[str],
    gene_symbols: dict[str, str],
    train_medians: np.ndarray,
    train_q25: np.ndarray,
    train_q75: np.ndarray,
    train_iqr: np.ndarray,
    discriminative_scores: np.ndarray,
    profile_matrix: np.ndarray,
    class_labels: list[str],
    weak_cutoff: float,
    strong_cutoff: float,
) -> pd.DataFrame:
    """只使用无标签 Test 表达值和 Train 规则生成每病例五条线索。"""
    if LABEL_COLUMN in test_features.columns or LABEL_COLUMN in test_metadata.columns:
        raise ValueError("Test 线索生成函数禁止接收真实标签。")

    values = test_features[candidate_genes].to_numpy(dtype=float)
    if not np.isfinite(values).all():
        raise ValueError("Test 候选基因表达中存在 NaN 或无穷值。")

    clue_rows: list[dict[str, object]] = []
    for case_index in range(len(test_features)):
        case_values = values[case_index]
        states = np.where(
            case_values <= train_q25,
            "LOW",
            np.where(case_values >= train_q75, "HIGH", "NORMAL"),
        )
        extremeness = np.abs(case_values - train_medians) / (train_iqr + EPSILON)
        extremeness = np.clip(extremeness, 0.0, 5.0)
        informativeness = discriminative_scores * extremeness

        if not np.isfinite(informativeness).all():
            raise ValueError("CaseInformativenessScore 出现 NaN 或无穷值。")

        # HIGH/LOW 优先；不足五条时才由 NORMAL 线索按同一分数补足。
        ranked_indices = sorted(
            range(len(candidate_genes)),
            key=lambda index: (
                states[index] == "NORMAL",
                -informativeness[index],
                candidate_genes[index],
            ),
        )[:CLUES_PER_CASE]

        for rank, gene_index in enumerate(ranked_indices, start=1):
            state = str(states[gene_index])
            supports = support_vector(state, profile_matrix[gene_index])
            supported_indices = [
                index
                for index in np.argsort(-supports, kind="stable")
                if supports[index] > 0
            ][:2]

            support_classes = [class_labels[index] for index in supported_indices]
            support_strengths = [
                strength_label(
                    float(supports[index]), weak_cutoff, strong_cutoff
                )
                for index in supported_indices
            ]
            while len(support_classes) < 2:
                support_classes.append("")
                support_strengths.append("")

            gene = candidate_genes[gene_index]
            clue_rows.append(
                {
                    "ModelID": test_metadata.iloc[case_index]["ModelID"],
                    "CellLineName": test_metadata.iloc[case_index]["CellLineName"],
                    "CluePhase": "GENE_SCAN" if rank <= 3 else "INITIAL",
                    "ClueRank": rank,
                    "GeneColumn": gene,
                    "GeneSymbol": gene_symbols.get(gene, ""),
                    "ExpressionValue": float(case_values[gene_index]),
                    "ExpressionState": state,
                    "CaseInformativenessScore": float(informativeness[gene_index]),
                    "SupportClass1": support_classes[0],
                    "SupportStrength1": support_strengths[0],
                    "SupportClass2": support_classes[1],
                    "SupportStrength2": support_strengths[1],
                }
            )

    return pd.DataFrame(clue_rows)


def build_evidence_board(
    clues: pd.DataFrame,
    profile_lookup: pd.DataFrame,
    class_labels: list[str],
) -> pd.DataFrame:
    """累加当前可见线索的支持并缩放为 0～4 bars。"""
    board_rows: list[dict[str, object]] = []
    for model_id, case_clues in clues.groupby("ModelID", sort=False):
        stages = {
            "INITIAL": case_clues.loc[case_clues["CluePhase"] == "INITIAL"],
            "AFTER_GENE_SCAN": case_clues,
        }
        for stage, visible_clues in stages.items():
            totals = np.zeros(len(class_labels), dtype=float)
            for clue in visible_clues.itertuples(index=False):
                class_z_values = profile_lookup.loc[clue.GeneColumn].to_numpy(
                    dtype=float
                )
                totals += support_vector(clue.ExpressionState, class_z_values)

            maximum = float(totals.max())
            bars = (
                np.rint(totals / maximum * 4).astype(int)
                if maximum > 0
                else np.zeros(len(class_labels), dtype=int)
            )
            for class_index, class_name in enumerate(class_labels):
                board_rows.append(
                    {
                        "ModelID": model_id,
                        "Stage": stage,
                        "Class": class_name,
                        "RawSupport": float(totals[class_index]),
                        "Bars": int(bars[class_index]),
                    }
                )

    return pd.DataFrame(board_rows)


def main() -> None:
    """构建候选基因、Train 规则、Test 无标签线索及证据板。"""
    train_df = pd.read_csv(TRAIN_PATH, low_memory=False)
    if LABEL_COLUMN not in train_df.columns:
        raise ValueError(f"Train 数据缺少标签列：{LABEL_COLUMN}")
    if train_df[LABEL_COLUMN].isna().any():
        raise ValueError("Train 的 OncotreeLineage 存在缺失值。")

    train_gene_columns = [
        column for column in train_df.columns if "(" in column and ")" in column
    ]
    if not train_gene_columns:
        raise ValueError("Train 中未识别到基因表达列。")

    # 明确排除 Test 真实标签：只加载身份字段与基因表达列。
    test_df = pd.read_csv(
        TEST_PATH,
        usecols=[*TEST_METADATA_COLUMNS, *train_gene_columns],
        low_memory=False,
    )
    if LABEL_COLUMN in test_df.columns:
        raise RuntimeError("Test 真实标签不应被加载到 Gene Clue System。")

    final_pipeline = joblib.load(MODEL_PATH)
    required_steps = ["variance", "feature_selection", "scaler", "model"]
    if list(final_pipeline.named_steps) != required_steps:
        raise ValueError("冻结模型的 Pipeline 步骤与预期不一致。")

    variance_step = final_pipeline.named_steps["variance"]
    selection_step = final_pipeline.named_steps["feature_selection"]
    model_step = final_pipeline.named_steps["model"]
    if selection_step.k != 2000 or model_step.C != 1.0:
        raise ValueError("冻结模型参数与 06 阶段锁定方案不一致。")
    if model_step.class_weight is not None or model_step.max_iter != 5000:
        raise ValueError("冻结 LogisticRegression 参数与锁定方案不一致。")

    genes_after_variance = np.asarray(train_gene_columns)[
        variance_step.get_support()
    ]
    selected_genes = genes_after_variance[selection_step.get_support()]
    class_labels = list(model_step.classes_)
    coefficients = pd.DataFrame(
        model_step.coef_, index=class_labels, columns=selected_genes
    )

    source_classes: dict[str, set[str]] = defaultdict(set)
    class_contributions: dict[str, int] = {}
    for class_name in class_labels:
        positive_coefficients = coefficients.loc[class_name]
        positive_coefficients = positive_coefficients.loc[
            positive_coefficients > 0
        ].nlargest(TOP_GENES_PER_CLASS)
        if len(positive_coefficients) < TOP_GENES_PER_CLASS:
            raise ValueError(f"{class_name} 的正向系数基因不足 15 个。")
        class_contributions[class_name] = len(positive_coefficients)
        for gene in positive_coefficients.index:
            source_classes[gene].add(class_name)

    candidate_genes = sorted(
        source_classes,
        key=lambda gene: (-float(coefficients[gene].abs().max()), gene),
    )
    if any(gene not in genes_after_variance for gene in candidate_genes):
        raise RuntimeError("零方差基因进入了候选线索池。")

    gene_symbols: dict[str, str] = {}
    pool_rows: list[dict[str, object]] = []
    for gene in candidate_genes:
        gene_symbol, entrez_id = parse_gene_column(gene)
        gene_symbols[gene] = gene_symbol
        pool_rows.append(
            {
                "GeneColumn": gene,
                "GeneSymbol": gene_symbol,
                "EntrezID": entrez_id,
                "SourceClasses": "|".join(sorted(source_classes[gene])),
                "MaxAbsCoefficient": float(coefficients[gene].abs().max()),
                "MaxPositiveCoefficient": float(coefficients[gene].max()),
            }
        )
    gene_pool = pd.DataFrame(pool_rows)

    train_expression = train_df[candidate_genes]
    if not np.isfinite(train_expression.to_numpy(dtype=float)).all():
        raise ValueError("Train 候选基因表达中存在 NaN 或无穷值。")

    global_mean = train_expression.mean()
    global_median = train_expression.median()
    global_std = train_expression.std(ddof=1)
    q25 = train_expression.quantile(0.25)
    q75 = train_expression.quantile(0.75)
    iqr = q75 - q25

    profile_tables = []
    for class_name in class_labels:
        class_expression = train_expression.loc[
            train_df[LABEL_COLUMN] == class_name
        ]
        class_mean = class_expression.mean()
        class_median = class_expression.median()
        profile_tables.append(
            pd.DataFrame(
                {
                    "GeneColumn": candidate_genes,
                    "GeneSymbol": [gene_symbols[gene] for gene in candidate_genes],
                    "Class": class_name,
                    "GlobalMean": global_mean.reindex(candidate_genes).to_numpy(),
                    "GlobalMedian": global_median.reindex(candidate_genes).to_numpy(),
                    "GlobalStd": global_std.reindex(candidate_genes).to_numpy(),
                    "Q25": q25.reindex(candidate_genes).to_numpy(),
                    "Q75": q75.reindex(candidate_genes).to_numpy(),
                    "ClassMean": class_mean.reindex(candidate_genes).to_numpy(),
                    "ClassMedian": class_median.reindex(candidate_genes).to_numpy(),
                    "ClassZMean": (
                        (class_mean - global_mean) / (global_std + EPSILON)
                    )
                    .reindex(candidate_genes)
                    .to_numpy(),
                }
            )
        )
    gene_profiles = pd.concat(profile_tables, ignore_index=True)
    if not np.isfinite(
        gene_profiles[
            [
                "GlobalMean",
                "GlobalMedian",
                "GlobalStd",
                "Q25",
                "Q75",
                "ClassMean",
                "ClassMedian",
                "ClassZMean",
            ]
        ].to_numpy(dtype=float)
    ).all():
        raise ValueError("Gene class profile 出现 NaN 或无穷值。")

    profile_lookup = (
        gene_profiles.pivot(index="GeneColumn", columns="Class", values="ClassZMean")
        .reindex(index=candidate_genes, columns=class_labels)
    )
    nonzero_support = np.abs(profile_lookup.to_numpy(dtype=float)).ravel()
    nonzero_support = nonzero_support[nonzero_support > 0]
    if nonzero_support.size == 0:
        raise ValueError("Train 未产生任何非零类别支持强度。")
    weak_cutoff, strong_cutoff = np.quantile(nonzero_support, [1 / 3, 2 / 3])

    discriminative_scores = profile_lookup.abs().max(axis=1)
    clues = generate_case_clues(
        test_features=test_df[train_gene_columns],
        test_metadata=test_df[TEST_METADATA_COLUMNS],
        candidate_genes=candidate_genes,
        gene_symbols=gene_symbols,
        train_medians=global_median.reindex(candidate_genes).to_numpy(dtype=float),
        train_q25=q25.reindex(candidate_genes).to_numpy(dtype=float),
        train_q75=q75.reindex(candidate_genes).to_numpy(dtype=float),
        train_iqr=iqr.reindex(candidate_genes).to_numpy(dtype=float),
        discriminative_scores=discriminative_scores.reindex(candidate_genes).to_numpy(
            dtype=float
        ),
        profile_matrix=profile_lookup.to_numpy(dtype=float),
        class_labels=class_labels,
        weak_cutoff=float(weak_cutoff),
        strong_cutoff=float(strong_cutoff),
    )

    evidence_board = build_evidence_board(clues, profile_lookup, class_labels)

    first_ten_ids = list(test_df["ModelID"].head(10))
    examples = clues.loc[clues["ModelID"].isin(first_ten_ids)].copy()
    case_order = {model_id: index for index, model_id in enumerate(first_ten_ids)}
    examples["_CaseOrder"] = examples["ModelID"].map(case_order)
    examples["_PhaseOrder"] = examples["CluePhase"].map(
        {"INITIAL": 0, "GENE_SCAN": 1}
    )
    examples = examples.sort_values(
        ["_CaseOrder", "_PhaseOrder", "ClueRank"]
    )
    examples_table = pd.DataFrame(
        {
            "ModelID": examples["ModelID"],
            "CellLineName": examples["CellLineName"],
            "Phase": examples["CluePhase"],
            "Gene": np.where(
                examples["GeneSymbol"] != "",
                examples["GeneSymbol"],
                examples["GeneColumn"],
            ),
            "State": examples["ExpressionState"],
            "Support1": np.where(
                examples["SupportClass1"] != "",
                examples["SupportClass1"]
                + " ("
                + examples["SupportStrength1"]
                + ")",
                "",
            ),
            "Support2": np.where(
                examples["SupportClass2"] != "",
                examples["SupportClass2"]
                + " ("
                + examples["SupportStrength2"]
                + ")",
                "",
            ),
            "Score": examples["CaseInformativenessScore"],
        }
    )

    # Train-only sanity checks；不使用 Test 真实标签评价线索。
    expected_cases = len(test_df)
    clue_counts = clues.groupby("ModelID").size()
    initial_counts = clues.loc[clues["CluePhase"] == "INITIAL"].groupby("ModelID").size()
    scan_counts = clues.loc[clues["CluePhase"] == "GENE_SCAN"].groupby("ModelID").size()
    duplicate_gene_cases = int(
        (clues.groupby("ModelID")["GeneColumn"].nunique() != CLUES_PER_CASE).sum()
    )
    if len(clue_counts) != expected_cases or not clue_counts.eq(5).all():
        raise RuntimeError("并非所有 Test 病例都得到 5 条线索。")
    if not initial_counts.reindex(clue_counts.index, fill_value=0).eq(2).all():
        raise RuntimeError("INITIAL 线索数量并非每个病例固定为 2。")
    if not scan_counts.reindex(clue_counts.index, fill_value=0).eq(3).all():
        raise RuntimeError("GENE_SCAN 线索数量并非每个病例固定为 3。")
    if duplicate_gene_cases:
        raise RuntimeError("至少一个 Test 病例存在重复 Gene 线索。")
    if not set(clues["SupportClass1"]).difference({""}).issubset(class_labels):
        raise RuntimeError("SupportClass1 出现无效癌症类别。")
    if not set(clues["SupportClass2"]).difference({""}).issubset(class_labels):
        raise RuntimeError("SupportClass2 出现无效癌症类别。")
    if set(gene_profiles["Class"]) != set(class_labels):
        raise RuntimeError("Gene profiles 未覆盖全部 8 个癌症类别。")
    if not np.isfinite(
        clues[["ExpressionValue", "CaseInformativenessScore"]].to_numpy(dtype=float)
    ).all():
        raise RuntimeError("Test clue 输出含 NaN 或无穷值。")
    if not np.isfinite(
        evidence_board[["RawSupport", "Bars"]].to_numpy(dtype=float)
    ).all():
        raise RuntimeError("Evidence Board 输出含 NaN 或无穷值。")

    state_counts = clues["ExpressionState"].value_counts().reindex(
        ["HIGH", "LOW", "NORMAL"], fill_value=0
    )
    state_percentages = state_counts / len(clues) * 100

    summary_rows: list[dict[str, object]] = [
        {"Metric": "CandidateGenes", "Value": len(candidate_genes)},
        {"Metric": "TrainSamples", "Value": len(train_df)},
        {"Metric": "TestCases", "Value": expected_cases},
        {"Metric": "TotalGeneratedClues", "Value": len(clues)},
        {"Metric": "InitialCluesPerCase", "Value": 2},
        {"Metric": "GeneScanCluesPerCase", "Value": 3},
        {"Metric": "HighCluePercentage", "Value": state_percentages["HIGH"]},
        {"Metric": "LowCluePercentage", "Value": state_percentages["LOW"]},
        {"Metric": "NormalCluePercentage", "Value": state_percentages["NORMAL"]},
        {"Metric": "SupportWeakCutoff", "Value": float(weak_cutoff)},
        {"Metric": "SupportStrongCutoff", "Value": float(strong_cutoff)},
    ]
    summary_rows.extend(
        {
            "Metric": f"CandidateContribution_{safe_metric_name(class_name)}",
            "Value": class_contributions[class_name],
        }
        for class_name in class_labels
    )
    summary = pd.DataFrame(summary_rows)

    TABLES_DIR.mkdir(parents=True, exist_ok=True)
    gene_pool.fillna("").to_csv(GENE_POOL_PATH, index=False, float_format="%.8f")
    gene_profiles.fillna("").to_csv(
        GENE_PROFILES_PATH, index=False, float_format="%.8f"
    )
    clues.fillna("").to_csv(TEST_CLUES_PATH, index=False, float_format="%.8f")
    examples_table.fillna("").to_csv(
        EXAMPLES_PATH, index=False, float_format="%.8f"
    )
    evidence_board.fillna("").to_csv(
        EVIDENCE_BOARD_PATH, index=False, float_format="%.8f"
    )
    summary.fillna("").to_csv(SUMMARY_PATH, index=False, float_format="%.8g")

    print("\n==============================")
    print("CancerTrace Gene Clue System")
    print("==============================")
    print(f"Train samples:\n{len(train_df):,}")
    print(f"Test cases:\n{expected_cases:,}")
    print(f"Candidate clue genes:\n{len(candidate_genes):,}")
    print("Initial clues per case:\n2")
    print("Gene Scan clues per case:\n3")
    print(f"Total Test clues:\n{len(clues):,}")
    print(
        "Expression states:\n"
        f"HIGH {state_counts['HIGH']:,} ({state_percentages['HIGH']:.2f}%)\n"
        f"LOW {state_counts['LOW']:,} ({state_percentages['LOW']:.2f}%)\n"
        f"NORMAL {state_counts['NORMAL']:,} ({state_percentages['NORMAL']:.2f}%)"
    )
    print("\nCandidate Pool 每类贡献：")
    for class_name in class_labels:
        print(f"- {class_name}: {class_contributions[class_name]}")

    for model_id in list(test_df["ModelID"].head(3)):
        case = clues.loc[clues["ModelID"] == model_id]
        cell_line_name = str(case.iloc[0]["CellLineName"])
        print(f"\nCASE\n{cell_line_name}")
        for phase, title in (("INITIAL", "INITIAL"), ("GENE_SCAN", "GENE SCAN")):
            print(f"{title}:")
            phase_clues = case.loc[case["CluePhase"] == phase].sort_values(
                "ClueRank"
            )
            for clue in phase_clues.itertuples(index=False):
                display_gene = clue.GeneSymbol or clue.GeneColumn
                supports = ", ".join(
                    value
                    for value in (
                        f"{clue.SupportClass1} ({clue.SupportStrength1})"
                        if clue.SupportClass1
                        else "",
                        f"{clue.SupportClass2} ({clue.SupportStrength2})"
                        if clue.SupportClass2
                        else "",
                    )
                    if value
                )
                print(
                    f"- {display_gene} {clue.ExpressionState} → supports {supports}"
                )

    print("\nTest真实标签未参与任何线索选择、排序或支持关系计算。")
    print("所有Gene Clue规则和表达阈值均仅来自645个Train样本。")
    print("07阶段未修改或重新调优最终AI模型。")


if __name__ == "__main__":
    main()
