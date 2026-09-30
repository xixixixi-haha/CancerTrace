"""Export frozen CancerTrace results as Unity-ready game data.

Stage 08 packages the locked Stage 06 predictions and Train-derived Stage 07.1
clues. It fits the specified Cancer Galaxy on Train only and transforms Test.
It never trains or modifies the final classifier.
"""

from __future__ import annotations

import json
from collections import Counter
from contextlib import contextmanager
from datetime import datetime
from pathlib import Path
from time import perf_counter
from typing import Any

_IMPORT_STARTED_AT = datetime.now().astimezone()
_IMPORT_STARTED = perf_counter()
print(
    f"[{_IMPORT_STARTED_AT.isoformat(timespec='seconds')}] START third-party imports",
    flush=True,
)

import numpy as np
import pandas as pd
import umap
from sklearn.decomposition import PCA
from sklearn.feature_selection import VarianceThreshold
from sklearn.preprocessing import StandardScaler

_IMPORT_FINISHED_AT = datetime.now().astimezone()
print(
    f"[{_IMPORT_FINISHED_AT.isoformat(timespec='seconds')}] END third-party imports "
    f"({perf_counter() - _IMPORT_STARTED:.2f} seconds)",
    flush=True,
)


ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "data" / "processed"
TABLES = ROOT / "results" / "tables"
OUT = ROOT / "results" / "game_data"

PATHS = {
    "train": DATA / "cancertrace_train.csv",
    "test": DATA / "cancertrace_test.csv",
    "pred": TABLES / "final_test_predictions.csv",
    "report": TABLES / "final_test_classification_report.csv",
    "metrics": TABLES / "final_test_metrics.csv",
    "confusions": TABLES / "top_test_confusions.csv",
    "clues": TABLES / "test_case_gene_clues_rebalanced.csv",
    "evidence": TABLES / "test_case_evidence_board_rebalanced.csv",
    "pool": TABLES / "gene_clue_pool_rebalanced.csv",
    "profiles": TABLES / "gene_class_profiles.csv",
    "train_dist": TABLES / "train_class_distribution.csv",
    "test_dist": TABLES / "test_class_distribution.csv",
}
TRAIN_GALAXY = TABLES / "cancer_galaxy_train_coordinates.csv"
TEST_GALAXY = TABLES / "cancer_galaxy_test_coordinates.csv"
CASES_QA = TABLES / "game_cases_qa.csv"

CLASSES = [
    "Lung", "Skin", "CNS/Brain", "Bowel", "Esophagus/Stomach", "Breast",
    "Bone", "Ovary/Fallopian Tube",
]
INFO = {
    "Lung": ("lung", "肺"),
    "Skin": ("skin", "皮肤"),
    "CNS/Brain": ("cns_brain", "中枢神经 / 脑"),
    "Bowel": ("bowel", "肠道"),
    "Esophagus/Stomach": ("esophagus_stomach", "食管 / 胃"),
    "Breast": ("breast", "乳腺"),
    "Bone": ("bone", "骨"),
    "Ovary/Fallopian Tube": ("ovary_fallopian_tube", "卵巢 / 输卵管"),
}
PROB_COLS = {
    "Lung": "Prob_Lung",
    "Skin": "Prob_Skin",
    "CNS/Brain": "Prob_CNS_Brain",
    "Bowel": "Prob_Bowel",
    "Esophagus/Stomach": "Prob_Esophagus_Stomach",
    "Breast": "Prob_Breast",
    "Bone": "Prob_Bone",
    "Ovary/Fallopian Tube": "Prob_Ovary_Fallopian_Tube",
}


@contextmanager
def timed_stage(name: str):
    """Print wall-clock start/end timestamps and elapsed seconds immediately."""
    started_at = datetime.now().astimezone()
    started = perf_counter()
    print(f"[{started_at.isoformat(timespec='seconds')}] START {name}", flush=True)
    try:
        yield
    except Exception:
        finished_at = datetime.now().astimezone()
        print(
            f"[{finished_at.isoformat(timespec='seconds')}] FAILED {name} "
            f"after {perf_counter() - started:.2f} seconds",
            flush=True,
        )
        raise
    else:
        finished_at = datetime.now().astimezone()
        print(
            f"[{finished_at.isoformat(timespec='seconds')}] END {name} "
            f"({perf_counter() - started:.2f} seconds)",
            flush=True,
        )


def require(df: pd.DataFrame, cols: list[str], source: Path) -> None:
    missing = [c for c in cols if c not in df.columns]
    if missing:
        raise ValueError(f"{source} is missing required columns: {missing}")


def fnum(value: Any, name: str) -> float:
    result = float(value)
    if not np.isfinite(result):
        raise ValueError(f"Non-finite {name}: {value!r}")
    return result


def compact(label: str) -> dict[str, str]:
    if label not in INFO:
        raise ValueError(f"Unknown cancer class: {label!r}")
    class_id, zh = INFO[label]
    return {"classId": class_id, "labelEn": label, "labelZh": zh}


def labeled(label: str, prefix: str = "") -> dict[str, str]:
    class_id, zh = INFO[label]
    return {
        f"{prefix}ClassId": class_id,
        f"{prefix}ClassLabelEn": label,
        f"{prefix}ClassLabelZh": zh,
    }


def load_inputs() -> dict[str, pd.DataFrame]:
    missing = [str(p) for p in PATHS.values() if not p.exists()]
    if missing:
        raise FileNotFoundError(f"Missing Stage 08 inputs: {missing}")
    frames = {
        "train": pd.read_csv(PATHS["train"], low_memory=False),
        "test": pd.read_csv(PATHS["test"], low_memory=False),
        **{k: pd.read_csv(PATHS[k]) for k in PATHS if k not in {"train", "test"}},
    }
    require(frames["train"], ["ModelID", "CellLineName", "OncotreeLineage"], PATHS["train"])
    require(frames["test"], ["ModelID", "CellLineName", "OncotreeLineage"], PATHS["test"])
    require(frames["pred"], ["ModelID", "CellLineName", "TrueClass", "PredictedClass",
            "Confidence", "Correct", *PROB_COLS.values()], PATHS["pred"])
    require(frames["clues"], ["ModelID", "CellLineName", "CluePhase", "ClueRank",
            "GeneColumn", "GeneSymbol", "ExpressionValue", "ExpressionState",
            "SupportClass1", "SupportStrength1", "SupportClass2", "SupportStrength2"], PATHS["clues"])
    require(frames["evidence"], ["ModelID", "Stage", "Class", "RawSupport", "Bars"], PATHS["evidence"])
    require(frames["report"], ["OncotreeLineage", "Precision", "Recall", "F1-score", "Support"], PATHS["report"])
    require(frames["confusions"], ["TrueClass", "PredictedClass", "ErrorCount"], PATHS["confusions"])
    require(frames["profiles"], ["GeneColumn", "GeneSymbol", "Class", "ClassZMean"], PATHS["profiles"])
    require(frames["pool"], ["GeneColumn", "GeneSymbol"], PATHS["pool"])

    if len(frames["train"]) != 645 or len(frames["test"]) != 162:
        raise ValueError(f"Expected Train/Test 645/162, found {len(frames['train'])}/{len(frames['test'])}")
    for name in ("train", "test", "pred"):
        if frames[name]["ModelID"].isna().any() or frames[name]["ModelID"].duplicated().any():
            raise ValueError(f"{name} ModelID values must be present and unique")
    test_ids = set(frames["test"]["ModelID"].astype(str))
    for name in ("pred", "clues", "evidence"):
        if set(frames[name]["ModelID"].astype(str)) != test_ids:
            raise ValueError(f"{name} ModelID set does not match Test")
    for name, col in (("train", "OncotreeLineage"), ("test", "OncotreeLineage"),
                      ("pred", "TrueClass"), ("pred", "PredictedClass"), ("evidence", "Class")):
        invalid = set(frames[name][col].dropna().astype(str)) - set(CLASSES)
        if invalid:
            raise ValueError(f"Unexpected classes in {name}.{col}: {sorted(invalid)}")
    return frames


def validate_train_galaxy(coords: pd.DataFrame) -> None:
    if not TRAIN_GALAXY.exists():
        print("Train Galaxy consistency QA: skipped (Stage 05 file not found)")
        return
    old = pd.read_csv(TRAIN_GALAXY)
    require(old, ["ModelID", "UMAP1", "UMAP2"], TRAIN_GALAXY)
    if len(old) != len(coords) or old["ModelID"].duplicated().any():
        raise RuntimeError("Train Galaxy QA failed: row count or stored ModelID uniqueness")
    if set(old["ModelID"]) != set(coords["ModelID"]):
        raise RuntimeError("Train Galaxy QA failed: ModelID sets differ")
    merged = old[["ModelID", "UMAP1", "UMAP2"]].merge(
        coords[["ModelID", "UMAP1", "UMAP2"]], on="ModelID", suffixes=("_old", "_new"), validate="one_to_one")
    values = merged[["UMAP1_old", "UMAP2_old", "UMAP1_new", "UMAP2_new"]].to_numpy(float)
    if not np.isfinite(values).all():
        raise RuntimeError("Train Galaxy QA failed: non-finite coordinate")
    rmse = float(np.sqrt(np.mean((values[:, 2:] - values[:, :2]) ** 2)))
    old_range, new_range = np.ptp(values[:, :2], axis=0), np.ptp(values[:, 2:], axis=0)
    range_change = np.abs(new_range - old_range) / np.maximum(old_range, 1e-12)
    if rmse > max(0.05, 0.02 * float(np.linalg.norm(old_range))) or np.any(range_change > 0.10):
        raise RuntimeError(f"Train Galaxy QA failed: RMSE={rmse:.6f}, range change={range_change.tolist()}")
    print(f"Train Galaxy consistency QA: PASS (aligned RMSE {rmse:.8f})")


def build_galaxy(train: pd.DataFrame, test: pd.DataFrame) -> tuple[dict[str, Any], dict[str, Any]]:
    genes = [c for c in train.columns if "(" in c and ")" in c]
    if not genes or any(c not in test.columns for c in genes):
        raise ValueError("Train/Test gene columns are missing or inconsistent")
    x_train, x_test = train[genes].to_numpy(float), test[genes].to_numpy(float)
    if not np.isfinite(x_train).all() or not np.isfinite(x_test).all():
        raise ValueError("Galaxy expression input contains non-finite values")

    with timed_stage("VarianceThreshold fit/transform"):
        variance = VarianceThreshold(threshold=0)
        a_train = variance.fit_transform(x_train)
        a_test = variance.transform(x_test)
    with timed_stage("StandardScaler fit/transform"):
        scaler = StandardScaler()
        b_train = scaler.fit_transform(a_train)
        b_test = scaler.transform(a_test)
    with timed_stage("PCA fit/transform"):
        pca = PCA(n_components=50, svd_solver="randomized", random_state=42)
        c_train = pca.fit_transform(b_train)
        c_test = pca.transform(b_test)

    train_pca_finite = bool(np.isfinite(c_train).all())
    test_pca_finite = bool(np.isfinite(c_test).all())
    print("\nUMAP input diagnostics:", flush=True)
    print(f"- Train PCA shape: {c_train.shape}", flush=True)
    print(f"- Test PCA shape: {c_test.shape}", flush=True)
    print(f"- Train PCA dtype: {c_train.dtype}", flush=True)
    print(f"- Test PCA dtype: {c_test.dtype}", flush=True)
    print(f"- Train PCA contains NaN/Inf: {not train_pca_finite}", flush=True)
    print(f"- Test PCA contains NaN/Inf: {not test_pca_finite}", flush=True)
    if c_train.shape != (645, 50) or c_test.shape != (162, 50):
        raise RuntimeError(f"Unexpected PCA shapes: Train {c_train.shape}, Test {c_test.shape}")
    if not train_pca_finite or not test_pca_finite:
        raise RuntimeError("PCA output contains NaN/Inf")

    reducer = umap.UMAP(n_components=2, n_neighbors=15, min_dist=0.1,
                        metric="euclidean", random_state=42)
    with timed_stage("UMAP fit_transform(train)"):
        u_train = reducer.fit_transform(c_train)
    print(
        f"UMAP transform input: shape={c_test.shape}, dtype={c_test.dtype}, "
        f"contains NaN/Inf={not bool(np.isfinite(c_test).all())}",
        flush=True,
    )
    with timed_stage("UMAP transform(test)"):
        # Transform the complete 162-row Test matrix exactly once. There is no
        # per-sample transform loop and Test is never used for fitting.
        u_test = reducer.transform(c_test)
    if not np.isfinite(u_train).all() or not np.isfinite(u_test).all():
        raise RuntimeError("Galaxy produced non-finite coordinates")

    train_coords = train[["ModelID", "CellLineName", "OncotreeLineage"]].copy()
    train_coords[["UMAP1", "UMAP2"]] = u_train
    test_coords = test[["ModelID", "CellLineName", "OncotreeLineage"]].rename(
        columns={"OncotreeLineage": "TrueClass"}).copy()
    test_coords[["UMAP1", "UMAP2"]] = u_test
    with timed_stage("Galaxy QA"):
        validate_train_galaxy(train_coords)
        if u_train.shape != (645, 2) or u_test.shape != (162, 2):
            raise RuntimeError(f"Unexpected UMAP shapes: Train {u_train.shape}, Test {u_test.shape}")
    test_coords.sort_values("ModelID").to_csv(TEST_GALAXY, index=False, float_format="%.8f")

    all_coords = np.vstack([u_train, u_test]).astype(float)
    minimum, maximum = all_coords.min(axis=0), all_coords.max(axis=0)
    if np.any(maximum <= minimum):
        raise RuntimeError("Galaxy display normalization has a zero span")
    display = 2 * (all_coords - minimum) / (maximum - minimum) - 1
    d_train, d_test = display[:645], display[645:]

    references = {}
    for i, row in train.reset_index(drop=True).iterrows():
        references[str(row.ModelID)] = {
            "modelId": str(row.ModelID), "cellLineName": str(row.CellLineName),
            "label": str(row.OncotreeLineage), "umapX": fnum(u_train[i, 0], "Train UMAP1"),
            "umapY": fnum(u_train[i, 1], "Train UMAP2"), "displayX": fnum(d_train[i, 0], "Train displayX"),
            "displayY": fnum(d_train[i, 1], "Train displayY"),
        }
    case_map = {}
    for i, row in test.reset_index(drop=True).iterrows():
        distances = np.linalg.norm(u_train.astype(float) - u_test[i].astype(float), axis=1)
        nearby = []
        for j in np.argsort(distances, kind="stable")[:5]:
            ref = train.iloc[int(j)]
            nearby.append({"modelId": str(ref.ModelID), "cellLineName": str(ref.CellLineName),
                           **compact(str(ref.OncotreeLineage)), "distance": fnum(distances[int(j)], "distance")})
        case_map[str(row.ModelID)] = {
            "modelId": str(row.ModelID), "cellLineName": str(row.CellLineName), "label": str(row.OncotreeLineage),
            "umapX": fnum(u_test[i, 0], "Test UMAP1"), "umapY": fnum(u_test[i, 1], "Test UMAP2"),
            "displayX": fnum(d_test[i, 0], "Test displayX"), "displayY": fnum(d_test[i, 1], "Test displayY"),
            "nearbyReferences": nearby,
        }

    nodes = []
    for seq, model_id in enumerate(sorted(references), 1):
        r = references[model_id]
        nodes.append({"nodeId": f"REF_{seq:03d}", "modelId": model_id, "cellLineName": r["cellLineName"],
                      "nodeType": "REFERENCE", **labeled(r["label"]), "umapX": r["umapX"], "umapY": r["umapY"],
                      "displayX": r["displayX"], "displayY": r["displayY"], "isDiscovered": True})
    for seq, model_id in enumerate(sorted(case_map), 1):
        r = case_map[model_id]
        nodes.append({"nodeId": f"CASE_NODE_{seq:03d}", "caseId": f"CASE_{seq:03d}", "modelId": model_id,
                      "cellLineName": r["cellLineName"], "nodeType": "CASE", **labeled(r["label"], "true"),
                      "umapX": r["umapX"], "umapY": r["umapY"], "displayX": r["displayX"],
                      "displayY": r["displayY"], "isDiscovered": False})
    counts = Counter(n["nodeType"] for n in nodes)
    if len(nodes) != 807 or len({n["nodeId"] for n in nodes}) != 807 or counts != {"REFERENCE": 645, "CASE": 162}:
        raise RuntimeError(f"Galaxy node QA failed: {len(nodes)}, {dict(counts)}")
    document = {"metadata": {"version": "1.0", "trainReferenceNodes": 645, "caseNodes": 162,
                "totalNodes": 807, "method": "Train-fit VarianceThreshold + StandardScaler + PCA50 + UMAP2; Test transformed only",
                "displayCoordinateRange": [-1, 1]}, "nodes": nodes}
    return document, case_map


def ai_record(row: pd.Series) -> dict[str, Any]:
    by_label = {label: fnum(row[col], f"{row.ModelID} {col}") for label, col in PROB_COLS.items()}
    probabilities = {INFO[label][0]: value for label, value in by_label.items()}
    # The frozen CSV stores rounded probabilities; its maximum observed sum
    # deviation is 2e-6. Accept only that small serialization-level error.
    if not np.isclose(sum(probabilities.values()), 1.0, atol=1e-5, rtol=0):
        raise RuntimeError(f"AI probabilities do not sum to 1 for {row.ModelID}")
    ranked = sorted(CLASSES, key=lambda label: (-by_label[label], CLASSES.index(label)))
    predicted = str(row.PredictedClass)
    confidence = fnum(row.Confidence, f"{row.ModelID} Confidence")
    if predicted != ranked[0]:
        raise RuntimeError(f"PredictedClass is not probability argmax for {row.ModelID}")
    if not np.isclose(confidence, by_label[ranked[0]], atol=1e-8, rtol=0):
        raise RuntimeError(f"Confidence is not maximum probability for {row.ModelID}")
    correct = bool(row.Correct)
    if correct != (predicted == str(row.TrueClass)):
        raise RuntimeError(f"Correct flag mismatch for {row.ModelID}")
    class_id, zh = INFO[predicted]
    return {
        "predictedClassId": class_id, "predictedClassLabelEn": predicted,
        "predictedClassLabelZh": zh, "confidence": confidence, "correct": correct,
        "top3": [{**compact(label), "probability": by_label[label]} for label in ranked[:3]],
        "probabilities": probabilities,
    }


def clue_records(group: pd.DataFrame, phase: str) -> list[dict[str, Any]]:
    selected = group[group["CluePhase"] == phase].sort_values(["ClueRank", "GeneColumn"], kind="stable")
    expected = 2 if phase == "INITIAL" else 3
    if len(selected) != expected:
        raise RuntimeError(f"{group.ModelID.iloc[0]} has {len(selected)} {phase} clues, expected {expected}")
    records = []
    for _, row in selected.iterrows():
        state = str(row.ExpressionState)
        if state not in {"HIGH", "LOW"}:
            raise RuntimeError(f"Invalid clue state {state}")
        support = []
        for number in (1, 2):
            label, strength = row[f"SupportClass{number}"], row[f"SupportStrength{number}"]
            if pd.isna(label) or str(label).strip() == "":
                continue
            if str(label) not in INFO or str(strength) not in {"Weak", "Moderate", "Strong"}:
                raise RuntimeError(f"Invalid clue support: {label}, {strength}")
            support.append({**compact(str(label)), "strength": str(strength)})
        if not 1 <= len(support) <= 2:
            raise RuntimeError("Each clue must have one or two support classes")
        records.append({"gene": str(row.GeneSymbol), "geneColumn": str(row.GeneColumn),
                        "expressionValue": fnum(row.ExpressionValue, "ExpressionValue"),
                        "state": state, "support": support})
    return records


def evidence_top3(group: pd.DataFrame, stage: str) -> list[dict[str, Any]]:
    selected = group[group["Stage"] == stage].copy()
    if len(selected) != 8 or set(selected["Class"]) != set(CLASSES):
        raise RuntimeError(f"{group.ModelID.iloc[0]} {stage} evidence is not exactly 8 classes")
    selected["ClassOrder"] = selected["Class"].map({c: i for i, c in enumerate(CLASSES)})
    selected = selected.sort_values(["Bars", "RawSupport", "ClassOrder"],
                                    ascending=[False, False, True], kind="stable").head(3)
    records = []
    for _, row in selected.iterrows():
        bars = int(row.Bars)
        if not 0 <= bars <= 4 or not np.isfinite(float(row.RawSupport)):
            raise RuntimeError("Evidence value QA failed")
        records.append({**compact(str(row["Class"])), "strength": bars})
    return records


def ambiguity(initial: list[dict[str, Any]], after: list[dict[str, Any]]) -> float:
    initial_gap = initial[0]["strength"] - initial[1]["strength"]
    after_gap = after[0]["strength"] - after[1]["strength"]
    score = 0.4 * ((4 - initial_gap) / 4) + 0.6 * ((4 - after_gap) / 4)
    if initial[0]["classId"] != after[0]["classId"]:
        score += 0.15
    return float(np.clip(score, 0, 1))


def assign_difficulty(cases: list[dict[str, Any]]) -> None:
    ordinary = []
    for case in cases:
        if not case["ai"]["correct"] and case["ai"]["confidence"] >= 0.80:
            case["difficulty"] = "ANOMALY"
        else:
            ordinary.append(case)
    ordinary.sort(key=lambda case: (case["difficultyScore"], case["modelId"]))
    for rank, case in enumerate(ordinary, 1):
        percentile = rank / len(ordinary)
        case["difficulty"] = "EASY" if percentile <= 0.30 else "NORMAL" if percentile <= 0.70 else "HARD"
    counts = Counter(case["difficulty"] for case in cases)
    if any(counts[name] == 0 for name in ("EASY", "NORMAL", "HARD")):
        raise RuntimeError(f"Difficulty bucket QA failed: {dict(counts)}")


def build_cases(frames: dict[str, pd.DataFrame], galaxy: dict[str, Any]) -> tuple[dict[str, Any], pd.DataFrame]:
    test = frames["test"].set_index("ModelID", drop=False)
    pred = frames["pred"].set_index("ModelID", drop=False)
    clue_groups = {str(k): v for k, v in frames["clues"].groupby("ModelID")}
    evidence_groups = {str(k): v for k, v in frames["evidence"].groupby("ModelID")}
    cases = []
    for seq, model_id in enumerate(sorted(test.index.astype(str)), 1):
        row, prediction = test.loc[model_id], pred.loc[model_id]
        true_label = str(row.OncotreeLineage)
        if str(prediction.TrueClass) != true_label or str(prediction.CellLineName) != str(row.CellLineName):
            raise RuntimeError(f"Prediction/Test identity mismatch for {model_id}")
        ai = ai_record(prediction)
        initial_clues = clue_records(clue_groups[model_id], "INITIAL")
        scan_clues = clue_records(clue_groups[model_id], "GENE_SCAN")
        initial = evidence_top3(evidence_groups[model_id], "INITIAL")
        after = evidence_top3(evidence_groups[model_id], "AFTER_GENE_SCAN")
        position = galaxy[model_id]
        cases.append({
            "caseId": f"CASE_{seq:03d}", "modelId": model_id, "cellLineName": str(row.CellLineName),
            **labeled(true_label, "true"), "ai": ai, "initialClues": initial_clues,
            "geneScanClues": scan_clues, "evidence": {"initialTop3": initial, "afterGeneScanTop3": after},
            "difficulty": "PENDING", "difficultyScore": ambiguity(initial, after),
            "galaxy": {key: position[key] for key in ("umapX", "umapY", "displayX", "displayY", "nearbyReferences")},
        })
    assign_difficulty(cases)
    if len(cases) != 162 or len({c["caseId"] for c in cases}) != 162 or len({c["modelId"] for c in cases}) != 162:
        raise RuntimeError("Case identity/count QA failed")
    for case in cases:
        if len(case["initialClues"]) != 2 or len(case["geneScanClues"]) != 3:
            raise RuntimeError(f"Clue count QA failed for {case['caseId']}")
        if len(case["ai"]["probabilities"]) != 8 or len(case["ai"]["top3"]) != 3:
            raise RuntimeError(f"AI probability/top3 QA failed for {case['caseId']}")
        if len(case["evidence"]["initialTop3"]) != 3 or len(case["evidence"]["afterGeneScanTop3"]) != 3:
            raise RuntimeError(f"Evidence Top3 QA failed for {case['caseId']}")
        if len(case["galaxy"]["nearbyReferences"]) != 5:
            raise RuntimeError(f"Nearby reference QA failed for {case['caseId']}")
        if case["difficulty"] not in {"EASY", "NORMAL", "HARD", "ANOMALY"}:
            raise RuntimeError(f"Difficulty QA failed for {case['caseId']}")
    correct = sum(c["ai"]["correct"] for c in cases)
    if correct != 139 or len(cases) - correct != 23:
        raise RuntimeError(f"Frozen AI result QA failed: {correct}/{len(cases) - correct}")
    metadata = {
        "version": "1.0", "generatedBy": "python/src/08_export_game_data.py", "totalCases": 162,
        "classCount": 8, "dataSourceNote": "Real cancer cell-line transcriptomic data.",
        "aiTestAccuracy": correct / len(cases),
        "scientificIntegrity": [
            "AI predictions come from the locked final model.",
            "All 162 game cases come from the independent Test Set.",
            "The Test Set was not used for model selection, hyperparameter tuning, feature selection, or Gene Clue construction.",
            "Gene Clue rules were constructed only from the 645 Train samples.",
        ],
        "scientificDisclaimer": "CancerTrace is an educational research game based on cancer cell-line transcriptomic data. It is not a clinical diagnostic tool.",
        "scientificDisclaimerZh": "CancerTrace 基于癌细胞系转录组数据，仅用于教学与研究展示，不构成临床诊断工具。",
    }
    qa = pd.DataFrame([{
        "CaseID": c["caseId"], "ModelID": c["modelId"], "CellLineName": c["cellLineName"],
        "TrueClass": c["trueClassLabelEn"], "AIPrediction": c["ai"]["predictedClassLabelEn"],
        "AIConfidence": c["ai"]["confidence"], "AICorrect": c["ai"]["correct"],
        "Difficulty": c["difficulty"], "DifficultyScore": c["difficultyScore"],
        "InitialEvidenceTop1": c["evidence"]["initialTop3"][0]["labelEn"],
        "AfterScanEvidenceTop1": c["evidence"]["afterGeneScanTop3"][0]["labelEn"],
        "GalaxyNearestClass": c["galaxy"]["nearbyReferences"][0]["labelEn"],
    } for c in cases])
    return {"metadata": metadata, "cases": cases}, qa


def expression_clues(label: str, pool: pd.DataFrame, profiles: pd.DataFrame) -> list[dict[str, str]]:
    candidates = profiles[(profiles["Class"] == label) & profiles["GeneColumn"].astype(str).isin(
        set(pool["GeneColumn"].astype(str)))].copy()
    candidates["AbsZ"] = candidates["ClassZMean"].abs()
    candidates = candidates.sort_values(["AbsZ", "GeneSymbol", "GeneColumn"],
                                        ascending=[False, True, True], kind="stable")
    candidates = candidates.drop_duplicates("GeneSymbol").head(5)
    if candidates.empty:
        raise RuntimeError(f"No Train expression clues for {label}")
    result = []
    for _, row in candidates.iterrows():
        z = fnum(row.ClassZMean, f"{label} ClassZMean")
        strength = "Strong" if abs(z) >= 1.5 else "Moderate" if abs(z) >= 0.75 else "Weak"
        result.append({"gene": str(row.GeneSymbol), "direction": "HIGH" if z > 0 else "LOW",
                       "strength": strength})
    return result


def build_profiles(frames: dict[str, pd.DataFrame]) -> dict[str, Any]:
    report = frames["report"].set_index("OncotreeLineage")
    train_counts = frames["train"].groupby("OncotreeLineage").size().to_dict()
    test_counts = frames["test"].groupby("OncotreeLineage").size().to_dict()
    confusion = frames["confusions"].copy()
    confusion["Order"] = confusion["PredictedClass"].map({c: i for i, c in enumerate(CLASSES)})
    result = []
    for label in CLASSES:
        if label not in report.index:
            raise RuntimeError(f"Classification report missing {label}")
        metric = report.loc[label]
        rows = confusion[confusion["TrueClass"] == label].sort_values(
            ["ErrorCount", "Order"], ascending=[False, True], kind="stable").head(2)
        common = [{**compact(str(row.PredictedClass)), "errorCount": int(row.ErrorCount)}
                  for _, row in rows.iterrows()]
        class_id, zh = INFO[label]
        profile = {
            "classId": class_id, "labelEn": label, "labelZh": zh,
            "trainSampleCount": int(train_counts.get(label, 0)),
            "testSampleCount": int(test_counts.get(label, 0)),
            "ai": {"precision": fnum(metric.Precision, f"{label} precision"),
                   "recall": fnum(metric.Recall, f"{label} recall"),
                   "f1": fnum(metric["F1-score"], f"{label} f1")},
            "commonConfusions": common,
            "trainingExpressionClues": expression_clues(label, frames["pool"], frames["profiles"]),
        }
        if profile["testSampleCount"] != int(metric.Support):
            raise RuntimeError(f"Classification support mismatch for {label}")
        result.append(profile)
    if len(result) != 8 or {p["classId"] for p in result} != {v[0] for v in INFO.values()}:
        raise RuntimeError("Class profile QA failed")
    return {"metadata": {"version": "1.0", "classCount": 8,
            "trainingExpressionCluesNote": "Representative expression patterns derived only from the 645 Train samples; they are not clinical diagnostic markers."},
            "classes": result}


def game_config() -> dict[str, Any]:
    return {
        "version": "1.0",
        "shift": {"casesPerShift": 5, "startingRP": 300},
        "costs": {"geneScan": 15, "galaxyScan": 25, "askAI": 35},
        "investigation": {"maxPaidActionsPerCase": 3, "initialClueCount": 2,
                          "geneScanClueCount": 3, "aiRequiresPreliminaryDiagnosis": True},
        "rewards": {"correctDiagnosisScore": 100, "efficientResearchScoreBonus": 20,
                    "efficientResearchRefundRP": 10, "efficientResearchMaxSpend": 25,
                    "humanBeatsAIScoreBonus": 0},
        "emergencyGrant": {"triggerBelowRP": 15, "grantRP": 60, "scorePenalty": 100,
                           "maxUsesPerShift": 1},
        "ranks": {"S": 540, "A": 480, "B": 390, "C": 300, "D": 0},
        "difficultyMix": {"easy": 1, "normal": 2, "hard": 1, "wildcard": 1},
    }


def dump_json(path: Path, value: dict[str, Any]) -> None:
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(value, handle, ensure_ascii=False, indent=2, allow_nan=False)
        handle.write("\n")


def write_readme() -> None:
    text = """# CancerTrace game data

- `game_cases.json`: the official 162-case gameplay dataset.
- `galaxy_nodes.json`: 645 Cancer Galaxy reference nodes and 162 case nodes.
- `class_profiles.json`: Research Archive data for the eight cancer classes.
- `game_config.json`: versioned gameplay rules for Unity.

All 162 cases come from the independent Test Set. The final AI model is frozen,
and its predictions and probabilities are exported without modification. Gene
Clue rules were constructed only from the 645 Train samples. The Test Set was
not used for model selection, tuning, feature selection, or Gene Clue rule
construction.

CancerTrace is an educational research game based on cancer cell-line
transcriptomic data. It is not a clinical diagnostic tool.
"""
    (OUT / "README.md").write_text(text, encoding="utf-8", newline="\n")


def reload_qa() -> dict[str, str]:
    files = {name: OUT / name for name in
             ("game_cases.json", "galaxy_nodes.json", "class_profiles.json", "game_config.json")}
    loaded = {}
    for name, path in files.items():
        with path.open(encoding="utf-8") as handle:
            loaded[name] = json.load(handle)
    if len(loaded["game_cases.json"].get("cases", [])) != 162:
        raise RuntimeError("Reload QA failed for game_cases.json")
    if len(loaded["galaxy_nodes.json"].get("nodes", [])) != 807:
        raise RuntimeError("Reload QA failed for galaxy_nodes.json")
    if len(loaded["class_profiles.json"].get("classes", [])) != 8:
        raise RuntimeError("Reload QA failed for class_profiles.json")
    required = {"version", "shift", "costs", "investigation", "rewards",
                "emergencyGrant", "ranks", "difficultyMix"}
    if not required.issubset(loaded["game_config.json"]):
        raise RuntimeError("Reload QA failed for game_config.json")
    return {name: "PASS" for name in files}


def print_summary(document: dict[str, Any], json_qa: dict[str, str]) -> None:
    cases = document["cases"]
    counts = Counter(case["difficulty"] for case in cases)
    correct = sum(case["ai"]["correct"] for case in cases)
    anomalies = [case for case in cases if case["difficulty"] == "ANOMALY"]
    print("\nANOMALY cases:")
    if not anomalies:
        print("(none)")
    for case in anomalies:
        print(f"- {case['cellLineName']} | {case['trueClassLabelEn']} | "
              f"{case['ai']['predictedClassLabelEn']} | {case['ai']['confidence']:.4f}")
    print("\n==============================\nCancerTrace Game Data Export\n==============================")
    print(f"\nGame cases:\n{len(cases)}")
    print("\nGalaxy nodes:\n807\n- Reference: 645\n- Cases: 162")
    print("\nClasses:\n8\n\nDifficulty:")
    for name in ("EASY", "NORMAL", "HARD", "ANOMALY"):
        print(f"{name.title()}: {counts[name]}")
    print(f"\nAI:\nCorrect {correct}\nWrong {len(cases)-correct}\nAccuracy {correct/len(cases):.2%}")
    print("\nGene Clues:\nInitial 2 / case\nGene Scan 3 / case")
    print("\nGalaxy:\nTrain fit 645\nTest transform 162\n\nJSON QA:")
    for name, status in json_qa.items():
        print(f"{name} {status}")
    print("\nGenerated:")
    for name in ("game_cases.json", "galaxy_nodes.json", "class_profiles.json", "game_config.json", "README.md"):
        print(f"results/game_data/{name}")
    print("\nCancerTrace Python / Bioinformatics game-data preparation completed.")
    print("\nFinal AI model was not modified.")
    print("\nTest cases were transformed into game content without being used for model tuning or Gene Clue construction.")
    print("\nUnity development can now begin.")


def main() -> None:
    TABLES.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    with timed_stage("load data"):
        frames = load_inputs()
    galaxy_document, galaxy_map = build_galaxy(frames["train"], frames["test"])
    with timed_stage("JSON export"):
        cases_document, qa = build_cases(frames, galaxy_map)
        profiles_document = build_profiles(frames)
        config_document = game_config()
        metric = frames["metrics"].set_index("Metric")["Value"].to_dict()
        if "Accuracy" not in metric or not np.isclose(cases_document["metadata"]["aiTestAccuracy"],
                                                        fnum(metric["Accuracy"], "Accuracy"), atol=1e-8):
            raise RuntimeError("Exported accuracy disagrees with final_test_metrics.csv")
        qa.to_csv(CASES_QA, index=False, float_format="%.8f")
        dump_json(OUT / "game_cases.json", cases_document)
        dump_json(OUT / "galaxy_nodes.json", galaxy_document)
        dump_json(OUT / "class_profiles.json", profiles_document)
        dump_json(OUT / "game_config.json", config_document)
        write_readme()
        json_qa = reload_qa()
    print_summary(cases_document, json_qa)


if __name__ == "__main__":
    main()
