"""
스탯 편집기 탭
"""

import math

from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QGroupBox, QLabel,
    QSpinBox, QPushButton, QTableWidget, QTableWidgetItem,
    QHeaderView, QFrame, QCheckBox, QMessageBox, QSplitter
)
from PyQt6.QtCore import Qt, QSettings
from PyQt6.QtGui import QColor
from matplotlib.backends.backend_qtagg import FigureCanvasQTAgg as FigureCanvas
from matplotlib.figure import Figure

from ..game_formulas import GameFormulas
from ..config_loader import load_json, save_json


class StatEditorTab(QWidget):
    """스탯 편집: 테이블에서 직접 수정, 원본/수정값 실시간 비교"""

    # 테이블 컬럼 매핑
    COL_NAME = 0
    COL_BASE = 1
    COL_GROWTH = 2
    COL_MULTI = 3
    COL_SOFTCAP = 4
    COL_EFFECT = 5
    COL_TOTAL_COST = 6  # 예상 누적 비용
    COL_TOTAL_EFFECT = 7  # 예상 총 효과
    COL_EXPECTED_CPS = 8  # 예상 CPS
    PARAM_KEYS = ['base_cost', 'growth_rate', 'multiplier', 'softcap_interval', 'effect_per_level']

    # 파라미터 해설문 (툴팁 및 설명 패널용)
    PARAMETER_DESCRIPTIONS = {
        'base_cost': {
            'name': '초기 비용',
            'short': 'Lv1 기준 비용',
            'detail': '업그레이드의 기본 비용입니다.\n\n- 높이면: 초반 진입장벽 상승\n- 낮추면: 초반 접근성 향상\n\n공식: base_cost x (1 + level x growth_rate) x multiplier^(level/softcap)',
            'range': '1~100 권장',
            'effect': '전체 비용 스케일 조절'
        },
        'growth_rate': {
            'name': '증가율',
            'short': '레벨당 선형 증가',
            'detail': '레벨이 오를 때마다 비용이 선형으로 증가하는 비율입니다.\n\n- 높이면: 후반 비용 빠르게 증가\n- 낮추면: 레벨 간 비용 차이 감소\n\n예: growth_rate=0.5일 때 Lv10은 (1+10x0.5)=6배',
            'range': '0.3~0.7 권장',
            'effect': '중반 비용 곡선 기울기'
        },
        'multiplier': {
            'name': '급등 배수',
            'short': '지수적 증가 배수',
            'detail': '소프트캡 주기마다 비용이 급등하는 배수입니다.\n\n- 높이면 (1.5+): 후반 비용 폭발적 증가\n- 낮추면 (1.3-): 선형에 가까운 비용 증가\n\n예: multiplier=1.5, softcap=10일 때 Lv20은 1.5^2=2.25배',
            'range': '1.3~1.8 권장 (강한 제한: 1.8+)',
            'effect': '후반 하드캡 강도'
        },
        'softcap_interval': {
            'name': '급등 주기',
            'short': '급등 적용 간격',
            'detail': '급등 배수(multiplier)가 적용되는 레벨 간격입니다.\n\n- 높이면 (20+): 급등 간격 넓어짐, 완만한 곡선\n- 낮추면 (5~10): 자주 급등, 가파른 곡선\n\nCPS 균형도가 낮을 때 증가 권장',
            'range': '10~20 권장',
            'effect': '비용 곡선 가파름'
        },
        'effect_per_level': {
            'name': '레벨당 효과',
            'short': '투자 보상',
            'detail': '레벨당 얻는 스탯 효과입니다.\n\n- 높이면: 투자 효율 증가, CPS 더 많이 감소\n- 낮추면: 고레벨까지 필요\n\n비용과 함께 조절하여 효율 균형 유지',
            'range': '스탯별 상이',
            'effect': '레벨당 전투력 증가량'
        }
    }

    # 헤더 툴팁 (간단한 설명)
    HEADER_TOOLTIPS = {
        0: '스탯 이름\n=영구 스탯, =인게임 스탯',
        1: '초기 비용\nLv1 기준 비용 (초반 난이도)',
        2: '증가율\n레벨당 선형 증가 (중반 곡선)',
        3: '급등 배수\n지수적 증가 (후반 폭발)',
        4: '급등 주기\n급등 적용 간격 (곡선 가파름)',
        5: '레벨당 효과\n투자 보상 (전투력 증가량)'
    }

    # 스탯별 상세 설명 및 데미지 공식 적용 위치
    STAT_DESCRIPTIONS = {
        # === 기본 능력 (base) ===
        'base_attack': {
            'name': '기본 공격력',
            'desc': '클릭/입력당 추가되는 고정 데미지입니다.',
            'formula_part': 'base_power + base_attack',
            'formula_full': 'damage = (base_power + [base_attack]) x (1 + attack%) x crit x multi x combo',
            'effect': '가산 데미지 - 초반에 효과적, 후반에 영향력 감소',
            'tip': '초반 진행에 필수적인 스탯. 다른 배율 스탯과 곱연산됨.'
        },
        'attack_percent': {
            'name': '공격력 %',
            'desc': '총 데미지에 퍼센트 배율을 적용합니다.',
            'formula_part': 'x (1 + attack_percent / 100)',
            'formula_full': 'damage = (base_power + base_attack) x (1 + [attack%/100]) x crit x multi x combo',
            'effect': '승산 배율 - 기본 공격력이 높을수록 효과 증가',
            'tip': 'base_attack과 시너지. 100%면 2배, 200%면 3배 데미지.'
        },
        'crit_chance': {
            'name': '크리티컬 확률',
            'desc': '크리티컬 히트 발생 확률을 증가시킵니다.',
            'formula_part': 'crit_expected = 1 + crit_chance x (crit_multi - 1)',
            'formula_full': 'damage = base x attack% x [1 + crit_chance x (crit_multi-1)] x multi x combo',
            'effect': '기대값 배율 - 크리티컬 배수(crit_damage)와 시너지',
            'tip': '기본 10%, 최대 100%. crit_damage와 함께 올려야 효율적.'
        },
        'crit_damage': {
            'name': '크리티컬 데미지',
            'desc': '크리티컬 히트 시 추가 배율입니다.',
            'formula_part': 'crit_multi = BASE_CRIT_MULTI + crit_damage',
            'formula_full': 'damage = base x attack% x [1 + crit_chance x (crit_multi-1)] x multi x combo',
            'effect': '크리티컬 배수 - 기본 2.0배, 증가분 추가',
            'tip': 'crit_chance가 높을수록 효과적. 두 스탯을 균형있게 투자.'
        },
        'multi_hit': {
            'name': '멀티히트 확률',
            'desc': '한 번의 입력으로 추가 타격이 발생할 확률입니다.',
            'formula_part': 'multi_expected = 1 + multi_hit_chance / 100',
            'formula_full': 'damage = base x attack% x crit x [1 + multi_hit%/100] x combo',
            'effect': '기대 타격 횟수 - 100%면 평균 2회 타격',
            'tip': '독립적인 배율. 다른 스탯과 곱연산으로 후반에 강력.'
        },
        'combo_damage': {
            'name': '콤보 데미지',
            'desc': '콤보 스택당 추가 데미지 배율입니다.',
            'formula_part': 'combo_multi = (1 + combo_damage/100) x 2^combo_stack',
            'formula_full': 'damage = base x attack% x crit x multi x [(1+combo_dmg%) x 2^stack]',
            'effect': '콤보 배율 강화 - 기본 2^stack에 추가 배율',
            'tip': '콤보 유지가 핵심. 스택 3에서 8배 -> combo_damage로 추가 강화.'
        },

        # === 재화 보너스 (currency) ===
        'gold_flat': {
            'name': '골드 획득량 (고정)',
            'desc': '몬스터 처치 시 추가 골드를 획득합니다.',
            'formula_part': 'gold = (base_gold + [gold_flat]) x gold_multi',
            'formula_full': 'gold_earned = (stagex1.5 + [gold_flat]) x (1 + gold_multi%)',
            'effect': '가산 골드 - 저스테이지에서 효과적',
            'tip': '초반 자금 확보에 유용. 후반에는 gold_multi가 더 효율적.'
        },
        'gold_multi': {
            'name': '골드 획득량 (%)',
            'desc': '골드 획득량에 퍼센트 배율을 적용합니다.',
            'formula_part': 'x (1 + gold_multi / 100)',
            'formula_full': 'gold_earned = (stagex1.5 + gold_flat) x (1 + [gold_multi%/100])',
            'effect': '승산 골드 배율 - 고스테이지에서 효과적',
            'tip': '스테이지가 높을수록 기본 골드가 많아 효율 증가.'
        },
        'gold_flat_perm': {
            'name': '영구 골드+',
            'desc': '영구적으로 몬스터 처치 시 추가 골드를 획득합니다.',
            'formula_part': 'gold = (base_gold + [gold_flat_perm]) x gold_multi',
            'formula_full': 'gold_earned = (stagex1.5 + [gold_flat_perm]) x (1 + gold_multi%)',
            'effect': '영구 가산 골드 - 크리스탈 투자로 모든 런에 적용',
            'tip': '리셋 후에도 유지. 초반 경제 부스트에 효과적.'
        },
        'gold_multi_perm': {
            'name': '영구 골드*',
            'desc': '영구적으로 골드 획득량에 배율을 적용합니다.',
            'formula_part': 'x (1 + gold_multi_perm / 100)',
            'formula_full': 'gold_earned = (base_gold + gold_flat) x (1 + [gold_multi_perm%/100])',
            'effect': '영구 승산 골드 배율 - 후반 파밍 효율 증가',
            'tip': '고스테이지 파밍에 필수. gold_flat_perm과 시너지.'
        },
        'crystal_flat': {
            'name': '크리스탈+',
            'desc': '보스 처치 시 추가 크리스탈을 획득합니다.',
            'formula_part': 'crystal = base_crystal + [crystal_flat]',
            'formula_full': 'crystal_drop = (stage/10 + [crystal_flat]) x (1 + crystal_multi%)',
            'effect': '가산 크리스탈 - 저스테이지 보스에서 효과적',
            'tip': '영구 강화 자원. 비용이 급격히 증가하므로 효율 계산 필요.'
        },
        'crystal_multi': {
            'name': '크리스탈*',
            'desc': '크리스탈 드롭 확률을 증가시킵니다.',
            'formula_part': 'drop_chance = BASE_CHANCE + crystal_multi / 100',
            'formula_full': 'crystal_chance = 0.1 + [crystal_multi%/100] (최대 100%)',
            'effect': '드롭 확률 증가 - 기본 10%에서 추가',
            'tip': '안정적인 크리스탈 수급. 100%에 가까울수록 효율 감소.'
        },

        # === 유틸리티 (utility) ===
        'time_extend': {
            'name': '시간 연장',
            'desc': '스테이지 제한 시간을 연장합니다.',
            'formula_part': 'time_limit = BASE_TIME + time_extend',
            'formula_full': 'required_CPS = HP / damage / [time_limit + time_extend]',
            'effect': '제한 시간 증가 - 필요 CPS 직접 감소',
            'tip': '기본 30초. DPS가 아닌 "여유 시간"을 늘리는 스탯.'
        },
        'upgrade_discount': {
            'name': '업그레이드 할인',
            'desc': '인게임 골드 업그레이드 비용을 할인합니다.',
            'formula_part': 'cost = base_cost x (1 - discount/100)',
            'formula_full': 'upgrade_cost = base_cost x (1 - [upgrade_discount%/100]) (최대 90%)',
            'effect': '비용 감소 - 인게임 업그레이드 효율 증가',
            'tip': '최대 90% 할인. 급등 배수가 높아 고레벨 어려움.'
        },

        # === 시작 보너스 (starting) ===
        'start_level': {
            'name': '시작 레벨',
            'desc': '게임 시작 시 초기 레벨을 설정합니다.',
            'formula_part': 'start_level = [effect]',
            'formula_full': '게임 시작 시 레벨 {n}에서 시작',
            'effect': '초반 스킵 - 저레벨 구간 생략',
            'tip': '반복 플레이 편의성. 진행도 저장 개념.'
        },
        'start_gold': {
            'name': '시작 골드',
            'desc': '게임 시작 시 초기 골드를 지급합니다.',
            'formula_part': 'start_gold = [effect]',
            'formula_full': '게임 시작 시 골드 {n}에서 시작',
            'effect': '초반 자금 - 즉시 업그레이드 가능',
            'tip': '빠른 초반 진행. 리셋 후 바로 강화 가능.'
        },
        'start_keyboard': {
            'name': '시작 키보드',
            'desc': '게임 시작 시 키보드 공격력을 설정합니다.',
            'formula_part': 'keyboard_power = [start_keyboard]',
            'formula_full': '게임 시작 시 키보드 공격력 +{n}',
            'effect': '초반 화력 - 키보드 기본 데미지 증가',
            'tip': '인게임 keyboard_power 초기값으로 작용.'
        },
        'start_mouse': {
            'name': '시작 마우스',
            'desc': '게임 시작 시 마우스 공격력을 설정합니다.',
            'formula_part': 'mouse_power = [start_mouse]',
            'formula_full': '게임 시작 시 마우스 공격력 +{n}',
            'effect': '초반 화력 - 마우스 기본 데미지 증가',
            'tip': '인게임 mouse_power 초기값으로 작용.'
        },
        'start_gold_flat': {
            'name': '시작 골드+',
            'desc': '게임 시작 시 골드+ 스탯을 설정합니다.',
            'formula_part': 'gold_flat = [start_gold_flat]',
            'formula_full': '게임 시작 시 골드+ {n}에서 시작',
            'effect': '초반 골드 수급 - 가산 골드 기본값',
            'tip': '인게임 gold_flat 초기값으로 작용.'
        },
        'start_gold_multi': {
            'name': '시작 골드*',
            'desc': '게임 시작 시 골드* 스탯을 설정합니다.',
            'formula_part': 'gold_multi = [start_gold_multi]',
            'formula_full': '게임 시작 시 골드* +{n}%에서 시작',
            'effect': '초반 골드 배율 - 승산 골드 기본값',
            'tip': '인게임 gold_multi 초기값으로 작용.'
        },
        'start_combo_flex': {
            'name': '시작 콤보유연성',
            'desc': '게임 시작 시 콤보 유연성을 설정합니다.',
            'formula_part': 'combo_flexibility = [start_combo_flex]',
            'formula_full': '게임 시작 시 콤보유연성 +{n}',
            'effect': '콤보 유지 용이 - 콤보 끊김 허용 시간 증가',
            'tip': '콤보 유지가 어려운 플레이어에게 유용.'
        },
        'start_combo_damage': {
            'name': '시작 콤보데미지',
            'desc': '게임 시작 시 콤보 데미지를 설정합니다.',
            'formula_part': 'combo_damage = [start_combo_damage]',
            'formula_full': '게임 시작 시 콤보데미지 +{n}%에서 시작',
            'effect': '콤보 배율 기본값 - 초반부터 콤보 강화',
            'tip': '인게임 combo_damage 초기값으로 작용.'
        },

        # === 인게임 스탯 (ingame) ===
        'keyboard_power': {
            'name': '키보드 공격력',
            'desc': '키보드 입력 시 가하는 데미지입니다.',
            'formula_part': 'damage = [keyboard_power] + base_attack',
            'formula_full': 'keyboard_damage = [keyboard_power] + base_attack + ...',
            'effect': '키보드 기본 화력 - 인게임에서 골드로 업그레이드',
            'tip': 'base_attack과 합산. 키보드 위주 플레이 시 우선 투자.'
        },
        'mouse_power': {
            'name': '마우스 공격력',
            'desc': '마우스 입력 시 가하는 데미지입니다.',
            'formula_part': 'damage = [mouse_power] + base_attack',
            'formula_full': 'mouse_damage = [mouse_power] + base_attack + ...',
            'effect': '마우스 기본 화력 - 인게임에서 골드로 업그레이드',
            'tip': 'base_attack과 합산. 마우스 위주 플레이 시 우선 투자.'
        },
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

        for stype, filename in [('permanent', 'PermanentStats.json'),
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
        layout.setContentsMargins(4, 4, 4, 4)  # 여백 최소화
        layout.setSpacing(4)

        # 상단 설명
        help_label = QLabel(
            "테이블에서 직접 수정 (더블클릭) | "
            "<b style='color:#4a90d9'>파란색=저장된 값</b>, "
            "<b style='color:#ff6b6b'>빨간색=저장값→수정값</b>"
        )
        help_label.setStyleSheet("color: #aaa; font-size: 11px; padding: 2px;")
        layout.addWidget(help_label)

        # 메인 영역 (스플리터로 동적 크기 조절)
        self.splitter = QSplitter(Qt.Orientation.Horizontal)

        # === 좌측: 편집 테이블 ===
        left = QGroupBox("스탯 편집")
        left_layout = QVBoxLayout(left)
        left_layout.setContentsMargins(6, 6, 6, 6)
        left_layout.setSpacing(4)

        self.stat_table = QTableWidget()
        self.stat_table.setColumnCount(9)
        self.stat_table.setHorizontalHeaderLabels([
            "스탯명", "초기비용", "증가율", "급등배수", "급등주기", "Lv당효과",
            "Lv30 누적비용", "Lv30 총효과", "예상CPS"
        ])
        # 기본 컬럼은 Stretch, 예상값 컬럼은 고정 너비
        header = self.stat_table.horizontalHeader()
        for i in range(6):
            header.setSectionResizeMode(i, QHeaderView.ResizeMode.Stretch)
        for i in range(6, 9):
            header.setSectionResizeMode(i, QHeaderView.ResizeMode.ResizeToContents)
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

        reset_btn = QPushButton("모든 수정 취소")
        reset_btn.clicked.connect(self._reset_all)
        reset_btn.setStyleSheet("background-color: #555;")
        btn_layout.addWidget(reset_btn)

        save_btn = QPushButton("모든 변경 저장")
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
        right = QGroupBox("그래프")
        right_layout = QVBoxLayout(right)
        right_layout.setContentsMargins(6, 6, 6, 6)
        right_layout.setSpacing(2)

        # 상단 옵션 (통합 + 파라미터)
        option_layout = QHBoxLayout()
        option_layout.setSpacing(8)

        self.integrated_checkbox = QCheckBox("통합")
        self.integrated_checkbox.setChecked(False)
        self.integrated_checkbox.stateChanged.connect(self._update_graph)
        self.integrated_checkbox.setStyleSheet("color: #4a90d9; font-weight: bold;")
        option_layout.addWidget(self.integrated_checkbox)

        option_layout.addWidget(QLabel("Lv:"))
        self.spin_level = QSpinBox()
        self.spin_level.setRange(1, 100)
        self.spin_level.setValue(30)
        self.spin_level.valueChanged.connect(self._on_param_changed)
        self.spin_level.setFixedWidth(50)
        option_layout.addWidget(self.spin_level)

        option_layout.addWidget(QLabel("공격력:"))
        self.spin_power = QSpinBox()
        self.spin_power.setRange(1, 1000)
        self.spin_power.setValue(20)
        self.spin_power.valueChanged.connect(self._on_param_changed)
        self.spin_power.setFixedWidth(50)
        option_layout.addWidget(self.spin_power)

        option_layout.addWidget(QLabel("스테이지:"))
        self.spin_stage = QSpinBox()
        self.spin_stage.setRange(1, 500)
        self.spin_stage.setValue(50)
        self.spin_stage.valueChanged.connect(self._on_param_changed)
        self.spin_stage.setFixedWidth(50)
        option_layout.addWidget(self.spin_stage)

        option_layout.addStretch()
        right_layout.addLayout(option_layout)

        # 그래프 표시 체크박스들
        graph_check_layout = QHBoxLayout()
        graph_check_layout.setSpacing(4)

        self.graph_checks = {}
        graph_options = [
            ('cost', '비용'),
            ('cps_lv', 'CPS(Lv)'),
            ('cps_stage', 'CPS(St)'),
            ('cumul', '누적비용'),
            ('gold', '골드'),
            ('crystal', '크리스탈'),
            ('efficiency', '효율'),
            ('change', '변화율'),
        ]
        for key, label in graph_options:
            cb = QCheckBox(label)
            cb.setChecked(key in ['cost', 'cps_lv', 'cps_stage', 'gold'])  # 기본 선택
            cb.stateChanged.connect(self._update_graph)
            cb.setStyleSheet("font-size: 10px;")
            self.graph_checks[key] = cb
            graph_check_layout.addWidget(cb)

        graph_check_layout.addStretch()
        right_layout.addLayout(graph_check_layout)

        # 그래프 캔버스
        self.figure = Figure(figsize=(8, 6), facecolor='#1e1e2e')
        self.canvas = FigureCanvas(self.figure)
        right_layout.addWidget(self.canvas, 1)

        # 정보
        self.info_label = QLabel("스탯을 선택하세요")
        self.info_label.setStyleSheet("color: #aaa; font-size: 10px;")
        self.info_label.setWordWrap(True)
        right_layout.addWidget(self.info_label)

        self.splitter.addWidget(right)
        self.splitter.setSizes([400, 600])  # 초기 비율 (좌:우 = 40:60)
        self.splitter.setHandleWidth(6)  # 드래그 핸들 너비
        self.splitter.setObjectName("statEditorSplitter")  # 상태 저장용
        self.splitter.splitterMoved.connect(self._save_splitter_state)  # 상태 저장
        self.splitter.setChildrenCollapsible(False)  # 패널 완전 접히지 않게
        layout.addWidget(self.splitter, 1)  # stretch=1로 공간 모두 차지

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
                prefix = "[P]" if stype == 'permanent' else "[I]"
                name_item = QTableWidgetItem(f"{prefix} {stat.get('name', sid)}")
                name_item.setFlags(name_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
                self.stat_table.setItem(row, self.COL_NAME, name_item)

                # 파라미터들 (편집 가능)
                file_vals = self._file_values.get(key, {})
                curr_vals = self._current_values.get(key, {})

                for col, param in enumerate(self.PARAM_KEYS, start=1):
                    file_val = file_vals.get(param, 1)  # JSON 파일에 저장된 값
                    curr_val = curr_vals.get(param, file_val)  # 현재 편집 중인 값

                    # 저장값과 다르면 "저장값->수정값" 형식, 같으면 저장값만
                    if abs(float(file_val) - float(curr_val)) > 0.0001:
                        text = f"{file_val}→{curr_val}"
                        color = QColor("#ff6b6b")
                    else:
                        text = str(file_val)  # 저장된 값 표시
                        color = QColor("#4a90d9")

                    item = QTableWidgetItem(text)
                    item.setForeground(color)
                    self.stat_table.setItem(row, col, item)

                # 예상 값 컬럼 (읽기 전용)
                for col in [self.COL_TOTAL_COST, self.COL_TOTAL_EFFECT, self.COL_EXPECTED_CPS]:
                    item = QTableWidgetItem("-")
                    item.setFlags(item.flags() & ~Qt.ItemFlag.ItemIsEditable)
                    item.setForeground(QColor("#888"))
                    self.stat_table.setItem(row, col, item)

        self.stat_table.blockSignals(False)
        self._update_change_summary()
        self._update_predictions()  # 예상 값 업데이트

    def _update_predictions(self):
        """테이블의 예상 값 컬럼 업데이트 - 정확한 공식 기반"""
        if not hasattr(self, 'spin_level'):
            return

        target_level = self.spin_level.value()
        base_power = self.spin_power.value()  # keyboard_power + mouse_power
        max_stage = self.spin_stage.value()

        # 상수 (StatFormulas.json)
        BASE_TIME = 30
        BASE_CRIT_CHANCE = 0.1  # 10%
        BASE_CRIT_MULTI = 2.0
        BASE_GOLD_MULTI = 1.5

        # 헤더 업데이트
        self.stat_table.setHorizontalHeaderItem(self.COL_TOTAL_COST,
            QTableWidgetItem(f"Lv{target_level} 비용"))
        self.stat_table.setHorizontalHeaderItem(self.COL_TOTAL_EFFECT,
            QTableWidgetItem(f"Lv{target_level} 효과"))
        self.stat_table.setHorizontalHeaderItem(self.COL_EXPECTED_CPS,
            QTableWidgetItem(f"St{max_stage} 예상"))

        self.stat_table.blockSignals(True)

        for row, (stype, sid, stat) in enumerate(self._stat_rows):
            key = (stype, sid)
            curr_vals = self._current_values.get(key, {})

            # 누적 비용 계산
            total_cost = sum(
                self._calc_upgrade_cost(
                    curr_vals.get('base_cost', 1),
                    curr_vals.get('growth_rate', 0.5),
                    curr_vals.get('multiplier', 1.5),
                    curr_vals.get('softcap_interval', 10),
                    lv
                ) for lv in range(1, target_level + 1)
            )

            # 총 효과 = effect_per_level × level
            effect_per_lv = curr_vals.get('effect_per_level', 1)
            total_effect = effect_per_lv * target_level

            # 기본 데미지 공식: (base_power + base_attack) × (1 + attack%) × crit_expected × multi_expected
            hp = GameFormulas.monster_hp(max_stage)
            base_gold = max_stage * BASE_GOLD_MULTI

            # 스탯별 효과 및 예상값 계산
            effect_text = ""
            result_text = ""
            result_color = QColor("#888")

            if sid == 'base_attack':
                # 가산 데미지: base_power + effect
                dmg = base_power + total_effect
                cps = hp / dmg / BASE_TIME
                effect_text = f"+{total_effect:.0f} DMG"
                result_text = f"CPS {cps:.1f}"
                result_color = self._cps_color(cps)

            elif sid == 'attack_percent':
                # 배수 데미지: base_power × (1 + effect/100)
                multiplier = 1 + total_effect / 100
                dmg = base_power * multiplier
                cps = hp / dmg / BASE_TIME
                effect_text = f"×{multiplier:.2f}"
                result_text = f"CPS {cps:.1f}"
                result_color = self._cps_color(cps)

            elif sid == 'crit_chance':
                # 크리티컬 확률: 기대값 = 1 + crit_chance × (crit_multi - 1)
                crit_chance = min(BASE_CRIT_CHANCE + total_effect / 100, 1.0)
                crit_expected = 1 + crit_chance * (BASE_CRIT_MULTI - 1)
                dmg = base_power * crit_expected
                cps = hp / dmg / BASE_TIME
                effect_text = f"{crit_chance*100:.0f}% 크리"
                result_text = f"×{crit_expected:.2f}"
                result_color = QColor("#ff6b6b")

            elif sid == 'crit_damage':
                # 크리티컬 배율: 기대값 = 1 + base_crit_chance × (crit_multi - 1)
                crit_multi = BASE_CRIT_MULTI + total_effect
                crit_expected = 1 + BASE_CRIT_CHANCE * (crit_multi - 1)
                dmg = base_power * crit_expected
                cps = hp / dmg / BASE_TIME
                effect_text = f"×{crit_multi:.1f} 크리"
                result_text = f"기대×{crit_expected:.2f}"
                result_color = QColor("#ff6b6b")

            elif sid == 'multi_hit':
                # 멀티히트: 기대 타격수 = 1 + effect/100
                multi_expected = 1 + total_effect / 100
                dmg = base_power * multi_expected
                cps = hp / dmg / BASE_TIME
                effect_text = f"{total_effect:.0f}% 더블"
                result_text = f"×{multi_expected:.2f}"
                result_color = QColor("#17a2b8")

            elif sid == 'time_extend':
                # 시간 연장: 필요 CPS 감소
                extended_time = BASE_TIME + total_effect
                cps = hp / base_power / extended_time
                effect_text = f"+{total_effect:.0f}초"
                result_text = f"CPS {cps:.1f}"
                result_color = self._cps_color(cps)

            elif sid in ['gold_flat_perm', 'gold_flat']:
                # 골드 가산
                gold = (base_gold + total_effect) * 1.0
                effect_text = f"+{total_effect:.0f}G"
                result_text = f"G:{gold:.0f}/킬"
                result_color = QColor("#ffc107")

            elif sid in ['gold_multi_perm', 'gold_multi']:
                # 골드 배수
                gold_multi = 1 + total_effect / 100
                gold = base_gold * gold_multi
                effect_text = f"×{gold_multi:.2f}G"
                result_text = f"G:{gold:.0f}/킬"
                result_color = QColor("#ffc107")

            elif sid == 'crystal_flat':
                # 크리스탈 드롭량
                effect_text = f"+{total_effect:.0f}크리"
                result_text = f"보스+{total_effect:.0f}"
                result_color = QColor("#17a2b8")

            elif sid == 'crystal_multi':
                # 크리스탈 드롭 확률
                drop_chance = min(0.1 + total_effect / 100, 1.0)  # 기본 10%
                effect_text = f"+{total_effect:.0f}%"
                result_text = f"확률{drop_chance*100:.0f}%"
                result_color = QColor("#17a2b8")

            elif sid == 'upgrade_discount':
                # 업그레이드 할인
                discount = min(total_effect, 90)  # 최대 90%
                effect_text = f"-{discount:.0f}%"
                result_text = f"비용×{1-discount/100:.2f}"
                result_color = QColor("#28a745")

            elif sid.startswith('start_'):
                # 시작 보너스 (CPS에 영향 없음)
                if 'gold' in sid:
                    effect_text = f"+{total_effect:.0f}G"
                    result_text = "시작보너스"
                elif 'level' in sid:
                    effect_text = f"Lv{total_effect:.0f}"
                    result_text = "시작보너스"
                else:
                    effect_text = f"+{total_effect:.0f}"
                    result_text = "시작보너스"
                result_color = QColor("#888")

            elif sid in ['keyboard_power', 'mouse_power']:
                # 인게임 공격력 (base_power에 포함)
                dmg = base_power + total_effect
                cps = hp / dmg / BASE_TIME
                effect_text = f"+{total_effect:.0f} DMG"
                result_text = f"CPS {cps:.1f}"
                result_color = self._cps_color(cps)

            else:
                # 기타 스탯
                effect_text = f"+{total_effect:.1f}"
                result_text = "-"
                result_color = QColor("#888")

            # 테이블 업데이트
            cost_item = self.stat_table.item(row, self.COL_TOTAL_COST)
            if cost_item:
                cost_item.setText(f"{total_cost:,.0f}")
                cost_item.setForeground(QColor("#ffc107"))

            effect_item = self.stat_table.item(row, self.COL_TOTAL_EFFECT)
            if effect_item:
                effect_item.setText(effect_text)
                effect_item.setForeground(QColor("#4ad94a"))

            result_item = self.stat_table.item(row, self.COL_EXPECTED_CPS)
            if result_item:
                result_item.setText(result_text)
                result_item.setForeground(result_color)

        self.stat_table.blockSignals(False)

    def _cps_color(self, cps: float) -> QColor:
        """CPS 값에 따른 색상 반환"""
        if cps > 15:
            return QColor("#ff4444")  # 입력 한계 초과
        elif cps > 8:
            return QColor("#ffc107")  # 어려움
        elif cps > 5:
            return QColor("#4a90d9")  # 도전적
        else:
            return QColor("#4ad94a")  # 여유

    def _on_param_changed(self):
        """파라미터 변경 시 예상값과 그래프 모두 업데이트"""
        self._update_predictions()
        self._update_graph()

    def _on_cell_changed(self, row, col):
        """셀 편집 시 호출"""
        # 편집 가능 컬럼: 1~5 (PARAM_KEYS)
        if col == 0 or col > len(self.PARAM_KEYS) or row >= len(self._stat_rows):
            return

        stype, sid, _ = self._stat_rows[row]
        key = (stype, sid)
        param = self.PARAM_KEYS[col - 1]

        # 값 파싱 (-> 포함 시 뒤의 값만)
        item = self.stat_table.item(row, col)
        text = item.text()
        if '->' in text:
            text = text.split('->')[-1]

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

        # 그래프 갱신
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
        """업그레이드 비용 계산: cost = base x (1 + level x growth_rate) x multiplier^(level / softcap_interval)"""
        cost = base_cost * (1 + level * growth_rate) * (multiplier ** (level / softcap_interval))
        return cost

    def _update_param_description(self, row: int, col: int):
        """파라미터/스탯 설명 패널 업데이트"""
        # 스탯 이름 열(col 0) 클릭 시 - 스탯 설명
        if col == 0 and 0 <= row < len(self._stat_rows):
            stype, sid, stat = self._stat_rows[row]
            if sid in self.STAT_DESCRIPTIONS:
                desc = self.STAT_DESCRIPTIONS[sid]
                self.desc_title.setText(f"{desc['name']} ({sid})")
                detail_text = (
                    f"{desc['desc']}\n\n"
                    f"공식 적용: {desc['formula_part']}\n"
                    f"효과: {desc['effect']}\n\n"
                    f"팁: {desc['tip']}"
                )
                self.desc_detail.setText(detail_text)
                self.desc_range.setText(f"전체 공식: {desc['formula_full']}")
            else:
                self.desc_title.setText(f"{stat.get('name', sid)}")
                self.desc_detail.setText("상세 설명이 등록되지 않은 스탯입니다.")
                self.desc_range.setText("")
            return

        # 파라미터 열 클릭 시 - 파라미터 설명
        param_key = self.PARAM_KEYS[col - 1] if 1 <= col <= len(self.PARAM_KEYS) else None
        if param_key and param_key in self.PARAMETER_DESCRIPTIONS:
            desc = self.PARAMETER_DESCRIPTIONS[param_key]
            self.desc_title.setText(f"{desc['name']} ({param_key})")
            self.desc_detail.setText(desc['detail'])
            self.desc_range.setText(f"권장 범위: {desc['range']}")
        else:
            self.desc_title.setText("스탯 또는 파라미터를 선택하세요")
            self.desc_detail.setText("테이블의 스탯 이름을 클릭하면 해당 스탯의 상세 설명과 데미지 공식 적용 위치가 표시됩니다.")
            self.desc_range.setText("")

    def _update_graph(self):
        """그래프 갱신 - 체크된 그래프만 동적 그리드로 표시"""
        self.figure.clear()

        # 체크된 그래프 목록
        checked = [k for k, cb in self.graph_checks.items() if cb.isChecked()]
        if not checked:
            self.canvas.draw()
            self.info_label.setText("표시할 그래프를 선택하세요")
            return

        # 그리드 크기 계산
        n = len(checked)
        if n <= 2:
            rows, cols = 1, n
        elif n <= 4:
            rows, cols = 2, 2
        elif n <= 6:
            rows, cols = 2, 3
        else:
            rows, cols = 2, 4

        max_level = self.spin_level.value()
        base_power = self.spin_power.value()
        max_stage = self.spin_stage.value()
        time_limit = 30
        levels = list(range(1, max_level + 1))
        stages = list(range(1, min(max_stage + 1, 101)))
        use_integrated = self.integrated_checkbox.isChecked()

        # 축 스타일
        def setup_ax(ax):
            ax.set_facecolor('#1e1e2e')
            ax.tick_params(colors='#888', labelsize=7)
            for spine in ax.spines.values():
                spine.set_color('#444')

        # 서브플롯 생성
        axes_grid = self.figure.subplots(rows, cols, squeeze=False)
        axes_list = [axes_grid[i // cols, i % cols] for i in range(n)]
        for ax in axes_list:
            setup_ax(ax)
        # 남는 칸 숨기기
        for i in range(n, rows * cols):
            axes_grid[i // cols, i % cols].set_visible(False)

        # === 데이터 계산 ===
        if use_integrated:
            # 통합 모드
            def calc_total(vals_dict, level):
                dmg = base_power
                extra_time = 0
                crit_ch, crit_m = 0.1, 2.0
                gold_flat, gold_multi = 0, 0
                for (st, sid), vals in vals_dict.items():
                    eff = vals.get('effect_per_level', 1) * level
                    if sid == 'base_attack': dmg += eff
                    elif sid == 'attack_percent': dmg *= (1 + eff / 100)
                    elif sid == 'crit_chance': crit_ch = min(0.1 + eff/100, 1.0)
                    elif sid == 'crit_damage': crit_m = 2.0 + eff
                    elif sid == 'multi_hit': dmg *= (1 + eff / 100)
                    elif sid == 'time_extend': extra_time = eff
                    elif sid == 'gold_flat': gold_flat = eff
                    elif sid == 'gold_multi': gold_multi = eff
                dmg *= (1 + crit_ch * (crit_m - 1))
                return max(dmg, 1), time_limit + extra_time, gold_flat, gold_multi

            file_data = [calc_total(self._file_values, lv) for lv in levels]
            curr_data = [calc_total(self._current_values, lv) for lv in levels]
            file_dmg = [d[0] for d in file_data]
            curr_dmg = [d[0] for d in curr_data]
            file_time = [d[1] for d in file_data]
            curr_time = [d[1] for d in curr_data]
            file_gflat = file_data[-1][2]
            file_gmulti = file_data[-1][3]
            curr_gflat = curr_data[-1][2]
            curr_gmulti = curr_data[-1][3]

            file_costs = [sum(self._calc_upgrade_cost(v.get('base_cost',1), v.get('growth_rate',0.5),
                         v.get('multiplier',1.5), v.get('softcap_interval',10), lv)
                         for v in self._file_values.values()) for lv in levels]
            curr_costs = [sum(self._calc_upgrade_cost(v.get('base_cost',1), v.get('growth_rate',0.5),
                         v.get('multiplier',1.5), v.get('softcap_interval',10), lv)
                         for v in self._current_values.values()) for lv in levels]

            file_cps_lv = [GameFormulas.monster_hp(max_stage) / d / t for d, t in zip(file_dmg, file_time)]
            curr_cps_lv = [GameFormulas.monster_hp(max_stage) / d / t for d, t in zip(curr_dmg, curr_time)]

            label_prefix = "[통합]"
            sid = "all"
        else:
            # 개별 모드
            if not self._selected_key:
                for ax in axes_list:
                    ax.text(0.5, 0.5, '스탯 선택', ha='center', va='center', color='#666', fontsize=10, transform=ax.transAxes)
                self.figure.tight_layout()
                self.canvas.draw()
                self.info_label.setText("테이블에서 스탯을 선택하세요")
                return

            stype, sid = self._selected_key
            file_vals = self._file_values.get(self._selected_key, {})
            curr_vals = self._current_values.get(self._selected_key, {})

            file_costs = [self._calc_upgrade_cost(file_vals.get('base_cost', 1), file_vals.get('growth_rate', 0.5),
                         file_vals.get('multiplier', 1.5), file_vals.get('softcap_interval', 10), lv) for lv in levels]
            curr_costs = [self._calc_upgrade_cost(curr_vals.get('base_cost', 1), curr_vals.get('growth_rate', 0.5),
                         curr_vals.get('multiplier', 1.5), curr_vals.get('softcap_interval', 10), lv) for lv in levels]

            def calc_single(vals, lv):
                eff = vals.get('effect_per_level', 1) * lv
                dmg, t = base_power, time_limit
                if sid == 'base_attack': dmg += eff
                elif sid == 'attack_percent': dmg *= (1 + eff / 100)
                elif sid == 'crit_chance': dmg *= (1 + min(0.1 + eff/100, 1.0))
                elif sid == 'multi_hit': dmg *= (1 + eff / 100)
                elif sid == 'time_extend': t += eff
                else: dmg += eff * 0.5
                return max(dmg, 1), t

            file_data = [calc_single(file_vals, lv) for lv in levels]
            curr_data = [calc_single(curr_vals, lv) for lv in levels]
            file_dmg = [d[0] for d in file_data]
            curr_dmg = [d[0] for d in curr_data]
            file_time = [d[1] for d in file_data]
            curr_time = [d[1] for d in curr_data]

            file_cps_lv = [GameFormulas.monster_hp(max_stage) / d / t for d, t in zip(file_dmg, file_time)]
            curr_cps_lv = [GameFormulas.monster_hp(max_stage) / d / t for d, t in zip(curr_dmg, curr_time)]

            file_eff = file_vals.get('effect_per_level', 1) * max_level
            curr_eff = curr_vals.get('effect_per_level', 1) * max_level
            file_gflat = file_eff if sid == 'gold_flat' else 0
            file_gmulti = file_eff if sid == 'gold_multi' else 0
            curr_gflat = curr_eff if sid == 'gold_flat' else 0
            curr_gmulti = curr_eff if sid == 'gold_multi' else 0

            label_prefix = f"[{sid}]"

        # 공통 데이터
        file_cumul = [sum(file_costs[:i+1]) for i in range(len(file_costs))]
        curr_cumul = [sum(curr_costs[:i+1]) for i in range(len(curr_costs))]

        f_dmg_final, f_time_final = file_dmg[-1], file_time[-1]
        c_dmg_final, c_time_final = curr_dmg[-1], curr_time[-1]
        file_cps_stage = [GameFormulas.monster_hp(s) / f_dmg_final / f_time_final for s in stages]
        curr_cps_stage = [GameFormulas.monster_hp(s) / c_dmg_final / c_time_final for s in stages]

        # 골드/크리스탈
        file_gold = [(s * 1.5 + file_gflat) * (1 + file_gmulti / 100) for s in stages]
        curr_gold = [(s * 1.5 + curr_gflat) * (1 + curr_gmulti / 100) for s in stages]
        file_gold_cumul = [sum(file_gold[:i+1]) for i in range(len(file_gold))]
        curr_gold_cumul = [sum(curr_gold[:i+1]) for i in range(len(curr_gold))]
        # 크리스탈 = 스테이지 클리어 보상(1💎/stage) + 골드 변환(1000G = 1💎)
        file_crystal = [s + g / 1000 for s, g in zip(stages, file_gold_cumul)]
        curr_crystal = [s + g / 1000 for s, g in zip(stages, curr_gold_cumul)]

        # === 그래프 그리기 ===
        ax_idx = 0
        for key in checked:
            if ax_idx >= len(axes_list):
                break
            ax = axes_list[ax_idx]
            ax_idx += 1

            if key == 'cost':
                ax.plot(levels, file_costs, color='#4a90d9', linewidth=1.5, label='저장')
                ax.plot(levels, curr_costs, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
                ax.set_title('업그레이드 비용', color='#ddd', fontsize=9)
                ax.legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')

            elif key == 'cps_lv':
                ax.plot(levels, file_cps_lv, color='#4a90d9', linewidth=1.5, label='저장')
                ax.plot(levels, curr_cps_lv, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
                ax.axhline(y=15, color='#ff4444', alpha=0.5, linestyle=':', linewidth=1)
                ax.axhline(y=5, color='#ffc107', alpha=0.5, linestyle=':', linewidth=1)
                ax.set_title(f'필요 CPS (Stage {max_stage})', color='#ddd', fontsize=9)
                ax.legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')

            elif key == 'cps_stage':
                ax.plot(stages, file_cps_stage, color='#4a90d9', linewidth=1.5, label='저장')
                ax.plot(stages, curr_cps_stage, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
                ax.axhline(y=15, color='#ff4444', alpha=0.5, linestyle=':', linewidth=1)
                ax.axhline(y=5, color='#ffc107', alpha=0.5, linestyle=':', linewidth=1)
                ax.set_title(f'CPS vs 스테이지 (Lv{max_level})', color='#ddd', fontsize=9)
                ax.legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')

            elif key == 'cumul':
                ax.plot(levels, file_cumul, color='#4a90d9', linewidth=1.5, label='저장')
                ax.plot(levels, curr_cumul, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
                ax.set_title('누적 비용', color='#ddd', fontsize=9)
                ax.legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')

            elif key == 'gold':
                ax.plot(stages, file_gold, color='#ffc107', linewidth=1.5, label='저장')
                ax.plot(stages, curr_gold, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
                ax.set_title('예상 골드 (스테이지별)', color='#ddd', fontsize=9)
                ax.legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')

            elif key == 'crystal':
                ax.plot(stages, file_crystal, color='#17a2b8', linewidth=1.5, label='저장')
                ax.plot(stages, curr_crystal, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
                ax.set_title('예상 크리스탈 (누적)', color='#ddd', fontsize=9)
                ax.legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')

            elif key == 'efficiency':
                eff_file = [d / max(c, 1) for d, c in zip(file_dmg, file_cumul)]
                eff_curr = [d / max(c, 1) for d, c in zip(curr_dmg, curr_cumul)]
                ax.plot(levels, eff_file, color='#28a745', linewidth=1.5, label='저장')
                ax.plot(levels, eff_curr, color='#ff6b6b', linewidth=1.5, label='수정', linestyle='--')
                ax.set_title('비용 효율 (DMG/Cost)', color='#ddd', fontsize=9)
                ax.legend(fontsize=6, facecolor='#2a2a3a', labelcolor='#ddd')

            elif key == 'change':
                cps_pct = [(f - c) / max(f, 0.01) * 100 for f, c in zip(file_cps_lv, curr_cps_lv)]
                ax.plot(levels, cps_pct, color='#28a745', linewidth=1.5)
                ax.axhline(y=0, color='#888', linestyle=':', alpha=0.5)
                ax.fill_between(levels, 0, cps_pct, alpha=0.3,
                               color='#28a745' if cps_pct[-1] >= 0 else '#ff6b6b')
                ax.set_title('CPS 감소율 (%) ↑좋음', color='#ddd', fontsize=9)

            ax.grid(True, alpha=0.2)

        # 정보 라벨
        cost_diff = ((curr_cumul[-1] - file_cumul[-1]) / max(file_cumul[-1], 1) * 100)
        self.info_label.setText(f"{label_prefix} Lv{max_level} 비용: {file_cumul[-1]:.0f}→{curr_cumul[-1]:.0f} "
                               f"({cost_diff:+.1f}%) | CPS: {file_cps_lv[-1]:.2f}→{curr_cps_lv[-1]:.2f}")

        self.figure.tight_layout()
        self.canvas.draw()

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
            self.change_label.setText(f"{len(changes)}개 변경: {', '.join(changes[:5])}{'...' if len(changes) > 5 else ''}")
            self.change_label.setStyleSheet("color: #ffc107; font-size: 10px;")
        else:
            self.change_label.setText("변경 없음")
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
            save_json('PermanentStats.json', self.config['permanent'])
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
