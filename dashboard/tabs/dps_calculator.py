"""
DPS 계산기 탭
"""

from PyQt6.QtWidgets import (
    QWidget, QHBoxLayout, QVBoxLayout, QGroupBox, QLabel,
    QSpinBox, QDoubleSpinBox, QPushButton, QTableWidget, QTableWidgetItem,
    QHeaderView, QFormLayout, QGridLayout, QFrame
)

from ..game_formulas import GameFormulas


class DPSCalculatorTab(QWidget):
    """DPS 계산기: 현재 스탯으로 데미지가 얼마?"""

    def __init__(self, config: dict):
        super().__init__()
        self.config = config
        self._setup_ui()

    def _setup_ui(self):
        layout = QHBoxLayout(self)

        # 좌측: 입력
        left = QGroupBox("스탯 입력")
        left_layout = QFormLayout(left)

        self.base_power = QSpinBox()
        self.base_power.setRange(1, 10000)
        self.base_power.setValue(10)
        left_layout.addRow("기본 공격력:", self.base_power)

        self.base_attack = QSpinBox()
        self.base_attack.setRange(0, 10000)
        self.base_attack.setValue(0)
        left_layout.addRow("가산 공격력:", self.base_attack)

        self.attack_percent = QDoubleSpinBox()
        self.attack_percent.setRange(0, 1000)
        self.attack_percent.setValue(0)
        self.attack_percent.setSuffix("%")
        left_layout.addRow("공격력 배수:", self.attack_percent)

        self.crit_chance = QDoubleSpinBox()
        self.crit_chance.setRange(0, 100)
        self.crit_chance.setValue(0)
        self.crit_chance.setSuffix("%")
        left_layout.addRow("크리티컬 확률+:", self.crit_chance)

        self.crit_damage = QDoubleSpinBox()
        self.crit_damage.setRange(0, 100)
        self.crit_damage.setValue(0)
        self.crit_damage.setDecimals(1)
        left_layout.addRow("크리티컬 배율+:", self.crit_damage)

        self.multi_hit = QDoubleSpinBox()
        self.multi_hit.setRange(0, 100)
        self.multi_hit.setValue(0)
        self.multi_hit.setSuffix("%")
        left_layout.addRow("멀티히트 확률:", self.multi_hit)

        self.combo_stack = QSpinBox()
        self.combo_stack.setRange(0, 3)
        self.combo_stack.setValue(0)
        left_layout.addRow("콤보 스택:", self.combo_stack)

        self.combo_damage = QDoubleSpinBox()
        self.combo_damage.setRange(0, 500)
        self.combo_damage.setValue(0)
        self.combo_damage.setSuffix("%")
        left_layout.addRow("콤보 데미지+:", self.combo_damage)

        self.clicks_per_sec = QDoubleSpinBox()
        self.clicks_per_sec.setRange(1, 20)
        self.clicks_per_sec.setValue(5)
        left_layout.addRow("초당 클릭:", self.clicks_per_sec)

        calc_btn = QPushButton("계산")
        calc_btn.clicked.connect(self._calculate)
        left_layout.addRow(calc_btn)

        layout.addWidget(left, 1)

        # 우측: 결과
        right = QWidget()
        right_layout = QVBoxLayout(right)

        # 결과 카드
        cards = QGridLayout()

        self.min_dmg = self._create_card("최소 데미지", "0")
        cards.addWidget(self.min_dmg, 0, 0)

        self.expected_dmg = self._create_card("기대 데미지", "0")
        cards.addWidget(self.expected_dmg, 0, 1)

        self.max_dmg = self._create_card("최대 데미지", "0")
        cards.addWidget(self.max_dmg, 0, 2)

        self.dps_card = self._create_card("DPS", "0")
        cards.addWidget(self.dps_card, 1, 0, 1, 3)

        right_layout.addLayout(cards)

        # 계산 과정
        self.steps_table = QTableWidget()
        self.steps_table.setColumnCount(3)
        self.steps_table.setHorizontalHeaderLabels(["단계", "계산", "결과"])
        self.steps_table.horizontalHeader().setSectionResizeMode(QHeaderView.ResizeMode.Stretch)
        right_layout.addWidget(self.steps_table)

        # 몬스터 처치 시간
        self.kill_table = QTableWidget()
        self.kill_table.setColumnCount(3)
        self.kill_table.setHorizontalHeaderLabels(["스테이지", "몬스터HP", "처치시간"])
        self.kill_table.horizontalHeader().setSectionResizeMode(QHeaderView.ResizeMode.Stretch)
        right_layout.addWidget(self.kill_table)

        layout.addWidget(right, 2)

    def _create_card(self, title: str, value: str) -> QFrame:
        card = QFrame()
        card.setStyleSheet("QFrame { background-color: #353535; border-radius: 8px; padding: 15px; }")
        layout = QVBoxLayout(card)
        label = QLabel(title)
        label.setStyleSheet("color: #b0b0b0;")
        layout.addWidget(label)
        value_label = QLabel(value)
        value_label.setObjectName("value")
        value_label.setStyleSheet("color: #4a90d9; font-size: 24px; font-weight: bold;")
        layout.addWidget(value_label)
        return card

    def _calculate(self):
        dmg = GameFormulas.calc_damage(
            self.base_power.value(),
            self.base_attack.value(),
            self.attack_percent.value(),
            self.crit_chance.value(),
            self.crit_damage.value(),
            self.multi_hit.value(),
            self.combo_stack.value(),
            self.combo_damage.value()
        )

        dps = dmg['expected'] * self.clicks_per_sec.value()

        # 결과 업데이트
        self.min_dmg.findChild(QLabel, "value").setText(f"{dmg['min']:,}")
        self.expected_dmg.findChild(QLabel, "value").setText(f"{dmg['expected']:,.1f}")
        self.max_dmg.findChild(QLabel, "value").setText(f"{dmg['max']:,}")
        self.dps_card.findChild(QLabel, "value").setText(f"{dps:,.0f}/sec")

        # 계산 과정
        steps = [
            ("기본", f"{self.base_power.value()}", f"{self.base_power.value()}"),
            ("가산", f"+ {self.base_attack.value()}", f"{dmg['raw']}"),
            ("배수", f"× {1 + self.attack_percent.value()/100:.2f}", f"{dmg['after_percent']:.1f}"),
            ("크리티컬 기대값", f"× {dmg['crit_expected']:.2f}", f"{dmg['after_percent'] * dmg['crit_expected']:.1f}"),
            ("멀티히트 기대값", f"× {dmg['multi_expected']:.2f}", f"{dmg['after_percent'] * dmg['crit_expected'] * dmg['multi_expected']:.1f}"),
            ("콤보", f"× {dmg['combo_multi']:.2f}", f"{dmg['expected']:.1f}"),
        ]

        self.steps_table.setRowCount(len(steps))
        for i, (name, calc, result) in enumerate(steps):
            self.steps_table.setItem(i, 0, QTableWidgetItem(name))
            self.steps_table.setItem(i, 1, QTableWidgetItem(calc))
            self.steps_table.setItem(i, 2, QTableWidgetItem(result))

        # 몬스터 처치 시간
        test_stages = [1, 10, 20, 30, 50, 100]
        self.kill_table.setRowCount(len(test_stages))
        for i, stage in enumerate(test_stages):
            hp = GameFormulas.monster_hp(stage)
            time_to_kill = hp / dps if dps > 0 else float('inf')
            self.kill_table.setItem(i, 0, QTableWidgetItem(str(stage)))
            self.kill_table.setItem(i, 1, QTableWidgetItem(f"{hp:,}"))
            self.kill_table.setItem(i, 2, QTableWidgetItem(f"{time_to_kill:.1f}초"))
