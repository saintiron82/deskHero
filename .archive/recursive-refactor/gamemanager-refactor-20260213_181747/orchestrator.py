#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Recursive Refactor Orchestrator - 트리 + DFS 기반 재귀적 문제 해결

통합 관리:
- 트리 상태 (task_registry.json)
- 실패 보고소 (failure_report.json)
- 테스트 항목/결과

Commands:
    init "goal"                     초기화
    status                          현재 노드 상태
    decompose NODE "g1" "g2"...     노드 분해
    fast NODE                       빠른 실행 (구현+테스트)
    update NODE --status STATUS     상태 갱신
    next                            다음 노드 (DFS)
    tree                            트리 전체 보기

    set-tests NODE "t1" "t2"...     테스트 항목 정의
    get-tests NODE                  테스트 항목 조회
    test-result NODE IDX pass|fail  테스트 결과 기록

    log-failure NODE                실패 기록
    get-failures [NODE]             실패 히스토리 조회
    get-context NODE                컨텍스트 조회 (실패 포함)
"""

import argparse
import json
import os
import sys
from datetime import datetime
from pathlib import Path
from typing import Optional, List

# Fix Windows console encoding for emojis
if sys.platform == 'win32':
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'strict')
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer, 'strict')

# Constants
STATE_DIR = Path(".agent/recursive-refactor")
REGISTRY_FILE = STATE_DIR / "task_registry.json"
REGISTRY_MD = STATE_DIR / "task_registry.md"
FAILURE_FILE = STATE_DIR / "failure_report.json"

STATUS_EMOJI = {
    "pending": "🔵",
    "decomposed": "🟡",
    "executing": "🟠",
    "fast-track": "⚡",
    "testing": "🔴",
    "passed": "✅",
    "failed": "🟣",
    "escalated": "⚠️"
}

ROLE_MAP = {
    "pending": "Planner",
    "executing": "Executor",
    "fast-track": "Executor",
    "testing": "Tester",
    "failed": "Analyzer",
}

# ============== File Operations ==============

def load_registry() -> dict:
    if not REGISTRY_FILE.exists():
        print("❌ 초기화되지 않음. 먼저 실행:")
        print("   python /mnt/skills/user/recursive-refactor/scripts/orchestrator.py init \"<목표>\"")
        sys.exit(1)
    with open(REGISTRY_FILE, encoding="utf-8") as f:
        return json.load(f)


def save_registry(registry: dict):
    registry["updated_at"] = datetime.now().isoformat()
    with open(REGISTRY_FILE, "w", encoding="utf-8") as f:
        json.dump(registry, f, indent=2, ensure_ascii=False)
    save_registry_md(registry)


def save_registry_md(registry: dict):
    lines = [
        "# Task Registry",
        "",
        f"**목표**: {registry['meta']['goal']}",
        f"**생성**: {registry['created_at'][:10]}",
        f"**갱신**: {registry['updated_at'][:10]}",
        "",
        "---",
        "",
        "## 현재 상태",
        "",
        f"- **노드**: `{registry['current_node']}`",
    ]

    current = registry["nodes"].get(registry["current_node"], {})
    status = current.get("status", "unknown")
    lines.append(f"- **상태**: {STATUS_EMOJI.get(status, '❓')} {status}")

    role = ROLE_MAP.get(status)
    if role:
        lines.append(f"- **역할**: {role}")

    lines.extend(["", "---", "", "## 트리 구조", ""])

    def render_tree(node_id: str, indent: int = 0):
        node = registry["nodes"].get(node_id)
        if not node:
            return
        prefix = "  " * indent + ("├─ " if indent > 0 else "")
        emoji = STATUS_EMOJI.get(node["status"], "❓")
        markers = []
        if node.get("is_leaf"):
            markers.append("[LEAF]")
        if node_id == registry["current_node"]:
            markers.append("◀ CURRENT")
        if node.get("retry_count", 0) > 0:
            markers.append(f"(retry {node['retry_count']})")
        marker_str = " ".join(markers)
        lines.append(f"{prefix}{emoji} **{node_id}**: {node['goal'][:40]}{'...' if len(node['goal']) > 40 else ''} {marker_str}")
        for child_id in node.get("children", []):
            render_tree(child_id, indent + 1)

    render_tree("ROOT")

    # 노드 상세
    lines.extend(["", "---", "", "## 노드 상세", ""])
    for node_id, node in registry["nodes"].items():
        emoji = STATUS_EMOJI.get(node["status"], "❓")
        lines.append(f"### {node_id}")
        lines.append(f"- **목표**: {node['goal']}")
        lines.append(f"- **상태**: {emoji} {node['status']}")
        lines.append(f"- **깊이**: {node['depth']}")
        if node.get("is_leaf"):
            lines.append("- **리프**: Yes")
        if node.get("parent"):
            lines.append(f"- **부모**: {node['parent']}")
        if node.get("children"):
            lines.append(f"- **자식**: {', '.join(node['children'])}")
        if node.get("retry_count", 0) > 0:
            lines.append(f"- **재시도**: {node['retry_count']}")
        if node.get("error"):
            lines.append(f"- **에러**: {node['error'][:80]}")
        if node.get("hint"):
            lines.append(f"- **힌트**: {node['hint']}")
        if node.get("test_criteria"):
            lines.append("- **테스트 항목**:")
            for i, tc in enumerate(node["test_criteria"], 1):
                status_mark = "✅" if tc.get("passed") == True else "❌" if tc.get("passed") == False else "⏳"
                lines.append(f"  {i}. {status_mark} {tc['name']}")
        lines.append("")

    with open(REGISTRY_MD, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


def load_failures() -> dict:
    if not FAILURE_FILE.exists():
        return {"failures": []}
    with open(FAILURE_FILE, encoding="utf-8") as f:
        return json.load(f)


def save_failures(failures: dict):
    with open(FAILURE_FILE, "w", encoding="utf-8") as f:
        json.dump(failures, f, indent=2, ensure_ascii=False)


# ============== Helper Functions ==============

def generate_child_id(parent_id: str, index: int) -> str:
    if parent_id == "ROOT":
        return f"NODE-{index}"
    return f"{parent_id}-{index}"


def find_next_node(registry: dict) -> Optional[str]:
    """DFS로 다음 실행 노드 찾기"""
    def dfs(node_id: str) -> Optional[str]:
        node = registry["nodes"].get(node_id)
        if not node:
            return None
        status = node["status"]
        if status in ["pending", "executing", "fast-track", "testing", "failed"]:
            return node_id
        if status == "decomposed":
            for child_id in node.get("children", []):
                result = dfs(child_id)
                if result:
                    return result
        return None
    return dfs("ROOT")


def check_parent_completion(registry: dict, node_id: str):
    """자식 완료 시 부모 자동 완료 체크"""
    node = registry["nodes"].get(node_id)
    if not node or not node.get("parent"):
        return
    parent_id = node["parent"]
    parent = registry["nodes"].get(parent_id)
    if not parent or parent["status"] != "decomposed":
        return
    all_passed = all(
        registry["nodes"].get(cid, {}).get("status") == "passed"
        for cid in parent.get("children", [])
    )
    if all_passed:
        parent["status"] = "passed"
        print(f"   ✅ 부모 {parent_id} 자동 완료 (모든 자식 passed)")
        check_parent_completion(registry, parent_id)


# ============== Commands ==============

def cmd_init(args):
    import shutil
    STATE_DIR.mkdir(parents=True, exist_ok=True)

    if REGISTRY_FILE.exists() and not args.force:
        print(f"⚠️  이미 초기화됨: {STATE_DIR}")
        print("   --force로 재초기화 가능")
        sys.exit(1)

    # 스크립트 복사
    current_script = Path(__file__).resolve()
    script_dir = current_script.parent
    local_script = STATE_DIR / "orchestrator.py"
    if current_script != local_script:
        shutil.copy2(current_script, local_script)

    # viewer.py도 복사
    viewer_script = script_dir / "viewer.py"
    if viewer_script.exists():
        shutil.copy2(viewer_script, STATE_DIR / "viewer.py")

    registry = {
        "created_at": datetime.now().isoformat(),
        "updated_at": datetime.now().isoformat(),
        "meta": {
            "goal": args.goal,
            "max_depth": args.max_depth,
            "max_retries": args.max_retries
        },
        "current_node": "ROOT",
        "nodes": {
            "ROOT": {
                "id": "ROOT",
                "goal": args.goal,
                "parent": None,
                "children": [],
                "depth": 0,
                "status": "pending",
                "is_leaf": False,
                "retry_count": 0,
                "test_criteria": []
            }
        }
    }

    save_registry(registry)
    save_failures({"failures": []})

    print(f"""
╔══════════════════════════════════════════════════════════════╗
║           🔄 Recursive Refactor 초기화 완료                   ║
╠══════════════════════════════════════════════════════════════╣
║ 목표: {args.goal[:50]:50} ║
╠══════════════════════════════════════════════════════════════╣
║ 다음 단계:                                                    ║
║   python .agent/recursive-refactor/orchestrator.py status    ║
║                                                              ║
║ 📊 GUI Viewer:                                                ║
║   python .agent/recursive-refactor/viewer.py --open          ║
╚══════════════════════════════════════════════════════════════╝

🚀 자율 실행 시작 - ROOT 완료까지 진행하세요.
""")


def cmd_status(args):
    registry = load_registry()
    current_id = registry["current_node"]
    current = registry["nodes"].get(current_id, {})
    status = current.get("status", "unknown")
    role = ROLE_MAP.get(status, "")

    print(f"""
{'='*60}
📍 현재: {current_id}
   상태: {STATUS_EMOJI.get(status, '❓')} {status}
   역할: {role}
   목표: {current.get('goal', 'N/A')}
   깊이: {current.get('depth', 0)}
{'='*60}""")

    if current.get("retry_count", 0) > 0:
        print(f"   재시도: {current['retry_count']}/{registry['meta']['max_retries']}")
    if current.get("error"):
        print(f"   ❌ 에러: {current['error'][:80]}")
    if current.get("hint"):
        print(f"   💡 힌트: {current['hint']}")

    # 테스트 항목 표시
    if current.get("test_criteria"):
        print(f"\n   📋 테스트 항목:")
        for i, tc in enumerate(current["test_criteria"], 1):
            if tc.get("passed") == True:
                mark = "✅"
            elif tc.get("passed") == False:
                mark = "❌"
            else:
                mark = "⏳"
            print(f"      {i}. {mark} {tc['name']}")

    print()

    # 역할별 가이드
    if status == "pending":
        print(f"""🔵 PENDING: 문제 분석 → 테스트 정의 → 실행 방법 결정

   1. 테스트 항목 정의 (성공 기준):
      python .agent/recursive-refactor/orchestrator.py set-tests {current_id} "빌드 성공" "API 응답 200" ...

   2. 실행 방법 선택:
      단순:   python .agent/recursive-refactor/orchestrator.py fast {current_id}
      복잡:   python .agent/recursive-refactor/orchestrator.py update {current_id} --status executing --leaf
      분해:   python .agent/recursive-refactor/orchestrator.py decompose {current_id} "목표1" "목표2" ...

   → 판단 후 즉시 실행. 멈추지 말 것.""")

    elif status == "fast-track":
        print(f"""⚡ FAST TRACK: 구현 + 테스트 한 번에

   1. 목표 구현
   2. 테스트 수행 (정의된 항목 또는 빌드/실행 확인)
   3. 결과:
      성공: python .agent/recursive-refactor/orchestrator.py update {current_id} --status passed
      실패: python .agent/recursive-refactor/orchestrator.py update {current_id} --status failed --error "<메시지>"

   → 구현하고 검증 후 즉시 상태 갱신.""")

    elif status == "executing":
        print(f"""🟠 EXECUTING: 구현 진행

   1. 목표 구현
   2. 완료 후:
      python .agent/recursive-refactor/orchestrator.py update {current_id} --status testing

   → 구현 완료 후 즉시 testing으로 전환.""")

    elif status == "testing":
        print(f"""🔴 TESTING: 테스트 수행

   1. 테스트 항목 확인:
      python .agent/recursive-refactor/orchestrator.py get-tests {current_id}

   2. 각 항목 수행 후 결과 기록:
      python .agent/recursive-refactor/orchestrator.py test-result {current_id} 1 pass
      python .agent/recursive-refactor/orchestrator.py test-result {current_id} 2 fail --reason "에러 내용"

   3. 최종 판정:
      모두 통과: python .agent/recursive-refactor/orchestrator.py update {current_id} --status passed
      실패 있음: python .agent/recursive-refactor/orchestrator.py update {current_id} --status failed

   → 테스트 후 즉시 상태 갱신.""")

    elif status == "failed":
        retry = current.get("retry_count", 0)
        max_retry = registry["meta"]["max_retries"]
        print(f"""🟣 FAILED: 실패 분석 필요

   1. 과거 실패 조회:
      python .agent/recursive-refactor/orchestrator.py get-failures {current_id}

   2. 실패 기록:
      python .agent/recursive-refactor/orchestrator.py log-failure {current_id} --approach "시도한 방법" --error "에러" --reason "원인"

   3. 판단 (재시도 {retry}/{max_retry}):""")
        if retry >= max_retry:
            print(f"""      ⚠️ 최대 재시도 도달 → escalate 권장
      python .agent/recursive-refactor/orchestrator.py update {current_id} --status escalated --reason "사유"
""")
        else:
            print(f"""      재시도: python .agent/recursive-refactor/orchestrator.py update {current_id} --status executing --hint "<수정방법>"
      재분해: python .agent/recursive-refactor/orchestrator.py decompose {current_id} "세부1" "세부2" ...
      포기:   python .agent/recursive-refactor/orchestrator.py update {current_id} --status escalated --reason "사유"

   → 분석 후 즉시 다음 행동.""")

    elif status == "passed":
        print(f"""✅ PASSED: 완료

   → 다음 노드로:
   python .agent/recursive-refactor/orchestrator.py next""")

    elif status == "escalated":
        print(f"""⚠️ ESCALATED: 사용자 개입 필요

   사유: {current.get('escalation_reason', 'N/A')}

   → 사용자에게 보고 후 지시 대기.""")

    elif status == "decomposed":
        print(f"""🟡 DECOMPOSED: 분해됨

   → 첫 번째 자식으로:
   python .agent/recursive-refactor/orchestrator.py next""")


def cmd_decompose(args):
    registry = load_registry()
    node_id = args.node_id
    node = registry["nodes"].get(node_id)

    if not node:
        print(f"❌ 노드 없음: {node_id}")
        sys.exit(1)

    if node["status"] not in ["pending", "failed"]:
        print(f"❌ 분해 불가 상태: {node['status']}")
        sys.exit(1)

    if node["depth"] >= registry["meta"]["max_depth"]:
        print(f"⚠️ 최대 깊이 도달 ({registry['meta']['max_depth']}). 리프로 처리.")
        node["is_leaf"] = True
        node["status"] = "executing"
        save_registry(registry)
        return

    goals = args.goals
    if len(goals) < 2:
        print("❌ 최소 2개 목표 필요")
        sys.exit(1)
    if len(goals) > 5:
        print("⚠️ 5개로 제한")
        goals = goals[:5]

    children_ids = []
    for i, goal in enumerate(goals, 1):
        child_id = generate_child_id(node_id, i)
        registry["nodes"][child_id] = {
            "id": child_id,
            "goal": goal,
            "parent": node_id,
            "children": [],
            "depth": node["depth"] + 1,
            "status": "pending",
            "is_leaf": False,
            "retry_count": 0,
            "test_criteria": []
        }
        children_ids.append(child_id)

    node["children"] = children_ids
    node["status"] = "decomposed"
    node["is_leaf"] = False  # 분해되면 더 이상 리프가 아님
    registry["current_node"] = children_ids[0]

    save_registry(registry)

    print(f"✅ {node_id} 분해 완료:")
    for cid in children_ids:
        print(f"   - {cid}: {registry['nodes'][cid]['goal'][:40]}")
    print(f"\n   현재: {registry['current_node']}")


def cmd_fast(args):
    registry = load_registry()
    node_id = args.node_id
    node = registry["nodes"].get(node_id)

    if not node:
        print(f"❌ 노드 없음: {node_id}")
        sys.exit(1)

    if node["status"] not in ["pending", "failed"]:
        print(f"❌ fast 불가 상태: {node['status']}")
        sys.exit(1)

    node["is_leaf"] = True
    node["fast_track"] = True
    node["status"] = "fast-track"
    save_registry(registry)

    print(f"""
{'='*60}
⚡ FAST TRACK: {node_id}
{'='*60}
목표: {node['goal']}
{'='*60}

→ 지금 바로:
  1. 목표 구현
  2. 검증 (빌드, 실행, 테스트 등)
  3. 결과 보고 후 next로 진행

성공: python .agent/recursive-refactor/orchestrator.py update {node_id} --status passed
실패: python .agent/recursive-refactor/orchestrator.py update {node_id} --status failed --error "<메시지>"
""")


def cmd_update(args):
    registry = load_registry()
    node_id = args.node_id
    node = registry["nodes"].get(node_id)

    if not node:
        print(f"❌ 노드 없음: {node_id}")
        sys.exit(1)

    old_status = node["status"]
    new_status = args.status

    node["status"] = new_status

    if args.leaf:
        node["is_leaf"] = True
    if args.error:
        node["error"] = args.error
        node["retry_count"] = node.get("retry_count", 0) + 1
    if args.hint:
        node["hint"] = args.hint
    if args.reason:
        node["escalation_reason"] = args.reason

    if new_status == "passed":
        check_parent_completion(registry, node_id)

    save_registry(registry)
    print(f"✅ {node_id}: {old_status} → {STATUS_EMOJI.get(new_status, '❓')} {new_status}")

    # Auto-advance: passed/failed 후 자동으로 next
    if args.advance and new_status in ["passed", "failed"]:
        next_node = find_next_node(registry)
        if next_node:
            registry["current_node"] = next_node
            save_registry(registry)
            print(f"   → 자동 이동: {next_node}")


def cmd_next(args):
    registry = load_registry()
    next_node = find_next_node(registry)

    if not next_node:
        root = registry["nodes"].get("ROOT")
        if root and root["status"] == "passed":
            print("""
🎉 ════════════════════════════════════════════════
   모든 작업 완료! ROOT passed.
   ════════════════════════════════════════════════

   task_registry.md에서 전체 결과 확인 가능.
""")
        else:
            print("⚠️ 진행 가능한 노드 없음.")
            print("   python .agent/recursive-refactor/orchestrator.py tree")
        return

    registry["current_node"] = next_node
    save_registry(registry)
    print(f"→ 다음 노드: {next_node}\n")
    cmd_status(args)


def cmd_tree(args):
    registry = load_registry()
    print(f"\n🌳 트리: {registry['meta']['goal'][:40]}...")
    print("=" * 60)

    def print_node(node_id: str, indent: int = 0):
        node = registry["nodes"].get(node_id)
        if not node:
            return
        prefix = "  " * indent + ("├─ " if indent > 0 else "")
        emoji = STATUS_EMOJI.get(node["status"], "❓")
        current = " ◀" if node_id == registry["current_node"] else ""
        leaf = " [L]" if node.get("is_leaf") else ""
        print(f"{prefix}{emoji} {node_id}{leaf}{current}: {node['goal'][:35]}")
        for child_id in node.get("children", []):
            print_node(child_id, indent + 1)

    print_node("ROOT")
    print("=" * 60)


def cmd_resume(args):
    """새 세션에서 작업 재개 - tree + status 한 번에"""
    if not REGISTRY_FILE.exists():
        print("❌ 진행 중인 작업 없음.")
        print("   새 작업: python /mnt/skills/user/recursive-refactor/scripts/orchestrator.py init \"<목표>\"")
        return

    registry = load_registry()
    root = registry["nodes"].get("ROOT", {})

    print(f"""
╔══════════════════════════════════════════════════════════════╗
║  🔄 Recursive Refactor 작업 재개                              ║
╠══════════════════════════════════════════════════════════════╣
║  목표: {registry['meta']['goal'][:50]:50} ║
╚══════════════════════════════════════════════════════════════╝
""")

    # 간략 트리
    print("📊 진행 상황:")
    passed = sum(1 for n in registry["nodes"].values() if n["status"] == "passed")
    total = len(registry["nodes"])
    print(f"   완료: {passed}/{total} 노드")

    # 현재 상태
    cmd_status(args)


# ============== Test Commands ==============

def cmd_set_tests(args):
    registry = load_registry()
    node_id = args.node_id
    node = registry["nodes"].get(node_id)

    if not node:
        print(f"❌ 노드 없음: {node_id}")
        sys.exit(1)

    node["test_criteria"] = [
        {"name": t, "passed": None} for t in args.tests
    ]
    save_registry(registry)

    print(f"✅ {node_id} 테스트 항목 설정:")
    for i, t in enumerate(args.tests, 1):
        print(f"   {i}. {t}")


def cmd_get_tests(args):
    registry = load_registry()
    node_id = args.node_id
    node = registry["nodes"].get(node_id)

    if not node:
        print(f"❌ 노드 없음: {node_id}")
        sys.exit(1)

    criteria = node.get("test_criteria", [])
    if not criteria:
        print(f"📋 {node_id}: 테스트 항목 없음")
        print(f"   설정: python .agent/recursive-refactor/orchestrator.py set-tests {node_id} \"항목1\" \"항목2\" ...")
        return

    print(f"📋 {node_id} 테스트 항목:")
    for i, tc in enumerate(criteria, 1):
        if tc.get("passed") == True:
            mark = "✅ 통과"
        elif tc.get("passed") == False:
            mark = f"❌ 실패: {tc.get('reason', '')}"
        else:
            mark = "⏳ 대기"
        print(f"   {i}. [{mark}] {tc['name']}")


def cmd_test_result(args):
    registry = load_registry()
    node_id = args.node_id
    node = registry["nodes"].get(node_id)

    if not node:
        print(f"❌ 노드 없음: {node_id}")
        sys.exit(1)

    criteria = node.get("test_criteria", [])
    idx = args.index - 1

    if idx < 0 or idx >= len(criteria):
        print(f"❌ 잘못된 인덱스: {args.index} (1-{len(criteria)})")
        sys.exit(1)

    criteria[idx]["passed"] = (args.result == "pass")
    if args.reason:
        criteria[idx]["reason"] = args.reason

    save_registry(registry)

    mark = "✅ 통과" if args.result == "pass" else "❌ 실패"
    print(f"✅ {node_id} 테스트 {args.index}: {mark}")


# ============== Failure Commands ==============

def cmd_log_failure(args):
    registry = load_registry()
    failures = load_failures()

    node_id = args.node_id
    node = registry["nodes"].get(node_id)

    if not node:
        print(f"❌ 노드 없음: {node_id}")
        sys.exit(1)

    failure = {
        "node_id": node_id,
        "attempt": node.get("retry_count", 0),
        "approach": args.approach or "N/A",
        "error": args.error or node.get("error", "N/A"),
        "reason": args.reason or "N/A",
        "timestamp": datetime.now().isoformat()
    }

    failures["failures"].append(failure)
    save_failures(failures)

    print(f"📝 실패 기록 완료: {node_id}")
    print(f"   접근법: {failure['approach'][:50]}")
    print(f"   에러: {failure['error'][:50]}")
    print(f"   원인: {failure['reason'][:50]}")


def cmd_get_failures(args):
    failures = load_failures()
    node_id = args.node_id

    if node_id:
        filtered = [f for f in failures["failures"] if f["node_id"] == node_id]
    else:
        filtered = failures["failures"]

    if not filtered:
        print(f"📋 실패 기록 없음" + (f" ({node_id})" if node_id else ""))
        return

    print(f"📋 실패 기록" + (f" ({node_id})" if node_id else "") + ":")
    for i, f in enumerate(filtered, 1):
        print(f"""
   [{i}] {f['node_id']} (시도 {f['attempt']})
       접근법: {f['approach'][:60]}
       에러: {f['error'][:60]}
       원인: {f['reason'][:60]}""")


def cmd_get_context(args):
    registry = load_registry()
    failures = load_failures()
    node_id = args.node_id
    node = registry["nodes"].get(node_id)

    if not node:
        print(f"❌ 노드 없음: {node_id}")
        sys.exit(1)

    print(f"""
{'='*60}
📎 컨텍스트: {node_id}
{'='*60}
목표: {node['goal']}
상태: {STATUS_EMOJI.get(node['status'], '❓')} {node['status']}
깊이: {node['depth']}
재시도: {node.get('retry_count', 0)}/{registry['meta']['max_retries']}
""")

    if node.get("hint"):
        print(f"💡 힌트: {node['hint']}")

    if node.get("error"):
        print(f"❌ 마지막 에러: {node['error']}")

    # 과거 실패
    node_failures = [f for f in failures["failures"] if f["node_id"] == node_id]
    if node_failures:
        print("\n📋 과거 실패:")
        for i, f in enumerate(node_failures, 1):
            print(f"   [{i}] {f['approach'][:40]} → {f['error'][:30]}")

    # 테스트 항목
    if node.get("test_criteria"):
        print("\n📋 테스트 항목:")
        for i, tc in enumerate(node["test_criteria"], 1):
            mark = "✅" if tc.get("passed") == True else "❌" if tc.get("passed") == False else "⏳"
            print(f"   {i}. {mark} {tc['name']}")


# ============== Main ==============

def main():
    parser = argparse.ArgumentParser(description="Recursive Refactor Orchestrator")
    subparsers = parser.add_subparsers(dest="command", required=True)

    # init
    p = subparsers.add_parser("init", help="초기화")
    p.add_argument("goal", help="최상위 목표")
    p.add_argument("--max-depth", type=int, default=5, help="최대 깊이")
    p.add_argument("--max-retries", type=int, default=3, help="최대 재시도")
    p.add_argument("--force", action="store_true", help="강제 재초기화")

    # status
    subparsers.add_parser("status", help="현재 상태")

    # resume
    subparsers.add_parser("resume", help="작업 재개 (새 세션용)")

    # decompose
    p = subparsers.add_parser("decompose", help="노드 분해")
    p.add_argument("node_id", help="노드 ID")
    p.add_argument("goals", nargs="+", help="자식 목표들 (2-5개)")

    # fast
    p = subparsers.add_parser("fast", help="빠른 실행")
    p.add_argument("node_id", help="노드 ID")

    # update
    p = subparsers.add_parser("update", help="상태 갱신")
    p.add_argument("node_id", help="노드 ID")
    p.add_argument("--status", required=True,
                   choices=["pending", "executing", "testing", "passed", "failed", "escalated"])
    p.add_argument("--leaf", action="store_true", help="리프 노드로 표시")
    p.add_argument("--error", help="에러 메시지")
    p.add_argument("--hint", help="재시도 힌트")
    p.add_argument("--reason", help="escalate 사유")
    p.add_argument("--advance", "-a", action="store_true", help="passed/failed 후 자동으로 next")

    # next
    subparsers.add_parser("next", help="다음 노드")

    # tree
    subparsers.add_parser("tree", help="트리 전체")

    # set-tests
    p = subparsers.add_parser("set-tests", help="테스트 항목 설정")
    p.add_argument("node_id", help="노드 ID")
    p.add_argument("tests", nargs="+", help="테스트 항목들")

    # get-tests
    p = subparsers.add_parser("get-tests", help="테스트 항목 조회")
    p.add_argument("node_id", help="노드 ID")

    # test-result
    p = subparsers.add_parser("test-result", help="테스트 결과 기록")
    p.add_argument("node_id", help="노드 ID")
    p.add_argument("index", type=int, help="테스트 인덱스 (1부터)")
    p.add_argument("result", choices=["pass", "fail"], help="결과")
    p.add_argument("--reason", help="실패 사유")

    # log-failure
    p = subparsers.add_parser("log-failure", help="실패 기록")
    p.add_argument("node_id", help="노드 ID")
    p.add_argument("--approach", help="시도한 접근법")
    p.add_argument("--error", help="에러 메시지")
    p.add_argument("--reason", help="실패 원인")

    # get-failures
    p = subparsers.add_parser("get-failures", help="실패 기록 조회")
    p.add_argument("node_id", nargs="?", help="노드 ID (없으면 전체)")

    # get-context
    p = subparsers.add_parser("get-context", help="노드 컨텍스트 조회")
    p.add_argument("node_id", help="노드 ID")

    args = parser.parse_args()

    cmd_map = {
        "init": cmd_init,
        "status": cmd_status,
        "resume": cmd_resume,
        "decompose": cmd_decompose,
        "fast": cmd_fast,
        "update": cmd_update,
        "next": cmd_next,
        "tree": cmd_tree,
        "set-tests": cmd_set_tests,
        "get-tests": cmd_get_tests,
        "test-result": cmd_test_result,
        "log-failure": cmd_log_failure,
        "get-failures": cmd_get_failures,
        "get-context": cmd_get_context,
    }

    cmd_map[args.command](args)


if __name__ == "__main__":
    main()
