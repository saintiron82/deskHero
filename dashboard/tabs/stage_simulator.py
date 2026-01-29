"""
스테이지 시뮬레이터 탭
"""

from PyQt6.QtWidgets import (
    QWidget, QHBoxLayout, QVBoxLayout, QGroupBox, QLabel,
    QSpinBox, QPushButton, QTableWidget, QTableWidgetItem,
    QHeaderView, QFormLayout, QFrame
)
from PyQt6.QtGui import QColor
from matplotlib.backends.backend_qtagg import FigureCanvasQTAgg as FigureCanvas
from matplotlib.figure import Figure

from ..game_formulas import GameFormulas


class StageSimulatorTab(QWidget):
    """스테이지 시뮬레이션: 몇 스테이지까지 갈 수 있는가?"""

    def __init__(self, config: dict):
        super().__init__()
        self.config = config
        self._setup_ui()

    def _setup_ui(self):
        layout = QHBoxLayout(self)

        # 좌측: 입력
        left = QGroupBox("입력")
        left_layout = QFormLayout(left)

        self.target_stage = QSpinBox()
        self.target_stage.setRange(1, 500)
        self.target_stage.setValue(30)
        left_layout.addRow("목표 스테이지:", self.target_stage)

        # 영구 스탯 입력
        left_layout.addRow(QLabel("--- 영구 스탯 (크리스탈) ---"))

        self.perm_stats = {}
        perm_config = self.config.get('permanent', {}).get('stats', {})
        important_stats = ['base_attack', 'attack_percent', 'crit_chance', 'crit_damage',
                          'gold_flat_perm', 'gold_multi_perm', 'start_gold', 'start_keyboard']

        for stat_id in important_stats:
            if stat_id in perm_config:
                stat = perm_config[stat_id]
                spin = QSpinBox()
                spin.setRange(0, 200)
                spin.setValue(0)
                self.perm_stats[stat_id] = spin
                left_layout.addRow(f"{stat.get('name', stat_id)}:", spin)

        self.calc_btn = QPushButton("시뮬레이션")
        self.calc_btn.clicked.connect(self._simulate)
        left_layout.addRow(self.calc_btn)

        layout.addWidget(left, 1)

        # 우측: 결과
        right = QWidget()
        right_layout = QVBoxLayout(right)

        # 결과 카드
        cards = QHBoxLayout()

        self.hp_label = self._create_result_card("몬스터 HP", "0")
        cards.addWidget(self.hp_label)

        self.dps_label = self._create_result_card("필요 DPS", "0")
        cards.addWidget(self.dps_label)

        self.gold_label = self._create_result_card("예상 골드", "0")
        cards.addWidget(self.gold_label)

        self.crystal_label = self._create_result_card("예상 크리스탈", "0")
        cards.addWidget(self.crystal_label)

        right_layout.addLayout(cards)

        # 스테이지별 테이블
        self.stage_table = QTableWidget()
        self.stage_table.setColumnCount(5)
        self.stage_table.setHorizontalHeaderLabels(["스테이지", "몬스터HP", "골드", "누적골드", "보스"])
        self.stage_table.horizontalHeader().setSectionResizeMode(QHeaderView.ResizeMode.Stretch)
        right_layout.addWidget(self.stage_table)

        # 차트
        self.chart = FigureCanvas(Figure(figsize=(8, 3), facecolor='#2b2b2b'))
        self.ax = self.chart.figure.add_subplot(111)
        self._style_chart()
        right_layout.addWidget(self.chart)

        layout.addWidget(right, 2)

    def _create_result_card(self, title: str, value: str) -> QFrame:
        card = QFrame()
        card.setStyleSheet("""
            QFrame {
                background-color: #353535;
                border-radius: 8px;
                padding: 10px;
            }
        """)
        layout = QVBoxLayout(card)
        label = QLabel(title)
        label.setStyleSheet("color: #b0b0b0; font-size: 12px;")
        layout.addWidget(label)

        value_label = QLabel(value)
        value_label.setObjectName("value")
        value_label.setStyleSheet("color: #4a90d9; font-size: 20px; font-weight: bold;")
        layout.addWidget(value_label)

        return card

    def _style_chart(self):
        self.ax.set_facecolor('#2b2b2b')
        self.ax.tick_params(colors='#b0b0b0')
        for spine in self.ax.spines.values():
            spine.set_color('#555555')

    def _simulate(self):
        target = self.target_stage.value()

        # 스탯 값 읽기
        stat_values = {k: v.value() for k, v in self.perm_stats.items()}
        perm_config = self.config.get('permanent', {}).get('stats', {})

        # 효과 계산
        def get_effect(stat_id):
            lv = stat_values.get(stat_id, 0)
            cfg = perm_config.get(stat_id, {})
            return cfg.get('effect_per_level', 1) * lv

        base_attack = get_effect('base_attack')
        attack_percent = get_effect('attack_percent')
        crit_chance = get_effect('crit_chance')
        crit_damage = get_effect('crit_damage')
        gold_flat = get_effect('gold_flat_perm')
        gold_multi = get_effect('gold_multi_perm')
        start_gold = get_effect('start_gold')
        start_keyboard = get_effect('start_keyboard')

        # 목표 스테이지 몬스터 HP
        target_hp = GameFormulas.monster_hp(target)

        # 예상 데미지 (키보드 공격력 = 10 + 시작 보너스 가정)
        base_power = 10 + int(start_keyboard)
        dmg = GameFormulas.calc_damage(
            base_power, int(base_attack), attack_percent,
            crit_chance, crit_damage, 0, 0, 0
        )

        # DPS (초당 5타 가정)
        clicks_per_sec = 5
        dps = dmg['expected'] * clicks_per_sec
        time_to_kill = target_hp / dps if dps > 0 else float('inf')

        # 골드 시뮬레이션
        total_gold = int(start_gold)
        stage_data = []

        for stage in range(1, target + 1):
            gold = GameFormulas.monster_gold(stage, int(gold_flat), gold_multi)
            total_gold += gold
            is_boss = GameFormulas.is_boss(stage)
            stage_data.append({
                'stage': stage,
                'hp': GameFormulas.monster_hp(stage),
                'gold': gold,
                'total': total_gold,
                'boss': is_boss
            })

        # 크리스탈 계산
        stage_crystals = target  # 스테이지 클리어 보상 (1💎 per stage)
        boss_count = target // 10
        boss_crystals = boss_count * 10  # 보스 드롭 (추정 평균)
        crystals = stage_crystals + boss_crystals

        # 결과 업데이트
        self.hp_label.findChild(QLabel, "value").setText(f"{target_hp:,}")
        self.dps_label.findChild(QLabel, "value").setText(f"{dps:,.0f}/s")
        self.gold_label.findChild(QLabel, "value").setText(f"{total_gold:,}")
        self.crystal_label.findChild(QLabel, "value").setText(f"{crystals}")

        # 테이블 업데이트 (10단위만)
        filtered = [d for d in stage_data if d['stage'] % 5 == 0 or d['stage'] == target]
        self.stage_table.setRowCount(len(filtered))
        for i, d in enumerate(filtered):
            self.stage_table.setItem(i, 0, QTableWidgetItem(str(d['stage'])))
            self.stage_table.setItem(i, 1, QTableWidgetItem(f"{d['hp']:,}"))
            self.stage_table.setItem(i, 2, QTableWidgetItem(f"{d['gold']:,}"))
            self.stage_table.setItem(i, 3, QTableWidgetItem(f"{d['total']:,}"))
            self.stage_table.setItem(i, 4, QTableWidgetItem("BOSS" if d['boss'] else ""))

        # 차트
        self.ax.clear()
        self._style_chart()
        stages = [d['stage'] for d in stage_data]
        hps = [d['hp'] for d in stage_data]
        golds = [d['total'] for d in stage_data]

        self.ax.plot(stages, hps, 'r-', label='Monster HP')
        ax2 = self.ax.twinx()
        ax2.plot(stages, golds, 'g--', label='Total Gold')
        ax2.tick_params(colors='#b0b0b0')

        self.ax.set_xlabel('Stage', color='#b0b0b0')
        self.ax.set_ylabel('HP', color='#ff6b6b')
        ax2.set_ylabel('Gold', color='#28a745')
        self.ax.legend(loc='upper left', facecolor='#353535', labelcolor='#e0e0e0')
        ax2.legend(loc='upper right', facecolor='#353535', labelcolor='#e0e0e0')

        self.chart.figure.tight_layout()
        self.chart.draw()
