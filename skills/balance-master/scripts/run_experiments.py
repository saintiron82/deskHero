#!/usr/bin/env python3
"""
Run independent balance experiments from a JSON spec.

Workflow:
1) Snapshot or load baseline config
2) For each experiment:
   - restore baseline
   - apply JSON changes
   - run simulator
   - snapshot config
   - summarize results
3) Pick best result and optionally export best config
"""

from __future__ import annotations

import argparse
import json
import os
import statistics
import subprocess
from datetime import datetime
from pathlib import Path
from shutil import copy2


DEFAULT_CONFIG_FILES = (
    "GameData.json",
    "PermanentStats.json",
    "InGameStatGrowth.json",
    "BossDrops.json",
)

DEFAULT_MONSTER_FILES = (
    "monsters/_index.json",
    "monsters/batch_01.json",
)


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, data: dict) -> None:
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def apply_value(current, spec):
    if isinstance(spec, dict) and "op" in spec:
        op = spec.get("op")
        value = spec.get("value")
        if op == "mul":
            if current is None:
                return value
            return current * value
        if op == "add":
            if current is None:
                return value
            return current + value
        if op == "set":
            return value
        raise ValueError(f"Unsupported op: {op}")
    return spec


def set_by_path(data: dict, path: str, spec):
    parts = path.split(".")
    cursor = data
    for key in parts[:-1]:
        if key not in cursor or not isinstance(cursor[key], dict):
            cursor[key] = {}
        cursor = cursor[key]
    last = parts[-1]
    current = cursor.get(last)
    cursor[last] = apply_value(current, spec)


def apply_changes(file_path: Path, changes: dict) -> None:
    data = read_json(file_path)
    for key, spec in changes.items():
        set_by_path(data, key, spec)
    write_json(file_path, data)


def sanitize_cost_multipliers(config_dir: Path, min_multiplier: float = 1.0) -> int:
    """
    Guard against decreasing-cost multipliers that can create runaway/infinite purchase loops.
    Returns count of fields clamped.
    """
    changed = 0
    for filename in ("PermanentStats.json", "InGameStatGrowth.json"):
        path = config_dir / filename
        data = read_json(path)
        stats = data.get("stats", {})
        file_changed = 0
        for stat in stats.values():
            if not isinstance(stat, dict):
                continue
            value = stat.get("multiplier")
            if isinstance(value, (int, float)) and value < min_multiplier:
                stat["multiplier"] = min_multiplier
                file_changed += 1
        if file_changed:
            write_json(path, data)
            changed += file_changed
    return changed


def snapshot_config(config_dir: Path, dest_dir: Path) -> None:
    copy_config(config_dir, dest_dir)


def restore_config(config_dir: Path, src_dir: Path) -> None:
    copy_config(src_dir, config_dir)


def copy_config(src_dir: Path, dest_dir: Path) -> None:
    (dest_dir / "monsters").mkdir(parents=True, exist_ok=True)
    for name in DEFAULT_CONFIG_FILES:
        copy2(src_dir / name, dest_dir / name)
    for name in DEFAULT_MONSTER_FILES:
        copy2(src_dir / name, dest_dir / name)


def run_simulation(
    output_base: Path,
    game_hours: float,
    cps: float,
    strategy: str,
    seed: int | None,
) -> None:
    cmd = [
        "dotnet",
        "run",
        "--no-build",
        "--project",
        "DeskWarrior.Simulator",
        "--",
        "--progress",
        "--game-hours",
        str(game_hours),
        "--cps",
        str(cps),
        "--strategy",
        strategy,
        "--output",
        str(output_base),
    ]
    if seed is not None:
        cmd.extend(["--seed", str(seed)])
    env = dict(os.environ)
    env["DOTNET_ROLL_FORWARD"] = "LatestMajor"
    subprocess.run(cmd, check=True, env=env)


def read_summary(csv_path: Path) -> dict:
    lines = csv_path.read_text(encoding="utf-8").splitlines()
    if len(lines) < 2:
        return {"death_level": 0, "sessions": 0, "hours": 0.0}
    header = lines[0].split(",")
    last = lines[-1].split(",")
    data = dict(zip(header, last))
    return {
        "death_level": int(float(data.get("Level", "0"))),
        "sessions": int(float(data.get("SessionNum", "0"))),
        "hours": float(data.get("Playtime", "0")) / 3600.0,
    }


def compute_level_after_hour(csv_path: Path, hour: int) -> int | None:
    # "first death after hour" metric
    target = hour * 3600.0
    with csv_path.open() as f:
        header = f.readline()
        if not header:
            return None
        cols = [h.strip() for h in header.split(",")]
        try:
            playtime_idx = cols.index("Playtime")
            level_idx = cols.index("Level")
        except ValueError:
            return None
        for line in f:
            parts = line.strip().split(",")
            if len(parts) <= max(playtime_idx, level_idx):
                continue
            try:
                playtime = float(parts[playtime_idx])
                level = int(float(parts[level_idx]))
            except ValueError:
                continue
            if playtime >= target:
                return level
    return None


def compute_goal_score(
    level_1h: int | None,
    level_50h: int | None,
    goal_1h: int,
    goal_50h: int,
    weight_1h: float,
    weight_50h: float,
) -> float:
    # Smaller error is better => higher score (negative error)
    err_1h = abs(level_1h - goal_1h) if level_1h is not None else goal_1h * 2
    err_50h = abs(level_50h - goal_50h) if level_50h is not None else goal_50h * 2
    return -((weight_1h * err_1h) + (weight_50h * err_50h))


def parse_seeds(value: str | None) -> list[int]:
    if not value:
        return []
    seeds: list[int] = []
    for token in value.replace(";", ",").split(","):
        token = token.strip()
        if not token:
            continue
        if "-" in token:
            start_str, end_str = token.split("-", 1)
            start = int(start_str.strip())
            end = int(end_str.strip())
            step = 1 if end >= start else -1
            seeds.extend(list(range(start, end + step, step)))
        else:
            seeds.append(int(token))
    return seeds


def mean_std(values: list[float]) -> tuple[float | None, float | None]:
    if not values:
        return None, None
    mean = statistics.fmean(values)
    std = statistics.pstdev(values) if len(values) > 1 else 0.0
    return mean, std


def main() -> None:
    parser = argparse.ArgumentParser(description="Run balance experiments from JSON spec.")
    parser.add_argument("--experiments", required=True, help="Path to experiments JSON")
    parser.add_argument("--config-dir", default="config", help="Config directory")
    parser.add_argument("--output-root", default="balanceDoc", help="Output root directory")
    parser.add_argument("--label", default="", help="Label for output folder")
    parser.add_argument("--game-hours", type=float, default=50, help="Game hours")
    parser.add_argument("--cps", type=float, default=5.0, help="CPS")
    parser.add_argument("--strategy", default="balanced", help="Upgrade strategy")
    parser.add_argument(
        "--metric",
        default="death_level",
        choices=["death_level", "sessions", "goal_score", "goal_score_stable"],
        help="Pick best by",
    )
    parser.add_argument("--baseline-dir", default="", help="Use a baseline config snapshot instead of current config")
    parser.add_argument("--promote-best", action="store_true", help="Export best config to run_root/best_config")
    parser.add_argument("--seed", type=int, default=None, help="Random seed for deterministic runs")
    parser.add_argument("--seeds", default="", help="Comma-separated list or ranges of seeds (e.g. 1,2,3 or 1-5)")
    parser.add_argument("--goal-hour1", type=int, default=1, help="Goal hour 1")
    parser.add_argument("--goal-level1", type=int, default=500, help="Goal level at hour 1")
    parser.add_argument("--goal-hour2", type=int, default=50, help="Goal hour 2")
    parser.add_argument("--goal-level2", type=int, default=3000, help="Goal level at hour 2")
    parser.add_argument("--goal-weight1", type=float, default=1.0, help="Weight for goal 1")
    parser.add_argument("--goal-weight2", type=float, default=1.0, help="Weight for goal 2")
    parser.add_argument(
        "--variance-penalty",
        type=float,
        default=0.6,
        help="Penalty weight for std dev when metric=goal_score_stable",
    )
    parser.add_argument(
        "--export-seed-summary",
        action="store_true",
        help="Write per-seed CSV for each experiment when using multiple seeds",
    )
    args = parser.parse_args()

    root = Path.cwd()
    config_dir = (root / args.config_dir).resolve()
    output_root = (root / args.output_root).resolve()

    label = args.label.strip() or datetime.now().strftime("%Y-%m-%d_evolution")
    run_root = output_root / label
    run_root.mkdir(parents=True, exist_ok=True)

    exp_path = Path(args.experiments)
    exp_data = read_json(exp_path)
    experiments = exp_data.get("experiments", [])
    if not experiments:
        raise ValueError("No experiments found in JSON.")

    baseline_dir = run_root / "baseline_config"
    if args.baseline_dir:
        baseline_src = Path(args.baseline_dir)
        if not baseline_src.is_absolute():
            baseline_src = root / baseline_src
        copy_config(baseline_src, baseline_dir)
    else:
        snapshot_config(config_dir, baseline_dir)

    summaries = []

    parsed_seeds = parse_seeds(args.seeds)
    if parsed_seeds:
        seed_list: list[int | None] = parsed_seeds
    elif args.seed is not None:
        seed_list = [args.seed]
    else:
        seed_list = [None]

    for exp in experiments:
        name = exp.get("name")
        if not name:
            raise ValueError("Experiment is missing a name.")

        restore_config(config_dir, baseline_dir)

        changes = exp.get("changes", {})
        for file_key, file_changes in changes.items():
            file_path = Path(file_key)
            if not file_path.is_absolute():
                file_path = root / file_path
            apply_changes(file_path, file_changes)

        clamped = sanitize_cost_multipliers(config_dir)
        if clamped:
            print(f"Note: clamped {clamped} cost multipliers to >= 1.0 for stability.")

        seed_rows = []
        for seed in seed_list:
            seed_suffix = f"seed{seed}" if seed is not None else "seed"
            out_base = run_root / f"{name}_{seed_suffix}"
            run_simulation(out_base, args.game_hours, args.cps, args.strategy, seed)

            csv_path = Path(f"{out_base}_sessions.csv")
            summary = read_summary(csv_path)
            level_1h = compute_level_after_hour(csv_path, args.goal_hour1)
            level_50h = compute_level_after_hour(csv_path, args.goal_hour2)
            err_1h = abs(level_1h - args.goal_level1) if level_1h is not None else args.goal_level1 * 2
            err_50h = abs(level_50h - args.goal_level2) if level_50h is not None else args.goal_level2 * 2
            goal_score = compute_goal_score(
                level_1h,
                level_50h,
                args.goal_level1,
                args.goal_level2,
                args.goal_weight1,
                args.goal_weight2,
            )
            seed_rows.append(
                {
                    "seed": seed,
                    "death_level": summary["death_level"],
                    "sessions": summary["sessions"],
                    "hours": summary["hours"],
                    "level_1h": level_1h,
                    "level_50h": level_50h,
                    "err_1h": err_1h,
                    "err_50h": err_50h,
                    "goal_score": goal_score,
                }
            )

        if args.export_seed_summary and len(seed_rows) > 1:
            seed_csv = run_root / f"{name}_seed_summary.csv"
            seed_csv.write_text(
                "seed,death_level,sessions,hours,level_1h,level_50h,err_1h,err_50h,goal_score\n"
                + "\n".join(
                    f"{row['seed']},{row['death_level']},{row['sessions']},{row['hours']:.6f},{row['level_1h']},{row['level_50h']},{row['err_1h']},{row['err_50h']},{row['goal_score']:.6f}"
                    for row in seed_rows
                )
                + "\n",
                encoding="utf-8",
            )

        death_levels = [row["death_level"] for row in seed_rows]
        sessions = [row["sessions"] for row in seed_rows]
        hours = [row["hours"] for row in seed_rows]
        level_1h_values = [row["level_1h"] for row in seed_rows if row["level_1h"] is not None]
        level_50h_values = [row["level_50h"] for row in seed_rows if row["level_50h"] is not None]
        err_1h_values = [float(row["err_1h"]) for row in seed_rows]
        err_50h_values = [float(row["err_50h"]) for row in seed_rows]

        death_level_mean, _ = mean_std([float(v) for v in death_levels])
        sessions_mean, _ = mean_std([float(v) for v in sessions])
        hours_mean, _ = mean_std([float(v) for v in hours])
        level_1h_mean, level_1h_std = mean_std([float(v) for v in level_1h_values])
        level_50h_mean, level_50h_std = mean_std([float(v) for v in level_50h_values])
        err_1h_mean, err_1h_std = mean_std(err_1h_values)
        err_50h_mean, err_50h_std = mean_std(err_50h_values)

        goal_score_mean = -(
            (args.goal_weight1 * (err_1h_mean or 0.0)) + (args.goal_weight2 * (err_50h_mean or 0.0))
        )
        penalty = args.variance_penalty
        goal_score_stable = -(
            (args.goal_weight1 * ((err_1h_mean or 0.0) + (penalty * (err_1h_std or 0.0))))
            + (args.goal_weight2 * ((err_50h_mean or 0.0) + (penalty * (err_50h_std or 0.0))))
        )

        summary = {
            "name": name,
            "seed_count": len(seed_rows),
            "death_level": int(round(death_level_mean or 0.0)),
            "sessions": int(round(sessions_mean or 0.0)),
            "hours": float(hours_mean or 0.0),
            "level_1h": level_1h_mean,
            "level_50h": level_50h_mean,
            "level_1h_std": level_1h_std,
            "level_50h_std": level_50h_std,
            "err_1h_mean": err_1h_mean,
            "err_1h_std": err_1h_std,
            "err_50h_mean": err_50h_mean,
            "err_50h_std": err_50h_std,
            "goal_score": goal_score_mean,
            "goal_score_stable": goal_score_stable,
        }
        summaries.append(summary)

        snapshot_config(config_dir, run_root / f"{name}_config")

    # Restore baseline at end
    restore_config(config_dir, baseline_dir)

    # Print summary
    print("\nSUMMARY")
    for s in summaries:
        print(s)

    if summaries:
        if args.metric == "death_level":
            best = max(summaries, key=lambda s: (s["death_level"], s["sessions"], s["hours"]))
        elif args.metric == "sessions":
            best = max(summaries, key=lambda s: (s["sessions"], s["death_level"], s["hours"]))
        else:
            metric_key = "goal_score" if args.metric == "goal_score" else "goal_score_stable"
            best = max(summaries, key=lambda s: (s.get(metric_key, s["goal_score"]), s["death_level"], s["sessions"]))
        print(f"\nBEST ({args.metric}): {best}")

        if args.promote_best:
            best_src = run_root / f"{best['name']}_config"
            best_dest = run_root / "best_config"
            if best_dest.exists():
                for path in best_dest.rglob("*"):
                    if path.is_file():
                        path.unlink()
                for path in sorted(best_dest.rglob("*"), reverse=True):
                    if path.is_dir():
                        path.rmdir()
            copy_config(best_src, best_dest)
            print(f"Best config exported to: {best_dest}")

    summary_json = run_root / "summary.json"
    summary_csv = run_root / "summary.csv"
    summary_json.write_text(json.dumps(summaries, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    summary_csv.write_text(
        "name,seed_count,death_level,sessions,hours,level_1h,level_1h_std,level_50h,level_50h_std,err_1h_mean,err_1h_std,err_50h_mean,err_50h_std,goal_score,goal_score_stable\n"
        + "\n".join(
            f"{s['name']},{s['seed_count']},{s['death_level']},{s['sessions']},{s['hours']:.6f},{s['level_1h']},{s['level_1h_std']},{s['level_50h']},{s['level_50h_std']},{s['err_1h_mean']},{s['err_1h_std']},{s['err_50h_mean']},{s['err_50h_std']},{s['goal_score']:.6f},{s['goal_score_stable']:.6f}"
            for s in summaries
        )
        + "\n",
        encoding="utf-8",
    )

if __name__ == "__main__":
    main()
