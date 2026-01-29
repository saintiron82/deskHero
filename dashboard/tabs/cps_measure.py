"""
CPS 측정기 탭
"""

import time

from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QGroupBox, QLabel,
    QSpinBox, QPushButton
)
from PyQt6.QtCore import Qt, QTimer


class CpsMeasureTab(QWidget):
    """CPS 측정기: 실제 입력 속도 측정"""

    def __init__(self, config: dict):
        super().__init__()
        self.config = config
        self.is_measuring = False
        self.input_count = 0
        self.start_time = None
        self.measure_duration = 10  # 측정 시간 (초)
        self._setup_ui()

    def _setup_ui(self):
        layout = QVBoxLayout(self)

        # 설명
        desc = QLabel("실제 입력 속도(CPS)를 측정합니다.\n시작 버튼을 누른 후 키보드나 마우스를 빠르게 입력하세요.")
        desc.setStyleSheet("font-size: 14px; padding: 10px;")
        desc.setAlignment(Qt.AlignmentFlag.AlignCenter)
        layout.addWidget(desc)

        # 측정 시간 설정
        time_layout = QHBoxLayout()
        time_layout.addStretch()
        time_layout.addWidget(QLabel("측정 시간:"))
        self.duration_spin = QSpinBox()
        self.duration_spin.setRange(5, 30)
        self.duration_spin.setValue(10)
        self.duration_spin.setSuffix(" 초")
        time_layout.addWidget(self.duration_spin)
        time_layout.addStretch()
        layout.addLayout(time_layout)

        # 큰 카운터 표시
        self.counter_label = QLabel("0")
        self.counter_label.setStyleSheet("""
            font-size: 120px;
            font-weight: bold;
            color: #4a90d9;
            padding: 20px;
        """)
        self.counter_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        layout.addWidget(self.counter_label)

        # 상태 표시
        self.status_label = QLabel("대기 중...")
        self.status_label.setStyleSheet("font-size: 18px; color: #888;")
        self.status_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        layout.addWidget(self.status_label)

        # 시작/중지 버튼
        self.start_btn = QPushButton("측정 시작 (Space 또는 클릭)")
        self.start_btn.setStyleSheet("""
            QPushButton {
                font-size: 20px;
                padding: 15px 40px;
                background-color: #4a90d9;
                border-radius: 8px;
            }
            QPushButton:hover {
                background-color: #5aa0e9;
            }
        """)
        self.start_btn.clicked.connect(self._toggle_measure)
        layout.addWidget(self.start_btn, alignment=Qt.AlignmentFlag.AlignCenter)

        # 결과 영역
        self.result_group = QGroupBox("측정 결과")
        result_layout = QVBoxLayout(self.result_group)

        self.cps_result = QLabel("평균 CPS: -")
        self.cps_result.setStyleSheet("font-size: 24px; font-weight: bold;")
        result_layout.addWidget(self.cps_result)

        self.grade_result = QLabel("등급: -")
        self.grade_result.setStyleSheet("font-size: 18px;")
        result_layout.addWidget(self.grade_result)

        # 밸런스 기준 참고
        ref_label = QLabel("""
<b>밸런스 판정 기준:</b><br>
필요 CPS < 5: 여유<br>
필요 CPS 5~8: 도전적<br>
필요 CPS 8~12: 어려움<br>
필요 CPS 12~15: 극한<br>
필요 CPS > 15: <b>입력 한계 초과</b>
        """)
        ref_label.setStyleSheet("font-size: 12px; color: #aaa; padding: 10px;")
        result_layout.addWidget(ref_label)

        layout.addWidget(self.result_group)
        layout.addStretch()

        # 타이머
        self.timer = QTimer()
        self.timer.timeout.connect(self._update_timer)

        # 키보드/마우스 이벤트 캡처
        self.setFocusPolicy(Qt.FocusPolicy.StrongFocus)

    def _toggle_measure(self):
        if self.is_measuring:
            self._stop_measure()
        else:
            self._start_measure()

    def _start_measure(self):
        self.is_measuring = True
        self.input_count = 0
        self.measure_duration = self.duration_spin.value()
        self.start_time = None  # 첫 입력 시 시작

        self.counter_label.setText("0")
        self.counter_label.setStyleSheet("""
            font-size: 120px;
            font-weight: bold;
            color: #4a90d9;
            padding: 20px;
        """)
        self.status_label.setText(f"입력을 시작하세요! ({self.measure_duration}초)")
        self.status_label.setStyleSheet("font-size: 18px; color: #4a90d9;")
        self.start_btn.setText("측정 중지")
        self.start_btn.setStyleSheet("""
            QPushButton {
                font-size: 20px;
                padding: 15px 40px;
                background-color: #d94a4a;
                border-radius: 8px;
            }
        """)
        self.cps_result.setText("측정 중...")
        self.grade_result.setText("")

        self.setFocus()

    def _stop_measure(self):
        self.is_measuring = False
        self.timer.stop()

        # 결과 계산
        if self.start_time and self.input_count > 0:
            elapsed = time.time() - self.start_time
            if elapsed > 0:
                cps = self.input_count / elapsed
                self._show_result(cps, elapsed)
            else:
                self._show_result(0, 0)
        else:
            self._show_result(0, 0)

        self.start_btn.setText("측정 시작 (Space 또는 클릭)")
        self.start_btn.setStyleSheet("""
            QPushButton {
                font-size: 20px;
                padding: 15px 40px;
                background-color: #4a90d9;
                border-radius: 8px;
            }
        """)
        self.status_label.setText("측정 완료!")
        self.status_label.setStyleSheet("font-size: 18px; color: #4ad94a;")

    def _update_timer(self):
        if self.start_time:
            elapsed = time.time() - self.start_time
            remaining = self.measure_duration - elapsed
            if remaining <= 0:
                self._stop_measure()
            else:
                self.status_label.setText(f"남은 시간: {remaining:.1f}초")

    def _register_input(self):
        if not self.is_measuring:
            return

        # 첫 입력 시 타이머 시작
        if self.start_time is None:
            self.start_time = time.time()
            self.timer.start(100)  # 0.1초마다 업데이트

        self.input_count += 1
        self.counter_label.setText(str(self.input_count))

    def _show_result(self, cps: float, elapsed: float):
        if cps <= 0:
            self.cps_result.setText("평균 CPS: 측정 실패")
            self.grade_result.setText("")
            return

        self.cps_result.setText(f"평균 CPS: {cps:.2f} ({self.input_count}회 / {elapsed:.1f}초)")

        # 등급 판정 (입력 한계: 15 CPS)
        if cps >= 15:
            grade = "초인 (CPS 15+ 입력한계)"
            color = "#ff4444"
        elif cps >= 12:
            grade = "프로 (CPS 12~15)"
            color = "#ffd700"
        elif cps >= 8:
            grade = "숙련자 (CPS 8~12)"
            color = "#4ad94a"
        elif cps >= 5:
            grade = "일반 (CPS 5~8)"
            color = "#4a90d9"
        elif cps >= 3:
            grade = "캐주얼 (CPS 3~5)"
            color = "#888"
        else:
            grade = "느림 (CPS < 3)"
            color = "#d94a4a"

        self.grade_result.setText(f"등급: {grade}")
        self.grade_result.setStyleSheet(f"font-size: 18px; color: {color};")
        self.counter_label.setStyleSheet(f"""
            font-size: 120px;
            font-weight: bold;
            color: {color};
            padding: 20px;
        """)

    def keyPressEvent(self, event):
        if self.is_measuring:
            self._register_input()
        elif event.key() == Qt.Key.Key_Space:
            self._toggle_measure()

    def mousePressEvent(self, event):
        # 버튼 클릭은 제외
        if self.is_measuring and not self.start_btn.underMouse():
            self._register_input()
