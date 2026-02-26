#!/usr/bin/env python3
"""
DeskWarrior Sprite Manager
배치 단위로 원본(Org)과 프로덕션(Production) 이미지를 비교하고 처리하는 GUI 도구

Usage:
    python tools/sprite_manager.py
"""

import os
import sys
import re
import subprocess
from pathlib import Path
from typing import Optional

try:
    from PyQt6.QtWidgets import (
        QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
        QLabel, QComboBox, QScrollArea, QGridLayout, QPushButton,
        QCheckBox, QGroupBox, QProgressBar, QMessageBox, QSplitter, QFrame,
        QStatusBar, QMenu,
    )
    from PyQt6.QtCore import Qt, QThread, pyqtSignal
    from PyQt6.QtGui import QPixmap, QImage, QPainter, QColor
except ImportError:
    print("Error: PyQt6 is not installed. Run: pip install PyQt6")
    sys.exit(1)

try:
    from PIL import Image
except ImportError:
    print("Error: Pillow is not installed. Run: pip install Pillow")
    sys.exit(1)


# ============================================================================
# Path Configuration
# ============================================================================

SCRIPT_DIR = Path(__file__).parent.absolute()
PROJECT_ROOT = SCRIPT_DIR.parent
ORG_BASE = PROJECT_ROOT / "Resources" / "Org"
PRODUCTION_BASE = PROJECT_ROOT / "Assets" / "Images" / "Production"
DOCS_DIR = PROJECT_ROOT / "docs"

DOCS_MAP = {
    "기획 문서":   DOCS_DIR / "monster_planning.md",
    "진행 현황":   DOCS_DIR / "monster_progress.md",
    "구조 가이드": DOCS_DIR / "BATCH_IMAGE_STRUCTURE_GUIDE.md",
}

PRODUCTION_SIZE = 256
BG_PRODUCTION_SIZE = 512   # 배경은 512x512
THUMB_SIZE = 96
CARD_W = 130
CARD_H = 170
GRID_COLS = 4

BATCH_MAP = {
    "Batch 1": ("Batch_01", "Batch1"),
    "Batch 2": ("Batch_02", "Batch2"),
    "Batch 3": ("Batch_03", "Batch3"),
    "Batch 4": ("Batch_04", "Batch4"),
    "Batch 5": ("Batch_05", "Batch5"),
    "Batch 6": ("Batch_06", "Batch6"),
    "Batch 7": ("Batch_07", "Batch7"),
    "Batch 8": ("Batch_08", "Batch8"),
    "Batch 9": ("Batch_09", "Batch9"),
}


# ============================================================================
# Batch JSON Data Reader (Single Source of Truth)
# ============================================================================

import json

MONSTERS_DIR = PROJECT_ROOT / "config" / "monsters"


def parse_batch_data() -> dict:
    """Read config/monsters/_index.json + batch_XX.json -> {batch_id: [(no, species_id, sprite_type, display_name)]}"""
    result = {}
    index_file = MONSTERS_DIR / "_index.json"
    if not index_file.exists():
        return result

    with open(index_file, "r", encoding="utf-8") as f:
        index_data = json.load(f)

    global_no = 0

    for batch_info in index_data.get("batches", []):
        batch_id = batch_info["batch_id"]
        batch_file = MONSTERS_DIR / batch_info["file"]
        if not batch_file.exists():
            continue

        with open(batch_file, "r", encoding="utf-8") as f:
            batch_data = json.load(f)

        entries = []
        for m in batch_data.get("monsters", []):
            global_no += 1
            species_id = m["species"]
            is_boss = m.get("is_boss", False)
            sprite_type = "Boss" if is_boss else "Monster"

            # Get Korean display name from normal variation
            display_name = species_id
            variations = m.get("variations", {})
            normal_var = variations.get("normal", {})
            loc = normal_var.get("localization", {})
            ko = loc.get("ko-KR", {})
            if ko.get("name"):
                display_name = ko["name"]

            entries.append((global_no, species_id, sprite_type, display_name))

        result[batch_id] = entries

    return result


_batch_data_cache = None


def get_planning_data() -> dict:
    """Read monster data from batch JSON files (single source of truth)."""
    global _batch_data_cache
    if _batch_data_cache is None:
        _batch_data_cache = parse_batch_data()
    return _batch_data_cache


def invalidate_planning_cache():
    global _batch_data_cache
    _batch_data_cache = None


# ============================================================================
# Progress Document Parser (재생성 필요 상태)
# ============================================================================

def parse_progress_redo() -> set:
    """Parse progress.md -> set of plan_no where 상태 column contains 재생성필요"""
    redo_nos = set()
    progress_file = DOCS_DIR / "monster_progress.md"
    if not progress_file.exists():
        return redo_nos

    text = progress_file.read_text(encoding="utf-8")
    for line in text.splitlines():
        parts = line.split("|")
        if len(parts) < 6:
            continue
        no_str = parts[1].strip()
        if not no_str.isdigit():
            continue
        status = parts[4].strip()  # 상태 column
        if "\uc7ac\uc0dd\uc131\ud544\uc694" in status:
            redo_nos.add(int(no_str))

    return redo_nos


def update_progress_redo(plan_nos: list, mark_redo: bool = True) -> int:
    """Update progress.md 상태 column: **[완료]** <-> **[⚠️재생성필요]**.
    파일명/비고 컬럼은 건드리지 않음.
    Returns count of changed lines."""
    progress_file = DOCS_DIR / "monster_progress.md"
    if not progress_file.exists():
        return 0

    text = progress_file.read_text(encoding="utf-8")
    lines = text.splitlines()
    nos_set = set(plan_nos)
    changed = 0

    for i, line in enumerate(lines):
        parts = line.split("|")
        if len(parts) < 6:
            continue
        no_str = parts[1].strip()
        if not no_str.isdigit() or int(no_str) not in nos_set:
            continue

        status = parts[4]  # 상태 column

        if mark_redo and "\uc7ac\uc0dd\uc131\ud544\uc694" not in status and "\uc644\ub8cc" in status:
            parts[4] = " **[\u26a0\ufe0f\uc7ac\uc0dd\uc131\ud544\uc694]** "
            lines[i] = "|".join(parts)
            changed += 1
        elif not mark_redo and "\uc7ac\uc0dd\uc131\ud544\uc694" in status:
            parts[4] = " **[\uc644\ub8cc]** "
            lines[i] = "|".join(parts)
            changed += 1

    if changed:
        progress_file.write_text("\n".join(lines) + "\n", encoding="utf-8")

    return changed


_redo_cache = None


def get_redo_set() -> set:
    global _redo_cache
    if _redo_cache is None:
        _redo_cache = parse_progress_redo()
    return _redo_cache


def invalidate_redo_cache():
    global _redo_cache
    _redo_cache = None


def _build_category_map():
    return {
        "Background": (
            PROJECT_ROOT / "Resources" / "Org" / "Background",
            PROJECT_ROOT / "Assets" / "Images" / "Production" / "Background",
            BG_PRODUCTION_SIZE,
        ),
        "Special": (
            PROJECT_ROOT / "Resources" / "Org" / "Special",
            PROJECT_ROOT / "Assets" / "Images" / "Production" / "Special",
            PRODUCTION_SIZE,
        ),
        "Hero": (
            PROJECT_ROOT / "Resources" / "Org" / "Hero",
            PROJECT_ROOT / "Assets" / "Images" / "Production" / "Hero",
            PRODUCTION_SIZE,
        ),
    }

CATEGORY_MAP = _build_category_map()

ELEMENT_SUFFIXES = ["_normal", "_fire", "_ice", "_wind", "_holy", "_dark"]


# ============================================================================
# Data Model
# ============================================================================

class SpriteEntry:
    """원본/프로덕션 스프라이트 쌍 + 기획 상태"""

    def __init__(self, species: str, sprite_type: str):
        self.species = species          # "monster_slime", "boss_dragon"
        self.sprite_type = sprite_type  # "Monster" or "Boss"
        self.org_files: list = []
        self.production_file: Optional[Path] = None
        self.planned: bool = False      # True if in planning doc
        self.plan_no: int = 0           # Number from planning doc
        self.plan_name: str = ""        # Korean display name
        self.redo_needed: bool = False  # True if progress.md marks ⚠️

    @property
    def is_processed(self) -> bool:
        return self.production_file is not None and self.production_file.exists()

    @property
    def has_org(self) -> bool:
        return bool(self.org_files) and any(f.exists() for f in self.org_files)

    @property
    def display_name(self) -> str:
        if self.plan_name:
            return self.plan_name
        name = self.species.replace("monster_", "").replace("boss_", "")
        return name.replace("_", " ").title()

    @property
    def org_base_file(self) -> Optional[Path]:
        """표시용 대표 원본 파일 (normal > first)"""
        if not self.org_files:
            return None
        for f in self.org_files:
            if "_normal" in f.name.lower():
                return f
        return self.org_files[0]


def _species_from_stem(stem: str) -> str:
    """파일명(확장자 제외)에서 species 추출 (속성 접미사 분리)"""
    for suffix in ELEMENT_SUFFIXES:
        if stem.endswith(suffix):
            return stem[: -len(suffix)]
    return stem


def _rel(path: Path) -> str:
    """PROJECT_ROOT 기준 상대 경로 문자열"""
    try:
        return str(path.relative_to(PROJECT_ROOT))
    except ValueError:
        return str(path)


def discover_flat(org_dir: Path, prod_dir: Path, category: str) -> list:
    """Background/Special/Hero 등 평탄한 폴더용 SpriteEntry 목록"""
    entries: dict = {}

    if org_dir.exists():
        for f in sorted(org_dir.glob("*.png")):
            species = _species_from_stem(f.stem)
            if species not in entries:
                entries[species] = SpriteEntry(species, category)
            entries[species].org_files.append(f)

    if prod_dir.exists():
        for f in sorted(prod_dir.glob("*.png")):
            species = f.stem
            if species not in entries:
                entries[species] = SpriteEntry(species, category)
            entries[species].production_file = f

    for species, entry in entries.items():
        if entry.production_file is None:
            entry.production_file = prod_dir / f"{species}.png"

    return sorted(entries.values(), key=lambda e: e.species)


def discover_sprites(batch_org: str, batch_prod: str, sprite_type: str) -> list:
    """Org / Production 폴더 스캔 후 SpriteEntry 목록 반환"""
    org_dir = ORG_BASE / batch_org / sprite_type
    prod_dir = PRODUCTION_BASE / batch_prod / sprite_type

    entries: dict = {}

    if org_dir.exists():
        for f in sorted(org_dir.glob("*.png")):
            species = _species_from_stem(f.stem)
            if species not in entries:
                entries[species] = SpriteEntry(species, sprite_type)
            entries[species].org_files.append(f)

    if prod_dir.exists():
        for f in sorted(prod_dir.glob("*.png")):
            species = f.stem
            if species not in entries:
                entries[species] = SpriteEntry(species, sprite_type)
            entries[species].production_file = f

    # Production 경로 미리 설정 (파일이 없어도 경로 지정)
    for species, entry in entries.items():
        if entry.production_file is None:
            entry.production_file = prod_dir / f"{species}.png"

    return sorted(entries.values(), key=lambda e: e.species)


def discover_with_planning(
    batch_id: int, batch_org: str, batch_prod: str, sprite_type: str
) -> list:
    """Discover sprites using planning doc as master list, merged with actual files."""
    planning = get_planning_data()
    planned_list = planning.get(batch_id, [])
    redo_set = get_redo_set()

    # Get file-based entries
    file_entries = discover_sprites(batch_org, batch_prod, sprite_type)
    file_dict = {e.species: e for e in file_entries}

    # Filter planned species by type
    typed_planned = [
        (no, sid, stype, name)
        for no, sid, stype, name in planned_list
        if stype == sprite_type
    ]

    result = []
    seen = set()

    # Add all planned species in planning-doc order
    for no, species_id, stype, display_name in typed_planned:
        prefix = "monster_" if stype == "Monster" else "boss_"
        full_species = f"{prefix}{species_id}"

        if full_species in file_dict:
            entry = file_dict[full_species]
        else:
            # Planned but no files yet
            entry = SpriteEntry(full_species, stype)
            prod_dir = PRODUCTION_BASE / batch_prod / stype
            entry.production_file = prod_dir / f"{full_species}.png"

        entry.planned = True
        entry.plan_no = no
        entry.plan_name = display_name
        entry.redo_needed = no in redo_set
        result.append(entry)
        seen.add(full_species)

    # Add any file-based entries NOT in planning (extra files)
    for entry in file_entries:
        if entry.species not in seen:
            entry.planned = False
            result.append(entry)

    return result


# ============================================================================
# Image Helpers
# ============================================================================

def _checkerboard(size: int) -> Image.Image:
    tile = 8
    img = Image.new("RGBA", (size, size))
    for y in range(0, size, tile):
        for x in range(0, size, tile):
            dark = (x // tile + y // tile) % 2 == 0
            color = (75, 75, 75, 255) if dark else (115, 115, 115, 255)
            for py in range(min(tile, size - y)):
                for px in range(min(tile, size - x)):
                    img.putpixel((x + px, y + py), color)
    return img


def pil_to_qpixmap(pil_img: Image.Image, size: int = THUMB_SIZE) -> QPixmap:
    """PIL → QPixmap (체커보드 배경 합성)"""
    bg = _checkerboard(size)
    thumb = pil_img.convert("RGBA").copy()
    thumb.thumbnail((size, size), Image.LANCZOS)
    x = (size - thumb.width) // 2
    y = (size - thumb.height) // 2
    bg.paste(thumb, (x, y), thumb)
    data = bg.tobytes("raw", "RGBA")
    qimg = QImage(data, size, size, QImage.Format.Format_RGBA8888)
    return QPixmap.fromImage(qimg)


def load_pixmap(path: Path, size: int = THUMB_SIZE) -> Optional[QPixmap]:
    try:
        img = Image.open(str(path)).convert("RGBA")
        return pil_to_qpixmap(img, size)
    except Exception:
        return None


def make_placeholder(size: int, text: str = "없음") -> QPixmap:
    px = QPixmap(size, size)
    px.fill(QColor(45, 45, 45))
    painter = QPainter(px)
    painter.setPen(QColor(140, 140, 140))
    painter.drawText(px.rect(), Qt.AlignmentFlag.AlignCenter, text)
    painter.end()
    return px


# ============================================================================
# Sprite Card Widget
# ============================================================================

class SpriteCard(QFrame):
    """썸네일 + 체크박스 + 이름"""

    selection_changed = pyqtSignal(bool)
    reset_requested   = pyqtSignal()   # production 파일 삭제 후 재처리 필요 상태로 전환

    def __init__(self, entry: SpriteEntry, panel: str, parent=None):
        super().__init__(parent)
        self.entry = entry
        self.panel = panel  # "org" | "production"

        self.setFixedSize(CARD_W, CARD_H)
        self.setFrameShape(QFrame.Shape.Box)
        self.setCursor(Qt.CursorShape.PointingHandCursor)

        layout = QVBoxLayout(self)
        layout.setContentsMargins(4, 4, 4, 4)
        layout.setSpacing(2)

        # 체크박스 (우측 상단)
        row = QHBoxLayout()
        row.addStretch()
        self.checkbox = QCheckBox()
        self.checkbox.stateChanged.connect(self._on_check)
        row.addWidget(self.checkbox)
        layout.addLayout(row)

        # 썸네일
        self.thumb = QLabel()
        self.thumb.setFixedSize(THUMB_SIZE, THUMB_SIZE)
        self.thumb.setAlignment(Qt.AlignmentFlag.AlignCenter)
        layout.addWidget(self.thumb, 0, Qt.AlignmentFlag.AlignCenter)

        # 이름 (with plan number prefix)
        prefix = f"#{entry.plan_no} " if entry.plan_no else ""
        self.lbl = QLabel(f"{prefix}{entry.display_name}")
        self.lbl.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.lbl.setWordWrap(True)
        font = self.lbl.font()
        font.setPointSize(8)
        self.lbl.setFont(font)
        layout.addWidget(self.lbl)

        # Pipeline status dots: 기획 / 원본 / 프로덕트
        self.status_lbl = QLabel()
        self.status_lbl.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.status_lbl.setTextFormat(Qt.TextFormat.RichText)
        self.status_lbl.setToolTip("\uae30\ud68d / \uc6d0\ubcf8 / \ud504\ub85c\ub355\ud2b8")
        sfont = self.status_lbl.font()
        sfont.setPointSize(9)
        self.status_lbl.setFont(sfont)
        layout.addWidget(self.status_lbl)

        self._update_style()
        self._update_status()
        self.refresh()

    # ── public ──────────────────────────────────────────────────────────────

    def refresh(self):
        path = self.entry.org_base_file if self.panel == "org" else self.entry.production_file
        if path and path.exists():
            px = load_pixmap(path, THUMB_SIZE)
            if px:
                self.thumb.setPixmap(px)
                self._update_status()
                return
        # Placeholder text based on state
        if self.panel == "org":
            text = "\ubbf8\uc0dd\uc131" if self.entry.planned else "?"
        else:
            text = "\uc5c6\uc74c"
        self.thumb.setPixmap(make_placeholder(THUMB_SIZE, text))
        self._update_status()

    @property
    def is_selected(self) -> bool:
        return self.checkbox.isChecked()

    def set_selected(self, value: bool):
        self.checkbox.setChecked(value)

    # ── private ─────────────────────────────────────────────────────────────

    def _on_check(self, state):
        self._update_style()
        self.selection_changed.emit(bool(state))

    def _update_style(self):
        if self.is_selected:
            self.setStyleSheet(
                "QFrame { border: 2px solid #4CAF50; border-radius: 4px; background: #1a2a1a; }"
            )
        elif self.entry.redo_needed:
            # Org marked for redo
            self.setStyleSheet(
                "QFrame { border: 2px dashed #FF7043; border-radius: 4px; background: #2a1a10; }"
            )
        elif self.entry.planned and not self.entry.has_org:
            # Planned but no org file yet
            self.setStyleSheet(
                "QFrame { border: 1px dashed #4FC3F7; border-radius: 4px; background: #181e28; }"
            )
        elif self.panel == "production" and not self.entry.is_processed:
            self.setStyleSheet(
                "QFrame { border: 1px solid #553333; border-radius: 4px; background: #201818; }"
            )
        else:
            self.setStyleSheet(
                "QFrame { border: 1px solid #444; border-radius: 4px; background: #1e1e1e; }"
            )

    def _update_status(self):
        """Update pipeline status dots: 기획 / 원본 / 프로덕트"""
        on_plan = "#4FC3F7"    # blue
        on_org = "#FFA726"     # orange
        on_prod = "#66BB6A"    # green
        redo_color = "#FF5252"  # red - redo needed
        off = "#444"

        pc = on_plan if self.entry.planned else off
        if self.entry.redo_needed:
            oc = redo_color
        else:
            oc = on_org if self.entry.has_org else off
        rc = on_prod if self.entry.is_processed else off

        org_sym = "\u26a0" if self.entry.redo_needed else "\u25cf"

        self.status_lbl.setText(
            f'<span style="color:{pc};">\u25cf</span> '
            f'<span style="color:{oc};">{org_sym}</span> '
            f'<span style="color:{rc};">\u25cf</span>'
        )

    def mousePressEvent(self, event):
        if event.button() == Qt.MouseButton.RightButton:
            return  # contextMenuEvent가 처리
        self.checkbox.toggle()

    def contextMenuEvent(self, event):
        menu = QMenu(self)

        # Org panel: redo toggle
        if self.panel == "org" and self.entry.planned and self.entry.plan_no:
            if self.entry.redo_needed:
                act = menu.addAction("\uc7ac\uc0dd\uc131 \ud574\uc81c  (\u26a0\ufe0f \u2192 \u2705)")
                act.triggered.connect(lambda: self._toggle_redo(False))
            elif self.entry.has_org:
                act = menu.addAction("\uc7ac\uc0dd\uc131 \ud544\uc694  (\u2705 \u2192 \u26a0\ufe0f)")
                act.triggered.connect(lambda: self._toggle_redo(True))

        # Production panel: reset
        if self.panel == "production" and self.entry.is_processed:
            act = menu.addAction("\uc7ac\ucc98\ub9ac \ud544\uc694\ub85c \ub418\ub3cc\ub9ac\uae30  (Production \uc0ad\uc81c)")
            act.triggered.connect(self._reset_production)

        if menu.actions():
            menu.exec(event.globalPos())

    def _toggle_redo(self, mark_redo: bool):
        """Toggle redo_needed status and update progress.md"""
        changed = update_progress_redo([self.entry.plan_no], mark_redo=mark_redo)
        if changed:
            self.entry.redo_needed = mark_redo
            invalidate_redo_cache()
            self._update_style()
            self._update_status()
            self.reset_requested.emit()

    def _reset_production(self):
        path = self.entry.production_file
        if path and path.exists():
            try:
                path.unlink()
            except Exception as e:
                QMessageBox.warning(self, "\uc0ad\uc81c \uc2e4\ud328", str(e))
                return
        self.entry.production_file = None
        self._update_style()
        self.refresh()
        self.reset_requested.emit()


# ============================================================================
# Background Worker Threads
# ============================================================================

class ProcessWorker(QThread):
    progress = pyqtSignal(int, int, str)
    finished = pyqtSignal(list)

    def __init__(self, tasks: list, flip: bool = False, size: int = PRODUCTION_SIZE):
        super().__init__()
        self.tasks = tasks   # [(org_path, prod_path), ...]
        self.flip = flip
        self.size = size

    def run(self):
        sys.path.insert(0, str(SCRIPT_DIR))
        try:
            import sprite_processor as sp
        except ImportError:
            self.finished.emit([{"status": "error", "error": "sprite_processor 모듈 없음"}])
            return

        results = []
        for i, (org, prod) in enumerate(self.tasks):
            self.progress.emit(i, len(self.tasks), f"처리 중: {org.name}")
            report = sp.process_single(
                str(org), str(prod),
                flip=self.flip,
                size=self.size,
                padding=85,
            )
            results.append(report)

        self.progress.emit(len(self.tasks), len(self.tasks), "완료")
        self.finished.emit(results)


class FlipWorker(QThread):
    progress = pyqtSignal(int, int, str)
    finished = pyqtSignal(list)

    def __init__(self, paths: list):
        super().__init__()
        self.paths = paths

    def run(self):
        results = []
        for i, path in enumerate(self.paths):
            self.progress.emit(i, len(self.paths), f"반전 중: {path.name}")
            try:
                img = Image.open(str(path)).convert("RGBA")
                flipped = img.transpose(Image.FLIP_LEFT_RIGHT)
                flipped.save(str(path), "PNG")
                results.append({"file": path.name, "status": "success"})
            except Exception as e:
                results.append({"file": path.name, "status": "error", "error": str(e)})

        self.progress.emit(len(self.paths), len(self.paths), "완료")
        self.finished.emit(results)


class RemoveBgWorker(QThread):
    """BG제거만 — 크롭/리사이즈 없이 AutoAlphaChannel만 실행"""
    progress = pyqtSignal(int, int, str)
    finished = pyqtSignal(list)

    AUTO_ALPHA = PROJECT_ROOT / "AutoAlphaChannel" / "AutoAlphaChannel.exe"

    def __init__(self, tasks: list):
        super().__init__()
        self.tasks = tasks  # [(org_path, prod_path), ...]

    def run(self):
        import subprocess, shutil, tempfile

        if not self.AUTO_ALPHA.exists():
            self.finished.emit([{
                "status": "error",
                "error": f"AutoAlphaChannel.exe 없음: {self.AUTO_ALPHA}",
            }])
            return

        results = []
        for i, (org, prod) in enumerate(self.tasks):
            self.progress.emit(i, len(self.tasks), f"BG제거 중: {org.name}")
            try:
                # AutoAlphaChannel은 입력 파일과 같은 폴더에 *A.png 생성
                with tempfile.TemporaryDirectory() as tmp:
                    tmp_in = Path(tmp) / org.name
                    shutil.copy2(str(org), str(tmp_in))

                    # batch_process_images.py 와 동일: -mode 0 (Auto)
                    cmd = [
                        str(self.AUTO_ALPHA),
                        "-i", str(tmp_in),
                        "-mode", "0",
                        "-erosion", "1",
                        "-overwrite",
                    ]
                    r = subprocess.run(cmd, capture_output=True, timeout=30)

                    if r.returncode != 0 or not tmp_in.exists():
                        raise RuntimeError(r.stderr.decode(errors="replace") or "출력 파일 없음")

                    prod.parent.mkdir(parents=True, exist_ok=True)
                    shutil.move(str(tmp_in), str(prod))

                results.append({"file": org.name, "status": "success"})
            except Exception as e:
                results.append({"file": org.name, "status": "error", "error": str(e)})

        self.progress.emit(len(self.tasks), len(self.tasks), "완료")
        self.finished.emit(results)


# ============================================================================
# Dark Style
# ============================================================================

DARK_STYLE = """
QMainWindow, QWidget { background: #1e1e1e; color: #e0e0e0; }
QGroupBox {
    border: 1px solid #444; border-radius: 4px; margin-top: 8px;
    font-weight: bold; color: #aaa;
}
QGroupBox::title { subcontrol-origin: margin; left: 8px; padding: 0 4px; }
QComboBox {
    background: #2a2a2a; border: 1px solid #555; border-radius: 3px;
    padding: 3px 8px; color: #e0e0e0; min-width: 80px;
}
QComboBox::drop-down { border: none; }
QComboBox QAbstractItemView {
    background: #2a2a2a; color: #e0e0e0;
    selection-background-color: #3d6a3d;
}
QPushButton {
    background: #2a2a2a; border: 1px solid #555; border-radius: 3px;
    padding: 4px 10px; color: #e0e0e0;
}
QPushButton:hover { background: #3a3a3a; }
QPushButton:pressed { background: #1a1a1a; }
QPushButton:disabled { color: #555; background: #222; }
QScrollArea { border: none; }
QScrollBar:vertical { background: #2a2a2a; width: 10px; border-radius: 4px; }
QScrollBar::handle:vertical { background: #555; border-radius: 4px; min-height: 20px; }
QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical { height: 0; }
QProgressBar {
    background: #2a2a2a; border: 1px solid #444; border-radius: 3px;
    text-align: center; color: #e0e0e0;
}
QProgressBar::chunk { background: #4CAF50; border-radius: 2px; }
QStatusBar { background: #181818; color: #aaa; }
QLabel { color: #e0e0e0; }
"""


# ============================================================================
# Main Window
# ============================================================================

class SpriteManager(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("DeskWarrior Sprite Manager")
        self.setMinimumSize(1200, 720)

        self.entries: list = []
        self.org_cards: list = []
        self.prod_cards: list = []
        self.worker: Optional[QThread] = None

        self._build_ui()
        self.setStyleSheet(DARK_STYLE)
        self._load()

    # ── UI Construction ─────────────────────────────────────────────────────

    def _build_ui(self):
        root = QWidget()
        self.setCentralWidget(root)
        vbox = QVBoxLayout(root)
        vbox.setContentsMargins(8, 8, 8, 6)
        vbox.setSpacing(6)

        # ── 컨트롤 바 ────────────────────────────────────────────────────
        ctrl = QHBoxLayout()

        ctrl.addWidget(QLabel("카테고리:"))
        self.batch_cb = QComboBox()
        all_cats = list(BATCH_MAP.keys()) + ["──────", "Background", "Special", "Hero"]
        self.batch_cb.addItems(all_cats)
        self.batch_cb.currentTextChanged.connect(self._on_category_changed)
        ctrl.addWidget(self.batch_cb)

        ctrl.addWidget(QLabel("  종류:"))
        self.type_cb = QComboBox()
        self.type_cb.addItems(["Monster", "Boss", "All"])
        self.type_cb.currentTextChanged.connect(self._load)
        ctrl.addWidget(self.type_cb)

        self.refresh_btn = QPushButton("\uc0c8\ub85c\uace0\uce68")
        self.refresh_btn.clicked.connect(self._on_refresh)
        ctrl.addWidget(self.refresh_btn)

        ctrl.addStretch()

        self.sel_all_btn = QPushButton("전체 선택")
        self.sel_all_btn.clicked.connect(self._select_all)
        ctrl.addWidget(self.sel_all_btn)

        self.sel_none_btn = QPushButton("선택 해제")
        self.sel_none_btn.clicked.connect(self._select_none)
        ctrl.addWidget(self.sel_none_btn)

        self.sel_unproc_btn = QPushButton("미처리만")
        self.sel_unproc_btn.clicked.connect(self._select_unprocessed)
        ctrl.addWidget(self.sel_unproc_btn)

        self.sel_noorg_btn = QPushButton("\ubbf8\uc0dd\uc131\ub9cc")
        self.sel_noorg_btn.setToolTip("\uae30\ud68d\ub418\uc5c8\uc9c0\ub9cc \uc6d0\ubcf8 \uc774\ubbf8\uc9c0\uac00 \uc5c6\ub294 \ud56d\ubaa9")
        self.sel_noorg_btn.clicked.connect(self._select_no_org)
        ctrl.addWidget(self.sel_noorg_btn)

        self.sel_redo_btn = QPushButton("\uc7ac\uc0dd\uc131\ub9cc")
        self.sel_redo_btn.setToolTip("\u26a0\ufe0f \uc7ac\uc0dd\uc131\ud544\uc694 \uc0c1\ud0dc\uc778 \ud56d\ubaa9\ub9cc \uc120\ud0dd")
        self.sel_redo_btn.clicked.connect(self._select_redo)
        ctrl.addWidget(self.sel_redo_btn)

        vbox.addLayout(ctrl)

        # ── 통계 + 범례 ──────────────────────────────────────────────────
        info_row = QHBoxLayout()
        self.info_lbl = QLabel()
        self.info_lbl.setStyleSheet("color: #ccc; font-size: 11px;")
        self.info_lbl.setTextFormat(Qt.TextFormat.RichText)
        info_row.addWidget(self.info_lbl)
        info_row.addStretch()
        legend = QLabel(
            '<span style="color:#666;font-size:10px;">'
            '\ud30c\uc774\ud504\ub77c\uc778: '
            '<span style="color:#4FC3F7;">\u25cf</span>\uae30\ud68d  '
            '<span style="color:#FFA726;">\u25cf</span>\uc6d0\ubcf8  '
            '<span style="color:#66BB6A;">\u25cf</span>\ud504\ub85c\ub355\ud2b8'
            '</span>'
        )
        legend.setTextFormat(Qt.TextFormat.RichText)
        info_row.addWidget(legend)
        vbox.addLayout(info_row)

        # ── 메인 스플리터 ────────────────────────────────────────────────
        splitter = QSplitter(Qt.Orientation.Horizontal)

        self._org_group, self.org_scroll, self.org_grid = self._make_panel("원본  (Resources/Org)")
        self._prod_group, self.prod_scroll, self.prod_grid = self._make_panel(
            "프로덕션  (Assets/Images/Production)"
        )
        splitter.addWidget(self._org_group)
        splitter.addWidget(self._prod_group)
        splitter.setStretchFactor(0, 1)
        splitter.setStretchFactor(1, 1)
        vbox.addWidget(splitter, stretch=1)

        # ── 진행 표시 ────────────────────────────────────────────────────
        prog_row = QHBoxLayout()
        self.prog_bar = QProgressBar()
        self.prog_bar.setVisible(False)
        self.prog_lbl = QLabel()
        self.prog_lbl.setVisible(False)
        prog_row.addWidget(self.prog_bar)
        prog_row.addWidget(self.prog_lbl)
        vbox.addLayout(prog_row)

        # ── 액션 버튼 바 ─────────────────────────────────────────────────
        actions = QHBoxLayout()

        self.process_btn = QPushButton("전체 처리  (BG제거 + 크롭 + 256)")
        self.process_btn.setStyleSheet(
            "QPushButton { background:#2d5a2d; color:white; font-weight:bold; padding:6px 14px; }"
            "QPushButton:hover { background:#3d7a3d; }"
            "QPushButton:disabled { background:#1d3a1d; color:#555; }"
        )
        self.process_btn.clicked.connect(lambda: self._run_process(flip=False))
        actions.addWidget(self.process_btn)

        self.process_flip_btn = QPushButton("전체 처리 + 좌우 반전")
        self.process_flip_btn.setStyleSheet(
            "QPushButton { background:#2d4a6a; color:white; font-weight:bold; padding:6px 14px; }"
            "QPushButton:hover { background:#3d5a8a; }"
            "QPushButton:disabled { background:#1d2a4a; color:#555; }"
        )
        self.process_flip_btn.clicked.connect(lambda: self._run_process(flip=True))
        actions.addWidget(self.process_flip_btn)

        self.removebg_btn = QPushButton("BG제거만")
        self.removebg_btn.setStyleSheet(
            "QPushButton { background:#5a3a6a; color:white; padding:6px 14px; }"
            "QPushButton:hover { background:#7a4a9a; }"
            "QPushButton:disabled { background:#2a1a3a; color:#555; }"
        )
        self.removebg_btn.clicked.connect(self._remove_bg)
        actions.addWidget(self.removebg_btn)

        actions.addWidget(self._vsep())

        self.flip_btn = QPushButton("좌우 반전  (Production만)")
        self.flip_btn.setStyleSheet(
            "QPushButton { background:#4a3a1a; color:white; padding:6px 14px; }"
            "QPushButton:hover { background:#6a5a2a; }"
            "QPushButton:disabled { background:#2a2a1a; color:#555; }"
        )
        self.flip_btn.clicked.connect(self._flip_production)
        actions.addWidget(self.flip_btn)

        actions.addWidget(self._vsep())

        self.reset_btn = QPushButton("선택 되돌리기  (재처리 필요)")
        self.reset_btn.setStyleSheet(
            "QPushButton { background:#6a2a2a; color:white; padding:6px 14px; }"
            "QPushButton:hover { background:#8a3a3a; }"
            "QPushButton:disabled { background:#2a1a1a; color:#555; }"
        )
        self.reset_btn.clicked.connect(self._reset_selected)
        actions.addWidget(self.reset_btn)

        actions.addWidget(self._vsep())

        self.redo_btn = QPushButton("\uc7ac\uc0dd\uc131 \uc9c0\uc815")
        self.redo_btn.setToolTip("progress.md\uc5d0\uc11c Org \u2705 \u2192 \u26a0\ufe0f \uc7ac\uc0dd\uc131\ud544\uc694\ub85c \ubcc0\uacbd")
        self.redo_btn.setStyleSheet(
            "QPushButton { background:#8a4a00; color:white; padding:6px 14px; }"
            "QPushButton:hover { background:#aa6a20; }"
            "QPushButton:disabled { background:#3a2a00; color:#555; }"
        )
        self.redo_btn.clicked.connect(self._mark_redo)
        actions.addWidget(self.redo_btn)

        self.redo_clear_btn = QPushButton("\uc7ac\uc0dd\uc131 \ud574\uc81c")
        self.redo_clear_btn.setToolTip("progress.md\uc5d0\uc11c Org \u26a0\ufe0f \u2192 \u2705 \uc644\ub8cc\ub85c \ubcf5\uc6d0")
        self.redo_clear_btn.setStyleSheet(
            "QPushButton { background:#2a6a4a; color:white; padding:6px 14px; }"
            "QPushButton:hover { background:#3a8a6a; }"
            "QPushButton:disabled { background:#1a3a2a; color:#555; }"
        )
        self.redo_clear_btn.clicked.connect(self._clear_redo)
        actions.addWidget(self.redo_clear_btn)

        actions.addStretch()

        open_org = QPushButton("Org 폴더 열기")
        open_org.clicked.connect(self._open_org)
        actions.addWidget(open_org)

        open_prod = QPushButton("Production 폴더 열기")
        open_prod.clicked.connect(self._open_prod)
        actions.addWidget(open_prod)

        actions.addWidget(self._vsep())

        for doc_name, doc_path in DOCS_MAP.items():
            btn = QPushButton(doc_name)
            btn.setToolTip(str(doc_path))
            btn.clicked.connect(lambda checked=False, p=doc_path: _open_doc(p))
            actions.addWidget(btn)

        vbox.addLayout(actions)

        self.status_bar = QStatusBar()
        self.setStatusBar(self.status_bar)

    def _make_panel(self, title: str):
        group = QGroupBox(title)
        vbox = QVBoxLayout(group)
        vbox.setContentsMargins(4, 8, 4, 4)

        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setHorizontalScrollBarPolicy(Qt.ScrollBarPolicy.ScrollBarAlwaysOff)

        content = QWidget()
        grid = QGridLayout(content)
        grid.setSpacing(6)
        grid.setAlignment(Qt.AlignmentFlag.AlignTop | Qt.AlignmentFlag.AlignLeft)
        scroll.setWidget(content)
        vbox.addWidget(scroll)

        return group, scroll, grid

    def _vsep(self) -> QFrame:
        sep = QFrame()
        sep.setFrameShape(QFrame.Shape.VLine)
        sep.setStyleSheet("color: #555;")
        return sep

    # ── Data Loading ────────────────────────────────────────────────────────

    def _on_refresh(self):
        invalidate_planning_cache()
        invalidate_redo_cache()
        self._load()

    def _on_category_changed(self, text: str):
        if text == "──────":
            # 구분선 선택 방지: 이전 항목으로 복귀
            self.batch_cb.blockSignals(True)
            self.batch_cb.setCurrentIndex(0)
            self.batch_cb.blockSignals(False)
            return
        is_batch = text in BATCH_MAP
        self.type_cb.setEnabled(is_batch)
        self._load()

    def _load(self):
        cat = self.batch_cb.currentText()
        if cat == "\u2500\u2500\u2500\u2500\u2500\u2500":
            return

        if cat in CATEGORY_MAP:
            # Background / Special / Hero (no planning data)
            org_dir, prod_dir, _ = CATEGORY_MAP[cat]
            all_entries = discover_flat(org_dir, prod_dir, cat)
            self._org_group.setTitle(f"\uc6d0\ubcf8  ({_rel(org_dir)})")
            self._prod_group.setTitle(f"\ud504\ub85c\ub355\uc158  ({_rel(prod_dir)})")
        else:
            # Batch 1-9: use planning doc as master list
            type_filter = self.type_cb.currentText()
            batch_org, batch_prod = BATCH_MAP.get(cat, ("Batch_01", "Batch1"))
            # Extract batch_id from key (e.g., "Batch 3" -> 3)
            batch_id = int(cat.split()[-1]) if cat.startswith("Batch") else 1
            all_entries = []
            if type_filter in ("Monster", "All"):
                all_entries += discover_with_planning(
                    batch_id, batch_org, batch_prod, "Monster"
                )
            if type_filter in ("Boss", "All"):
                all_entries += discover_with_planning(
                    batch_id, batch_org, batch_prod, "Boss"
                )
            self._org_group.setTitle("\uc6d0\ubcf8  (Resources/Org)")
            self._prod_group.setTitle("\ud504\ub85c\ub355\uc158  (Assets/Images/Production)")

        self.entries = all_entries
        self._rebuild_cards()
        self._update_info()
        self.status_bar.showMessage(f"{cat}  \ub85c\ub4dc\ub428  ({len(all_entries)}\uc885)")

    def _rebuild_cards(self):
        for card in self.org_cards:
            card.deleteLater()
        for card in self.prod_cards:
            card.deleteLater()
        self.org_cards.clear()
        self.prod_cards.clear()

        def clear_grid(grid: QGridLayout):
            while grid.count():
                item = grid.takeAt(0)
                if item.widget():
                    item.widget().deleteLater()

        clear_grid(self.org_grid)
        clear_grid(self.prod_grid)

        for i, entry in enumerate(self.entries):
            row, col = divmod(i, GRID_COLS)

            oc = SpriteCard(entry, "org")
            pc = SpriteCard(entry, "production")

            oc.selection_changed.connect(lambda v, _pc=pc: _pc.set_selected(v))
            pc.selection_changed.connect(lambda v, _oc=oc: _oc.set_selected(v))
            pc.reset_requested.connect(lambda _oc=oc, _pc=pc: self._on_reset_card(_oc, _pc))

            self.org_grid.addWidget(oc, row, col)
            self.prod_grid.addWidget(pc, row, col)
            self.org_cards.append(oc)
            self.prod_cards.append(pc)

    def _on_reset_card(self, oc: SpriteCard, pc: SpriteCard):
        """단일 카드 되돌리기 후 org 카드 스타일 및 통계 갱신"""
        oc._update_style()
        oc.refresh()
        self._update_info()

    def _update_info(self):
        total = len(self.entries)
        planned = sum(1 for e in self.entries if e.planned)
        has_org = sum(1 for e in self.entries if e.has_org)
        processed = sum(1 for e in self.entries if e.is_processed)
        # Show 3-stage pipeline stats with colored indicators
        parts = [f"\ucd1d {total}\uc885"]
        if planned:
            parts.append(
                f'<span style="color:#4FC3F7;">\u25cf</span> \uae30\ud68d: {planned}'
            )
        parts.append(
            f'<span style="color:#FFA726;">\u25cf</span> \uc6d0\ubcf8: {has_org}/{total}'
        )
        parts.append(
            f'<span style="color:#66BB6A;">\u25cf</span> \ud504\ub85c\ub355\ud2b8: {processed}/{total}'
        )
        redo = sum(1 for e in self.entries if e.redo_needed)
        no_org = total - has_org
        if redo:
            parts.append(
                f'<span style="color:#FF5252;">\u26a0</span> \uc7ac\uc0dd\uc131: {redo}'
            )
        if no_org:
            parts.append(f'\u2502 \ubbf8\uc0dd\uc131: {no_org}')
        self.info_lbl.setTextFormat(Qt.TextFormat.RichText)
        self.info_lbl.setText("   ".join(parts))

    # ── Selection ───────────────────────────────────────────────────────────

    def _select_all(self):
        for c in self.org_cards:
            c.set_selected(True)

    def _select_none(self):
        for c in self.org_cards:
            c.set_selected(False)

    def _select_unprocessed(self):
        for c, e in zip(self.org_cards, self.entries):
            c.set_selected(not e.is_processed)

    def _select_no_org(self):
        for c, e in zip(self.org_cards, self.entries):
            c.set_selected(e.planned and not e.has_org)

    def _select_redo(self):
        for c, e in zip(self.org_cards, self.entries):
            c.set_selected(e.redo_needed)

    def _selected_entries(self) -> list:
        return [e for c, e in zip(self.org_cards, self.entries) if c.is_selected]

    # ── Actions ─────────────────────────────────────────────────────────────

    def _run_process(self, flip: bool):
        selected = self._selected_entries()
        if not selected:
            QMessageBox.information(self, "알림", "처리할 스프라이트를 선택하세요.")
            return

        no_org = [e for e in selected if not e.org_base_file or not e.org_base_file.exists()]
        if no_org:
            names = ", ".join(e.display_name for e in no_org[:6])
            if len(no_org) > 6:
                names += f" 외 {len(no_org) - 6}종"
            reply = QMessageBox.question(
                self, "원본 파일 없음",
                f"원본 없는 항목은 건너뜁니다:\n{names}\n\n계속할까요?",
                QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
            )
            if reply != QMessageBox.StandardButton.Yes:
                return
            selected = [e for e in selected if e.org_base_file and e.org_base_file.exists()]

        if not selected:
            return

        cat = self.batch_cb.currentText()
        if cat in CATEGORY_MAP:
            _, prod_dir_base, target_size = CATEGORY_MAP[cat]
            tasks = []
            for entry in selected:
                prod_dir_base.mkdir(parents=True, exist_ok=True)
                prod_path = prod_dir_base / f"{entry.species}.png"
                tasks.append((entry.org_base_file, prod_path))
            self._start_worker(ProcessWorker(tasks, flip=flip, size=target_size))
        else:
            _, batch_prod = BATCH_MAP.get(cat, ("Batch_01", "Batch1"))
            tasks = []
            for entry in selected:
                prod_dir = PRODUCTION_BASE / batch_prod / entry.sprite_type
                prod_dir.mkdir(parents=True, exist_ok=True)
                prod_path = prod_dir / f"{entry.species}.png"
                tasks.append((entry.org_base_file, prod_path))
            self._start_worker(ProcessWorker(tasks, flip=flip))

    def _flip_production(self):
        selected = self._selected_entries()
        if not selected:
            QMessageBox.information(self, "알림", "반전할 스프라이트를 선택하세요.")
            return
        valid = [e for e in selected if e.is_processed]
        if not valid:
            QMessageBox.warning(self, "경고", "선택된 항목 중 Production 파일이 없습니다.")
            return
        self._start_worker(FlipWorker([e.production_file for e in valid]))

    def _remove_bg(self):
        """BG제거만 — 크롭/리사이즈 없이 초록 배경만 제거 후 Production에 저장"""
        selected = self._selected_entries()
        if not selected:
            QMessageBox.information(self, "알림", "처리할 스프라이트를 선택하세요.")
            return

        no_org = [e for e in selected if not e.org_base_file or not e.org_base_file.exists()]
        if no_org:
            names = ", ".join(e.display_name for e in no_org[:6])
            reply = QMessageBox.question(
                self, "원본 파일 없음",
                f"원본 없는 항목은 건너뜁니다:\n{names}\n\n계속할까요?",
                QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
            )
            if reply != QMessageBox.StandardButton.Yes:
                return
            selected = [e for e in selected if e.org_base_file and e.org_base_file.exists()]

        if not selected:
            return

        cat = self.batch_cb.currentText()
        tasks = []
        if cat in CATEGORY_MAP:
            _, prod_dir_base, _ = CATEGORY_MAP[cat]
            for entry in selected:
                prod_dir_base.mkdir(parents=True, exist_ok=True)
                tasks.append((entry.org_base_file, prod_dir_base / f"{entry.species}.png"))
        else:
            _, batch_prod = BATCH_MAP.get(cat, ("Batch_01", "Batch1"))
            for entry in selected:
                prod_dir = PRODUCTION_BASE / batch_prod / entry.sprite_type
                prod_dir.mkdir(parents=True, exist_ok=True)
                tasks.append((entry.org_base_file, prod_dir / f"{entry.species}.png"))

        self._start_worker(RemoveBgWorker(tasks))

    def _start_worker(self, worker: QThread):
        self._set_buttons_enabled(False)
        self.prog_bar.setMaximum(0)
        self.prog_bar.setValue(0)
        self.prog_bar.setVisible(True)
        self.prog_lbl.setText("")
        self.prog_lbl.setVisible(True)
        self.worker = worker
        worker.progress.connect(self._on_progress)
        worker.finished.connect(self._on_finished)
        worker.start()

    def _open_org(self):
        cat = self.batch_cb.currentText()
        if cat in CATEGORY_MAP:
            folder, _, _ = CATEGORY_MAP[cat]
        else:
            type_filter = self.type_cb.currentText()
            batch_org, _ = BATCH_MAP.get(cat, ("Batch_01", "Batch1"))
            folder = ORG_BASE / batch_org / (type_filter if type_filter != "All" else "")
            if not folder.exists():
                folder = ORG_BASE / batch_org
        _open_explorer(folder)

    def _open_prod(self):
        cat = self.batch_cb.currentText()
        if cat in CATEGORY_MAP:
            _, folder, _ = CATEGORY_MAP[cat]
        else:
            type_filter = self.type_cb.currentText()
            _, batch_prod = BATCH_MAP.get(cat, ("Batch_01", "Batch1"))
            folder = PRODUCTION_BASE / batch_prod / (type_filter if type_filter != "All" else "")
            if not folder.exists():
                folder = PRODUCTION_BASE / batch_prod
        _open_explorer(folder)

    # ── Progress / Callbacks ────────────────────────────────────────────────

    def _on_progress(self, current: int, total: int, msg: str):
        if self.prog_bar.maximum() == 0 and total > 0:
            self.prog_bar.setMaximum(total)
        self.prog_bar.setValue(current)
        self.prog_lbl.setText(f"{current}/{total}")
        self.status_bar.showMessage(msg)

    def _on_finished(self, results: list):
        self._set_buttons_enabled(True)
        self.prog_bar.setVisible(False)
        self.prog_lbl.setVisible(False)

        success = sum(1 for r in results if r.get("status") == "success")
        failed = sum(1 for r in results if r.get("status") == "error")
        self.status_bar.showMessage(f"완료  성공: {success}개   실패: {failed}개")

        if failed:
            errs = [r.get("error", "?") for r in results if r.get("status") == "error"]
            QMessageBox.warning(
                self, "일부 실패",
                f"성공: {success}개\n실패: {failed}개\n\n" + "\n".join(errs[:5]),
            )

        self._load()

    def _reset_selected(self):
        """선택된 Production 이미지를 삭제해 재처리 필요 상태로 되돌림"""
        targets = [pc for pc in self.prod_cards if pc.is_selected and pc.entry.is_processed]
        if not targets:
            self.status_bar.showMessage("선택된 처리완료 항목이 없습니다.")
            return

        answer = QMessageBox.question(
            self,
            "재처리 필요로 되돌리기",
            f"선택된 {len(targets)}개의 Production 이미지를 삭제하고\n재처리 필요 상태로 되돌리시겠습니까?",
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
        )
        if answer != QMessageBox.StandardButton.Yes:
            return

        failed = 0
        for pc in targets:
            path = pc.entry.production_file
            if path and path.exists():
                try:
                    path.unlink()
                except Exception:
                    failed += 1
                    continue
            pc.entry.production_file = None
            pc._update_style()
            pc.refresh()

        # 대응하는 org 카드 스타일도 갱신
        for oc in self.org_cards:
            oc._update_style()
            oc.refresh()

        self._update_info()
        msg = f"{len(targets) - failed}개 되돌림 완료"
        if failed:
            msg += f"  ({failed}개 실패)"
        self.status_bar.showMessage(msg)

    def _mark_redo(self):
        """선택된 Org 완료 항목을 progress.md에서 ⚠️ 재생성필요로 변경"""
        selected = self._selected_entries()
        targets = [e for e in selected if e.planned and e.has_org and e.plan_no and not e.redo_needed]
        if not targets:
            self.status_bar.showMessage("\uc7ac\uc0dd\uc131 \uc9c0\uc815\ud560 \ud56d\ubaa9\uc744 \uc120\ud0dd\ud558\uc138\uc694. (Org \u2705 \uc0c1\ud0dc\ub9cc \uac00\ub2a5)")
            return

        names = ", ".join(f"#{e.plan_no} {e.display_name}" for e in targets[:8])
        if len(targets) > 8:
            names += f" \uc678 {len(targets) - 8}\uc885"

        answer = QMessageBox.question(
            self,
            "\uc7ac\uc0dd\uc131 \ud544\uc694 \uc9c0\uc815",
            f"progress.md\uc5d0\uc11c {len(targets)}\uac1c \ud56d\ubaa9\uc758 Org \uc0c1\ud0dc\ub97c\n"
            f"\u2705 \u2192 \u26a0\ufe0f \uc7ac\uc0dd\uc131\ud544\uc694\ub85c \ubcc0\uacbd\ud569\ub2c8\ub2e4.\n\n{names}",
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
        )
        if answer != QMessageBox.StandardButton.Yes:
            return

        plan_nos = [e.plan_no for e in targets]
        changed = update_progress_redo(plan_nos, mark_redo=True)
        invalidate_redo_cache()

        if changed == 0:
            self.status_bar.showMessage("\uc7ac\uc0dd\uc131 \uc9c0\uc815 \uc2e4\ud328: progress.md\uc5d0\uc11c \ub300\uc0c1 \ud56d\ubaa9\uc744 \ucc3e\uc9c0 \ubabb\ud588\uc2b5\ub2c8\ub2e4.")
            return

        # Update entry states and cards (only if file was changed)
        nos_changed = set(plan_nos)
        for e in targets:
            if e.plan_no in nos_changed:
                e.redo_needed = True
        for oc in self.org_cards:
            oc._update_style()
            oc._update_status()
        for pc in self.prod_cards:
            pc._update_style()
            pc._update_status()

        self._update_info()
        self.status_bar.showMessage(f"\uc7ac\uc0dd\uc131 \uc9c0\uc815: {changed}\uac1c \ubcc0\uacbd\ub428")

    def _clear_redo(self):
        """선택된 ⚠️ 항목을 progress.md에서 ✅로 복원"""
        selected = self._selected_entries()
        targets = [e for e in selected if e.redo_needed and e.plan_no]
        if not targets:
            self.status_bar.showMessage("\uc7ac\uc0dd\uc131 \ud574\uc81c\ud560 \ud56d\ubaa9\uc744 \uc120\ud0dd\ud558\uc138\uc694. (\u26a0\ufe0f \uc0c1\ud0dc\ub9cc \uac00\ub2a5)")
            return

        plan_nos = [e.plan_no for e in targets]
        changed = update_progress_redo(plan_nos, mark_redo=False)
        invalidate_redo_cache()

        for e in targets:
            e.redo_needed = False
        for oc in self.org_cards:
            oc._update_style()
            oc._update_status()
        for pc in self.prod_cards:
            pc._update_style()
            pc._update_status()

        self._update_info()
        self.status_bar.showMessage(f"\uc7ac\uc0dd\uc131 \ud574\uc81c: {changed}\uac1c \ubcf5\uc6d0\ub428")

    def _set_buttons_enabled(self, v: bool):
        for btn in (self.process_btn, self.process_flip_btn, self.removebg_btn,
                    self.flip_btn, self.reset_btn, self.refresh_btn,
                    self.redo_btn, self.redo_clear_btn):
            btn.setEnabled(v)


# ============================================================================
# Helpers
# ============================================================================

def _open_explorer(path: Path):
    try:
        subprocess.Popen(["explorer", str(path)])
    except Exception:
        pass


def _open_doc(path: Path):
    try:
        os.startfile(str(path))
    except Exception:
        subprocess.Popen(["notepad", str(path)])


# ============================================================================
# Entry Point
# ============================================================================

def main():
    app = QApplication(sys.argv)
    app.setStyle("Fusion")
    w = SpriteManager()
    w.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
