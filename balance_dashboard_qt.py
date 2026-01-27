"""
DeskWarrior Balance Dashboard - PyQt6 Version
실제 게임 시뮬레이션 기반 밸런스 도구
"""

import json
import math
import os
import sys
from typing import Dict

from PyQt6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QTabWidget, QGroupBox, QLabel, QSpinBox, QDoubleSpinBox,
    QPushButton, QTableWidget, QTableWidgetItem, QHeaderView,
    QMessageBox, QFormLayout, QGridLayout, QScrollArea, QFrame,
    QInputDialog, QComboBox, QPlainTextEdit, QLineEdit, QDockWidget,
    QSplitter
)
from PyQt6.QtCore import Qt, QProcess, QSettings, QByteArray
from PyQt6.QtGui import QFont, QColor

import matplotlib
matplotlib.use('QtAgg')
from matplotlib.backends.backend_qtagg import FigureCanvasQTAgg as FigureCanvas
from matplotlib.figure import Figure
import matplotlib.font_manager as fm

# 생성된 공식 모듈 import (Single Source of Truth)
def _get_tools_dir():
    """tools 디렉토리 경로 (exe/Python 모두 지원)"""
    if hasattr(sys, '_MEIPASS'):
        # PyInstaller exe 실행 시
        return os.path.join(sys._MEIPASS, 'tools')
    else:
        # Python 직접 실행 시
        return os.path.join(os.path.dirname(os.path.abspath(__file__)), 'tools')

sys.path.insert(0, _get_tools_dir())
import stat_formulas_generated as SF

# 한글 폰트 설정 (Windows: Malgun Gothic)
plt_font_path = None
for font in fm.fontManager.ttflist:
    if 'Malgun' in font.name or 'malgun' in font.fname.lower():
        plt_font_path = font.fname
        break

if plt_font_path:
    matplotlib.rcParams['font.family'] = fm.FontProperties(fname=plt_font_path).get_name()
else:
    # 폴백: 시스템 기본 sans-serif
    matplotlib.rcParams['font.family'] = 'sans-serif'
    matplotlib.rcParams['font.sans-serif'] = ['Malgun Gothic', 'NanumGothic', 'Arial Unicode MS', 'DejaVu Sans']

matplotlib.rcParams['axes.unicode_minus'] = False  # 마이너스 기호 깨짐 방지


# ============================================================
# 설정 로드
# ============================================================

def get_config_dir() -> str:
    if hasattr(sys, '_MEIPASS'):
        # 패키징된 exe 실행 시: exe가 dist/ 폴더에 있으므로 상위 폴더의 config 사용
        exe_dir = os.path.dirname(sys.executable)
        # dist/BalanceDashboard.exe -> dist/../config = config
        config_dir = os.path.join(os.path.dirname(exe_dir), 'config')
        if os.path.exists(config_dir):
            return config_dir
        # 폴백: exe 옆의 config 폴더
        return os.path.join(exe_dir, 'config')
    return os.path.join(os.path.dirname(os.path.abspath(__file__)), 'config')


def load_json(filename: str) -> dict:
    filepath = os.path.join(get_config_dir(), filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        return json.load(f)


def save_json(filename: str, data: dict):
    filepath = os.path.join(get_config_dir(), filename)
    if os.path.exists(filepath):
        with open(filepath, 'r', encoding='utf-8') as f:
            backup = f.read()
        with open(filepath + '.backup', 'w', encoding='utf-8') as f:
            f.write(backup)
    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)


# ============================================================
# 게임 공식 (stat_formulas_generated.py 래핑)
# ============================================================

class GameFormulas:
    """
    게임 공식 계산 - stat_formulas_generated.py 사용

    주의: 이 클래스는 자동 생성된 공식 모듈(SF)을 래핑합니다.
    공식 변경 시 config/StatFormulas.json 수정 후 코드 생성기 실행.
    """

    # 상수는 생성된 모듈에서 가져옴 (Single Source of Truth)
    BASE_HP = SF.BASE_HP
    HP_GROWTH = SF.HP_GROWTH
    BOSS_INTERVAL = SF.BOSS_INTERVAL
    BOSS_HP_MULTI = SF.BOSS_HP_MULTI
    BASE_GOLD_MULTI = SF.BASE_GOLD_MULTI
    TIME_LIMIT = SF.BASE_TIME_LIMIT
    BASE_CRIT_CHANCE = SF.BASE_CRIT_CHANCE
    BASE_CRIT_MULTI = SF.BASE_CRIT_MULTIPLIER

    @staticmethod
    def monster_hp(stage: int) -> int:
        """스테이지별 몬스터 HP (보스 포함)"""
        if GameFormulas.is_boss(stage):
            return SF.calc_boss_hp(stage)
        return SF.calc_monster_hp(stage)

    @staticmethod
    def is_boss(stage: int) -> bool:
        """보스 스테이지인지"""
        return stage > 0 and stage % SF.BOSS_INTERVAL == 0

    @staticmethod
    def monster_gold(stage: int, gold_flat: int = 0, gold_multi: float = 0) -> int:
        """몬스터 처치 골드"""
        base = SF.calc_base_gold(stage)
        return int((base + gold_flat) * (1 + gold_multi / 100))

    @staticmethod
    def calc_damage(base_power: int, base_attack: int, attack_percent: float,
                    crit_chance: float, crit_multi: float,
                    multi_hit_chance: float, combo_stack: int, combo_damage: float) -> dict:
        """데미지 계산 (상세 정보 포함)"""
        # 기본 데미지
        raw = base_power + base_attack
        after_percent = raw * (1 + attack_percent / 100)

        # 크리티컬 기대값 계산
        total_crit_chance = min(SF.BASE_CRIT_CHANCE + crit_chance / 100, 1.0)
        total_crit_multi = SF.BASE_CRIT_MULTIPLIER + crit_multi
        crit_expected = 1 + total_crit_chance * (total_crit_multi - 1)

        # 멀티히트 기대값
        multi_expected = 1 + multi_hit_chance / 100

        # 콤보 배율 (생성된 공식 사용)
        combo_multi = SF.calc_combo_multiplier(combo_damage, combo_stack)

        # 최종 기대 데미지
        expected = after_percent * crit_expected * multi_expected * combo_multi

        return {
            'raw': raw,
            'after_percent': after_percent,
            'crit_chance': total_crit_chance,
            'crit_multi': total_crit_multi,
            'crit_expected': crit_expected,
            'multi_expected': multi_expected,
            'combo_multi': combo_multi,
            'expected': expected,
            'min': int(after_percent),  # 논크리티컬
            'max': int(after_percent * total_crit_multi * 2 * combo_multi)  # 풀버프
        }

    @staticmethod
    def upgrade_cost(base: float, growth: float, multi: float, softcap: int, level: int) -> int:
        """업그레이드 비용 (생성된 공식 사용)"""
        return SF.calc_upgrade_cost(base, growth, multi, softcap, level)

    @staticmethod
    def total_cost(base: float, growth: float, multi: float, softcap: int,
                   from_lv: int, to_lv: int) -> int:
        """총 업그레이드 비용"""
        return sum(
            SF.calc_upgrade_cost(base, growth, multi, softcap, lv)
            for lv in range(from_lv, to_lv)
        )


# ============================================================
# 스테이지 시뮬레이터 탭
# ============================================================

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

        # 크리스탈 (보스 처치 시)
        boss_count = target // 10
        crystals = boss_count * 10  # 기본 10개씩

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


# ============================================================
# DPS 계산기 탭
# ============================================================

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


# ============================================================
# 투자 가이드 탭
# ============================================================

class InvestmentGuideTab(QWidget):
    """투자 가이드: 무엇을 업그레이드해야 효율적인가?"""

    def __init__(self, config: dict):
        super().__init__()
        self.config = config
        self._setup_ui()

    def _setup_ui(self):
        layout = QVBoxLayout(self)

        # 입력
        input_group = QGroupBox("현재 보유 재화")
        input_layout = QHBoxLayout(input_group)

        input_layout.addWidget(QLabel("크리스탈:"))
        self.crystals = QSpinBox()
        self.crystals.setRange(0, 1000000)
        self.crystals.setValue(100)
        input_layout.addWidget(self.crystals)

        calc_btn = QPushButton("추천 업그레이드")
        calc_btn.clicked.connect(self._calculate)
        input_layout.addWidget(calc_btn)

        input_layout.addStretch()
        layout.addWidget(input_group)

        # 결과 테이블
        self.result_table = QTableWidget()
        self.result_table.setColumnCount(6)
        self.result_table.setHorizontalHeaderLabels([
            "스탯", "현재Lv", "다음비용", "효과", "효율(효과/비용)", "추천"
        ])
        self.result_table.horizontalHeader().setSectionResizeMode(QHeaderView.ResizeMode.Stretch)
        layout.addWidget(self.result_table)

    def _calculate(self):
        budget = self.crystals.value()
        perm_config = self.config.get('permanent', {}).get('stats', {})

        # 각 스탯의 효율 계산
        results = []
        for stat_id, stat in perm_config.items():
            # 현재 레벨 0 가정 (실제로는 저장된 값 사용)
            current_lv = 0
            next_cost = GameFormulas.upgrade_cost(
                stat.get('base_cost', 1),
                stat.get('growth_rate', 0.5),
                stat.get('multiplier', 1.5),
                stat.get('softcap_interval', 10),
                current_lv + 1
            )
            effect = stat.get('effect_per_level', 1)
            efficiency = effect / next_cost if next_cost > 0 else 0

            can_afford = next_cost <= budget

            results.append({
                'name': stat.get('name', stat_id),
                'current_lv': current_lv,
                'next_cost': next_cost,
                'effect': effect,
                'efficiency': efficiency,
                'affordable': can_afford
            })

        # 효율 순 정렬
        results.sort(key=lambda x: x['efficiency'], reverse=True)

        # 테이블 업데이트
        self.result_table.setRowCount(len(results))
        for i, r in enumerate(results):
            self.result_table.setItem(i, 0, QTableWidgetItem(r['name']))
            self.result_table.setItem(i, 1, QTableWidgetItem(str(r['current_lv'])))
            self.result_table.setItem(i, 2, QTableWidgetItem(f"{r['next_cost']:,}"))
            self.result_table.setItem(i, 3, QTableWidgetItem(f"{r['effect']}"))
            self.result_table.setItem(i, 4, QTableWidgetItem(f"{r['efficiency']:.4f}"))

            recommend = "구매 가능" if r['affordable'] else "재화 부족"
            item = QTableWidgetItem(recommend)
            if r['affordable']:
                item.setBackground(QColor("#28a745"))
            self.result_table.setItem(i, 5, item)


# ============================================================
# 스탯 편집 탭
# ============================================================

class StatEditorTab(QWidget):
    """스탯 편집: 테이블에서 직접 수정, 원본/수정값 실시간 비교"""

    # 테이블 컬럼 매핑
    COL_NAME = 0
    COL_BASE = 1
    COL_GROWTH = 2
    COL_MULTI = 3
    COL_SOFTCAP = 4
    COL_EFFECT = 5
    PARAM_KEYS = ['base_cost', 'growth_rate', 'multiplier', 'softcap_interval', 'effect_per_level']

    # 파라미터 해설문 (툴팁 및 설명 패널용)
    PARAMETER_DESCRIPTIONS = {
        'base_cost': {
            'name': '초기 비용',
            'short': 'Lv1 기준 비용',
            'detail': '업그레이드의 기본 비용입니다.\n\n• 높이면: 초반 진입장벽 상승\n• 낮추면: 초반 접근성 향상\n\n공식: base_cost × (1 + level × growth_rate) × multiplier^(level/softcap)',
            'range': '1~100 권장',
            'effect': '전체 비용 스케일 조절'
        },
        'growth_rate': {
            'name': '증가율',
            'short': '레벨당 선형 증가',
            'detail': '레벨이 오를 때마다 비용이 선형으로 증가하는 비율입니다.\n\n• 높이면: 후반 비용 빠르게 증가\n• 낮추면: 레벨 간 비용 차이 감소\n\n예: growth_rate=0.5일 때 Lv10은 (1+10×0.5)=6배',
            'range': '0.3~0.7 권장',
            'effect': '중반 비용 곡선 기울기'
        },
        'multiplier': {
            'name': '급등 배수',
            'short': '지수적 증가 배수',
            'detail': '소프트캡 주기마다 비용이 급등하는 배수입니다.\n\n• 높이면 (1.5+): 후반 비용 폭발적 증가\n• 낮추면 (1.3-): 선형에 가까운 비용 증가\n\n예: multiplier=1.5, softcap=10일 때 Lv20은 1.5²=2.25배',
            'range': '1.3~1.8 권장 (강한 제한: 1.8+)',
            'effect': '후반 하드캡 강도'
        },
        'softcap_interval': {
            'name': '급등 주기',
            'short': '급등 적용 간격',
            'detail': '급등 배수(multiplier)가 적용되는 레벨 간격입니다.\n\n• 높이면 (20+): 급등 간격 넓어짐, 완만한 곡선\n• 낮추면 (5~10): 자주 급등, 가파른 곡선\n\nCPS 균형도가 낮을 때 증가 권장',
            'range': '10~20 권장',
            'effect': '비용 곡선 가파름'
        },
        'effect_per_level': {
            'name': '레벨당 효과',
            'short': '투자 보상',
            'detail': '레벨당 얻는 스탯 효과입니다.\n\n• 높이면: 투자 효율 증가, CPS 더 많이 감소\n• 낮추면: 고레벨까지 필요\n\n비용과 함께 조절하여 효율 균형 유지',
            'range': '스탯별 상이',
            'effect': '레벨당 전투력 증가량'
        }
    }

    # 헤더 툴팁 (간단한 설명)
    HEADER_TOOLTIPS = {
        0: '스탯 이름\n🔷=영구 스탯, 🟡=인게임 스탯',
        1: '초기 비용\nLv1 기준 비용 (초반 난이도)',
        2: '증가율\n레벨당 선형 증가 (중반 곡선)',
        3: '급등 배수\n지수적 증가 (후반 폭발)',
        4: '급등 주기\n급등 적용 간격 (곡선 가파름)',
        5: '레벨당 효과\n투자 보상 (전투력 증가량)'
    }

    # 스탯별 상세 설명 및 데미지 공식 적용 위치
    STAT_DESCRIPTIONS = {
        'base_attack': {
            'name': '기본 공격력',
            'desc': '클릭/입력당 추가되는 고정 데미지입니다.',
            'formula_part': 'base_power + base_attack',
            'formula_full': 'damage = (base_power + [base_attack]) × (1 + attack%) × crit × multi × combo',
            'effect': '가산 데미지 - 초반에 효과적, 후반에 영향력 감소',
            'tip': '초반 진행에 필수적인 스탯. 다른 배율 스탯과 곱연산됨.'
        },
        'attack_percent': {
            'name': '공격력 %',
            'desc': '총 데미지에 퍼센트 배율을 적용합니다.',
            'formula_part': '× (1 + attack_percent / 100)',
            'formula_full': 'damage = (base_power + base_attack) × (1 + [attack%/100]) × crit × multi × combo',
            'effect': '승산 배율 - 기본 공격력이 높을수록 효과 증가',
            'tip': 'base_attack과 시너지. 100%면 2배, 200%면 3배 데미지.'
        },
        'crit_chance': {
            'name': '크리티컬 확률',
            'desc': '크리티컬 히트 발생 확률을 증가시킵니다.',
            'formula_part': 'crit_expected = 1 + crit_chance × (crit_multi - 1)',
            'formula_full': 'damage = base × attack% × [1 + crit_chance × (crit_multi-1)] × multi × combo',
            'effect': '기대값 배율 - 크리티컬 배수(crit_damage)와 시너지',
            'tip': '기본 10%, 최대 100%. crit_damage와 함께 올려야 효율적.'
        },
        'crit_damage': {
            'name': '크리티컬 데미지',
            'desc': '크리티컬 히트 시 추가 배율입니다.',
            'formula_part': 'crit_multi = BASE_CRIT_MULTI + crit_damage',
            'formula_full': 'damage = base × attack% × [1 + crit_chance × (crit_multi-1)] × multi × combo',
            'effect': '크리티컬 배수 - 기본 2.0배, 증가분 추가',
            'tip': 'crit_chance가 높을수록 효과적. 두 스탯을 균형있게 투자.'
        },
        'multi_hit': {
            'name': '멀티히트 확률',
            'desc': '한 번의 입력으로 추가 타격이 발생할 확률입니다.',
            'formula_part': 'multi_expected = 1 + multi_hit_chance / 100',
            'formula_full': 'damage = base × attack% × crit × [1 + multi_hit%/100] × combo',
            'effect': '기대 타격 횟수 - 100%면 평균 2회 타격',
            'tip': '독립적인 배율. 다른 스탯과 곱연산으로 후반에 강력.'
        },
        'time_extend': {
            'name': '시간 연장',
            'desc': '스테이지 제한 시간을 연장합니다.',
            'formula_part': 'time_limit = BASE_TIME + time_extend',
            'formula_full': 'required_CPS = HP / damage / [time_limit + time_extend]',
            'effect': '제한 시간 증가 - 필요 CPS 직접 감소',
            'tip': '기본 30초. DPS가 아닌 "여유 시간"을 늘리는 스탯.'
        },
        'gold_flat': {
            'name': '골드 획득량 (고정)',
            'desc': '몬스터 처치 시 추가 골드를 획득합니다.',
            'formula_part': 'gold = (base_gold + [gold_flat]) × gold_multi',
            'formula_full': 'gold_earned = (stage×1.5 + [gold_flat]) × (1 + gold_multi%)',
            'effect': '가산 골드 - 저스테이지에서 효과적',
            'tip': '초반 자금 확보에 유용. 후반에는 gold_multi가 더 효율적.'
        },
        'gold_multi': {
            'name': '골드 획득량 (%)',
            'desc': '골드 획득량에 퍼센트 배율을 적용합니다.',
            'formula_part': '× (1 + gold_multi / 100)',
            'formula_full': 'gold_earned = (stage×1.5 + gold_flat) × (1 + [gold_multi%/100])',
            'effect': '승산 골드 배율 - 고스테이지에서 효과적',
            'tip': '스테이지가 높을수록 기본 골드가 많아 효율 증가.'
        },
        'combo_damage': {
            'name': '콤보 데미지',
            'desc': '콤보 스택당 추가 데미지 배율입니다.',
            'formula_part': 'combo_multi = (1 + combo_damage/100) × 2^combo_stack',
            'formula_full': 'damage = base × attack% × crit × multi × [(1+combo_dmg%) × 2^stack]',
            'effect': '콤보 배율 강화 - 기본 2^stack에 추가 배율',
            'tip': '콤보 유지가 핵심. 스택 3에서 8배 → combo_damage로 추가 강화.'
        }
    }

    def __init__(self, config: dict):
        super().__init__()
        self.config = config
        self._file_values = {}  # {(type, id): {param: value}} 파일 원본값
        self._current_values = {}  # {(type, id): {param: value}} 현재 편집값
        self._stat_rows = []  # [(type, id, stat_dict), ...]
        self.settings = QSettings("DeskWarrior", "BalanceDashboard")  # 레이아웃 상태 저장용
        self._load_all_from_file()
        self._setup_ui()
        self._restore_splitter_state()  # splitter 상태 복원

    def _load_all_from_file(self):
        """파일에서 모든 원본값 로드"""
        self._file_values.clear()
        self._current_values.clear()

        for stype, filename in [('permanent', 'PermanentStatGrowth.json'),
                                 ('ingame', 'InGameStatGrowth.json')]:
            try:
                data = load_json(filename)
                for sid, stat in data.get('stats', {}).items():
                    vals = {
                        'base_cost': stat.get('base_cost', 1),
                        'growth_rate': stat.get('growth_rate', 0.5),
                        'multiplier': stat.get('multiplier', 1.5),
                        'softcap_interval': stat.get('softcap_interval', 10),
                        'effect_per_level': stat.get('effect_per_level', 1),
                    }
                    self._file_values[(stype, sid)] = vals.copy()
                    self._current_values[(stype, sid)] = vals.copy()
            except:
                pass

    def _setup_ui(self):
        layout = QVBoxLayout(self)

        # 상단 설명
        help_label = QLabel(
            "📐 테이블에서 직접 수정 (더블클릭) | "
            "<b style='color:#4a90d9'>파란글씨=원본</b>, "
            "<b style='color:#ff6b6b'>빨간글씨=수정됨</b>"
        )
        help_label.setStyleSheet("color: #aaa; font-size: 11px; padding: 4px;")
        layout.addWidget(help_label)

        # 메인 영역 (스플리터로 동적 크기 조절)
        self.splitter = QSplitter(Qt.Orientation.Horizontal)

        # === 좌측: 편집 테이블 ===
        left = QGroupBox("스탯 편집")
        left_layout = QVBoxLayout(left)

        self.stat_table = QTableWidget()
        self.stat_table.setColumnCount(6)
        self.stat_table.setHorizontalHeaderLabels([
            "스탯명", "초기비용", "증가율", "급등배수", "급등주기", "Lv당효과"
        ])
        self.stat_table.horizontalHeader().setSectionResizeMode(QHeaderView.ResizeMode.Stretch)
        self.stat_table.cellChanged.connect(self._on_cell_changed)
        self.stat_table.itemSelectionChanged.connect(self._on_selection_changed)

        # 헤더 툴팁 설정
        for col, tooltip in self.HEADER_TOOLTIPS.items():
            header_item = self.stat_table.horizontalHeaderItem(col)
            if header_item:
                header_item.setToolTip(tooltip)

        left_layout.addWidget(self.stat_table)

        # 파라미터 설명 패널
        self.desc_frame = QFrame()
        self.desc_frame.setStyleSheet("""
            QFrame {
                background-color: #2a2a3a;
                border: 1px solid #4a90d9;
                border-radius: 6px;
            }
        """)
        desc_layout = QVBoxLayout(self.desc_frame)
        desc_layout.setContentsMargins(8, 6, 8, 6)
        desc_layout.setSpacing(2)

        self.desc_title = QLabel("파라미터를 선택하세요")
        self.desc_title.setStyleSheet("color: #4a90d9; font-weight: bold; font-size: 11px;")
        desc_layout.addWidget(self.desc_title)

        self.desc_detail = QLabel("")
        self.desc_detail.setStyleSheet("color: #b0b0b0; font-size: 10px;")
        self.desc_detail.setWordWrap(True)
        desc_layout.addWidget(self.desc_detail)

        self.desc_range = QLabel("")
        self.desc_range.setStyleSheet("color: #ffc107; font-size: 10px;")
        desc_layout.addWidget(self.desc_range)

        self.desc_frame.setMinimumHeight(80)
        self.desc_frame.setMaximumHeight(160)
        left_layout.addWidget(self.desc_frame)

        # 버튼
        btn_layout = QHBoxLayout()

        reset_btn = QPushButton("🔄 모든 수정 취소")
        reset_btn.clicked.connect(self._reset_all)
        reset_btn.setStyleSheet("background-color: #555;")
        btn_layout.addWidget(reset_btn)

        save_btn = QPushButton("💾 모든 변경 저장")
        save_btn.clicked.connect(self._save_all)
        save_btn.setStyleSheet("background-color: #28a745;")
        btn_layout.addWidget(save_btn)

        left_layout.addLayout(btn_layout)

        # 변경 요약 (테이블 채우기 전에 생성)
        self.change_label = QLabel("변경 없음")
        self.change_label.setStyleSheet("color: #888; font-size: 10px;")

        # 테이블 채우기
        self._populate_table()
        left_layout.addWidget(self.change_label)

        self.splitter.addWidget(left)

        # === 우측: 그래프 ===
        right = QGroupBox("📈 그래프 (선택된 스탯)")
        right_layout = QVBoxLayout(right)

        # 그래프 타입 선택
        graph_type_layout = QHBoxLayout()
        graph_type_layout.addWidget(QLabel("그래프:"))
        self.graph_type_combo = QComboBox()
        self.graph_type_combo.addItems([
            "📊 비용/CPS (기본)",
            "💰 골드/크리스탈",
            "📈 통합 (전체 적용)"
        ])
        self.graph_type_combo.currentIndexChanged.connect(self._update_graph)
        self.graph_type_combo.setMinimumWidth(150)
        graph_type_layout.addWidget(self.graph_type_combo)
        graph_type_layout.addStretch()
        right_layout.addLayout(graph_type_layout)

        # 시뮬 파라미터
        param_layout = QHBoxLayout()
        param_layout.addWidget(QLabel("업글Lv:"))
        self.spin_level = QSpinBox()
        self.spin_level.setRange(1, 100)
        self.spin_level.setValue(30)
        self.spin_level.valueChanged.connect(self._update_graph)
        param_layout.addWidget(self.spin_level)

        param_layout.addWidget(QLabel("기본공격력:"))
        self.spin_power = QSpinBox()
        self.spin_power.setRange(1, 1000)
        self.spin_power.setValue(20)
        self.spin_power.valueChanged.connect(self._update_graph)
        param_layout.addWidget(self.spin_power)

        param_layout.addWidget(QLabel("스테이지:"))
        self.spin_stage = QSpinBox()
        self.spin_stage.setRange(1, 500)
        self.spin_stage.setValue(50)
        self.spin_stage.valueChanged.connect(self._update_graph)
        param_layout.addWidget(self.spin_stage)

        param_layout.addStretch()
        right_layout.addLayout(param_layout)

        # 2x2 그래프 그리드
        self.figure = Figure(figsize=(8, 6), facecolor='#1e1e2e')
        self.canvas = FigureCanvas(self.figure)
        right_layout.addWidget(self.canvas)

        # 정보
        self.info_label = QLabel("스탯을 선택하세요")
        self.info_label.setStyleSheet("color: #aaa; font-size: 10px;")
        self.info_label.setWordWrap(True)
        right_layout.addWidget(self.info_label)

        self.splitter.addWidget(right)
        self.splitter.setSizes([400, 600])  # 초기 비율 (좌:우 = 40:60)
        self.splitter.setHandleWidth(8)  # 드래그 핸들 너비
        self.splitter.setObjectName("statEditorSplitter")  # 상태 저장용
        self.splitter.splitterMoved.connect(self._save_splitter_state)  # 상태 저장
        layout.addWidget(self.splitter)

        self._selected_key = None  # (type, id)

    def _save_splitter_state(self):
        """splitter 상태 저장"""
        self.settings.setValue("statEditor/splitterSizes", self.splitter.sizes())

    def _restore_splitter_state(self):
        """splitter 상태 복원"""
        sizes = self.settings.value("statEditor/splitterSizes")
        if sizes:
            # QSettings에서 리스트로 복원
            try:
                int_sizes = [int(s) for s in sizes]
                if len(int_sizes) == 2 and all(s > 0 for s in int_sizes):
                    self.splitter.setSizes(int_sizes)
            except (ValueError, TypeError):
                pass  # 복원 실패 시 기본값 사용

    def _populate_table(self):
        """테이블 채우기 - 원본/수정 비교 색상 표시"""
        self.stat_table.blockSignals(True)
        self.stat_table.setRowCount(0)
        self._stat_rows = []

        for stype in ['permanent', 'ingame']:
            stats = self.config.get(stype, {}).get('stats', {})
            for sid, stat in stats.items():
                key = (stype, sid)
                self._stat_rows.append((stype, sid, stat))
                row = self.stat_table.rowCount()
                self.stat_table.insertRow(row)

                # 이름 (읽기 전용)
                prefix = "🔷" if stype == 'permanent' else "🟡"
                name_item = QTableWidgetItem(f"{prefix} {stat.get('name', sid)}")
                name_item.setFlags(name_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
                self.stat_table.setItem(row, self.COL_NAME, name_item)

                # 파라미터들 (편집 가능)
                file_vals = self._file_values.get(key, {})
                curr_vals = self._current_values.get(key, {})

                for col, param in enumerate(self.PARAM_KEYS, start=1):
                    file_val = file_vals.get(param, 1)
                    curr_val = curr_vals.get(param, file_val)

                    # 원본과 다르면 "원본→수정" 형식, 같으면 값만
                    if abs(float(file_val) - float(curr_val)) > 0.0001:
                        text = f"{file_val}→{curr_val}"
                        color = QColor("#ff6b6b")
                    else:
                        text = str(curr_val)
                        color = QColor("#4a90d9")

                    item = QTableWidgetItem(text)
                    item.setForeground(color)
                    self.stat_table.setItem(row, col, item)

        self.stat_table.blockSignals(False)
        self._update_change_summary()

    def _on_cell_changed(self, row, col):
        """셀 편집 시 호출"""
        if col == 0 or row >= len(self._stat_rows):
            return

        stype, sid, _ = self._stat_rows[row]
        key = (stype, sid)
        param = self.PARAM_KEYS[col - 1]

        # 값 파싱 (→ 포함 시 뒤의 값만)
        item = self.stat_table.item(row, col)
        text = item.text()
        if '→' in text:
            text = text.split('→')[-1]

        try:
            if param in ['softcap_interval', 'base_cost']:
                new_val = int(float(text))
            else:
                new_val = float(text)
        except:
            # 파싱 실패 시 원래 값 복원
            self._populate_table()
            return

        # 현재값 업데이트
        if key not in self._current_values:
            self._current_values[key] = self._file_values.get(key, {}).copy()
        self._current_values[key][param] = new_val

        # 선택 키 업데이트 (편집한 행을 선택 상태로)
        self._selected_key = key

        # 테이블 갱신 (색상 업데이트) - 시그널 차단 상태로
        self._populate_table()

        # 행 선택 복원 (시그널 차단하여 _on_selection_changed 방지)
        self.stat_table.blockSignals(True)
        self.stat_table.selectRow(row)
        self.stat_table.blockSignals(False)

        # 그래프 갱신 (항상) - 디버그 출력 추가
        print(f"[DEBUG] _on_cell_changed: key={key}, param={param}, new_val={new_val}")
        print(f"[DEBUG] file_vals={self._file_values.get(key, {})}")
        print(f"[DEBUG] curr_vals={self._current_values.get(key, {})}")
        self._update_graph()

    def _on_selection_changed(self):
        """행 선택 변경"""
        row = self.stat_table.currentRow()
        col = self.stat_table.currentColumn()

        if 0 <= row < len(self._stat_rows):
            stype, sid, _ = self._stat_rows[row]
            self._selected_key = (stype, sid)
            self._update_graph()

        # 스탯/파라미터 설명 업데이트
        self._update_param_description(row, col)

    def _calc_upgrade_cost(self, base_cost, growth_rate, multiplier, softcap_interval, level):
        """업그레이드 비용 계산: cost = base × (1 + level × growth_rate) × multiplier^(level / softcap_interval)"""
        import math
        cost = base_cost * (1 + level * growth_rate) * (multiplier ** (level / softcap_interval))
        return cost

    def _update_param_description(self, row: int, col: int):
        """파라미터/스탯 설명 패널 업데이트"""
        # 스탯 이름 열(col 0) 클릭 시 - 스탯 설명
        if col == 0 and 0 <= row < len(self._stat_rows):
            stype, sid, stat = self._stat_rows[row]
            if sid in self.STAT_DESCRIPTIONS:
                desc = self.STAT_DESCRIPTIONS[sid]
                self.desc_title.setText(f"⚔️ {desc['name']} ({sid})")
                detail_text = (
                    f"{desc['desc']}\n\n"
                    f"📐 공식 적용: {desc['formula_part']}\n"
                    f"💡 효과: {desc['effect']}\n\n"
                    f"📝 팁: {desc['tip']}"
                )
                self.desc_detail.setText(detail_text)
                self.desc_range.setText(f"전체 공식: {desc['formula_full']}")
            else:
                self.desc_title.setText(f"⚔️ {stat.get('name', sid)}")
                self.desc_detail.setText("상세 설명이 등록되지 않은 스탯입니다.")
                self.desc_range.setText("")
            return

        # 파라미터 열 클릭 시 - 파라미터 설명
        param_key = self.PARAM_KEYS[col - 1] if 1 <= col <= len(self.PARAM_KEYS) else None
        if param_key and param_key in self.PARAMETER_DESCRIPTIONS:
            desc = self.PARAMETER_DESCRIPTIONS[param_key]
            self.desc_title.setText(f"📊 {desc['name']} ({param_key})")
            self.desc_detail.setText(desc['detail'])
            self.desc_range.setText(f"권장 범위: {desc['range']}")
        else:
            self.desc_title.setText("스탯 또는 파라미터를 선택하세요")
            self.desc_detail.setText("테이블의 스탯 이름을 클릭하면 해당 스탯의 상세 설명과 데미지 공식 적용 위치가 표시됩니다.")
            self.desc_range.setText("")

    def _calc_sensitivity_analysis(self, file_vals: dict, max_level: int, max_stage: int, base_power: int):
        """파라미터 민감도 분석 계산"""
        results = {}
        variation_range = [-0.3, -0.2, -0.1, 0, 0.1, 0.2, 0.3]  # ±30% 변동

        # 기준값 계산 (현재 파라미터로 Lv30 누적 비용)
        base_cumulative = sum(
            self._calc_upgrade_cost(
                file_vals.get('base_cost', 1),
                file_vals.get('growth_rate', 0.5),
                file_vals.get('multiplier', 1.5),
                file_vals.get('softcap_interval', 10),
                lv
            ) for lv in range(1, max_level + 1)
        )

        for param_key in self.PARAM_KEYS:
            param_results = []
            base_val = file_vals.get(param_key, 1)

            for var in variation_range:
                # 파라미터 변동 적용
                modified_vals = file_vals.copy()
                modified_vals[param_key] = base_val * (1 + var)

                # 누적 비용 계산
                cumulative = sum(
                    self._calc_upgrade_cost(
                        modified_vals.get('base_cost', 1),
                        modified_vals.get('growth_rate', 0.5),
                        modified_vals.get('multiplier', 1.5),
                        modified_vals.get('softcap_interval', 10),
                        lv
                    ) for lv in range(1, max_level + 1)
                )

                # 기준 대비 변화율
                change_pct = ((cumulative - base_cumulative) / base_cumulative * 100) if base_cumulative > 0 else 0
                param_results.append({
                    'variation': var * 100,
                    'cumulative': cumulative,
                    'change_pct': change_pct
                })

            # 민감도 점수 (±30% 변동 시 비용 변화율의 절대값)
            max_change = max(abs(r['change_pct']) for r in param_results)
            results[param_key] = {
                'data': param_results,
                'sensitivity': max_change
            }

        return results

    def _update_graph(self):
        """선택된 스탯의 그래프 갱신 (2x2 그리드)"""
        self.figure.clear()

        if not self._selected_key:
            self.canvas.draw()
            self.info_label.setText("스탯을 선택하세요")
            return

        stype, sid = self._selected_key
        file_vals = self._file_values.get(self._selected_key, {})
        curr_vals = self._current_values.get(self._selected_key, {})

        max_level = self.spin_level.value()
        base_power = self.spin_power.value()
        max_stage = self.spin_stage.value()
        time_limit = 30

        # 비용 곡선 계산 (레벨별)
        levels = list(range(1, max_level + 1))

        file_costs = []
        curr_costs = []
        for lv in levels:
            file_cost = self._calc_upgrade_cost(
                file_vals.get('base_cost', 1),
                file_vals.get('growth_rate', 0.5),
                file_vals.get('multiplier', 1.5),
                file_vals.get('softcap_interval', 10),
                lv
            )
            curr_cost = self._calc_upgrade_cost(
                curr_vals.get('base_cost', 1),
                curr_vals.get('growth_rate', 0.5),
                curr_vals.get('multiplier', 1.5),
                curr_vals.get('softcap_interval', 10),
                lv
            )
            file_costs.append(file_cost)
            curr_costs.append(curr_cost)

        # 누적 비용
        file_cumulative = []
        curr_cumulative = []
        file_sum, curr_sum = 0, 0
        for fc, cc in zip(file_costs, curr_costs):
            file_sum += fc
            curr_sum += cc
            file_cumulative.append(file_sum)
            curr_cumulative.append(curr_sum)

        # CPS 계산 헬퍼
        def calc_cps_for_level(vals, lv):
            effect = vals.get('effect_per_level', 1) * lv
            dmg = base_power
            hp = GameFormulas.monster_hp(max_stage)
            if sid == 'base_attack':
                dmg += effect
            elif sid == 'attack_percent':
                dmg *= (1 + effect / 100)
            elif sid == 'crit_chance':
                dmg *= (1 + min(0.1 + effect/100, 1.0))
            elif sid == 'multi_hit':
                dmg *= (1 + effect / 100)
            elif sid == 'time_extend':
                return hp / max(dmg, 1) / (time_limit + effect)
            else:
                dmg += effect * 0.5
            return hp / max(dmg, 1) / time_limit

        # 그래프 타입에 따라 다른 그래프 표시
        graph_type = self.graph_type_combo.currentIndex()
        stages = list(range(1, min(max_stage + 1, 101)))

        # 공통 헬퍼 함수
        def calc_damage(effect):
            dmg = base_power
            if sid == 'base_attack':
                dmg += effect
            elif sid == 'attack_percent':
                dmg *= (1 + effect / 100)
            elif sid == 'crit_chance':
                dmg *= (1 + min(0.1 + effect/100, 1.0))
            elif sid == 'multi_hit':
                dmg *= (1 + effect / 100)
            else:
                dmg += effect * 0.5
            return max(dmg, 1)

        def calc_time(effect):
            if sid == 'time_extend':
                return time_limit + effect
            return time_limit

        def setup_axes(axes_list):
            for ax in axes_list:
                ax.set_facecolor('#1e1e2e')
                ax.tick_params(colors='#888', labelsize=7)
                for spine in ax.spines.values():
                    spine.set_color('#444')

        file_effect_val = file_vals.get('effect_per_level', 1) * max_level
        curr_effect_val = curr_vals.get('effect_per_level', 1) * max_level

        if graph_type == 0:  # 📊 비용/CPS (기본)
            axes = self.figure.subplots(2, 2)
            setup_axes([axes[0,0], axes[0,1], axes[1,0], axes[1,1]])

            # (0,0) 업그레이드 비용
            axes[0,0].plot(levels, file_costs, color='#4a90d9', linewidth=1.5, label='원본')
            axes[0,0].plot(levels, curr_costs, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[0,0].set_title('업그레이드 비용', color='#ddd', fontsize=9)
            axes[0,0].set_xlabel('레벨', color='#888', fontsize=8)
            axes[0,0].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[0,0].grid(True, alpha=0.2)

            # (0,1) 누적 비용
            axes[0,1].plot(levels, file_cumulative, color='#4a90d9', linewidth=1.5, label='원본')
            axes[0,1].plot(levels, curr_cumulative, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[0,1].set_title('누적 비용', color='#ddd', fontsize=9)
            axes[0,1].set_xlabel('레벨', color='#888', fontsize=8)
            axes[0,1].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[0,1].grid(True, alpha=0.2)

            # (1,0) CPS 곡선 (레벨별)
            file_cps_by_level = [calc_cps_for_level(file_vals, lv) for lv in levels]
            curr_cps_by_level = [calc_cps_for_level(curr_vals, lv) for lv in levels]
            axes[1,0].plot(levels, file_cps_by_level, color='#4a90d9', linewidth=1.5, label='원본')
            axes[1,0].plot(levels, curr_cps_by_level, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[1,0].axhline(y=15, color='#ff4444', alpha=0.5, linestyle=':', linewidth=1)
            axes[1,0].axhline(y=5, color='#ffc107', alpha=0.5, linestyle=':', linewidth=1)
            axes[1,0].set_title(f'필요 CPS (Stage {max_stage})', color='#ddd', fontsize=9)
            axes[1,0].set_xlabel('레벨', color='#888', fontsize=8)
            axes[1,0].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[1,0].grid(True, alpha=0.2)

            # (1,1) CPS vs 스테이지
            file_dmg = calc_damage(file_effect_val)
            curr_dmg = calc_damage(curr_effect_val)
            file_time = calc_time(file_effect_val)
            curr_time = calc_time(curr_effect_val)
            file_cps_stage = [(GameFormulas.monster_hp(s) / file_dmg) / file_time for s in stages]
            curr_cps_stage = [(GameFormulas.monster_hp(s) / curr_dmg) / curr_time for s in stages]
            axes[1,1].plot(stages, file_cps_stage, color='#4a90d9', linewidth=1.5, label='원본')
            axes[1,1].plot(stages, curr_cps_stage, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[1,1].axhline(y=15, color='#ff4444', alpha=0.5, linestyle=':', linewidth=1)
            axes[1,1].axhline(y=5, color='#ffc107', alpha=0.5, linestyle=':', linewidth=1)
            axes[1,1].set_title(f'CPS vs 스테이지 (Lv{max_level})', color='#ddd', fontsize=9)
            axes[1,1].set_xlabel('스테이지', color='#888', fontsize=8)
            axes[1,1].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[1,1].grid(True, alpha=0.2)

        elif graph_type == 1:  # 💰 골드/크리스탈
            axes = self.figure.subplots(2, 2)
            setup_axes([axes[0,0], axes[0,1], axes[1,0], axes[1,1]])

            # 골드 계산 (스테이지별)
            def calc_gold(stage, gold_flat=0, gold_multi=0):
                base_gold = stage * 1.5
                return (base_gold + gold_flat) * (1 + gold_multi / 100)

            # gold 스탯 효과 적용
            gold_flat_effect = file_effect_val if sid == 'gold_flat' else 0
            gold_multi_effect = file_effect_val if sid == 'gold_multi' else 0
            curr_gold_flat = curr_effect_val if sid == 'gold_flat' else 0
            curr_gold_multi = curr_effect_val if sid == 'gold_multi' else 0

            # (0,0) 스테이지별 골드 획득
            file_gold = [calc_gold(s, gold_flat_effect, gold_multi_effect) for s in stages]
            curr_gold = [calc_gold(s, curr_gold_flat, curr_gold_multi) for s in stages]
            axes[0,0].plot(stages, file_gold, color='#ffc107', linewidth=1.5, label='원본')
            axes[0,0].plot(stages, curr_gold, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[0,0].set_title('스테이지별 골드 획득', color='#ddd', fontsize=9)
            axes[0,0].set_xlabel('스테이지', color='#888', fontsize=8)
            axes[0,0].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[0,0].grid(True, alpha=0.2)

            # (0,1) 누적 골드 (100마리 처치 가정)
            file_cumul_gold = [sum(file_gold[:i+1]) for i in range(len(file_gold))]
            curr_cumul_gold = [sum(curr_gold[:i+1]) for i in range(len(curr_gold))]
            axes[0,1].plot(stages, file_cumul_gold, color='#ffc107', linewidth=1.5, label='원본')
            axes[0,1].plot(stages, curr_cumul_gold, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[0,1].set_title('누적 골드 (진행 기준)', color='#ddd', fontsize=9)
            axes[0,1].set_xlabel('스테이지', color='#888', fontsize=8)
            axes[0,1].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[0,1].grid(True, alpha=0.2)

            # (1,0) 크리스탈 환산 (1000골드 = 1크리스탈)
            file_crystal = [g / 1000 for g in file_cumul_gold]
            curr_crystal = [g / 1000 for g in curr_cumul_gold]
            axes[1,0].plot(stages, file_crystal, color='#17a2b8', linewidth=1.5, label='원본')
            axes[1,0].plot(stages, curr_crystal, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[1,0].set_title('예상 크리스탈 (누적 골드/1000)', color='#ddd', fontsize=9)
            axes[1,0].set_xlabel('스테이지', color='#888', fontsize=8)
            axes[1,0].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[1,0].grid(True, alpha=0.2)

            # (1,1) 골드 효율 (골드/비용)
            gold_efficiency_file = [g / max(c, 1) for g, c in zip(file_gold, file_cumulative[:len(stages)] if len(file_cumulative) >= len(stages) else file_cumulative + [file_cumulative[-1]]*(len(stages)-len(file_cumulative)))]
            gold_efficiency_curr = [g / max(c, 1) for g, c in zip(curr_gold, curr_cumulative[:len(stages)] if len(curr_cumulative) >= len(stages) else curr_cumulative + [curr_cumulative[-1]]*(len(stages)-len(curr_cumulative)))]
            axes[1,1].plot(stages[:len(gold_efficiency_file)], gold_efficiency_file, color='#28a745', linewidth=1.5, label='원본')
            axes[1,1].plot(stages[:len(gold_efficiency_curr)], gold_efficiency_curr, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[1,1].set_title('골드/업글비용 효율', color='#ddd', fontsize=9)
            axes[1,1].set_xlabel('스테이지', color='#888', fontsize=8)
            axes[1,1].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[1,1].grid(True, alpha=0.2)

        elif graph_type == 2:  # 📈 통합 (모든 수정 스탯 반영)
            axes = self.figure.subplots(2, 3)
            all_axes = [axes[0,0], axes[0,1], axes[0,2], axes[1,0], axes[1,1], axes[1,2]]
            setup_axes(all_axes)

            # 변경된 스탯 목록 확인
            changed_stats = []
            for (st, stat_id), curr_vals_item in self._current_values.items():
                file_vals_item = self._file_values.get((st, stat_id), {})
                for param in self.PARAM_KEYS:
                    if abs(float(curr_vals_item.get(param, 0)) - float(file_vals_item.get(param, 0))) > 0.0001:
                        changed_stats.append(stat_id)
                        break
            changed_stats = list(set(changed_stats))

            # 전체 스탯 효과를 합산한 데미지/시간 계산
            def calc_total_damage_and_time(vals_dict, level):
                dmg = base_power
                extra_time = 0
                crit_chance_val = 0.1  # 기본 크리티컬 확률
                crit_multi = 2.0  # 기본 크리티컬 배수

                for (st, stat_id), vals in vals_dict.items():
                    effect = vals.get('effect_per_level', 1) * level
                    if stat_id == 'base_attack':
                        dmg += effect
                    elif stat_id == 'attack_percent':
                        dmg *= (1 + effect / 100)
                    elif stat_id == 'crit_chance':
                        crit_chance_val = min(0.1 + effect/100, 1.0)
                    elif stat_id == 'crit_damage':
                        crit_multi = 2.0 + effect
                    elif stat_id == 'multi_hit':
                        dmg *= (1 + effect / 100)
                    elif stat_id == 'time_extend':
                        extra_time = effect

                # 크리티컬 기대값 적용
                crit_expected = 1 + crit_chance_val * (crit_multi - 1)
                dmg *= crit_expected

                return max(dmg, 1), time_limit + extra_time

            # (0,0) 총 데미지 (모든 스탯)
            file_total_dmg = [calc_total_damage_and_time(self._file_values, lv)[0] for lv in levels]
            curr_total_dmg = [calc_total_damage_and_time(self._current_values, lv)[0] for lv in levels]
            axes[0,0].plot(levels, file_total_dmg, color='#4a90d9', linewidth=1.5, label='원본')
            axes[0,0].plot(levels, curr_total_dmg, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            title_suffix = f" ({len(changed_stats)}개 변경)" if changed_stats else ""
            axes[0,0].set_title(f'총 데미지{title_suffix}', color='#ddd', fontsize=9)
            axes[0,0].set_xlabel('레벨', color='#888', fontsize=8)
            axes[0,0].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[0,0].grid(True, alpha=0.2)

            # (0,1) 필요 CPS (전체 스탯 + 시간 연장 반영)
            file_cps_total = []
            curr_cps_total = []
            for lv in levels:
                f_dmg, f_time = calc_total_damage_and_time(self._file_values, lv)
                c_dmg, c_time = calc_total_damage_and_time(self._current_values, lv)
                file_cps_total.append(GameFormulas.monster_hp(max_stage) / f_dmg / f_time)
                curr_cps_total.append(GameFormulas.monster_hp(max_stage) / c_dmg / c_time)

            axes[0,1].plot(levels, file_cps_total, color='#4a90d9', linewidth=1.5, label='원본')
            axes[0,1].plot(levels, curr_cps_total, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[0,1].axhline(y=15, color='#ff4444', alpha=0.5, linestyle=':', linewidth=1)
            axes[0,1].axhline(y=5, color='#ffc107', alpha=0.5, linestyle=':', linewidth=1)
            axes[0,1].set_title(f'필요 CPS (Stage {max_stage})', color='#ddd', fontsize=9)
            axes[0,1].set_xlabel('레벨', color='#888', fontsize=8)
            axes[0,1].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[0,1].grid(True, alpha=0.2)

            # (0,2) 총 비용 (전체 스탯)
            all_file_costs = [sum(self._calc_upgrade_cost(v.get('base_cost',1), v.get('growth_rate',0.5), v.get('multiplier',1.5), v.get('softcap_interval',10), lv) for v in self._file_values.values()) for lv in levels]
            all_curr_costs = [sum(self._calc_upgrade_cost(v.get('base_cost',1), v.get('growth_rate',0.5), v.get('multiplier',1.5), v.get('softcap_interval',10), lv) for v in self._current_values.values()) for lv in levels]
            axes[0,2].plot(levels, all_file_costs, color='#4a90d9', linewidth=1.5, label='원본')
            axes[0,2].plot(levels, all_curr_costs, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[0,2].set_title('총 업글 비용 (전체)', color='#ddd', fontsize=9)
            axes[0,2].set_xlabel('레벨', color='#888', fontsize=8)
            axes[0,2].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[0,2].grid(True, alpha=0.2)

            # (1,0) CPS vs 스테이지 (전체 스탯)
            f_dmg_max, f_time_max = calc_total_damage_and_time(self._file_values, max_level)
            c_dmg_max, c_time_max = calc_total_damage_and_time(self._current_values, max_level)
            file_cps_stage_total = [GameFormulas.monster_hp(s) / f_dmg_max / f_time_max for s in stages]
            curr_cps_stage_total = [GameFormulas.monster_hp(s) / c_dmg_max / c_time_max for s in stages]
            axes[1,0].plot(stages, file_cps_stage_total, color='#4a90d9', linewidth=1.5, label='원본')
            axes[1,0].plot(stages, curr_cps_stage_total, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[1,0].axhline(y=15, color='#ff4444', alpha=0.5, linestyle=':', linewidth=1)
            axes[1,0].axhline(y=5, color='#ffc107', alpha=0.5, linestyle=':', linewidth=1)
            axes[1,0].set_title(f'CPS vs 스테이지 (Lv{max_level})', color='#ddd', fontsize=9)
            axes[1,0].set_xlabel('스테이지', color='#888', fontsize=8)
            axes[1,0].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[1,0].grid(True, alpha=0.2)

            # (1,1) 누적 비용 비교
            all_file_cumul = []
            all_curr_cumul = []
            f_sum, c_sum = 0, 0
            for f_c, c_c in zip(all_file_costs, all_curr_costs):
                f_sum += f_c
                c_sum += c_c
                all_file_cumul.append(f_sum)
                all_curr_cumul.append(c_sum)
            axes[1,1].plot(levels, all_file_cumul, color='#4a90d9', linewidth=1.5, label='원본')
            axes[1,1].plot(levels, all_curr_cumul, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
            axes[1,1].set_title('누적 총 비용', color='#ddd', fontsize=9)
            axes[1,1].set_xlabel('레벨', color='#888', fontsize=8)
            axes[1,1].legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')
            axes[1,1].grid(True, alpha=0.2)

            # (1,2) 변경 효과 요약 (데미지 증가율)
            dmg_increase_pct = [(c - f) / max(f, 1) * 100 for f, c in zip(file_total_dmg, curr_total_dmg)]
            axes[1,2].plot(levels, dmg_increase_pct, color='#28a745', linewidth=1.5)
            axes[1,2].axhline(y=0, color='#888', linestyle=':', alpha=0.5)
            axes[1,2].fill_between(levels, 0, dmg_increase_pct, alpha=0.3,
                                   color='#28a745' if dmg_increase_pct[-1] >= 0 else '#ff6b6b')
            axes[1,2].set_title('데미지 변화율 (%)', color='#ddd', fontsize=9)
            axes[1,2].set_xlabel('레벨', color='#888', fontsize=8)
            axes[1,2].grid(True, alpha=0.2)

        self.figure.tight_layout()
        self.canvas.draw()

        # 정보 표시
        file_total = sum(file_costs)
        curr_total = sum(curr_costs)
        cost_diff_pct = ((curr_total - file_total) / file_total * 100) if file_total > 0 else 0
        file_final_cps = calc_cps_for_level(file_vals, max_level)
        curr_final_cps = calc_cps_for_level(curr_vals, max_level)

        self.info_label.setText(
            f"Lv{max_level} 총비용: {file_total:.0f}→{curr_total:.0f} ({cost_diff_pct:+.1f}%) | "
            f"Stage{max_stage} CPS: {file_final_cps:.2f}→{curr_final_cps:.2f}"
        )

    def _update_change_summary(self):
        """변경 사항 요약"""
        changes = []
        for key in self._current_values:
            file_vals = self._file_values.get(key, {})
            curr_vals = self._current_values.get(key, {})
            for param in self.PARAM_KEYS:
                fv = file_vals.get(param, 0)
                cv = curr_vals.get(param, 0)
                if abs(float(fv) - float(cv)) > 0.0001:
                    stype, sid = key
                    changes.append(f"{sid}.{param}")

        if changes:
            self.change_label.setText(f"⚡ {len(changes)}개 변경: {', '.join(changes[:5])}{'...' if len(changes) > 5 else ''}")
            self.change_label.setStyleSheet("color: #ffc107; font-size: 10px;")
        else:
            self.change_label.setText("✓ 변경 없음")
            self.change_label.setStyleSheet("color: #888; font-size: 10px;")

    def _reset_all(self):
        """모든 수정 취소"""
        self._current_values = {k: v.copy() for k, v in self._file_values.items()}
        self._populate_table()
        self._update_graph()

    def _save_all(self):
        """모든 변경 저장"""
        # 변경된 것 수집
        perm_changed = False
        ingame_changed = False

        for (stype, sid), curr_vals in self._current_values.items():
            file_vals = self._file_values.get((stype, sid), {})

            has_change = False
            for param in self.PARAM_KEYS:
                if abs(float(file_vals.get(param, 0)) - float(curr_vals.get(param, 0))) > 0.0001:
                    has_change = True
                    break

            if has_change:
                # config 업데이트
                cfg = self.config[stype]['stats'][sid]
                cfg['base_cost'] = int(curr_vals.get('base_cost', 1))
                cfg['growth_rate'] = curr_vals.get('growth_rate', 0.5)
                cfg['multiplier'] = curr_vals.get('multiplier', 1.5)
                cfg['softcap_interval'] = int(curr_vals.get('softcap_interval', 10))
                cfg['effect_per_level'] = curr_vals.get('effect_per_level', 1)

                if stype == 'permanent':
                    perm_changed = True
                else:
                    ingame_changed = True

        # 파일 저장
        if perm_changed:
            save_json('PermanentStatGrowth.json', self.config['permanent'])
        if ingame_changed:
            save_json('InGameStatGrowth.json', self.config['ingame'])

        if perm_changed or ingame_changed:
            # 파일값 갱신
            self._load_all_from_file()
            self._populate_table()
            self._update_graph()
            QMessageBox.information(self, "저장 완료", "모든 변경이 저장되었습니다.")
        else:
            QMessageBox.information(self, "알림", "변경된 내용이 없습니다.")


# ============================================================
# 비교 분석기 탭 (다중 프리셋 비교)
# ============================================================

# 기본 색상 팔레트
DEFAULT_COLORS = ["#4a90d9", "#28a745", "#dc3545", "#ffc107", "#17a2b8", "#6f42c1", "#fd7e14", "#20c997"]


class PresetListItem(QWidget):
    """프리셋 목록 아이템 위젯"""

    def __init__(self, preset_id: str, preset_data: dict, parent=None):
        super().__init__(parent)
        self.preset_id = preset_id
        self.preset_data = preset_data

        layout = QHBoxLayout(self)
        layout.setContentsMargins(5, 2, 5, 2)

        # 체크박스
        self.checkbox = QWidget()
        self.checkbox.setFixedSize(16, 16)
        self.checkbox.setStyleSheet(f"background-color: {preset_data.get('color', '#4a90d9')}; border-radius: 3px;")
        layout.addWidget(self.checkbox)

        # 이름
        self.name_label = QLabel(preset_data.get('name', preset_id))
        self.name_label.setStyleSheet("color: #e0e0e0;")
        layout.addWidget(self.name_label, 1)

        # 잠금 표시
        if preset_data.get('is_locked', False):
            lock_label = QLabel("🔒")
            lock_label.setStyleSheet("font-size: 10px;")
            layout.addWidget(lock_label)


# ============================================================
# CPS 측정기 탭
# ============================================================

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
필요 CPS < 5: ✅ 여유<br>
필요 CPS 5~8: ⚠️ 도전적<br>
필요 CPS 8~12: ⚠️ 어려움<br>
필요 CPS 12~15: ❌ 극한<br>
필요 CPS > 15: ❌ <b>입력 한계 초과</b>
        """)
        ref_label.setStyleSheet("font-size: 12px; color: #aaa; padding: 10px;")
        result_layout.addWidget(ref_label)

        layout.addWidget(self.result_group)
        layout.addStretch()

        # 타이머
        from PyQt6.QtCore import QTimer
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
            import time
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
        import time
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

        import time

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
            grade = "🔥 초인 (CPS 15+ 입력한계)"
            color = "#ff4444"
        elif cps >= 12:
            grade = "🏆 프로 (CPS 12~15)"
            color = "#ffd700"
        elif cps >= 8:
            grade = "⭐ 숙련자 (CPS 8~12)"
            color = "#4ad94a"
        elif cps >= 5:
            grade = "✅ 일반 (CPS 5~8)"
            color = "#4a90d9"
        elif cps >= 3:
            grade = "🔵 캐주얼 (CPS 3~5)"
            color = "#888"
        else:
            grade = "🐢 느림 (CPS < 3)"
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


class ComparisonAnalyzerTab(QWidget):
    """비교 분석기: N개 프리셋 동시 비교"""

    def __init__(self, config: dict):
        super().__init__()
        self.config = config
        self.presets = {}
        self.selected_preset_ids = set()
        self.editing_preset_id = None
        self.level_spinboxes = {}

        self._load_presets()
        self._sync_live_preset()
        self._setup_ui()

    # ==================== 데이터 레이어 ====================

    def _load_presets(self):
        """프리셋 로드"""
        try:
            data = load_json('BalancePresets.json')
            self.presets = data.get('presets', {})
        except:
            self.presets = self._create_default_presets()

    def _save_presets(self):
        """프리셋 저장"""
        data = {
            '_comment': '밸런스 비교용 프리셋 목록',
            '_schema_version': '1.0',
            'presets': self.presets,
            'default_colors': DEFAULT_COLORS,
            'settings': {'max_presets': 10, 'auto_save': True}
        }
        save_json('BalancePresets.json', data)

    def _create_default_presets(self) -> dict:
        """기본 프리셋 생성"""
        return {
            'live': {
                'name': '라이브 (현재)',
                'description': '현재 실제 게임에 적용된 레벨',
                'is_locked': True,
                'color': '#4a90d9',
                'levels': {}
            }
        }

    def _sync_live_preset(self):
        """PlayerLevels.json에서 라이브 프리셋 동기화"""
        try:
            data = load_json('PlayerLevels.json')
            live_levels = data.get('permanent_levels', {})
            if 'live' in self.presets:
                self.presets['live']['levels'] = live_levels
        except:
            pass

    def _create_preset(self, name: str, levels: dict, color: str = None, description: str = ''):
        """새 프리셋 생성"""
        # 고유 ID 생성
        base_id = name.lower().replace(' ', '_')[:20]
        preset_id = base_id
        counter = 1
        while preset_id in self.presets:
            preset_id = f"{base_id}_{counter}"
            counter += 1

        if color is None:
            used_colors = {p.get('color') for p in self.presets.values()}
            for c in DEFAULT_COLORS:
                if c not in used_colors:
                    color = c
                    break
            else:
                color = DEFAULT_COLORS[len(self.presets) % len(DEFAULT_COLORS)]

        self.presets[preset_id] = {
            'name': name,
            'description': description,
            'is_locked': False,
            'color': color,
            'levels': levels.copy()
        }
        self._save_presets()
        return preset_id

    def _update_preset(self, preset_id: str, levels: dict = None, name: str = None):
        """프리셋 업데이트"""
        if preset_id not in self.presets:
            return False
        if self.presets[preset_id].get('is_locked', False):
            return False

        if levels is not None:
            self.presets[preset_id]['levels'] = levels.copy()
        if name is not None:
            self.presets[preset_id]['name'] = name

        self._save_presets()
        return True

    def _delete_preset(self, preset_id: str) -> bool:
        """프리셋 삭제"""
        if preset_id not in self.presets:
            return False
        if self.presets[preset_id].get('is_locked', False):
            return False

        del self.presets[preset_id]
        self.selected_preset_ids.discard(preset_id)
        self._save_presets()
        return True

    # ==================== UI 설정 ====================

    def _setup_ui(self):
        layout = QHBoxLayout(self)

        # 좌측: 프리셋 관리 패널
        left = QWidget()
        left.setMaximumWidth(350)
        left_layout = QVBoxLayout(left)

        # 프리셋 목록 그룹
        preset_group = QGroupBox("프리셋 목록 (비교할 항목 선택)")
        preset_layout = QVBoxLayout(preset_group)

        self.preset_list = QTableWidget()
        self.preset_list.setColumnCount(3)
        self.preset_list.setHorizontalHeaderLabels(["선택", "프리셋", "DPS"])
        self.preset_list.horizontalHeader().setSectionResizeMode(0, QHeaderView.ResizeMode.Fixed)
        self.preset_list.horizontalHeader().setSectionResizeMode(1, QHeaderView.ResizeMode.Stretch)
        self.preset_list.horizontalHeader().setSectionResizeMode(2, QHeaderView.ResizeMode.Fixed)
        self.preset_list.setColumnWidth(0, 40)
        self.preset_list.setColumnWidth(2, 70)
        self.preset_list.setSelectionBehavior(QTableWidget.SelectionBehavior.SelectRows)
        self.preset_list.setSelectionMode(QTableWidget.SelectionMode.SingleSelection)
        self.preset_list.cellClicked.connect(self._on_preset_clicked)
        preset_layout.addWidget(self.preset_list)

        # 프리셋 버튼들
        preset_btn_layout = QHBoxLayout()
        new_btn = QPushButton("+ 새로")
        new_btn.clicked.connect(self._on_new_preset)
        preset_btn_layout.addWidget(new_btn)

        duplicate_btn = QPushButton("복제")
        duplicate_btn.clicked.connect(self._on_duplicate_preset)
        preset_btn_layout.addWidget(duplicate_btn)

        delete_btn = QPushButton("삭제")
        delete_btn.setStyleSheet("background-color: #dc3545;")
        delete_btn.clicked.connect(self._on_delete_preset)
        preset_btn_layout.addWidget(delete_btn)

        preset_layout.addLayout(preset_btn_layout)
        left_layout.addWidget(preset_group)

        # 편집 영역
        self.edit_group = QGroupBox("편집")
        edit_layout = QVBoxLayout(self.edit_group)

        # 편집 중인 프리셋 이름
        self.edit_name_label = QLabel("프리셋을 선택하세요")
        self.edit_name_label.setStyleSheet("font-size: 13px; font-weight: bold; color: #4a90d9;")
        edit_layout.addWidget(self.edit_name_label)

        # 스크롤 영역 (스탯 입력) - 카테고리별 정리
        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll_widget = QWidget()
        self.stat_layout = QVBoxLayout(scroll_widget)

        perm_config = self.config.get('permanent', {}).get('stats', {})

        # 카테고리별 스탯 정의
        stat_categories = [
            {
                'name': '🗡️ 기본 능력',
                'color': '#ff6b6b',
                'stats': [
                    ('base_attack', '타격당 고정 데미지 추가'),
                    ('attack_percent', '최종 데미지 % 증가'),
                    ('crit_chance', '크리티컬 확률 (기본 10%)'),
                    ('crit_damage', '크리티컬 배율 (기본 2.0x)'),
                    ('multi_hit', '2회 타격 확률'),
                ]
            },
            {
                'name': '💰 재화 보너스',
                'color': '#ffd700',
                'stats': [
                    ('gold_flat_perm', '처치 시 고정 골드 추가'),
                    ('gold_multi_perm', '골드 획득량 % 증가'),
                    ('crystal_flat', '보스 처치 시 크리스탈 추가'),
                    ('crystal_multi', '크리스탈 드롭 확률 % 증가'),
                ]
            },
            {
                'name': '⚙️ 유틸리티',
                'color': '#17a2b8',
                'stats': [
                    ('time_extend', '스테이지 제한시간 증가'),
                    ('upgrade_discount', '인게임 업그레이드 비용 감소'),
                ]
            },
            {
                'name': '🚀 시작 보너스',
                'color': '#28a745',
                'stats': [
                    ('start_level', '시작 스테이지'),
                    ('start_gold', '시작 골드'),
                    ('start_keyboard', '키보드 공격력 초기값'),
                    ('start_mouse', '마우스 공격력 초기값'),
                    ('start_gold_flat', '골드+ 초기값'),
                    ('start_gold_multi', '골드* 초기값'),
                    ('start_combo_flex', '콤보 허용시간 증가'),
                    ('start_combo_damage', '콤보 데미지 보너스'),
                ]
            },
        ]

        for category in stat_categories:
            # 카테고리 헤더
            cat_label = QLabel(category['name'])
            cat_label.setStyleSheet(f"color: {category['color']}; font-weight: bold; font-size: 11px; margin-top: 8px;")
            self.stat_layout.addWidget(cat_label)

            # 스탯 그리드
            cat_grid = QGridLayout()
            cat_grid.setColumnStretch(0, 2)
            cat_grid.setColumnStretch(1, 1)

            for row_idx, (stat_id, description) in enumerate(category['stats']):
                if stat_id not in perm_config:
                    continue
                stat = perm_config[stat_id]

                # 스탯 이름 + 툴팁
                name_label = QLabel(stat.get('name', stat_id))
                name_label.setToolTip(f"{description}\n레벨당: {stat.get('effect_per_level', 1)}")
                name_label.setMinimumWidth(90)
                cat_grid.addWidget(name_label, row_idx, 0)

                # 레벨 입력
                spin = QSpinBox()
                spin.setRange(0, 200)
                spin.setValue(0)
                spin.setFixedWidth(60)
                spin.valueChanged.connect(lambda v, sid=stat_id: self._on_level_changed(sid, v))
                self.level_spinboxes[stat_id] = spin
                cat_grid.addWidget(spin, row_idx, 1)

            self.stat_layout.addLayout(cat_grid)

        self.stat_layout.addStretch()
        scroll.setWidget(scroll_widget)
        edit_layout.addWidget(scroll)

        # 저장 버튼
        save_edit_btn = QPushButton("변경 저장")
        save_edit_btn.clicked.connect(self._on_save_edit)
        save_edit_btn.setStyleSheet("background-color: #28a745;")
        edit_layout.addWidget(save_edit_btn)

        left_layout.addWidget(self.edit_group)

        # 스테이지 범위 설정
        range_group = QGroupBox("시뮬레이션 범위")
        range_layout = QGridLayout(range_group)

        range_layout.addWidget(QLabel("시작 스테이지:"), 0, 0)
        self.start_stage_spin = QSpinBox()
        self.start_stage_spin.setRange(1, 500)
        self.start_stage_spin.setValue(1)
        self.start_stage_spin.setFixedWidth(70)
        range_layout.addWidget(self.start_stage_spin, 0, 1)

        range_layout.addWidget(QLabel("종료 스테이지:"), 1, 0)
        self.end_stage_spin = QSpinBox()
        self.end_stage_spin.setRange(1, 500)
        self.end_stage_spin.setValue(50)
        self.end_stage_spin.setFixedWidth(70)
        range_layout.addWidget(self.end_stage_spin, 1, 1)

        # 스테이지 간격 (테이블용)
        range_layout.addWidget(QLabel("테이블 간격:"), 2, 0)
        self.stage_interval_spin = QSpinBox()
        self.stage_interval_spin.setRange(1, 20)
        self.stage_interval_spin.setValue(5)
        self.stage_interval_spin.setFixedWidth(70)
        range_layout.addWidget(self.stage_interval_spin, 2, 1)

        left_layout.addWidget(range_group)

        # 분석 버튼
        analyze_btn = QPushButton("분석 실행")
        analyze_btn.clicked.connect(self._analyze)
        analyze_btn.setStyleSheet("background-color: #28a745; font-size: 14px; padding: 12px;")
        left_layout.addWidget(analyze_btn)

        layout.addWidget(left)

        # 우측: 결과 영역
        right = QWidget()
        right_layout = QVBoxLayout(right)

        # DPS 카드 영역 (동적)
        self.cards_layout = QHBoxLayout()
        self.cards_container = QWidget()
        self.cards_container.setLayout(self.cards_layout)
        right_layout.addWidget(self.cards_container)

        # 그래프 영역
        self.chart = FigureCanvas(Figure(figsize=(10, 5), facecolor='#2b2b2b'))
        self.ax = self.chart.figure.add_subplot(111)
        self._style_chart()
        right_layout.addWidget(self.chart)

        # 비교 테이블
        self.compare_table = QTableWidget()
        self.compare_table.setMaximumHeight(280)
        right_layout.addWidget(self.compare_table)

        layout.addWidget(right, 2)

        # 초기 프리셋 목록 표시
        self._refresh_preset_list()

    def _style_chart(self):
        self.ax.set_facecolor('#2b2b2b')
        self.ax.tick_params(colors='#b0b0b0')
        for spine in self.ax.spines.values():
            spine.set_color('#555555')

    def _refresh_preset_list(self):
        """프리셋 목록 새로고침"""
        self.preset_list.setRowCount(len(self.presets))

        for i, (preset_id, preset) in enumerate(self.presets.items()):
            # 체크박스
            checkbox = QTableWidgetItem()
            checkbox.setFlags(Qt.ItemFlag.ItemIsUserCheckable | Qt.ItemFlag.ItemIsEnabled)
            checkbox.setCheckState(Qt.CheckState.Checked if preset_id in self.selected_preset_ids else Qt.CheckState.Unchecked)
            self.preset_list.setItem(i, 0, checkbox)

            # 이름 + 색상
            name_item = QTableWidgetItem(preset.get('name', preset_id))
            color = preset.get('color', '#4a90d9')
            name_item.setForeground(QColor(color))
            if preset.get('is_locked', False):
                name_item.setText(f"🔒 {preset.get('name', preset_id)}")
            self.preset_list.setItem(i, 1, name_item)

            # DPS 계산
            effects = self._calc_total_effect(preset.get('levels', {}))
            result = self._calc_dps(effects)
            dps_item = QTableWidgetItem(f"{result['dps']:,.0f}")
            dps_item.setForeground(QColor(color))
            self.preset_list.setItem(i, 2, dps_item)

    def _on_preset_clicked(self, row: int, col: int):
        """프리셋 클릭 처리"""
        preset_ids = list(self.presets.keys())
        if row >= len(preset_ids):
            return

        preset_id = preset_ids[row]

        if col == 0:
            # 체크박스 토글
            item = self.preset_list.item(row, 0)
            if item.checkState() == Qt.CheckState.Checked:
                self.selected_preset_ids.add(preset_id)
            else:
                self.selected_preset_ids.discard(preset_id)
        else:
            # 편집 모드 진입
            self._start_editing(preset_id)

    def _start_editing(self, preset_id: str):
        """프리셋 편집 시작"""
        if preset_id not in self.presets:
            return

        self.editing_preset_id = preset_id
        preset = self.presets[preset_id]

        # 이름 표시
        name = preset.get('name', preset_id)
        if preset.get('is_locked', False):
            self.edit_name_label.setText(f"🔒 {name} (읽기 전용)")
        else:
            self.edit_name_label.setText(f"편집: {name}")

        # 레벨 값 로드
        levels = preset.get('levels', {})
        for stat_id, spin in self.level_spinboxes.items():
            spin.blockSignals(True)
            spin.setValue(levels.get(stat_id, 0))
            spin.blockSignals(False)
            spin.setEnabled(not preset.get('is_locked', False))

    def _on_level_changed(self, stat_id: str, value: int):
        """레벨 변경 시"""
        pass  # 저장 버튼 클릭 시 반영

    def _on_save_edit(self):
        """편집 저장"""
        if not self.editing_preset_id:
            return

        if self.presets.get(self.editing_preset_id, {}).get('is_locked', False):
            QMessageBox.warning(self, "경고", "잠긴 프리셋은 수정할 수 없습니다.")
            return

        # 현재 스핀박스 값 수집
        levels = {stat_id: spin.value() for stat_id, spin in self.level_spinboxes.items()}

        if self._update_preset(self.editing_preset_id, levels=levels):
            QMessageBox.information(self, "저장", "프리셋이 저장되었습니다.")
            self._refresh_preset_list()
        else:
            QMessageBox.warning(self, "오류", "저장에 실패했습니다.")

    def _on_new_preset(self):
        """새 프리셋 생성"""
        name, ok = QInputDialog.getText(self, "새 프리셋", "프리셋 이름:")
        if ok and name:
            # 현재 편집 중인 레벨 복사
            levels = {stat_id: spin.value() for stat_id, spin in self.level_spinboxes.items()}
            preset_id = self._create_preset(name, levels)
            self._refresh_preset_list()
            self._start_editing(preset_id)

    def _on_duplicate_preset(self):
        """프리셋 복제"""
        if not self.editing_preset_id:
            QMessageBox.warning(self, "경고", "복제할 프리셋을 선택하세요.")
            return

        source = self.presets.get(self.editing_preset_id)
        if not source:
            return

        name, ok = QInputDialog.getText(self, "프리셋 복제", "새 프리셋 이름:", text=f"{source['name']} (복사)")
        if ok and name:
            preset_id = self._create_preset(name, source.get('levels', {}))
            self._refresh_preset_list()
            self._start_editing(preset_id)

    def _on_delete_preset(self):
        """프리셋 삭제"""
        if not self.editing_preset_id:
            QMessageBox.warning(self, "경고", "삭제할 프리셋을 선택하세요.")
            return

        preset = self.presets.get(self.editing_preset_id)
        if not preset:
            return

        if preset.get('is_locked', False):
            QMessageBox.warning(self, "경고", "잠긴 프리셋은 삭제할 수 없습니다.")
            return

        reply = QMessageBox.question(self, "확인", f"'{preset['name']}' 프리셋을 삭제하시겠습니까?")
        if reply == QMessageBox.StandardButton.Yes:
            self._delete_preset(self.editing_preset_id)
            self.editing_preset_id = None
            self._refresh_preset_list()
            self.edit_name_label.setText("프리셋을 선택하세요")

    # ==================== 계산 로직 ====================

    def _calc_total_effect(self, levels: dict) -> dict:
        """레벨로부터 총 효과 계산"""
        perm_config = self.config.get('permanent', {}).get('stats', {})
        effects = {}

        for stat_id, level in levels.items():
            if stat_id in perm_config:
                effect_per = perm_config[stat_id].get('effect_per_level', 1)
                effects[stat_id] = effect_per * level

        return effects

    def _calc_upgrade_cost(self, levels: dict) -> dict:
        """프리셋 달성에 필요한 총 업그레이드 횟수와 비용 계산"""
        perm_config = self.config.get('permanent', {}).get('stats', {})

        total_upgrades = 0
        total_cost = 0
        stat_costs = {}

        for stat_id, target_level in levels.items():
            if stat_id not in perm_config or target_level <= 0:
                continue

            stat = perm_config[stat_id]
            base = stat.get('base_cost', 1)
            growth = stat.get('growth_rate', 0.5)
            multi = stat.get('multiplier', 1.5)
            softcap = stat.get('softcap_interval', 10)

            # 0레벨에서 target_level까지 업그레이드
            upgrades = target_level
            cost = GameFormulas.total_cost(base, growth, multi, softcap, 0, target_level)

            total_upgrades += upgrades
            total_cost += cost
            stat_costs[stat_id] = {'upgrades': upgrades, 'cost': cost}

        return {
            'total_upgrades': total_upgrades,
            'total_cost': total_cost,
            'stat_costs': stat_costs
        }

    def _calc_dps(self, effects: dict) -> dict:
        """효과로부터 DPS 계산"""
        # 기본 공격력 (키보드 + 마우스 평균으로 계산)
        keyboard_power = 10 + effects.get('start_keyboard', 0)
        mouse_power = 10 + effects.get('start_mouse', 0)
        base_power = (keyboard_power + mouse_power) / 2  # 키보드/마우스 혼합 사용 가정

        base_attack = effects.get('base_attack', 0)
        attack_percent = effects.get('attack_percent', 0)
        crit_chance = effects.get('crit_chance', 0)
        crit_damage = effects.get('crit_damage', 0)
        multi_hit = effects.get('multi_hit', 0)

        # 콤보 관련 (시작 보너스)
        combo_damage = effects.get('start_combo_damage', 0)
        # combo_stack은 플레이 스타일에 따라 다르므로 평균 1.5로 가정
        avg_combo_stack = 1.5

        dmg = GameFormulas.calc_damage(
            int(base_power), int(base_attack), attack_percent,
            crit_chance, crit_damage, multi_hit, combo_damage, avg_combo_stack
        )

        clicks_per_sec = 5
        dps = dmg['expected'] * clicks_per_sec

        # 시간 관련
        time_extend = effects.get('time_extend', 0)
        time_limit = 30 + time_extend

        # 시작 레벨
        start_level = int(effects.get('start_level', 0))

        return {
            'damage': dmg['expected'],
            'dps': dps,
            'crit_chance': dmg['crit_chance'],
            'crit_multi': dmg['crit_multi'],
            'time_limit': time_limit,
            'start_level': start_level,
            'keyboard_power': keyboard_power,
            'mouse_power': mouse_power
        }

    # ==================== 분석 ====================

    def _analyze(self):
        """선택된 프리셋들 분석"""
        if not self.selected_preset_ids:
            QMessageBox.warning(self, "경고", "비교할 프리셋을 선택하세요.")
            return

        # 선택된 프리셋 데이터 수집
        selected_presets = []
        for preset_id in self.selected_preset_ids:
            if preset_id in self.presets:
                preset = self.presets[preset_id]
                effects = self._calc_total_effect(preset.get('levels', {}))
                result = self._calc_dps(effects)
                selected_presets.append({
                    'id': preset_id,
                    'name': preset.get('name', preset_id),
                    'color': preset.get('color', '#4a90d9'),
                    'levels': preset.get('levels', {}),
                    'effects': effects,
                    'dps': result['dps']
                })

        # DPS 카드 업데이트
        self._update_dps_cards(selected_presets)

        # 그래프 그리기
        self._draw_comparison_chart(selected_presets)

        # 테이블 업데이트
        self._update_compare_table(selected_presets)

    def _update_dps_cards(self, presets: list):
        """클릭 횟수 중심 카드 생성"""
        # 기존 카드 제거
        while self.cards_layout.count():
            item = self.cards_layout.takeAt(0)
            if item.widget():
                item.widget().deleteLater()

        # 목표 스테이지 (분석 종료 스테이지)
        target_stage = self.end_stage_spin.value()

        for preset in presets:
            card = QFrame()
            card.setStyleSheet(f"""
                QFrame {{
                    background-color: #353535;
                    border-radius: 8px;
                    border-left: 4px solid {preset['color']};
                    padding: 8px;
                }}
            """)
            card_layout = QVBoxLayout(card)
            card_layout.setSpacing(3)

            # 프리셋 이름
            name_label = QLabel(preset['name'])
            name_label.setStyleSheet(f"color: {preset['color']}; font-size: 12px; font-weight: bold;")
            card_layout.addWidget(name_label)

            # 계산 결과
            result = self._calc_dps(preset['effects'])
            damage = result['damage']
            time_limit = result['time_limit']

            # 목표 스테이지 클릭수 계산 (핵심!)
            target_hp = GameFormulas.monster_hp(target_stage)
            clicks_needed = int(target_hp / damage) if damage > 0 else 9999
            cps_needed = clicks_needed / time_limit if time_limit > 0 else 999

            # === 클릭 횟수 (가장 중요!) ===
            click_label = QLabel(f"👆 {clicks_needed:,}회 클릭")
            click_label.setStyleSheet("color: #ff6b6b; font-size: 14px; font-weight: bold;")
            card_layout.addWidget(click_label)

            # CPS 난이도 색상
            if cps_needed <= 5:
                cps_color = '#28a745'
                cps_text = '쉬움'
            elif cps_needed <= 10:
                cps_color = '#ffc107'
                cps_text = '보통'
            elif cps_needed <= 15:
                cps_color = '#fd7e14'
                cps_text = '어려움'
            else:
                cps_color = '#dc3545'
                cps_text = '불가능'

            # 필요 CPS
            cps_label = QLabel(f"⚡ {cps_needed:.1f} CPS ({cps_text})")
            cps_label.setStyleSheet(f"color: {cps_color}; font-size: 11px;")
            card_layout.addWidget(cps_label)

            # 제한시간 표시 (시간 연장 효과 포함)
            time_extend = time_limit - 30
            if time_extend > 0:
                time_label = QLabel(f"⏱️ {time_limit}초 (기본 +{time_extend}초)")
                time_label.setStyleSheet("color: #28a745; font-size: 10px;")
            else:
                time_label = QLabel(f"⏱️ {time_limit}초")
                time_label.setStyleSheet("color: #17a2b8; font-size: 10px;")
            card_layout.addWidget(time_label)

            # 구분선
            line = QFrame()
            line.setFrameShape(QFrame.Shape.HLine)
            line.setStyleSheet("color: #555555;")
            card_layout.addWidget(line)

            # 업그레이드 비용
            upgrade_info = self._calc_upgrade_cost(preset['levels'])
            cost_label = QLabel(f"💎 {upgrade_info['total_cost']:,} 크리스탈")
            cost_label.setStyleSheet("color: #17a2b8; font-size: 10px;")
            card_layout.addWidget(cost_label)

            self.cards_layout.addWidget(card)

    def _draw_comparison_chart(self, presets: list):
        """다중 프리셋 비교 그래프 (처치시간 + 필요 CPS)"""
        self.chart.figure.clear()

        # 2개의 서브플롯 생성
        self.ax1 = self.chart.figure.add_subplot(121)  # 처치 시간
        self.ax2 = self.chart.figure.add_subplot(122)  # 필요 CPS

        for ax in [self.ax1, self.ax2]:
            ax.set_facecolor('#2b2b2b')
            ax.tick_params(colors='#b0b0b0')
            for spine in ax.spines.values():
                spine.set_color('#555555')

        # 스테이지 범위 가져오기
        start_stage = self.start_stage_spin.value()
        end_stage = self.end_stage_spin.value()
        if start_stage > end_stage:
            start_stage, end_stage = end_stage, start_stage
        stages = list(range(start_stage, end_stage + 1))
        linestyles = ['-', '--', '-.', ':']
        markers = ['o', 's', '^', 'D', 'v', '<', '>', 'p']

        all_clicks = []
        all_cps = []

        for idx, preset in enumerate(presets):
            effects = preset['effects']
            result = self._calc_dps(effects)
            damage_per_hit = result['damage']
            preset_time_limit = result['time_limit']

            clicks_list = []
            cps_list = []

            for stage in stages:
                hp = GameFormulas.monster_hp(stage)

                # 필요 클릭 수 (핵심!)
                clicks = hp / damage_per_hit if damage_per_hit > 0 else 9999
                clicks_list.append(clicks)

                # 필요 CPS (프리셋별 제한시간 사용)
                cps = clicks / preset_time_limit
                cps_list.append(cps)

            all_clicks.extend(clicks_list)
            all_cps.extend(cps_list)

            # 클릭수 그래프 (핵심!)
            self.ax1.plot(
                stages, clicks_list,
                color=preset['color'],
                linestyle=linestyles[idx % len(linestyles)],
                linewidth=2,
                label=preset['name'],
                marker=markers[idx % len(markers)],
                markersize=3,
                markevery=5
            )

            # 필요 CPS 그래프
            self.ax2.plot(
                stages, cps_list,
                color=preset['color'],
                linestyle=linestyles[idx % len(linestyles)],
                linewidth=2,
                label=preset['name'],
                marker=markers[idx % len(markers)],
                markersize=3,
                markevery=5
            )

        # Y축 범위 계산 (데이터 기반 자동 스케일링)
        max_clicks = max(all_clicks) if all_clicks else 1000
        max_cps = max(all_cps) if all_cps else 20

        # 약간의 여유 추가 (10%)
        clicks_ylim = max_clicks * 1.1
        cps_ylim = max(20, max_cps * 1.1)

        # 클릭수 그래프 설정
        self.ax1.set_xlabel('Stage', color='#e0e0e0')
        self.ax1.set_ylabel('필요 클릭수', color='#e0e0e0')
        self.ax1.set_title('👆 스테이지별 필요 클릭 횟수', color='#ff6b6b', fontsize=11, fontweight='bold')
        self.ax1.legend(loc='upper left', facecolor='#353535', labelcolor='#e0e0e0', fontsize=8)
        self.ax1.set_ylim(0, clicks_ylim)
        self.ax1.grid(True, alpha=0.2, color='#555555')

        # 필요 CPS 그래프 설정
        self.ax2.axhline(y=5, color='#28a745', linestyle=':', label='쉬움 (5 CPS)', alpha=0.7)
        self.ax2.axhline(y=10, color='#ffc107', linestyle=':', label='보통 (10 CPS)', alpha=0.7)
        self.ax2.axhline(y=15, color='#ff6b6b', linestyle=':', label='어려움 (15 CPS)', alpha=0.7)
        self.ax2.set_xlabel('Stage', color='#e0e0e0')
        self.ax2.set_ylabel('필요 CPS (클릭/초)', color='#e0e0e0')
        self.ax2.set_title('⚡ 클리어에 필요한 입력 속도', color='#e0e0e0', fontsize=11)
        self.ax2.legend(loc='upper left', facecolor='#353535', labelcolor='#e0e0e0', fontsize=8)
        self.ax2.set_ylim(0, cps_ylim)
        self.ax2.grid(True, alpha=0.2, color='#555555')

        self.chart.figure.tight_layout()
        self.chart.draw()

    def _update_compare_table(self, presets: list):
        """비교 테이블 업데이트"""
        # 컬럼 설정
        headers = ["항목"] + [p['name'] for p in presets]
        self.compare_table.setColumnCount(len(headers))
        self.compare_table.setHorizontalHeaderLabels(headers)
        self.compare_table.horizontalHeader().setSectionResizeMode(QHeaderView.ResizeMode.Stretch)

        rows = []

        # 각 프리셋의 계산 결과 미리 계산
        preset_results = []
        upgrade_infos = []
        for p in presets:
            result = self._calc_dps(p['effects'])
            preset_results.append(result)
            upgrade_info = self._calc_upgrade_cost(p['levels'])
            upgrade_infos.append(upgrade_info)

        # === ⏱️ 제한시간 ===
        time_values = []
        time_colors = []
        for r in preset_results:
            time_extend = r['time_limit'] - 30
            if time_extend > 0:
                time_values.append(f"{r['time_limit']}초 (+{time_extend})")
                time_colors.append('#28a745')
            else:
                time_values.append(f"{r['time_limit']}초")
                time_colors.append('#17a2b8')
        time_row = {'name': '⏱️ 제한시간', 'values': time_values, 'colors': time_colors}
        rows.append(time_row)

        # === ⚔️ 전투력 정보 ===
        dmg_row = {'name': '👆 타격당 데미지', 'values': [f"{r['damage']:,.1f}" for r in preset_results], 'colors': ['#ff6b6b'] * len(presets)}
        rows.append(dmg_row)

        # === 💎 투자 비용 ===
        cost_row = {'name': '💎 크리스탈 비용', 'values': [f"{u['total_cost']:,}" for u in upgrade_infos], 'colors': ['#17a2b8'] * len(presets)}
        rows.append(cost_row)

        # === 스테이지별 분석 ===
        start_stage = self.start_stage_spin.value()
        end_stage = self.end_stage_spin.value()
        interval = self.stage_interval_spin.value()
        if start_stage > end_stage:
            start_stage, end_stage = end_stage, start_stage

        test_stages = list(range(start_stage, end_stage + 1, interval))
        if end_stage not in test_stages:
            test_stages.append(end_stage)

        for stage in test_stages:
            hp = GameFormulas.monster_hp(stage)
            is_boss = GameFormulas.is_boss(stage)
            stage_label = f"Stage {stage}" + (" 👹" if is_boss else "")

            clicks_row = {'name': f'{stage_label} 클릭수', 'values': [], 'colors': []}
            cps_row = {'name': f'{stage_label} 필요CPS', 'values': [], 'colors': []}

            for idx, p in enumerate(presets):
                result = preset_results[idx]
                damage = result['damage']
                time_limit = result['time_limit']  # 프리셋별 제한시간 사용

                if damage > 0:
                    clicks = hp / damage
                    cps = clicks / time_limit

                    clicks_row['values'].append(f"{clicks:,.0f}")
                    clicks_row['colors'].append(p['color'])

                    # CPS 색상 (난이도 표시)
                    if cps <= 5:
                        cps_color = '#28a745'  # 녹색 - 쉬움
                    elif cps <= 10:
                        cps_color = '#ffc107'  # 노랑 - 보통
                    elif cps <= 15:
                        cps_color = '#fd7e14'  # 주황 - 어려움
                    else:
                        cps_color = '#dc3545'  # 빨강 - 불가능

                    cps_row['values'].append(f"{cps:.1f}")
                    cps_row['colors'].append(cps_color)
                else:
                    clicks_row['values'].append("N/A")
                    clicks_row['colors'].append('#666666')
                    cps_row['values'].append("N/A")
                    cps_row['colors'].append('#666666')

            rows.append(clicks_row)
            rows.append(cps_row)

        self.compare_table.setRowCount(len(rows))
        for i, row in enumerate(rows):
            name_item = QTableWidgetItem(row['name'])
            name_item.setForeground(QColor('#e0e0e0'))
            self.compare_table.setItem(i, 0, name_item)

            for j, val in enumerate(row['values']):
                item = QTableWidgetItem(val)
                # 행별 색상이 있으면 사용, 없으면 프리셋 색상 사용
                if 'colors' in row and j < len(row['colors']):
                    item.setForeground(QColor(row['colors'][j]))
                else:
                    item.setForeground(QColor(presets[j]['color']))
                self.compare_table.setItem(i, j + 1, item)


# ============================================================
# 내장 터미널
# ============================================================

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

        self.prompt_label = QLabel("❯")
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
        self.input.setPlaceholderText("명령어 입력... (Enter로 실행, ↑↓ 히스토리)")
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

        self._append_output(f"\n❯ {cmd}\n")
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
        prompt += "### 영구 업그레이드 (PermanentStatGrowth.json)\n```json\n"
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


# ============================================================
# 메인 윈도우
# ============================================================

class BalanceDashboard(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("DeskWarrior Balance Dashboard")
        self.setMinimumSize(1200, 800)

        # QSettings 초기화
        self.settings = QSettings("DeskWarrior", "BalanceDashboard")

        # 설정 로드
        try:
            self.config = {
                'permanent': load_json('PermanentStatGrowth.json'),
                'ingame': load_json('InGameStatGrowth.json'),
                'formulas': load_json('StatFormulas.json'),
                'game': load_json('GameData.json')
            }
        except Exception as e:
            QMessageBox.critical(self, "Error", f"Config load failed: {e}")
            self.config = {'permanent': {'stats': {}}, 'ingame': {'stats': {}}}

        self._setup_ui()
        self._apply_style()
        self._restore_layout()  # 저장된 레이아웃 복원

    def _save_layout(self):
        """레이아웃 상태 저장"""
        self.settings.setValue("geometry", self.saveGeometry())
        self.settings.setValue("windowState", self.saveState())

    def _restore_layout(self):
        """저장된 레이아웃 복원"""
        geometry = self.settings.value("geometry")
        state = self.settings.value("windowState")

        if geometry:
            self.restoreGeometry(geometry)
        if state:
            self.restoreState(state)

    def _reset_layout(self):
        """레이아웃 초기화"""
        self.settings.clear()
        QMessageBox.information(self, "레이아웃 초기화",
                               "레이아웃이 초기화되었습니다.\n프로그램을 다시 시작하면 기본 레이아웃이 적용됩니다.")

    def closeEvent(self, event):
        """종료 시 레이아웃 저장"""
        self._save_layout()
        super().closeEvent(event)

    def _setup_ui(self):
        # 중앙 위젯 (스탯 편집기를 메인으로)
        self.setCentralWidget(StatEditorTab(self.config))

        # 도킹 가능한 패널들
        dock_configs = [
            ("비교 분석기", ComparisonAnalyzerTab, Qt.DockWidgetArea.LeftDockWidgetArea),
            ("스테이지 시뮬", StageSimulatorTab, Qt.DockWidgetArea.RightDockWidgetArea),
            ("DPS 계산기", DPSCalculatorTab, Qt.DockWidgetArea.RightDockWidgetArea),
            ("투자 가이드", InvestmentGuideTab, Qt.DockWidgetArea.BottomDockWidgetArea),
            ("CPS 측정기", CpsMeasureTab, Qt.DockWidgetArea.BottomDockWidgetArea),
            ("터미널", TerminalTab, Qt.DockWidgetArea.BottomDockWidgetArea),
        ]

        self.docks = {}
        for title, widget_class, area in dock_configs:
            dock = QDockWidget(title, self)
            dock.setWidget(widget_class(self.config))
            dock.setAllowedAreas(
                Qt.DockWidgetArea.LeftDockWidgetArea |
                Qt.DockWidgetArea.RightDockWidgetArea |
                Qt.DockWidgetArea.BottomDockWidgetArea |
                Qt.DockWidgetArea.TopDockWidgetArea
            )
            dock.setFeatures(
                QDockWidget.DockWidgetFeature.DockWidgetMovable |
                QDockWidget.DockWidgetFeature.DockWidgetFloatable |
                QDockWidget.DockWidgetFeature.DockWidgetClosable
            )
            self.addDockWidget(area, dock)
            self.docks[title] = dock

        # 메뉴바에 뷰 메뉴 추가 (닫은 독 다시 열기)
        view_menu = self.menuBar().addMenu("보기")
        for title, dock in self.docks.items():
            action = dock.toggleViewAction()
            view_menu.addAction(action)

        view_menu.addSeparator()
        reset_action = view_menu.addAction("🔄 레이아웃 초기화")
        reset_action.triggered.connect(self._reset_layout)

    def _apply_style(self):
        self.setStyleSheet("""
            /* 소프트 다크 테마 */
            QMainWindow, QWidget {
                background-color: #2b2b2b;
                color: #e0e0e0;
            }
            QMenuBar {
                background-color: #353535;
                color: #e0e0e0;
                border-bottom: 1px solid #555;
            }
            QMenuBar::item:selected {
                background-color: #5c9ce6;
            }
            QMenu {
                background-color: #353535;
                color: #e0e0e0;
                border: 1px solid #555;
            }
            QMenu::item:selected {
                background-color: #5c9ce6;
            }
            QDockWidget {
                titlebar-close-icon: url(close.png);
                titlebar-normal-icon: url(float.png);
                color: #e0e0e0;
                font-weight: bold;
            }
            QDockWidget::title {
                background-color: #404040;
                padding: 6px;
                border: 1px solid #555;
            }
            QDockWidget::close-button, QDockWidget::float-button {
                background-color: #505050;
                border: none;
                padding: 2px;
            }
            QDockWidget::close-button:hover, QDockWidget::float-button:hover {
                background-color: #5c9ce6;
            }
            QGroupBox {
                font-weight: bold;
                border: 1px solid #555555;
                border-radius: 6px;
                margin-top: 12px;
                padding-top: 12px;
                background-color: #353535;
            }
            QGroupBox::title {
                subcontrol-origin: margin;
                left: 12px;
                padding: 0 8px;
                color: #ffffff;
            }
            QLabel {
                color: #e0e0e0;
            }
            QLineEdit, QSpinBox, QDoubleSpinBox {
                background-color: #404040;
                border: 1px solid #606060;
                border-radius: 4px;
                padding: 6px;
                color: #ffffff;
                selection-background-color: #5c9ce6;
            }
            QLineEdit:focus, QSpinBox:focus, QDoubleSpinBox:focus {
                border: 1px solid #5c9ce6;
            }
            QTableWidget {
                background-color: #353535;
                border: 1px solid #555555;
                gridline-color: #454545;
                color: #e0e0e0;
                alternate-background-color: #3a3a3a;
            }
            QTableWidget::item {
                padding: 4px;
            }
            QTableWidget::item:selected {
                background-color: #454555;
            }
            QTableWidget::item:focus {
                background-color: #454555;
            }
            QTableWidget QAbstractItemView {
                outline: 0;
            }
            QTableWidget QLineEdit {
                background-color: transparent;
                border: none;
                padding: 0px;
                margin: -2px;
                color: #ffffff;
            }
            QHeaderView::section {
                background-color: #404040;
                padding: 8px;
                border: 1px solid #555555;
                color: #ffffff;
                font-weight: bold;
            }
            QTabWidget::pane {
                border: 1px solid #555555;
                background-color: #2b2b2b;
            }
            QTabBar::tab {
                background-color: #353535;
                padding: 10px 20px;
                margin-right: 2px;
                border-top-left-radius: 6px;
                border-top-right-radius: 6px;
                color: #b0b0b0;
            }
            QTabBar::tab:selected {
                background-color: #5c9ce6;
                color: #ffffff;
            }
            QTabBar::tab:hover:!selected {
                background-color: #454545;
            }
            QPushButton {
                background-color: #5c9ce6;
                color: white;
                padding: 8px 16px;
                border: none;
                border-radius: 4px;
                font-weight: bold;
            }
            QPushButton:hover {
                background-color: #4a8bd4;
            }
            QPushButton:pressed {
                background-color: #3a7bc4;
            }
            QScrollArea {
                border: none;
                background-color: transparent;
            }
            QScrollBar:vertical {
                background-color: #353535;
                width: 12px;
                border-radius: 6px;
            }
            QScrollBar::handle:vertical {
                background-color: #555555;
                border-radius: 6px;
                min-height: 30px;
            }
            QScrollBar::handle:vertical:hover {
                background-color: #666666;
            }
        """)


def main():
    app = QApplication(sys.argv)
    app.setStyle('Fusion')
    window = BalanceDashboard()
    window.show()
    sys.exit(app.exec())


if __name__ == '__main__':
    main()
