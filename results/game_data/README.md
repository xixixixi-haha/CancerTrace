# CancerTrace game data

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
