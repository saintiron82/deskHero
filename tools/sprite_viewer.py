"""Batch 1 Monster Sprite Viewer - 모든 몬스터를 한 화면에서 확인"""
import sys
import os
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageTk
import tkinter as tk
from tkinter import ttk

PROJECT_ROOT = Path(__file__).parent.parent
BATCHES_DIR = PROJECT_ROOT / "Assets" / "Images" / "Batches" / "Batch_01" / "Monster"

# Batch 1 monsters (batch_01.json 기준)
BATCH1_MONSTERS = [
    "monster_slime", "monster_bat", "monster_skeleton", "monster_goblin",
    "monster_orc", "monster_ghost", "monster_golem", "monster_mushroom",
    "monster_spider", "monster_wolf", "monster_snake", "monster_boar",
]

CELL_SIZE = 280
SPRITE_SIZE = 200
COLS = 4
BG_COLOR = (40, 40, 50)
LABEL_COLOR = (220, 220, 220)
BORDER_OK = (80, 200, 80)
BORDER_MISSING = (200, 80, 80)


def create_overview_image():
    """모든 몬스터를 그리드로 배치한 이미지 생성"""
    rows = (len(BATCH1_MONSTERS) + COLS - 1) // COLS
    width = COLS * CELL_SIZE + 20
    height = rows * CELL_SIZE + 60

    canvas = Image.new("RGBA", (width, height), BG_COLOR + (255,))
    draw = ImageDraw.Draw(canvas)

    # Title
    try:
        title_font = ImageFont.truetype("arial.ttf", 20)
        label_font = ImageFont.truetype("arial.ttf", 14)
    except OSError:
        title_font = ImageFont.load_default()
        label_font = ImageFont.load_default()

    draw.text((10, 10), "Batch 1 Monster Sprites (Game Path)", fill=LABEL_COLOR, font=title_font)

    for idx, monster_id in enumerate(BATCH1_MONSTERS):
        col = idx % COLS
        row = idx // COLS
        x = col * CELL_SIZE + 10
        y = row * CELL_SIZE + 50

        sprite_path = BATCHES_DIR / f"{monster_id}.png"

        if sprite_path.exists():
            # Load and resize sprite
            sprite = Image.open(sprite_path).convert("RGBA")
            sprite = sprite.resize((SPRITE_SIZE, SPRITE_SIZE), Image.LANCZOS)

            # Draw border
            border_x = x + (CELL_SIZE - SPRITE_SIZE) // 2 - 3
            border_y = y + 5 - 3
            draw.rectangle(
                [border_x, border_y, border_x + SPRITE_SIZE + 5, border_y + SPRITE_SIZE + 5],
                outline=BORDER_OK, width=2
            )

            # Checkerboard background for transparency
            checker = Image.new("RGBA", (SPRITE_SIZE, SPRITE_SIZE), (60, 60, 70, 255))
            checker_draw = ImageDraw.Draw(checker)
            cs = 20
            for cy in range(0, SPRITE_SIZE, cs):
                for cx in range(0, SPRITE_SIZE, cs):
                    if (cx // cs + cy // cs) % 2 == 0:
                        checker_draw.rectangle([cx, cy, cx + cs - 1, cy + cs - 1], fill=(80, 80, 90, 255))

            checker = Image.alpha_composite(checker, sprite)
            canvas.paste(checker, (x + (CELL_SIZE - SPRITE_SIZE) // 2, y + 5))

            # Size info
            orig = Image.open(sprite_path)
            size_text = f"{orig.size[0]}x{orig.size[1]}"
            status = "OK"
        else:
            draw.rectangle(
                [x + 20, y + 5, x + CELL_SIZE - 20, y + SPRITE_SIZE + 5],
                outline=BORDER_MISSING, width=2
            )
            draw.text((x + 60, y + 90), "MISSING", fill=BORDER_MISSING, font=title_font)
            size_text = ""
            status = "MISSING"

        # Label
        name = monster_id.replace("monster_", "")
        label = f"{name} [{size_text}] {status}"
        draw.text((x + 10, y + SPRITE_SIZE + 12), label, fill=LABEL_COLOR, font=label_font)

    return canvas


class SpriteViewerApp:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("Batch 1 Sprite Viewer")
        self.root.configure(bg="#282832")

        # Generate image
        self.pil_image = create_overview_image()
        self.tk_image = ImageTk.PhotoImage(self.pil_image)

        # Scrollable canvas
        frame = ttk.Frame(self.root)
        frame.pack(fill=tk.BOTH, expand=True)

        self.canvas = tk.Canvas(frame, bg="#282832",
                                scrollregion=(0, 0, self.pil_image.width, self.pil_image.height))

        vsb = ttk.Scrollbar(frame, orient=tk.VERTICAL, command=self.canvas.yview)
        hsb = ttk.Scrollbar(frame, orient=tk.HORIZONTAL, command=self.canvas.xview)
        self.canvas.configure(yscrollcommand=vsb.set, xscrollcommand=hsb.set)

        vsb.pack(side=tk.RIGHT, fill=tk.Y)
        hsb.pack(side=tk.BOTTOM, fill=tk.X)
        self.canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        self.canvas.create_image(0, 0, anchor=tk.NW, image=self.tk_image)

        # Buttons
        btn_frame = ttk.Frame(self.root)
        btn_frame.pack(fill=tk.X, pady=5)
        ttk.Button(btn_frame, text="Save as PNG", command=self.save_png).pack(side=tk.LEFT, padx=10)
        ttk.Button(btn_frame, text="Refresh", command=self.refresh).pack(side=tk.LEFT, padx=10)
        ttk.Button(btn_frame, text="Close", command=self.root.destroy).pack(side=tk.RIGHT, padx=10)

        self.root.geometry(f"{min(self.pil_image.width + 30, 1200)}x{min(self.pil_image.height + 60, 900)}")

    def save_png(self):
        out = PROJECT_ROOT / "tools" / "batch1_sprite_overview.png"
        self.pil_image.save(out)
        print(f"Saved: {out}")

    def refresh(self):
        self.pil_image = create_overview_image()
        self.tk_image = ImageTk.PhotoImage(self.pil_image)
        self.canvas.delete("all")
        self.canvas.create_image(0, 0, anchor=tk.NW, image=self.tk_image)
        self.canvas.configure(scrollregion=(0, 0, self.pil_image.width, self.pil_image.height))

    def run(self):
        self.root.mainloop()


if __name__ == "__main__":
    app = SpriteViewerApp()
    app.run()
