"""对 CancerTrace Gene Clue System 做 Train-only 玩法 QA 与轻量平衡。"""

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
ORIGINAL_CLUES_PATH = PROJECT_ROOT / "results" / "tables" / "test_case_gene_clues.csv"
TABLES_DIR = PROJECT_ROOT / "results" / "tables"

POOL_PATH = TABLES_DIR / "gene_clue_pool_rebalanced.csv"
CLUES_PATH = TABLES_DIR / "test_case_gene_clues_rebalanced.csv"
BOARD_PATH = TABLES_DIR / "test_case_evidence_board_rebalanced.csv"
QA_PATH = TABLES_DIR / "evidence_board_qa.csv"
SUMMARY_PATH = TABLES_DIR / "gene_clue_rebalance_summary.csv"

LABEL_COLUMN = "OncotreeLineage"
TEST_METADATA_COLUMNS = ["ModelID", "CellLineName"]
TOP_GENES_PER_CLASS_ORIENTATION = 10
CLUES_PER_CASE = 5
EPSILON = 1e-8
INITIAL_CLEAR_WINNER_WARNING_PERCENT = 25.0


def parse_gene_column(gene_column: str) -> tuple[str, str]:
    """从基因列名中安全解析 GeneSymbol 与 EntrezID。"""
    match = re.match(r"^\s*(.*?)\s*\(([^()]*)\)\s*$", gene_column)
    if not match:
        return "", ""
    return match.group(1).strip(), match.group(2).strip()


def support_vector(state: str, class_z_values: np.ndarray) -> np.ndarray:
    """根据 HIGH/LOW 状态产生八类非负支持值。"""
    if state == "HIGH":
        return np.maximum(class_z_values, 0.0)
    if state == "LOW":
        return np.maximum(-class_z_values, 0.0)
    return np.zeros_like(class_z_values, dtype=float)


def strength_label(value: float, moderate_cutoff: float, strong_cutoff: float) -> str:
    """使用 Train support 分布的三分位数标记 Weak/Moderate/Strong。"""
    if value <= 0:
        return ""
    if value >= strong_cutoff:
        return "Strong"
    if value >= moderate_cutoff:
        return "Moderate"
    return "Weak"


def generate_case_clues(
    test_features: pd.DataFrame,
    test_metadata: pd.DataFrame,
    candidate_genes: list[str],
    gene_symbols: dict[str, str],
    medians: np.ndarray,
    q25: np.ndarray,
    q75: np.ndarray,
    iqr: np.ndarray,
    discriminative_scores: np.ndarray,
    profile_matrix: np.ndarray,
    class_labels: list[str],
    moderate_cutoff: float,
    strong_cutoff: float,
) -> pd.DataFrame:
    """不接收 y_test，仅用 Test 表达值与 Train 规则生成线索。"""
    if LABEL_COLUMN in test_features.columns or LABEL_COLUMN in test_metadata.columns:
        raise ValueError("Test 线索函数禁止接收真实标签。")

    values = test_features[candidate_genes].to_numpy(dtype=float)
    if not np.isfinite(values).all():
        raise ValueError("Test 候选基因表达存在 NaN 或无穷值。")

    rows: list[dict[str, object]] = []
    for case_index in range(len(test_features)):
        case_values = values[case_index]
        states = np.where(
            case_values <= q25,
            "LOW",
            np.where(case_values >= q75, "HIGH", "NORMAL"),
        )
        extremeness = np.clip(
            np.abs(case_values - medians) / (iqr + EPSILON), 0.0, 5.0
        )
        scores = discriminative_scores * extremeness
        if not np.isfinite(scores).all():
            raise ValueError("CaseInformativenessScore 存在 NaN 或无穷值。")

        ranked = sorted(
            range(len(candidate_genes)),
            key=lambda index: (
                states[index] == "NORMAL",
                -scores[index],
                candidate_genes[index],
            ),
        )
        selected = ranked[:CLUES_PER_CASE]

        # 只在信息量接近时增加方向多样性；前三名 Gene Scan 不受影响。
        selected_states = {str(states[index]) for index in selected}
        if len(selected_states) == 1 and next(iter(selected_states)) in {"HIGH", "LOW"}:
            dominant_state = next(iter(selected_states))
            opposite_state = "LOW" if dominant_state == "HIGH" else "HIGH"
            fifth_score = float(scores[selected[-1]])
            alternatives = [
                index
                for index in ranked[5:10]
                if states[index] == opposite_state
                and scores[index] >= fifth_score * 0.70
            ]
            if alternatives:
                selected[-1] = alternatives[0]

        for rank, gene_index in enumerate(selected, start=1):
            state = str(states[gene_index])
            supports = support_vector(state, profile_matrix[gene_index])
            supported_indices = [
                index
                for index in np.argsort(-supports, kind="stable")
                if supports[index] > 0
            ][:2]
            classes = [class_labels[index] for index in supported_indices]
            strengths = [
                strength_label(
                    float(supports[index]), moderate_cutoff, strong_cutoff
                )
                for index in supported_indices
            ]
            while len(classes) < 2:
                classes.append("")
                strengths.append("")

            gene = candidate_genes[gene_index]
            rows.append(
                {
                    "ModelID": test_metadata.iloc[case_index]["ModelID"],
                    "CellLineName": test_metadata.iloc[case_index]["CellLineName"],
                    "CluePhase": "GENE_SCAN" if rank <= 3 else "INITIAL",
                    "ClueRank": rank,
                    "GeneColumn": gene,
                    "GeneSymbol": gene_symbols.get(gene, ""),
                    "ExpressionValue": float(case_values[gene_index]),
                    "ExpressionState": state,
                    "CaseInformativenessScore": float(scores[gene_index]),
                    "SupportClass1": classes[0],
                    "SupportStrength1": strengths[0],
                    "SupportClass2": classes[1],
                    "SupportStrength2": strengths[1],
                }
            )

    return pd.DataFrame(rows)


def build_evidence_board(
    clues: pd.DataFrame,
    profile_lookup: pd.DataFrame,
    class_labels: list[str],
) -> pd.DataFrame:
    """生成 INITIAL 与 Gene Scan 后的八类 RawSupport 和 0～4 Bars。"""
    rows: list[dict[str, object]] = []
    for model_id, case_clues in clues.groupby("ModelID", sort=False):
        stage_clues = {
            "INITIAL": case_clues.loc[case_clues["CluePhase"] == "INITIAL"],
            "AFTER_GENE_SCAN": case_clues,
        }
        for stage, visible in stage_clues.items():
            totals = np.zeros(len(class_labels), dtype=float)
            for clue in visible.itertuples(index=False):
                z_values = profile_lookup.loc[clue.GeneColumn].to_numpy(dtype=float)
                totals += support_vector(clue.ExpressionState, z_values)
            maximum = float(totals.max())
            bars = (
                np.rint(totals / maximum * 4).astype(int)
                if maximum > 0
                else np.zeros(len(class_labels), dtype=int)
            )
            for index, class_name in enumerate(class_labels):
                rows.append(
                    {
                        "ModelID": model_id,
                        "Stage": stage,
                        "Class": class_name,
                        "RawSupport": float(totals[index]),
                        "Bars": int(bars[index]),
                    }
                )
    return pd.DataFrame(rows)


def build_evidence_qa(board: pd.DataFrame) -> pd.DataFrame:
    """记录每个病例和阶段的 Top1/Top2 证据差距。"""
    rows: list[dict[str, object]] = []
    for (model_id, stage), group in board.groupby(["ModelID", "Stage"], sort=False):
        ranked = group.sort_values(
            ["RawSupport", "Class"], ascending=[False, True]
        ).reset_index(drop=True)
        top1 = ranked.iloc[0]
        top2 = ranked.iloc[1]
        rows.append(
            {
                "ModelID": model_id,
                "Stage": stage,
                "Top1Class": top1["Class"],
                "Top2Class": top2["Class"],
                "Top1Bars": int(top1["Bars"]),
                "Top2Bars": int(top2["Bars"]),
                "Top1MinusTop2": float(top1["RawSupport"] - top2["RawSupport"]),
                "Top1Ratio": float(
                    top1["RawSupport"] / (top2["RawSupport"] + EPSILON)
                ),
            }
        )
    return pd.DataFrame(rows)


def stage_qa_stats(qa: pd.DataFrame, stage: str) -> dict[str, object]:
    """汇总某 Evidence Board 阶段的清晰赢家、bar gap 和 ratio。"""
    stage_qa = qa.loc[qa["Stage"] == stage].copy()
    clear_winner = (stage_qa["Top1Bars"] == 4) & (stage_qa["Top2Bars"] == 0)
    bar_gap = stage_qa["Top1Bars"] - stage_qa["Top2Bars"]
    gap_distribution = {
        gap: float((bar_gap == gap).mean() * 100) for gap in range(5)
    }
    return {
        "ClearWinnerPercentage": float(clear_winner.mean() * 100),
        "GapDistribution": gap_distribution,
        "RatioMedian": float(stage_qa["Top1Ratio"].median()),
        "RatioQ25": float(stage_qa["Top1Ratio"].quantile(0.25)),
        "RatioQ75": float(stage_qa["Top1Ratio"].quantile(0.75)),
    }


def print_case_example(
    cell_line_name: str,
    clues: pd.DataFrame,
    board: pd.DataFrame,
) -> None:
    """打印一个病例的分阶段线索和 Evidence Board Top 3。"""
    case = clues.loc[clues["CellLineName"] == cell_line_name]
    if case.empty:
        return
    model_id = case.iloc[0]["ModelID"]
    print(f"\nCASE\n{cell_line_name}")
    for phase, title in (("INITIAL", "INITIAL"), ("GENE_SCAN", "GENE SCAN")):
        print(f"{title}:")
        for clue in case.loc[case["CluePhase"] == phase].sort_values(
            "ClueRank"
        ).itertuples(index=False):
            gene = clue.GeneSymbol or clue.GeneColumn
            support1 = (
                f"{clue.SupportClass1} ({clue.SupportStrength1})"
                if clue.SupportClass1
                else ""
            )
            support2 = (
                f"{clue.SupportClass2} ({clue.SupportStrength2})"
                if clue.SupportClass2
                else ""
            )
            supports = ", ".join(value for value in (support1, support2) if value)
            print(f"- {gene} {clue.ExpressionState} → {supports}")

    for stage in ("INITIAL", "AFTER_GENE_SCAN"):
        top3 = (
            board.loc[(board["ModelID"] == model_id) & (board["Stage"] == stage)]
            .sort_values(["RawSupport", "Class"], ascending=[False, True])
            .head(3)
        )
        print(f"{stage} Evidence Board Top 3:")
        for row in top3.itertuples(index=False):
            print(f"- {row.Class}: {row.Bars} bars (RawSupport {row.RawSupport:.3f})")


def main() -> None:
    """重建双向候选池、生成无标签 Test 线索并完成玩法 QA。"""
    train_df = pd.read_csv(TRAIN_PATH, low_memory=False)
    if LABEL_COLUMN not in train_df.columns or train_df[LABEL_COLUMN].isna().any():
        raise ValueError("Train 标签缺失或包含空值。")
    gene_columns = [
        column for column in train_df.columns if "(" in column and ")" in column
    ]
    if not gene_columns:
        raise ValueError("Train 中未识别到基因表达列。")

    frozen_pipeline = joblib.load(MODEL_PATH)
    required_steps = ["variance", "feature_selection", "scaler", "model"]
    if list(frozen_pipeline.named_steps) != required_steps:
        raise ValueError("冻结模型的 Pipeline 结构与预期不一致。")
    if frozen_pipeline.named_steps["feature_selection"].k != 2000:
        raise ValueError("冻结模型的 k 不等于 2000。")
    model_step = frozen_pipeline.named_steps["model"]
    if model_step.C != 1.0 or model_step.class_weight is not None:
        raise ValueError("冻结模型参数与 06 阶段锁定方案不一致。")

    class_labels = list(model_step.classes_)
    expression = train_df[gene_columns]
    expression_values = expression.to_numpy(dtype=float)
    if not np.isfinite(expression_values).all():
        raise ValueError("Train 基因表达存在 NaN 或无穷值。")

    global_mean_all = expression.mean()
    global_std_all = expression.std(ddof=1)
    variance_supported = set(
        np.asarray(gene_columns)[
            frozen_pipeline.named_steps["variance"].get_support()
        ]
    )
    valid_genes = [
        gene
        for gene in gene_columns
        if gene in variance_supported
        and np.isfinite(global_mean_all[gene])
        and np.isfinite(global_std_all[gene])
        and global_std_all[gene] > 0
    ]
    if not valid_genes:
        raise ValueError("没有符合过滤规则的非零方差基因。")

    class_means = (
        train_df.groupby(LABEL_COLUMN, observed=True)[valid_genes]
        .mean()
        .reindex(class_labels)
        .T
    )
    class_z_all = class_means.sub(global_mean_all[valid_genes], axis=0).div(
        global_std_all[valid_genes] + EPSILON, axis=0
    )
    if not np.isfinite(class_z_all.to_numpy(dtype=float)).all():
        raise ValueError("Train ClassZMean 出现 NaN 或无穷值。")

    source_classes: dict[str, set[str]] = defaultdict(set)
    orientations: dict[str, set[str]] = defaultdict(set)
    for class_name in class_labels:
        class_z = class_z_all[class_name]
        high_genes = class_z.loc[class_z > 0].nlargest(
            TOP_GENES_PER_CLASS_ORIENTATION
        )
        low_genes = class_z.loc[class_z < 0].nsmallest(
            TOP_GENES_PER_CLASS_ORIENTATION
        )
        if len(high_genes) < 10 or len(low_genes) < 10:
            raise ValueError(f"{class_name} 缺少足够的 HIGH/LOW oriented 基因。")
        for gene in high_genes.index:
            source_classes[gene].add(class_name)
            orientations[gene].add("HIGH")
        for gene in low_genes.index:
            source_classes[gene].add(class_name)
            orientations[gene].add("LOW")

    candidate_genes = sorted(
        source_classes,
        key=lambda gene: (-float(class_z_all.loc[gene].abs().max()), gene),
    )
    gene_symbols: dict[str, str] = {}
    pool_rows: list[dict[str, object]] = []
    for gene in candidate_genes:
        symbol, entrez_id = parse_gene_column(gene)
        gene_symbols[gene] = symbol
        orientation = "BOTH" if len(orientations[gene]) > 1 else next(iter(orientations[gene]))
        pool_rows.append(
            {
                "GeneColumn": gene,
                "GeneSymbol": symbol,
                "EntrezID": entrez_id,
                "SourceClasses": "|".join(sorted(source_classes[gene])),
                "Orientation": orientation,
                "MaxPositiveClassZ": float(class_z_all.loc[gene].max()),
                "MinNegativeClassZ": float(class_z_all.loc[gene].min()),
                "MaxAbsClassZ": float(class_z_all.loc[gene].abs().max()),
            }
        )
    pool = pd.DataFrame(pool_rows)

    candidate_train = train_df[candidate_genes]
    medians = candidate_train.median()
    q25 = candidate_train.quantile(0.25)
    q75 = candidate_train.quantile(0.75)
    iqr = q75 - q25
    profile_lookup = class_z_all.loc[candidate_genes, class_labels]
    discriminative_scores = profile_lookup.abs().max(axis=1)

    nonzero_support = np.abs(profile_lookup.to_numpy(dtype=float)).ravel()
    nonzero_support = nonzero_support[nonzero_support > 0]
    moderate_cutoff, strong_cutoff = np.quantile(nonzero_support, [1 / 3, 2 / 3])

    # Test 真值标签不进入内存：只读身份字段与新版候选基因。
    test_df = pd.read_csv(
        TEST_PATH,
        usecols=[*TEST_METADATA_COLUMNS, *candidate_genes],
        low_memory=False,
    )
    if LABEL_COLUMN in test_df.columns:
        raise RuntimeError("07.1 禁止加载 Test 真实标签。")

    clues = generate_case_clues(
        test_features=test_df[candidate_genes],
        test_metadata=test_df[TEST_METADATA_COLUMNS],
        candidate_genes=candidate_genes,
        gene_symbols=gene_symbols,
        medians=medians.reindex(candidate_genes).to_numpy(dtype=float),
        q25=q25.reindex(candidate_genes).to_numpy(dtype=float),
        q75=q75.reindex(candidate_genes).to_numpy(dtype=float),
        iqr=iqr.reindex(candidate_genes).to_numpy(dtype=float),
        discriminative_scores=discriminative_scores.to_numpy(dtype=float),
        profile_matrix=profile_lookup.to_numpy(dtype=float),
        class_labels=class_labels,
        moderate_cutoff=float(moderate_cutoff),
        strong_cutoff=float(strong_cutoff),
    )
    board = build_evidence_board(clues, profile_lookup, class_labels)
    qa = build_evidence_qa(board)

    # 完整性检查不依赖 Test 标签。
    case_groups = clues.groupby("ModelID", sort=False)
    if len(case_groups) != len(test_df) or not case_groups.size().eq(5).all():
        raise RuntimeError("并非所有 Test 病例都得到 5 条线索。")
    if not clues.loc[clues["CluePhase"] == "INITIAL"].groupby("ModelID").size().eq(2).all():
        raise RuntimeError("INITIAL 不是每例固定 2 条。")
    if not clues.loc[clues["CluePhase"] == "GENE_SCAN"].groupby("ModelID").size().eq(3).all():
        raise RuntimeError("GENE_SCAN 不是每例固定 3 条。")
    if (case_groups["GeneColumn"].nunique() != 5).any():
        raise RuntimeError("至少一个病例存在重复 Gene 线索。")
    if not np.isfinite(
        clues[["ExpressionValue", "CaseInformativenessScore"]].to_numpy(dtype=float)
    ).all():
        raise RuntimeError("线索输出存在 NaN 或无穷值。")
    if not np.isfinite(board[["RawSupport", "Bars"]].to_numpy(dtype=float)).all():
        raise RuntimeError("Evidence Board 存在 NaN 或无穷值。")
    if not np.isfinite(qa[["Top1MinusTop2", "Top1Ratio"]].to_numpy(dtype=float)).all():
        raise RuntimeError("Evidence Board QA 存在 NaN 或无穷值。")
    if board["Bars"].min() < 0 or board["Bars"].max() > 4:
        raise RuntimeError("Evidence Board Bars 超出 0～4。")

    state_counts = clues["ExpressionState"].value_counts().reindex(
        ["HIGH", "LOW", "NORMAL"], fill_value=0
    )
    state_percentages = state_counts / len(clues) * 100
    phase_percentages: dict[str, pd.Series] = {}
    for phase in ("INITIAL", "GENE_SCAN"):
        phase_clues = clues.loc[clues["CluePhase"] == phase]
        phase_percentages[phase] = (
            phase_clues["ExpressionState"]
            .value_counts()
            .reindex(["HIGH", "LOW", "NORMAL"], fill_value=0)
            / len(phase_clues)
            * 100
        )

    direction_sets = case_groups["ExpressionState"].agg(set)
    all_high_percentage = float(
        direction_sets.apply(lambda states: states == {"HIGH"}).mean() * 100
    )
    all_low_percentage = float(
        direction_sets.apply(lambda states: states == {"LOW"}).mean() * 100
    )
    mixed_percentage = float(
        direction_sets.apply(lambda states: {"HIGH", "LOW"}.issubset(states)).mean()
        * 100
    )
    initial_direction_sets = (
        clues.loc[clues["CluePhase"] == "INITIAL"]
        .groupby("ModelID")["ExpressionState"]
        .agg(set)
    )
    initial_mixed_percentage = float(
        initial_direction_sets.apply(
            lambda states: {"HIGH", "LOW"}.issubset(states)
        ).mean()
        * 100
    )

    initial_stats = stage_qa_stats(qa, "INITIAL")
    after_stats = stage_qa_stats(qa, "AFTER_GENE_SCAN")

    original_high = np.nan
    original_low = np.nan
    if ORIGINAL_CLUES_PATH.exists():
        original_states = pd.read_csv(
            ORIGINAL_CLUES_PATH, usecols=["ExpressionState"]
        )["ExpressionState"]
        original_high = float((original_states == "HIGH").mean() * 100)
        original_low = float((original_states == "LOW").mean() * 100)

    summary_rows: list[dict[str, object]] = [
        {"Metric": "CandidateGenes", "Value": len(candidate_genes)},
        {"Metric": "TotalTestCases", "Value": len(test_df)},
        {"Metric": "TotalClues", "Value": len(clues)},
        {"Metric": "OriginalHighPercentage", "Value": original_high},
        {"Metric": "OriginalLowPercentage", "Value": original_low},
        {"Metric": "RebalancedHighPercentage", "Value": state_percentages["HIGH"]},
        {"Metric": "RebalancedLowPercentage", "Value": state_percentages["LOW"]},
        {"Metric": "RebalancedNormalPercentage", "Value": state_percentages["NORMAL"]},
        {"Metric": "MixedDirectionCasePercentage", "Value": mixed_percentage},
        {"Metric": "InitialMixedDirectionPercentage", "Value": initial_mixed_percentage},
        {"Metric": "AllHighCasePercentage", "Value": all_high_percentage},
        {"Metric": "AllLowCasePercentage", "Value": all_low_percentage},
        {"Metric": "InitialTop1ClearWinnerPercentage", "Value": initial_stats["ClearWinnerPercentage"]},
        {"Metric": "AfterScanTop1ClearWinnerPercentage", "Value": after_stats["ClearWinnerPercentage"]},
    ]
    for phase in ("INITIAL", "GENE_SCAN"):
        for state in ("HIGH", "LOW", "NORMAL"):
            summary_rows.append(
                {
                    "Metric": f"{phase.title().replace('_', '')}{state.title()}Percentage",
                    "Value": phase_percentages[phase][state],
                }
            )
    for prefix, stats in (("Initial", initial_stats), ("AfterScan", after_stats)):
        summary_rows.extend(
            [
                {"Metric": f"{prefix}Top1RatioMedian", "Value": stats["RatioMedian"]},
                {"Metric": f"{prefix}Top1RatioQ25", "Value": stats["RatioQ25"]},
                {"Metric": f"{prefix}Top1RatioQ75", "Value": stats["RatioQ75"]},
            ]
        )
        summary_rows.extend(
            {
                "Metric": f"{prefix}BarGap{gap}Percentage",
                "Value": stats["GapDistribution"][gap],
            }
            for gap in range(5)
        )
    summary = pd.DataFrame(summary_rows)

    TABLES_DIR.mkdir(parents=True, exist_ok=True)
    pool.fillna("").to_csv(POOL_PATH, index=False, float_format="%.8g")
    clues.fillna("").to_csv(CLUES_PATH, index=False, float_format="%.8g")
    board.fillna("").to_csv(BOARD_PATH, index=False, float_format="%.8g")
    qa.fillna("").to_csv(QA_PATH, index=False, float_format="%.8g")
    summary.fillna("").to_csv(SUMMARY_PATH, index=False, float_format="%.8g")

    print("\n==============================")
    print("CancerTrace Gene Clue Rebalance")
    print("==============================")
    print(f"Candidate genes:\n{len(candidate_genes):,}")
    print("Original:")
    if np.isfinite(original_high):
        print(f"HIGH {original_high:.2f}%\nLOW {original_low:.2f}%")
    else:
        print("原 07 线索表不存在，无法比较。")
    print("Rebalanced:")
    print(
        f"HIGH {state_percentages['HIGH']:.2f}%\n"
        f"LOW {state_percentages['LOW']:.2f}%\n"
        f"NORMAL {state_percentages['NORMAL']:.2f}%"
    )
    print(f"Cases with mixed HIGH/LOW clues:\n{mixed_percentage:.2f}%")
    print(f"INITIAL mixed-direction cases:\n{initial_mixed_percentage:.2f}%")

    print("\nEvidence Board QA")
    for title, stats in (("INITIAL", initial_stats), ("AFTER GENE SCAN", after_stats)):
        print(f"\n{title}:")
        print(f"Clear Winner cases:\n{stats['ClearWinnerPercentage']:.2f}%")
        gaps = ", ".join(
            f"{gap}: {stats['GapDistribution'][gap]:.2f}%" for gap in range(5)
        )
        print(f"Top1-Top2 bar gap distribution:\n{gaps}")
        print(f"Median Top1/Top2 ratio:\n{stats['RatioMedian']:.3f}")
        print(
            f"Top1/Top2 ratio Q25/Q75:\n"
            f"{stats['RatioQ25']:.3f} / {stats['RatioQ75']:.3f}"
        )

    if initial_stats["ClearWinnerPercentage"] >= INITIAL_CLEAR_WINNER_WARNING_PERCENT:
        print(
            "WARNING：INITIAL 阶段 Top1=4 且 Top2=0 的病例比例较高；"
            "本脚本仅报告，不自动调整规则。"
        )

    preferred_examples = ["Hey-A8", "MMAc", "NCI-H322"]
    available_names = set(clues["CellLineName"])
    examples = [name for name in preferred_examples if name in available_names]
    if len(examples) < 3:
        examples.extend(
            name
            for name in clues["CellLineName"].drop_duplicates()
            if name not in examples
        )
    for cell_line_name in examples[:3]:
        print_case_example(cell_line_name, clues, board)

    print("\n07.1仅调整Gene Clue System，不修改最终AI模型。")
    print("Test真实标签未用于候选基因、线索排序、表达阈值或Evidence Board。")
    print("所有类别表达统计均仅来自645个Train样本。")


if __name__ == "__main__":
    main()
