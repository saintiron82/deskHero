# StatEditorTab 임시 파일 - 테스트용

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

    def __init__(self, config: dict):
        super().__init__()
        self.config = config
        self._file_values = {}  # {(type, id): {param: value}} 파일 원본값
        self._current_values = {}  # {(type, id): {param: value}} 현재 편집값
        self._stat_rows = []  # [(type, id, stat_dict), ...]
        self._load_all_from_file()
        self._setup_ui()

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

        # 메인 영역
        main_layout = QHBoxLayout()

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
        self._populate_table()
        left_layout.addWidget(self.stat_table)

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

        # 변경 요약
        self.change_label = QLabel("변경 없음")
        self.change_label.setStyleSheet("color: #888; font-size: 10px;")
        left_layout.addWidget(self.change_label)

        main_layout.addWidget(left, 3)

        # === 우측: CPS 그래프 ===
        right = QGroupBox("📈 CPS 비교 (선택된 스탯)")
        right_layout = QVBoxLayout(right)

        # 시뮬 파라미터
        param_layout = QHBoxLayout()
        param_layout.addWidget(QLabel("스탯Lv:"))
        self.spin_level = QSpinBox()
        self.spin_level.setRange(1, 100)
        self.spin_level.setValue(10)
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

        # 그래프
        self.figure = Figure(figsize=(4, 3), facecolor='#1e1e2e')
        self.canvas = FigureCanvas(self.figure)
        right_layout.addWidget(self.canvas)

        # 정보
        self.info_label = QLabel("스탯을 선택하세요")
        self.info_label.setStyleSheet("color: #aaa; font-size: 10px;")
        self.info_label.setWordWrap(True)
        right_layout.addWidget(self.info_label)

        main_layout.addWidget(right, 2)
        layout.addLayout(main_layout)

        self._selected_key = None  # (type, id)

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

        # 테이블 갱신 (색상 업데이트)
        self._populate_table()

        # 그래프 갱신
        if self._selected_key == key:
            self._update_graph()

    def _on_selection_changed(self):
        """행 선택 변경"""
        row = self.stat_table.currentRow()
        if 0 <= row < len(self._stat_rows):
            stype, sid, _ = self._stat_rows[row]
            self._selected_key = (stype, sid)
            self._update_graph()

    def _update_graph(self):
        """선택된 스탯의 CPS 그래프 갱신"""
        self.figure.clear()

        if not self._selected_key:
            self.canvas.draw()
            self.info_label.setText("스탯을 선택하세요")
            return

        stype, sid = self._selected_key
        file_vals = self._file_values.get(self._selected_key, {})
        curr_vals = self._current_values.get(self._selected_key, {})

        level = self.spin_level.value()
        base_power = self.spin_power.value()
        max_stage = self.spin_stage.value()
        time_limit = 30  # 기본 제한시간

        # 효과 계산
        file_effect = file_vals.get('effect_per_level', 1) * level
        curr_effect = curr_vals.get('effect_per_level', 1) * level

        # 데미지 계산 (간단화)
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
            elif sid == 'time_extend':
                pass  # 시간에 영향
            else:
                dmg += effect * 0.5  # 기타
            return max(dmg, 1)

        def calc_time(effect):
            if sid == 'time_extend':
                return time_limit + effect
            return time_limit

        file_dmg = calc_damage(file_effect)
        curr_dmg = calc_damage(curr_effect)
        file_time = calc_time(file_effect)
        curr_time = calc_time(curr_effect)

        # CPS 계산
        stages = list(range(1, max_stage + 1))
        file_cps = []
        curr_cps = []
        for s in stages:
            hp = GameFormulas.monster_hp(s)
            file_cps.append((hp / file_dmg) / file_time)
            curr_cps.append((hp / curr_dmg) / curr_time)

        # 그래프
        ax = self.figure.add_subplot(111)
        ax.set_facecolor('#1e1e2e')

        ax.plot(stages, file_cps, color='#4a90d9', linewidth=2,
                label=f'원본 (효과:{file_effect:.1f})', linestyle='-')
        ax.plot(stages, curr_cps, color='#ff6b6b', linewidth=2,
                label=f'수정 (효과:{curr_effect:.1f})', linestyle='--')

        ax.axhline(y=10, color='#ffc107', alpha=0.5, linestyle=':', label='인간한계(10CPS)')
        ax.set_xlabel('스테이지', color='#888', fontsize=9)
        ax.set_ylabel('필요 CPS', color='#888', fontsize=9)
        ax.tick_params(colors='#888', labelsize=8)
        ax.legend(fontsize=8, facecolor='#2a2a3a', labelcolor='#ddd')
        ax.set_ylim(bottom=0)
        ax.grid(True, alpha=0.2)

        for spine in ax.spines.values():
            spine.set_color('#444')

        self.figure.tight_layout()
        self.canvas.draw()

        # 정보
        mid = max_stage // 2
        hp = GameFormulas.monster_hp(mid)
        f_cps = (hp / file_dmg) / file_time
        c_cps = (hp / curr_dmg) / curr_time
        diff = c_cps - f_cps

        self.info_label.setText(
            f"Stage {mid}: 원본 CPS={f_cps:.1f}, 수정 CPS={c_cps:.1f} "
            f"({'%.1f' % diff if diff >= 0 else '%.1f' % diff})"
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
