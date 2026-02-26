#!/usr/bin/env python3
"""
Run multiple generations of balance experiments and promote the best each time.

Each generation:
  - uses baseline (first: current config or provided baseline dir)
  - runs run_experiments.py with --promote-best on main experiments
  - optionally runs injection experiments from a fixed seed baseline
  - records best result
  - uses best_config as baseline for next generation
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
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


def read_run_summary(run_root: Path) -> dict[str, dict]:
    summary_path = run_root / "summary.json"
    if not summary_path.exists():
        return {}
    data = json.loads(summary_path.read_text(encoding="utf-8"))
    if not isinstance(data, list):
        return {}
    summaries = {}
    for row in data:
        if isinstance(row, dict) and row.get("name"):
            summaries[row["name"]] = row
    return summaries


def compute_level_after_hour(csv_path: Path, hour: int) -> int | None:
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
    err_1h = abs(level_1h - goal_1h) if level_1h is not None else goal_1h * 2
    err_50h = abs(level_50h - goal_50h) if level_50h is not None else goal_50h * 2
    return -((weight_1h * err_1h) + (weight_50h * err_50h))


def run_generation(
    script_path: Path,
    experiments_path: Path,
    label: str,
    game_hours: float,
    cps: float,
    strategy: str,
    metric: str,
    goal_hour1: int,
    goal_level1: int,
    goal_hour2: int,
    goal_level2: int,
    goal_weight1: float,
    goal_weight2: float,
    baseline_dir: str | None,
    seed: int | None,
    seeds: str,
    variance_penalty: float,
) -> None:
    cmd = [
        sys.executable,
        str(script_path),
        "--experiments",
        str(experiments_path),
        "--label",
        label,
        "--game-hours",
        str(game_hours),
        "--cps",
        str(cps),
        "--strategy",
        strategy,
        "--metric",
        metric,
        "--goal-hour1",
        str(goal_hour1),
        "--goal-level1",
        str(goal_level1),
        "--goal-hour2",
        str(goal_hour2),
        "--goal-level2",
        str(goal_level2),
        "--goal-weight1",
        str(goal_weight1),
        "--goal-weight2",
        str(goal_weight2),
        "--promote-best",
    ]
    if baseline_dir:
        cmd.extend(["--baseline-dir", baseline_dir])
    if seed is not None:
        cmd.extend(["--seed", str(seed)])
    if seeds:
        cmd.extend(["--seeds", seeds])
    if metric == "goal_score_stable":
        cmd.extend(["--variance-penalty", str(variance_penalty)])
    subprocess.run(cmd, check=True)


def copy_config(src_dir: Path, dest_dir: Path) -> None:
    (dest_dir / "monsters").mkdir(parents=True, exist_ok=True)
    for name in DEFAULT_CONFIG_FILES:
        copy2(src_dir / name, dest_dir / name)
    for name in DEFAULT_MONSTER_FILES:
        copy2(src_dir / name, dest_dir / name)


def write_experiments(path: Path, experiments: list[dict]) -> None:
    path.write_text(json.dumps({"experiments": experiments}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_single_experiment(path: Path, name: str) -> None:
    path.write_text(
        json.dumps({"experiments": [{"name": name, "changes": {}}]}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def evaluate_baseline(
    script_path: Path,
    output_root: Path,
    base_label: str,
    baseline_dir: str,
    game_hours: float,
    cps: float,
    strategy: str,
    metric: str,
    goal_hour1: int,
    goal_level1: int,
    goal_hour2: int,
    goal_level2: int,
    goal_weight1: float,
    goal_weight2: float,
    seed: int | None,
    seeds: str,
    variance_penalty: float,
) -> dict:
    eval_label = f"{base_label}_baseline_eval"
    eval_root = output_root / eval_label
    eval_root.mkdir(parents=True, exist_ok=True)
    eval_json = eval_root / "baseline.json"
    write_single_experiment(eval_json, "baseline_eval")
    run_generation(
        script_path=script_path,
        experiments_path=eval_json,
        label=eval_label,
        game_hours=game_hours,
        cps=cps,
        strategy=strategy,
        metric=metric,
        goal_hour1=goal_hour1,
        goal_level1=goal_level1,
        goal_hour2=goal_hour2,
        goal_level2=goal_level2,
        goal_weight1=goal_weight1,
        goal_weight2=goal_weight2,
        baseline_dir=baseline_dir,
        seed=seed,
        seeds=seeds,
        variance_penalty=variance_penalty,
    )
    summary_map = read_run_summary(eval_root)
    summary = summary_map.get("baseline_eval") if summary_map else None
    if summary:
        return summary

    csv_path = eval_root / "baseline_eval_sessions.csv"
    summary = read_summary(csv_path)
    level_1h = compute_level_after_hour(csv_path, goal_hour1)
    level_50h = compute_level_after_hour(csv_path, goal_hour2)
    goal_score = compute_goal_score(
        level_1h,
        level_50h,
        goal_level1,
        goal_level2,
        goal_weight1,
        goal_weight2,
    )
    summary["level_1h"] = level_1h
    summary["level_50h"] = level_50h
    summary["goal_score"] = goal_score
    return summary


def collect_results(
    run_root: Path,
    names: list[str],
    goal_hour1: int,
    goal_level1: int,
    goal_hour2: int,
    goal_level2: int,
    goal_weight1: float,
    goal_weight2: float,
) -> list[dict]:
    results = []
    summary_map = read_run_summary(run_root)
    for name in names:
        if summary_map and name in summary_map:
            summary = summary_map[name]
            summary["name"] = name
            results.append(summary)
            continue
        csv_path = run_root / f"{name}_sessions.csv"
        summary = read_summary(csv_path)
        level_1h = compute_level_after_hour(csv_path, goal_hour1)
        level_50h = compute_level_after_hour(csv_path, goal_hour2)
        goal_score = compute_goal_score(
            level_1h,
            level_50h,
            goal_level1,
            goal_level2,
            goal_weight1,
            goal_weight2,
        )
        summary["level_1h"] = level_1h
        summary["level_50h"] = level_50h
        summary["goal_score"] = goal_score
        summary["name"] = name
        results.append(summary)
    return results


def select_items(items: list[dict], count: int) -> list[dict]:
    if count < 0:
        return items
    if count == 0:
        return []
    return items[: min(count, len(items))]


def main() -> None:
    parser = argparse.ArgumentParser(description="Run multi-generation balance evolution.")
    parser.add_argument("--experiments", required=True, help="Path to experiments JSON")
    parser.add_argument("--generations", type=int, default=3, help="Number of generations to run")
    parser.add_argument("--config-dir", default="config", help="Config directory")
    parser.add_argument("--output-root", default="balanceDoc", help="Output root directory")
    parser.add_argument("--label", default="", help="Base label for output folders")
    parser.add_argument("--game-hours", type=float, default=50, help="Game hours")
    parser.add_argument("--cps", type=float, default=5.0, help="CPS")
    parser.add_argument("--strategy", default="balanced", help="Upgrade strategy")
    parser.add_argument(
        "--metric",
        default="death_level",
        choices=["death_level", "sessions", "goal_score", "goal_score_stable"],
        help="Pick best by",
    )
    parser.add_argument("--baseline-dir", default="", help="Use a baseline config snapshot for generation 1")
    parser.add_argument(
        "--promote-to",
        default="",
        help="Copy each generation best_config to a stable path (overwrites existing)",
    )
    parser.add_argument("--main-count", type=int, default=-1, help="How many main experiments to run (-1 = all)")
    parser.add_argument("--inject-count", type=int, default=-1, help="How many injections to run (-1 = all, 0 = none)")
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
        help="When metric=goal_score, require both goal errors to not worsen vs baseline",
    )
    args = parser.parse_args()

    root = Path.cwd()
    config_dir = (root / args.config_dir).resolve()
    output_root = (root / args.output_root).resolve()
    output_root.mkdir(parents=True, exist_ok=True)

    base_label = args.label.strip() or datetime.now().strftime("%Y-%m-%d_evolution")
    exp_path = Path(args.experiments)
    exp_data = read_json(exp_path)
    experiments = exp_data.get("experiments", [])
    injections = exp_data.get("injections", [])
    if not experiments:
        raise ValueError("No experiments found in JSON.")
    experiment_names = [exp.get("name") for exp in experiments if exp.get("name")]
    if len(experiment_names) != len(experiments):
        raise ValueError("All experiments must have a name.")
    injection_names = [exp.get("name") for exp in injections if exp.get("name")]
    if injections and len(injection_names) != len(injections):
        raise ValueError("All injections must have a name.")

    script_path = root / "skills" / "balance-master" / "scripts" / "run_experiments.py"
    baseline_dir = args.baseline_dir.strip() or None
    if args.require_goal_improvement:
        baseline_dir = baseline_dir or str(config_dir)
    promote_to = args.promote_to.strip()
    promote_to_path = Path(promote_to).resolve() if promote_to else None

    summary_rows = []

    baseline_stats = None
    baseline_level_1h = None
    baseline_level_50h = None
    baseline_err1 = None
    baseline_err2 = None
    baseline_std1 = None
    baseline_std2 = None
    baseline_seed_count = 1
    if args.require_goal_improvement and args.metric in ("goal_score", "goal_score_stable"):
        baseline_stats = evaluate_baseline(
            script_path=script_path,
            output_root=output_root,
            base_label=base_label,
            baseline_dir=baseline_dir,
            game_hours=args.game_hours,
            cps=args.cps,
            strategy=args.strategy,
            metric=args.metric,
            goal_hour1=args.goal_hour1,
            goal_level1=args.goal_level1,
            goal_hour2=args.goal_hour2,
            goal_level2=args.goal_level2,
            goal_weight1=args.goal_weight1,
            goal_weight2=args.goal_weight2,
            seed=args.seed,
            seeds=args.seeds,
            variance_penalty=args.variance_penalty,
        )
        baseline_level_1h = baseline_stats.get("level_1h")
        baseline_level_50h = baseline_stats.get("level_50h")
        baseline_err1 = baseline_stats.get("err_1h_mean")
        baseline_err2 = baseline_stats.get("err_50h_mean")
        if baseline_err1 is None:
            baseline_err1 = abs(baseline_level_1h - args.goal_level1) if baseline_level_1h is not None else None
        if baseline_err2 is None:
            baseline_err2 = abs(baseline_level_50h - args.goal_level2) if baseline_level_50h is not None else None
        baseline_std1 = baseline_stats.get("err_1h_std")
        baseline_std2 = baseline_stats.get("err_50h_std")
        baseline_seed_count = int(baseline_stats.get("seed_count", 1) or 1)

    seed_baseline = output_root / f"{base_label}_seed_baseline"
    if baseline_dir:
        copy_config(Path(baseline_dir).resolve(), seed_baseline)
    else:
        copy_config(config_dir, seed_baseline)

    for gen in range(1, args.generations + 1):
        gen_label = f"{base_label}_gen{gen:02d}"
        main_label = f"{gen_label}_main"
        main_root = output_root / main_label
        results = []

        main_experiments = select_items(experiments, args.main_count)
        main_names = [exp["name"] for exp in main_experiments]
        if main_experiments:
            main_root.mkdir(parents=True, exist_ok=True)
            main_json = main_root / "experiments.json"
            write_experiments(main_json, main_experiments)
            run_generation(
                script_path=script_path,
                experiments_path=main_json,
                label=main_label,
                game_hours=args.game_hours,
                cps=args.cps,
                strategy=args.strategy,
                metric=args.metric,
                goal_hour1=args.goal_hour1,
                goal_level1=args.goal_level1,
                goal_hour2=args.goal_hour2,
                goal_level2=args.goal_level2,
                goal_weight1=args.goal_weight1,
                goal_weight2=args.goal_weight2,
                baseline_dir=baseline_dir,
                seed=args.seed,
                seeds=args.seeds,
                variance_penalty=args.variance_penalty,
            )
            results = collect_results(
                main_root,
                main_names,
                args.goal_hour1,
                args.goal_level1,
                args.goal_hour2,
                args.goal_level2,
                args.goal_weight1,
                args.goal_weight2,
            )

        injection_root = None
        injection_results = []
        injection_experiments = select_items(injections, args.inject_count)
        injection_names = [exp["name"] for exp in injection_experiments]
        if injection_experiments:
            injection_label = f"{gen_label}_inject"
            injection_root = output_root / injection_label
            injection_json = injection_root / "injections.json"
            injection_root.mkdir(parents=True, exist_ok=True)
            write_experiments(injection_json, injection_experiments)
            run_generation(
                script_path=script_path,
                experiments_path=injection_json,
                label=injection_label,
                game_hours=args.game_hours,
                cps=args.cps,
                strategy=args.strategy,
                metric=args.metric,
                goal_hour1=args.goal_hour1,
                goal_level1=args.goal_level1,
                goal_hour2=args.goal_hour2,
                goal_level2=args.goal_level2,
                goal_weight1=args.goal_weight1,
                goal_weight2=args.goal_weight2,
                baseline_dir=str(seed_baseline),
                seed=args.seed,
                seeds=args.seeds,
                variance_penalty=args.variance_penalty,
            )
            injection_results = collect_results(
                injection_root,
                injection_names,
                args.goal_hour1,
                args.goal_level1,
                args.goal_hour2,
                args.goal_level2,
                args.goal_weight1,
                args.goal_weight2,
            )
            results = results + injection_results

        if not results:
            raise ValueError("No experiments ran. Check --main-count and --inject-count.")

        filtered_results = results
        if args.require_goal_improvement and args.metric in ("goal_score", "goal_score_stable"):
            filtered_results = []
            for r in results:
                level_1h = r.get("level_1h")
                level_50h = r.get("level_50h")
                if level_1h is None or level_50h is None:
                    continue
                err1 = r.get("err_1h_mean")
                err2 = r.get("err_50h_mean")
                if err1 is None:
                    err1 = abs(level_1h - args.goal_level1)
                if err2 is None:
                    err2 = abs(level_50h - args.goal_level2)

                pass_mean = True
                if baseline_err1 is not None and baseline_err2 is not None:
                    pass_mean = err1 <= baseline_err1 and err2 <= baseline_err2

                r_std1 = r.get("err_1h_std")
                r_std2 = r.get("err_50h_std")
                r_seed_count = int(r.get("seed_count", 1) or 1)
                pass_std = True
                if (
                    baseline_seed_count > 1
                    and r_seed_count > 1
                    and baseline_std1 is not None
                    and baseline_std2 is not None
                    and r_std1 is not None
                    and r_std2 is not None
                ):
                    pass_std = (r_std1 <= baseline_std1) and (r_std2 <= baseline_std2)

                if pass_mean and pass_std:
                    filtered_results.append(r)

        if not filtered_results and args.require_goal_improvement and args.metric == "goal_score":
            best = {
                "name": "baseline_hold",
                "death_level": baseline_stats.get("death_level", 0),
                "sessions": baseline_stats.get("sessions", 0),
                "hours": baseline_stats.get("hours", 0.0),
                "level_1h": baseline_level_1h,
                "level_50h": baseline_level_50h,
                "goal_score": baseline_stats.get("goal_score", 0.0),
            }
            best_source = "baseline"
            best_config_path = Path(baseline_dir)
        else:
            if args.metric == "death_level":
                best = max(filtered_results, key=lambda s: (s["death_level"], s["sessions"], s["hours"]))
            elif args.metric == "sessions":
                best = max(filtered_results, key=lambda s: (s["sessions"], s["death_level"], s["hours"]))
            elif args.metric == "goal_score":
                best = max(filtered_results, key=lambda s: (s.get("goal_score", 0.0), s["death_level"], s["sessions"]))
            else:
                best = max(
                    filtered_results,
                    key=lambda s: (s.get("goal_score_stable", s.get("goal_score", 0.0)), s["death_level"], s["sessions"]),
                )
            if injection_results and any(s["name"] == best["name"] for s in injection_results):
                best_config_path = injection_root / "best_config"
                best_source = "inject"
            else:
                best_config_path = main_root / "best_config"
                best_source = "main"

        summary_rows.append(
            {
                "generation": gen,
                "label": gen_label,
                "best_name": best["name"],
                "metric": args.metric,
                "death_level": best["death_level"],
                "sessions": best["sessions"],
                "hours": round(best["hours"], 2),
                "level_1h": best.get("level_1h"),
                "level_50h": best.get("level_50h"),
                "goal_score": round(float(best.get("goal_score", 0.0)), 3),
                "goal_score_stable": round(float(best.get("goal_score_stable", best.get("goal_score", 0.0))), 3),
                "seed_count": int(best.get("seed_count", 1) or 1),
            }
        )

        baseline_dir = str(best_config_path)
        if args.require_goal_improvement and args.metric in ("goal_score", "goal_score_stable") and best_source != "baseline":
            baseline_level_1h = best.get("level_1h")
            baseline_level_50h = best.get("level_50h")
            baseline_err1 = best.get("err_1h_mean")
            baseline_err2 = best.get("err_50h_mean")
            if baseline_err1 is None:
                baseline_err1 = abs(baseline_level_1h - args.goal_level1) if baseline_level_1h is not None else baseline_err1
            if baseline_err2 is None:
                baseline_err2 = abs(baseline_level_50h - args.goal_level2) if baseline_level_50h is not None else baseline_err2
            baseline_std1 = best.get("err_1h_std")
            baseline_std2 = best.get("err_50h_std")
            baseline_seed_count = int(best.get("seed_count", baseline_seed_count) or baseline_seed_count)
            baseline_stats = best
        if promote_to_path:
            if best_config_path.resolve() != promote_to_path.resolve():
                if promote_to_path.exists():
                    for path in promote_to_path.rglob("*"):
                        if path.is_file():
                            path.unlink()
                    for path in sorted(promote_to_path.rglob("*"), reverse=True):
                        if path.is_dir():
                            path.rmdir()
                copy_config(best_config_path, promote_to_path)
        summary_rows[-1]["best_source"] = best_source

    summary_json = output_root / f"{base_label}_summary.json"
    summary_csv = output_root / f"{base_label}_summary.csv"
    summary_json.write_text(json.dumps(summary_rows, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    summary_csv.write_text(
        "generation,label,best_name,best_source,metric,death_level,sessions,hours,level_1h,level_50h,goal_score,goal_score_stable,seed_count\n"
        + "\n".join(
            f"{row['generation']},{row['label']},{row['best_name']},{row['best_source']},{row['metric']},{row['death_level']},{row['sessions']},{row['hours']},{row['level_1h']},{row['level_50h']},{row['goal_score']},{row['goal_score_stable']},{row['seed_count']}"
            for row in summary_rows
        )
        + "\n",
        encoding="utf-8",
    )

    print(f"Evolution summary saved to: {summary_json}")
    print(f"Evolution summary saved to: {summary_csv}")


if __name__ == "__main__":
    main()
