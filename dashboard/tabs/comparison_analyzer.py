"""
비교 분석기 탭
"""

from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QGroupBox, QLabel,
    QSpinBox, QPushButton, QTableWidget, QTableWidgetItem,
    QHeaderView, QFrame, QScrollArea, QGridLayout, QMessageBox,
    QInputDialog
)
from PyQt6.QtCore import Qt
from PyQt6.QtGui import QColor
from matplotlib.backends.backend_qtagg import FigureCanvasQTAgg as FigureCanvas
from matplotlib.figure import Figure

from ..game_formulas import GameFormulas
from ..config_loader import load_json, save_json


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
            lock_label = QLabel("[L]")
            lock_label.setStyleSheet("font-size: 10px;")
            layout.addWidget(lock_label)


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
                'name': '기본 능력',
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
                'name': '재화 보너스',
                'color': '#ffd700',
                'stats': [
                    ('gold_flat_perm', '처치 시 고정 골드 추가'),
                    ('gold_multi_perm', '골드 획득량 % 증가'),
                    ('crystal_flat', '보스 처치 시 크리스탈 추가'),
                    ('crystal_multi', '크리스탈 드롭 확률 % 증가'),
                ]
            },
            {
                'name': '유틸리티',
                'color': '#17a2b8',
                'stats': [
                    ('time_extend', '스테이지 제한시간 증가'),
                    ('upgrade_discount', '인게임 업그레이드 비용 감소'),
                ]
            },
            {
                'name': '시작 보너스',
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
                name_item.setText(f"[L] {preset.get('name', preset_id)}")
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
            self.edit_name_label.setText(f"[L] {name} (읽기 전용)")
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
            click_label = QLabel(f"{clicks_needed:,}회 클릭")
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
            cps_label = QLabel(f"{cps_needed:.1f} CPS ({cps_text})")
            cps_label.setStyleSheet(f"color: {cps_color}; font-size: 11px;")
            card_layout.addWidget(cps_label)

            # 제한시간 표시 (시간 연장 효과 포함)
            time_extend = time_limit - 30
            if time_extend > 0:
                time_label = QLabel(f"{time_limit}초 (기본 +{time_extend}초)")
                time_label.setStyleSheet("color: #28a745; font-size: 10px;")
            else:
                time_label = QLabel(f"{time_limit}초")
                time_label.setStyleSheet("color: #17a2b8; font-size: 10px;")
            card_layout.addWidget(time_label)

            # 구분선
            line = QFrame()
            line.setFrameShape(QFrame.Shape.HLine)
            line.setStyleSheet("color: #555555;")
            card_layout.addWidget(line)

            # 업그레이드 비용
            upgrade_info = self._calc_upgrade_cost(preset['levels'])
            cost_label = QLabel(f"{upgrade_info['total_cost']:,} 크리스탈")
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
        self.ax1.set_title('스테이지별 필요 클릭 횟수', color='#ff6b6b', fontsize=11, fontweight='bold')
        self.ax1.legend(loc='upper left', facecolor='#353535', labelcolor='#e0e0e0', fontsize=8)
        self.ax1.set_ylim(0, clicks_ylim)
        self.ax1.grid(True, alpha=0.2, color='#555555')

        # 필요 CPS 그래프 설정
        self.ax2.axhline(y=5, color='#28a745', linestyle=':', label='쉬움 (5 CPS)', alpha=0.7)
        self.ax2.axhline(y=10, color='#ffc107', linestyle=':', label='보통 (10 CPS)', alpha=0.7)
        self.ax2.axhline(y=15, color='#ff6b6b', linestyle=':', label='어려움 (15 CPS)', alpha=0.7)
        self.ax2.set_xlabel('Stage', color='#e0e0e0')
        self.ax2.set_ylabel('필요 CPS (클릭/초)', color='#e0e0e0')
        self.ax2.set_title('클리어에 필요한 입력 속도', color='#e0e0e0', fontsize=11)
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

        # === 제한시간 ===
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
        time_row = {'name': '제한시간', 'values': time_values, 'colors': time_colors}
        rows.append(time_row)

        # === 전투력 정보 ===
        dmg_row = {'name': '타격당 데미지', 'values': [f"{r['damage']:,.1f}" for r in preset_results], 'colors': ['#ff6b6b'] * len(presets)}
        rows.append(dmg_row)

        # === 투자 비용 ===
        cost_row = {'name': '크리스탈 비용', 'values': [f"{u['total_cost']:,}" for u in upgrade_infos], 'colors': ['#17a2b8'] * len(presets)}
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
            stage_label = f"Stage {stage}" + (" BOSS" if is_boss else "")

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
