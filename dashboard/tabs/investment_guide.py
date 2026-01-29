"""
투자 가이드 탭
"""

from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QGroupBox, QLabel,
    QSpinBox, QPushButton, QTableWidget, QTableWidgetItem,
    QHeaderView
)
from PyQt6.QtGui import QColor

from ..game_formulas import GameFormulas


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
