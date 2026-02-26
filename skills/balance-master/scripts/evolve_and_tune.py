#!/usr/bin/env python3
"""
Run N generations of evolution, then tune the injection strength based on win rate.

This script:
1) Runs evolve.py for the requested generations (default 50).
2) Reads the summary CSV to compute injection win rate.
3) Adjusts the injection parameters in experiments JSON (time_econ_overdrive).
4) Writes a tuning log to balanceDoc/injection_tuning_log.csv.
"""

from __future__ import annotations

import argparse
import csv
import json
import subprocess
import sys
from datetime import datetime
from pathlib import Path


ADJUST_RULES = {
    "config/PermanentStats.json": {
        "stats.time_extend.effect_per_level": {"kind": "effect", "min": 0.2, "max": 0.6},
        "stats.time_extend.base_cost": {"kind": "cost", "min": 0.5, "max": 0.95},
        "stats.time_extend.multiplier": {"kind": "cost", "min": 0.75, "max": 1.0},
        "stats.time_extend.tier_config.effect_multiplier_per_tier": {"kind": "effect", "min": 1.0, "max": 1.1},
        "stats.upgrade_discount.effect_per_level": {"kind": "effect", "min": 3.0, "max": 6.0},
        "stats.upgrade_discount.multiplier": {"kind": "cost", "min": 0.7, "max": 1.0},
        "stats.upgrade_discount.growth_rate": {"kind": "cost", "min": 0.5, "max": 1.0},
    },
    "config/InGameStatGrowth.json": {
        "stats.keyboard_power.base_cost": {"kind": "cost", "min": 0.6, "max": 1.0},
        "stats.keyboard_power.multiplier": {"kind": "cost", "min": 0.7, "max": 1.0},
        "stats.keyboard_power.growth_rate": {"kind": "cost", "min": 0.7, "max": 1.0},
        "stats.mouse_power.base_cost": {"kind": "cost", "min": 0.6, "max": 1.0},
        "stats.mouse_power.multiplier": {"kind": "cost", "min": 0.7, "max": 1.0},
        "stats.mouse_power.growth_rate": {"kind": "cost", "min": 0.7, "max": 1.0},
    },
}


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, data: dict) -> None:
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def clamp(value: float, min_value: float, max_value: float) -> float:
    return max(min_value, min(max_value, value))


def adjust_value(value: float, kind: str, delta: float, min_value: float, max_value: float) -> float:
    if kind == "effect":
        factor = 1.0 + delta
    else:
        # cost tuning: stronger => lower cost multipliers
        factor = 1.0 - (delta * 0.5)
    return clamp(value * factor, min_value, max_value)


def tune_injection(experiments_path: Path, delta: float) -> dict:
    data = read_json(experiments_path)
    injections = data.get("injections", [])
    if not injections:
        return {}
    target = injections[0]

    changes = target.get("changes", {})
    updated = {}

    for file_key, rules in ADJUST_RULES.items():
        file_changes = changes.get(file_key, {})
        for path, rule in rules.items():
            spec = file_changes.get(path)
            if not spec or not isinstance(spec, dict) or "value" not in spec:
                continue
            old = float(spec["value"])
            new = adjust_value(old, rule["kind"], delta, rule["min"], rule["max"])
            spec["value"] = round(new, 6)
            updated[f"{file_key}:{path}"] = {"old": old, "new": spec["value"]}

    write_json(experiments_path, data)
    return updated


def compute_inject_win_rate(summary_csv: Path) -> float:
    rows = []
    with summary_csv.open() as f:
        reader = csv.DictReader(f)
        for row in reader:
            rows.append(row)
    if not rows:
        return 0.0
    inject_wins = sum(1 for r in rows if r.get("best_source") == "inject")
    return inject_wins / len(rows)


def choose_delta(win_rate: float) -> float:
    if win_rate < 0.2:
        return 0.08
    if win_rate < 0.3:
        return 0.05
    if win_rate < 0.4:
        return 0.03
    if win_rate > 0.7:
        return -0.08
    if win_rate > 0.6:
        return -0.05
    if win_rate > 0.5:
        return -0.03
    return 0.0


def append_log(log_path: Path, label: str, win_rate: float, delta: float, updates: dict) -> None:
    log_path.parent.mkdir(parents=True, exist_ok=True)
    is_new = not log_path.exists()
    with log_path.open("a", newline="") as f:
        writer = csv.writer(f)
        if is_new:
            writer.writerow(["timestamp", "label", "inject_win_rate", "delta", "updates"])
        writer.writerow([datetime.now().isoformat(timespec="seconds"), label, f"{win_rate:.3f}", f"{delta:.3f}", json.dumps(updates, ensure_ascii=False)])


def main() -> None:
    parser = argparse.ArgumentParser(description="Run evolution cycles and tune injection strength.")
    parser.add_argument("--experiments", required=True, help="Path to experiments JSON")
    parser.add_argument("--generations", type=int, default=50, help="Generations per cycle")
    parser.add_argument("--label", default="", help="Base label for output")
    parser.add_argument("--output-root", default="balanceDoc", help="Output root directory")
    parser.add_argument("--baseline-dir", default="balanceDoc/latest_best_config", help="Baseline config")
    parser.add_argument("--game-hours", type=float, default=50, help="Game hours")
    parser.add_argument("--cps", type=float, default=5.0, help="CPS")
    parser.add_argument("--strategy", default="balanced", help="Upgrade strategy")
    parser.add_argument("--main-count", type=int, default=3, help="Main experiments count")
    parser.add_argument("--inject-count", type=int, default=1, help="Injection experiments count")
    parser.add_argument("--metric", default="death_level", choices=["death_level", "sessions", "goal_score"], help="Pick best by")
    parser.add_argument("--goal-hour1", type=int, default=1, help="Goal hour 1")
    parser.add_argument("--goal-level1", type=int, default=500, help="Goal level at hour 1")
    parser.add_argument("--goal-hour2", type=int, default=50, help="Goal hour 2")
    parser.add_argument("--goal-level2", type=int, default=3000, help="Goal level at hour 2")
    parser.add_argument("--goal-weight1", type=float, default=1.0, help="Weight for goal 1")
    parser.add_argument("--goal-weight2", type=float, default=1.0, help="Weight for goal 2")
    parser.add_argument("--seed", type=int, default=None, help="Random seed for deterministic runs")
    parser.add_argument("--seeds", default="", help="Comma-separated list or ranges of seeds for stability scoring")
    parser.add_argument("--variance-penalty", type=float, default=0.6, help="Std penalty when metric=goal_score_stable")
    parser.add_argument(
        "--require-goal-improvement",
        action="store_true",
        help="Require both goal errors to not worsen vs baseline when metric=goal_score",
    )
    args = parser.parse_args()

    root = Path.cwd()
    experiments_path = (root / args.experiments).resolve()
    output_root = (root / args.output_root).resolve()

    label = args.label.strip() or datetime.now().strftime("auto_evolution_cycle_%Y-%m-%d_%H%M")

    evolve_cmd = [
        sys.executable,
        str(root / "skills" / "balance-master" / "scripts" / "evolve.py"),
        "--experiments",
        str(experiments_path),
        "--generations",
        str(args.generations),
        "--label",
        label,
        "--game-hours",
        str(args.game_hours),
        "--cps",
        str(args.cps),
        "--strategy",
        args.strategy,
        "--metric",
        args.metric,
        "--main-count",
        str(args.main_count),
        "--inject-count",
        str(args.inject_count),
        "--baseline-dir",
        args.baseline_dir,
        "--promote-to",
        "balanceDoc/latest_best_config",
        "--goal-hour1",
        str(args.goal_hour1),
        "--goal-level1",
        str(args.goal_level1),
        "--goal-hour2",
        str(args.goal_hour2),
        "--goal-level2",
        str(args.goal_level2),
        "--goal-weight1",
        str(args.goal_weight1),
        "--goal-weight2",
        str(args.goal_weight2),
        "--require-goal-improvement",
    ]
    if args.seed is not None:
        evolve_cmd.extend(["--seed", str(args.seed)])
    if args.seeds:
        evolve_cmd.extend(["--seeds", args.seeds])
    if args.metric == "goal_score_stable":
        evolve_cmd.extend(["--variance-penalty", str(args.variance_penalty)])
    subprocess.run(evolve_cmd, check=True)

    summary_csv = output_root / f"{label}_summary.csv"
    win_rate = compute_inject_win_rate(summary_csv)
    delta = choose_delta(win_rate)
    updates = {}
    if delta != 0.0:
        updates = tune_injection(experiments_path, delta)

    log_path = output_root / "injection_tuning_log.csv"
    append_log(log_path, label, win_rate, delta, updates)

    print(f"Injection win rate: {win_rate:.2%}")
    print(f"Tuning delta applied: {delta:+.3f}")
    if updates:
        print(f"Updated {len(updates)} parameters in {experiments_path}")


if __name__ == "__main__":
    main()
