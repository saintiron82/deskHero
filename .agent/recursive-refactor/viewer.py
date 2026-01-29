#!/usr/bin/env python3
"""
Recursive Refactor Viewer - 트리 상태를 HTML로 시각화

Usage:
    python .agent/recursive-refactor/viewer.py          # HTML 생성 후 경로 출력
    python .agent/recursive-refactor/viewer.py --open   # 생성 후 브라우저 열기
    python .agent/recursive-refactor/viewer.py --watch  # 파일 변경 감지 후 자동 갱신
"""

import argparse
import json
import os
import webbrowser
from pathlib import Path
from datetime import datetime

STATE_DIR = Path(".agent/recursive-refactor")
REGISTRY_FILE = STATE_DIR / "task_registry.json"
FAILURE_FILE = STATE_DIR / "failure_report.json"
OUTPUT_HTML = STATE_DIR / "viewer.html"

HTML_TEMPLATE = '''<!DOCTYPE html>
<html lang="ko">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Recursive Refactor - {goal}</title>
    <style>
        :root {{
            --bg: #1a1a2e;
            --card: #16213e;
            --accent: #0f3460;
            --text: #eee;
            --muted: #888;
            --passed: #00d26a;
            --failed: #ff6b6b;
            --pending: #4dabf7;
            --executing: #ffa94d;
            --testing: #ff8787;
            --decomposed: #ffd43b;
            --escalated: #da77f2;
        }}
        * {{ box-sizing: border-box; margin: 0; padding: 0; }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: var(--bg);
            color: var(--text);
            padding: 20px;
            min-height: 100vh;
        }}
        .container {{ max-width: 1200px; margin: 0 auto; }}
        header {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 30px;
            padding-bottom: 20px;
            border-bottom: 1px solid var(--accent);
        }}
        h1 {{ font-size: 1.5rem; font-weight: 600; }}
        .meta {{ color: var(--muted); font-size: 0.85rem; }}
        .progress-bar {{
            background: var(--accent);
            border-radius: 10px;
            height: 8px;
            margin: 20px 0;
            overflow: hidden;
        }}
        .progress-fill {{
            background: linear-gradient(90deg, var(--passed), #40c057);
            height: 100%;
            transition: width 0.3s;
        }}
        .stats {{
            display: flex;
            gap: 20px;
            margin-bottom: 30px;
        }}
        .stat {{
            background: var(--card);
            padding: 15px 20px;
            border-radius: 10px;
            flex: 1;
        }}
        .stat-value {{ font-size: 2rem; font-weight: 700; }}
        .stat-label {{ color: var(--muted); font-size: 0.8rem; text-transform: uppercase; }}
        
        .tree-container {{
            background: var(--card);
            border-radius: 12px;
            padding: 20px;
            margin-bottom: 20px;
        }}
        .tree {{ list-style: none; padding-left: 0; }}
        .tree ul {{ list-style: none; padding-left: 30px; border-left: 2px solid var(--accent); margin-left: 10px; }}
        .tree li {{ padding: 8px 0; }}
        .node {{
            display: inline-flex;
            align-items: center;
            gap: 10px;
            padding: 8px 15px;
            background: var(--bg);
            border-radius: 8px;
            cursor: pointer;
            transition: all 0.2s;
            border: 2px solid transparent;
        }}
        .node:hover {{ border-color: var(--accent); transform: translateX(5px); }}
        .node.current {{ border-color: var(--pending); box-shadow: 0 0 15px rgba(77, 171, 247, 0.3); }}
        .node.passed {{ border-left: 4px solid var(--passed); }}
        .node.failed {{ border-left: 4px solid var(--failed); }}
        .node.pending {{ border-left: 4px solid var(--pending); }}
        .node.executing, .node.fast-track {{ border-left: 4px solid var(--executing); }}
        .node.testing {{ border-left: 4px solid var(--testing); }}
        .node.decomposed {{ border-left: 4px solid var(--decomposed); }}
        .node.escalated {{ border-left: 4px solid var(--escalated); }}
        
        .status-badge {{
            font-size: 0.7rem;
            padding: 3px 8px;
            border-radius: 4px;
            text-transform: uppercase;
            font-weight: 600;
        }}
        .status-badge.passed {{ background: var(--passed); color: #000; }}
        .status-badge.failed {{ background: var(--failed); color: #fff; }}
        .status-badge.pending {{ background: var(--pending); color: #000; }}
        .status-badge.executing {{ background: var(--executing); color: #000; }}
        .status-badge.fast-track {{ background: var(--executing); color: #000; }}
        .status-badge.testing {{ background: var(--testing); color: #000; }}
        .status-badge.decomposed {{ background: var(--decomposed); color: #000; }}
        .status-badge.escalated {{ background: var(--escalated); color: #000; }}
        
        .node-id {{ color: var(--muted); font-size: 0.8rem; font-family: monospace; }}
        .node-goal {{ font-weight: 500; }}
        .leaf-badge {{ font-size: 0.65rem; color: var(--muted); }}
        
        .detail-panel {{
            background: var(--card);
            border-radius: 12px;
            padding: 20px;
            display: none;
        }}
        .detail-panel.active {{ display: block; }}
        .detail-header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px; }}
        .detail-title {{ font-size: 1.2rem; font-weight: 600; }}
        .detail-section {{ margin: 15px 0; padding: 15px; background: var(--bg); border-radius: 8px; }}
        .detail-section h4 {{ color: var(--muted); font-size: 0.75rem; text-transform: uppercase; margin-bottom: 10px; }}
        
        .command-box {{
            background: #0d1117;
            border: 1px solid var(--accent);
            border-radius: 6px;
            padding: 12px;
            font-family: 'Fira Code', monospace;
            font-size: 0.85rem;
            margin: 5px 0;
            cursor: pointer;
            transition: all 0.2s;
            position: relative;
        }}
        .command-box:hover {{ background: #161b22; border-color: var(--pending); }}
        .command-box::after {{
            content: '📋 Click to copy';
            position: absolute;
            right: 10px;
            top: 50%;
            transform: translateY(-50%);
            font-size: 0.7rem;
            color: var(--muted);
            opacity: 0;
            transition: opacity 0.2s;
        }}
        .command-box:hover::after {{ opacity: 1; }}
        .command-box.copied::after {{ content: '✓ Copied!'; color: var(--passed); opacity: 1; }}
        
        .test-list {{ list-style: none; }}
        .test-item {{ padding: 5px 0; display: flex; align-items: center; gap: 8px; }}
        .test-icon {{ font-size: 1rem; }}
        
        .failure-list {{ list-style: none; }}
        .failure-item {{
            background: rgba(255, 107, 107, 0.1);
            border-left: 3px solid var(--failed);
            padding: 10px;
            margin: 8px 0;
            border-radius: 0 6px 6px 0;
        }}
        .failure-approach {{ font-weight: 500; }}
        .failure-error {{ color: var(--failed); font-size: 0.85rem; }}
        .failure-reason {{ color: var(--muted); font-size: 0.8rem; }}
        
        .actions {{ display: flex; gap: 10px; flex-wrap: wrap; margin-top: 15px; }}
        .action-btn {{
            padding: 8px 16px;
            border: none;
            border-radius: 6px;
            cursor: pointer;
            font-size: 0.85rem;
            font-weight: 500;
            transition: all 0.2s;
        }}
        .action-btn.primary {{ background: var(--pending); color: #000; }}
        .action-btn.success {{ background: var(--passed); color: #000; }}
        .action-btn.danger {{ background: var(--failed); color: #fff; }}
        .action-btn:hover {{ transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,0.3); }}
        
        footer {{
            text-align: center;
            color: var(--muted);
            font-size: 0.8rem;
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid var(--accent);
        }}
        
        @media (max-width: 768px) {{
            .stats {{ flex-direction: column; }}
            .tree ul {{ padding-left: 15px; }}
        }}
    </style>
</head>
<body>
    <div class="container">
        <header>
            <div>
                <h1>🔄 Recursive Refactor</h1>
                <div class="meta">{goal}</div>
            </div>
            <div class="meta">Updated: {updated_at}</div>
        </header>
        
        <div class="progress-bar">
            <div class="progress-fill" style="width: {progress}%"></div>
        </div>
        
        <div class="stats">
            <div class="stat">
                <div class="stat-value">{passed_count}</div>
                <div class="stat-label">Passed</div>
            </div>
            <div class="stat">
                <div class="stat-value">{total_count}</div>
                <div class="stat-label">Total Nodes</div>
            </div>
            <div class="stat">
                <div class="stat-value">{failed_count}</div>
                <div class="stat-label">Failed</div>
            </div>
            <div class="stat">
                <div class="stat-value">{current_node}</div>
                <div class="stat-label">Current</div>
            </div>
        </div>
        
        <div class="tree-container">
            <h3 style="margin-bottom: 15px;">📊 Task Tree</h3>
            {tree_html}
        </div>
        
        <div id="detail-panel" class="detail-panel">
            <div class="detail-header">
                <div class="detail-title" id="detail-title">Select a node</div>
                <span class="status-badge" id="detail-status"></span>
            </div>
            <div class="detail-section">
                <h4>Goal</h4>
                <div id="detail-goal"></div>
            </div>
            <div class="detail-section" id="detail-error-section" style="display:none;">
                <h4>Last Error</h4>
                <div id="detail-error" style="color: var(--failed);"></div>
            </div>
            <div class="detail-section" id="detail-hint-section" style="display:none;">
                <h4>Hint</h4>
                <div id="detail-hint" style="color: var(--passed);"></div>
            </div>
            <div class="detail-section" id="detail-tests-section" style="display:none;">
                <h4>Test Criteria</h4>
                <ul class="test-list" id="detail-tests"></ul>
            </div>
            <div class="detail-section" id="detail-failures-section" style="display:none;">
                <h4>Failure History</h4>
                <ul class="failure-list" id="detail-failures"></ul>
            </div>
            <div class="detail-section">
                <h4>Commands</h4>
                <div id="detail-commands"></div>
            </div>
        </div>
        
        <footer>
            Recursive Refactor Viewer • Generated {generated_at}
        </footer>
    </div>
    
    <script>
        const registry = {registry_json};
        const failures = {failures_json};
        
        function selectNode(nodeId) {{
            const node = registry.nodes[nodeId];
            if (!node) return;
            
            document.getElementById('detail-panel').classList.add('active');
            document.getElementById('detail-title').textContent = nodeId;
            document.getElementById('detail-status').textContent = node.status;
            document.getElementById('detail-status').className = 'status-badge ' + node.status;
            document.getElementById('detail-goal').textContent = node.goal;
            
            // Error
            const errorSection = document.getElementById('detail-error-section');
            if (node.error) {{
                errorSection.style.display = 'block';
                document.getElementById('detail-error').textContent = node.error;
            }} else {{
                errorSection.style.display = 'none';
            }}
            
            // Hint
            const hintSection = document.getElementById('detail-hint-section');
            if (node.hint) {{
                hintSection.style.display = 'block';
                document.getElementById('detail-hint').textContent = node.hint;
            }} else {{
                hintSection.style.display = 'none';
            }}
            
            // Tests
            const testsSection = document.getElementById('detail-tests-section');
            const testsList = document.getElementById('detail-tests');
            if (node.test_criteria && node.test_criteria.length > 0) {{
                testsSection.style.display = 'block';
                testsList.innerHTML = node.test_criteria.map((t, i) => {{
                    const icon = t.passed === true ? '✅' : t.passed === false ? '❌' : '⏳';
                    return `<li class="test-item"><span class="test-icon">${{icon}}</span>${{t.name}}</li>`;
                }}).join('');
            }} else {{
                testsSection.style.display = 'none';
            }}
            
            // Failures
            const failuresSection = document.getElementById('detail-failures-section');
            const failuresList = document.getElementById('detail-failures');
            const nodeFailures = failures.failures.filter(f => f.node_id === nodeId);
            if (nodeFailures.length > 0) {{
                failuresSection.style.display = 'block';
                failuresList.innerHTML = nodeFailures.map(f => `
                    <li class="failure-item">
                        <div class="failure-approach">Approach: ${{f.approach}}</div>
                        <div class="failure-error">Error: ${{f.error}}</div>
                        <div class="failure-reason">Reason: ${{f.reason}}</div>
                    </li>
                `).join('');
            }} else {{
                failuresSection.style.display = 'none';
            }}
            
            // Commands
            const commandsDiv = document.getElementById('detail-commands');
            let commands = [];
            
            switch (node.status) {{
                case 'pending':
                    commands = [
                        `python .agent/recursive-refactor/orchestrator.py fast ${{nodeId}}`,
                        `python .agent/recursive-refactor/orchestrator.py decompose ${{nodeId}} "목표1" "목표2"`,
                        `python .agent/recursive-refactor/orchestrator.py set-tests ${{nodeId}} "테스트1" "테스트2"`,
                    ];
                    break;
                case 'executing':
                case 'fast-track':
                    commands = [
                        `python .agent/recursive-refactor/orchestrator.py update ${{nodeId}} --status testing`,
                        `python .agent/recursive-refactor/orchestrator.py update ${{nodeId}} --status passed -a`,
                        `python .agent/recursive-refactor/orchestrator.py update ${{nodeId}} --status failed --error "에러" -a`,
                    ];
                    break;
                case 'testing':
                    commands = [
                        `python .agent/recursive-refactor/orchestrator.py test-result ${{nodeId}} 1 pass`,
                        `python .agent/recursive-refactor/orchestrator.py update ${{nodeId}} --status passed -a`,
                        `python .agent/recursive-refactor/orchestrator.py update ${{nodeId}} --status failed -a`,
                    ];
                    break;
                case 'failed':
                    commands = [
                        `python .agent/recursive-refactor/orchestrator.py get-failures ${{nodeId}}`,
                        `python .agent/recursive-refactor/orchestrator.py update ${{nodeId}} --status executing --hint "힌트"`,
                        `python .agent/recursive-refactor/orchestrator.py decompose ${{nodeId}} "세부1" "세부2"`,
                        `python .agent/recursive-refactor/orchestrator.py update ${{nodeId}} --status escalated --reason "사유"`,
                    ];
                    break;
                case 'passed':
                    commands = [`python .agent/recursive-refactor/orchestrator.py next`];
                    break;
                default:
                    commands = [`python .agent/recursive-refactor/orchestrator.py status`];
            }}
            
            commandsDiv.innerHTML = commands.map(cmd => 
                `<div class="command-box" onclick="copyCommand(this, '${{cmd}}')">${{cmd}}</div>`
            ).join('');
        }}
        
        function copyCommand(el, cmd) {{
            navigator.clipboard.writeText(cmd).then(() => {{
                el.classList.add('copied');
                setTimeout(() => el.classList.remove('copied'), 1500);
            }});
        }}
        
        // Auto-select current node
        document.addEventListener('DOMContentLoaded', () => {{
            selectNode(registry.current_node);
        }});
    </script>
</body>
</html>
'''


def load_registry():
    if not REGISTRY_FILE.exists():
        return None
    with open(REGISTRY_FILE, encoding="utf-8") as f:
        return json.load(f)


def load_failures():
    if not FAILURE_FILE.exists():
        return {"failures": []}
    with open(FAILURE_FILE, encoding="utf-8") as f:
        return json.load(f)


def render_tree_html(registry, node_id="ROOT", depth=0):
    node = registry["nodes"].get(node_id)
    if not node:
        return ""
    
    is_current = node_id == registry["current_node"]
    status = node["status"]
    
    classes = [status]
    if is_current:
        classes.append("current")
    
    leaf_badge = '<span class="leaf-badge">[LEAF]</span>' if node.get("is_leaf") else ""
    current_marker = " ◀" if is_current else ""
    
    html = f'''
    <li>
        <div class="node {" ".join(classes)}" onclick="selectNode('{node_id}')">
            <span class="node-id">{node_id}</span>
            <span class="node-goal">{node["goal"][:40]}{"..." if len(node["goal"]) > 40 else ""}</span>
            <span class="status-badge {status}">{status}</span>
            {leaf_badge}
            {current_marker}
        </div>
    '''
    
    if node.get("children"):
        html += "<ul>"
        for child_id in node["children"]:
            html += render_tree_html(registry, child_id, depth + 1)
        html += "</ul>"
    
    html += "</li>"
    return html


def generate_html(registry, failures):
    nodes = registry["nodes"]
    passed_count = sum(1 for n in nodes.values() if n["status"] == "passed")
    failed_count = sum(1 for n in nodes.values() if n["status"] == "failed")
    total_count = len(nodes)
    progress = int((passed_count / total_count) * 100) if total_count > 0 else 0
    
    tree_html = f'<ul class="tree">{render_tree_html(registry)}</ul>'
    
    html = HTML_TEMPLATE.format(
        goal=registry["meta"]["goal"],
        updated_at=registry.get("updated_at", "")[:19].replace("T", " "),
        progress=progress,
        passed_count=passed_count,
        failed_count=failed_count,
        total_count=total_count,
        current_node=registry["current_node"],
        tree_html=tree_html,
        registry_json=json.dumps(registry, ensure_ascii=False),
        failures_json=json.dumps(failures, ensure_ascii=False),
        generated_at=datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    )
    
    return html


def main():
    parser = argparse.ArgumentParser(description="Recursive Refactor Viewer")
    parser.add_argument("--open", "-o", action="store_true", help="Open in browser")
    parser.add_argument("--watch", "-w", action="store_true", help="Watch for changes")
    args = parser.parse_args()
    
    registry = load_registry()
    if not registry:
        print("❌ 진행 중인 작업 없음")
        print("   먼저 init 실행: python /mnt/skills/user/recursive-refactor/scripts/orchestrator.py init \"<목표>\"")
        return
    
    failures = load_failures()
    html = generate_html(registry, failures)
    
    OUTPUT_HTML.write_text(html, encoding="utf-8")
    output_path = OUTPUT_HTML.resolve()
    
    print(f"""
╔══════════════════════════════════════════════════════════════╗
║  📊 Recursive Refactor Viewer                                ║
╠══════════════════════════════════════════════════════════════╣
║  HTML 생성 완료: {str(output_path)[:45]:45} ║
╚══════════════════════════════════════════════════════════════╝

🌐 브라우저에서 열기:
   file://{output_path}

또는:
   python .agent/recursive-refactor/viewer.py --open
""")
    
    if args.open:
        webbrowser.open(f"file://{output_path}")
        print("✅ 브라우저에서 열림")


if __name__ == "__main__":
    main()
