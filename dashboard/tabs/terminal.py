"""
내장 터미널 탭
"""

from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel,
    QPushButton, QPlainTextEdit, QLineEdit, QApplication
)
from PyQt6.QtCore import Qt, QProcess


class TerminalTab(QWidget):
    """내장 터미널 - 명령어 실행 및 AI 에이전트 호출"""

    def __init__(self, config: dict):
        super().__init__()
        self.config = config
        self.process = None
        self.command_history = []
        self.history_index = -1
        self._setup_ui()

    def _setup_ui(self):
        layout = QVBoxLayout(self)

        # 상단 버튼들
        btn_layout = QHBoxLayout()

        # 빠른 명령 버튼들
        claude_btn = QPushButton("Claude Code 실행")
        claude_btn.clicked.connect(lambda: self._run_command("claude"))
        btn_layout.addWidget(claude_btn)

        balance_btn = QPushButton("밸런스 분석 요청")
        balance_btn.clicked.connect(self._request_balance_analysis)
        btn_layout.addWidget(balance_btn)

        clear_btn = QPushButton("화면 지우기")
        clear_btn.clicked.connect(self._clear_output)
        btn_layout.addWidget(clear_btn)

        kill_btn = QPushButton("프로세스 종료")
        kill_btn.clicked.connect(self._kill_process)
        kill_btn.setStyleSheet("background-color: #d94a4a;")
        btn_layout.addWidget(kill_btn)

        btn_layout.addStretch()
        layout.addLayout(btn_layout)

        # 출력 영역
        self.output = QPlainTextEdit()
        self.output.setReadOnly(True)
        self.output.setStyleSheet("""
            QPlainTextEdit {
                background-color: #1a1a1a;
                color: #00ff00;
                font-family: 'Consolas', 'Courier New', monospace;
                font-size: 12px;
                border: 1px solid #333;
            }
        """)
        self.output.setPlainText("DeskWarrior Balance Dashboard Terminal\n" + "=" * 50 + "\n\n명령어를 입력하세요. (예: dir, python --version, claude)\n\n")
        layout.addWidget(self.output)

        # 입력 영역
        input_layout = QHBoxLayout()

        self.prompt_label = QLabel(">")
        self.prompt_label.setStyleSheet("color: #00ff00; font-size: 14px; font-weight: bold;")
        input_layout.addWidget(self.prompt_label)

        self.input = QLineEdit()
        self.input.setStyleSheet("""
            QLineEdit {
                background-color: #1a1a1a;
                color: #ffffff;
                font-family: 'Consolas', 'Courier New', monospace;
                font-size: 12px;
                border: 1px solid #333;
                padding: 8px;
            }
        """)
        self.input.setPlaceholderText("명령어 입력... (Enter로 실행, 히스토리)")
        self.input.returnPressed.connect(self._execute_command)
        input_layout.addWidget(self.input)

        layout.addLayout(input_layout)

    def keyPressEvent(self, event):
        # 히스토리 탐색
        if event.key() == Qt.Key.Key_Up:
            if self.command_history and self.history_index < len(self.command_history) - 1:
                self.history_index += 1
                self.input.setText(self.command_history[-(self.history_index + 1)])
        elif event.key() == Qt.Key.Key_Down:
            if self.history_index > 0:
                self.history_index -= 1
                self.input.setText(self.command_history[-(self.history_index + 1)])
            elif self.history_index == 0:
                self.history_index = -1
                self.input.clear()
        else:
            super().keyPressEvent(event)

    def _execute_command(self):
        cmd = self.input.text().strip()
        if not cmd:
            return

        self.command_history.append(cmd)
        self.history_index = -1
        self.input.clear()

        self._append_output(f"\n> {cmd}\n")
        self._run_command(cmd)

    def _run_command(self, cmd: str):
        if self.process and self.process.state() != QProcess.ProcessState.NotRunning:
            self._append_output("[경고] 이미 실행 중인 프로세스가 있습니다.\n")
            return

        self.process = QProcess(self)
        self.process.readyReadStandardOutput.connect(self._read_stdout)
        self.process.readyReadStandardError.connect(self._read_stderr)
        self.process.finished.connect(self._process_finished)

        # Windows cmd를 통해 실행
        self.process.start("cmd.exe", ["/c", cmd])

    def _read_stdout(self):
        if self.process:
            data = self.process.readAllStandardOutput()
            text = bytes(data).decode('utf-8', errors='replace')
            self._append_output(text)

    def _read_stderr(self):
        if self.process:
            data = self.process.readAllStandardError()
            text = bytes(data).decode('utf-8', errors='replace')
            self._append_output(text, error=True)

    def _process_finished(self, exit_code, exit_status):
        self._append_output(f"\n[프로세스 종료: 코드 {exit_code}]\n")

    def _append_output(self, text: str, error: bool = False):
        cursor = self.output.textCursor()
        cursor.movePosition(cursor.MoveOperation.End)
        self.output.setTextCursor(cursor)
        self.output.insertPlainText(text)
        self.output.verticalScrollBar().setValue(self.output.verticalScrollBar().maximum())

    def _clear_output(self):
        self.output.clear()

    def _kill_process(self):
        if self.process and self.process.state() != QProcess.ProcessState.NotRunning:
            self.process.kill()
            self._append_output("\n[프로세스 강제 종료됨]\n")

    def _request_balance_analysis(self):
        """현재 설정을 기반으로 밸런스 분석 요청 텍스트 생성"""
        analysis_prompt = self._generate_analysis_prompt()

        # 클립보드에 복사
        clipboard = QApplication.clipboard()
        clipboard.setText(analysis_prompt)

        self._append_output("\n" + "=" * 50 + "\n")
        self._append_output("[밸런스 분석 요청이 클립보드에 복사되었습니다]\n")
        self._append_output("Claude Code에 붙여넣기 하세요.\n")
        self._append_output("=" * 50 + "\n\n")
        self._append_output(analysis_prompt[:500] + "...\n")

    def _generate_analysis_prompt(self) -> str:
        """분석 요청 프롬프트 생성"""
        prompt = "# 밸런스 분석 요청\n\n"
        prompt += "현재 DeskWarrior 게임의 밸런스를 분석해주세요.\n\n"
        prompt += "## 현재 설정값\n\n"

        # 영구 스탯
        prompt += "### 영구 업그레이드 (PermanentStats.json)\n```json\n"
        perm_stats = self.config.get('permanent', {}).get('stats', {})
        for sid, stat in perm_stats.items():
            prompt += f"{sid}: base_cost={stat.get('base_cost')}, growth_rate={stat.get('growth_rate')}, "
            prompt += f"multiplier={stat.get('multiplier')}, softcap={stat.get('softcap_interval')}, "
            prompt += f"effect={stat.get('effect_per_level')}\n"
        prompt += "```\n\n"

        prompt += "### 분석 요청사항\n"
        prompt += "1. 필요 CPS 기반 난이도 분석 (Lv1~50)\n"
        prompt += "2. 골드 이코노미 분석\n"
        prompt += "3. 밸런스 문제점 및 개선 제안\n"

        return prompt
