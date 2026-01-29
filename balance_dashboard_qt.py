"""
DeskWarrior Balance Dashboard - PyQt6 Version
실제 게임 시뮬레이션 기반 밸런스 도구
"""

import sys

from PyQt6.QtWidgets import (
    QApplication, QMainWindow, QDockWidget, QMessageBox, QWidget
)
from PyQt6.QtCore import Qt, QSettings

# 모듈화된 컴포넌트 import
from dashboard import load_json, setup_matplotlib_fonts
from dashboard.tabs import (
    StageSimulatorTab, DPSCalculatorTab, InvestmentGuideTab,
    CpsMeasureTab, TerminalTab, StatEditorTab, ComparisonAnalyzerTab
)

# matplotlib 한글 폰트 설정
setup_matplotlib_fonts()


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
        # 중앙 위젯 없이 모든 패널을 도킹 가능하게
        # (빈 위젯을 중앙에 두면 도킹 영역 확보)
        self.setCentralWidget(QWidget())

        # 도킹 가능한 패널들 (object_name은 레이아웃 저장/복원에 필수)
        dock_configs = [
            ("stat_editor", "스탯 편집기", StatEditorTab, Qt.DockWidgetArea.LeftDockWidgetArea),
            ("comparison", "비교 분석기", ComparisonAnalyzerTab, Qt.DockWidgetArea.LeftDockWidgetArea),
            ("stage_sim", "스테이지 시뮬", StageSimulatorTab, Qt.DockWidgetArea.RightDockWidgetArea),
            ("dps_calc", "DPS 계산기", DPSCalculatorTab, Qt.DockWidgetArea.RightDockWidgetArea),
            ("invest", "투자 가이드", InvestmentGuideTab, Qt.DockWidgetArea.BottomDockWidgetArea),
            ("cps", "CPS 측정기", CpsMeasureTab, Qt.DockWidgetArea.BottomDockWidgetArea),
            ("terminal", "터미널", TerminalTab, Qt.DockWidgetArea.BottomDockWidgetArea),
        ]

        self.docks = {}
        for obj_name, title, widget_class, area in dock_configs:
            dock = QDockWidget(title, self)
            dock.setObjectName(obj_name)  # 레이아웃 저장/복원에 필수
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
