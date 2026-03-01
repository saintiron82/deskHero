#!/usr/bin/env python3
"""
DeskWarrior Sprite Processor
Raw monster images -> Production-ready sprites pipeline

Pipeline:
    Stage 1: Background removal (AutoAlphaChannel.exe)
    Stage 2: Bounding box analysis + margin adjustment
    Stage 3: Resize to production standard (256x256)
    Stage 4: Horizontal flip (optional, agent decides)

Usage:
    python sprite_processor.py process <input> <output> [--flip] [--size 256] [--padding 85]
    python sprite_processor.py analyze <input>
    python sprite_processor.py batch <input_dir> <output_dir> [--size 256] [--padding 85] [--force]
"""

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile

try:
    from PIL import Image
except ImportError:
    print("Error: Pillow is not installed. Run: pip install Pillow")
    sys.exit(1)


# ============================================================================
# Path Configuration
# ============================================================================

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)
AUTO_ALPHA_EXE = os.path.join(PROJECT_ROOT, "AutoAlphaChannel", "AutoAlphaChannel.exe")

PRODUCTION_SIZE = 256


# ============================================================================
# Stage 1: Background Removal
# ============================================================================

def remove_background(input_path: str, output_path: str,
                      color: str = "#00FF00", tolerance: int = 30,
                      erosion: int = 1) -> str:
    """Remove green background using AutoAlphaChannel.exe.

    Copies input to temp file first to preserve the original.
    Returns output path on success, None on failure.
    """
    if not os.path.exists(AUTO_ALPHA_EXE):
        print(f"ERROR: AutoAlphaChannel.exe not found at {AUTO_ALPHA_EXE}")
        return None

    # Copy to temp file to avoid modifying/locking the original
    os.makedirs(os.path.dirname(output_path) or ".", exist_ok=True)
    shutil.copy2(input_path, output_path)

    cmd = [
        AUTO_ALPHA_EXE,
        "-i", output_path,
        "-mode", "1",
        "-color", color,
        "-tolerance", str(tolerance),
        "-erosion", str(erosion),
        "-overwrite",
    ]

    result = subprocess.run(cmd, capture_output=True, text=True, timeout=30)

    if result.returncode != 0:
        print(f"ERROR: AutoAlphaChannel failed: {result.stderr}")
        os.remove(output_path)
        return None

    if not os.path.exists(output_path):
        print(f"ERROR: Processed output not found: {output_path}")
        return None

    return output_path


# ============================================================================
# Stage 2: Bounding Box Analysis + Margin Adjustment
# ============================================================================

def analyze_sprite(img: Image.Image) -> dict:
    """Analyze sprite to get bounding box and fill ratio.

    Returns dict with: bbox, sprite_size, canvas_size, fill_ratio, center_offset
    """
    if img.mode != "RGBA":
        img = img.convert("RGBA")

    bbox = img.getbbox()
    if bbox is None:
        return {
            "bbox": None,
            "sprite_size": (0, 0),
            "canvas_size": img.size,
            "fill_ratio": 0.0,
            "center_offset": (0, 0),
        }

    left, top, right, bottom = bbox
    sprite_w = right - left
    sprite_h = bottom - top
    canvas_w, canvas_h = img.size

    fill_w = sprite_w / canvas_w if canvas_w > 0 else 0
    fill_h = sprite_h / canvas_h if canvas_h > 0 else 0
    fill_ratio = max(fill_w, fill_h)

    # Center offset: how far sprite center is from canvas center
    sprite_cx = left + sprite_w / 2
    sprite_cy = top + sprite_h / 2
    canvas_cx = canvas_w / 2
    canvas_cy = canvas_h / 2

    return {
        "bbox": (left, top, right, bottom),
        "sprite_size": (sprite_w, sprite_h),
        "canvas_size": (canvas_w, canvas_h),
        "fill_ratio": round(fill_ratio, 3),
        "center_offset": (round(sprite_cx - canvas_cx, 1), round(sprite_cy - canvas_cy, 1)),
    }


def adjust_margins(img: Image.Image, target_fill: float = 0.85,
                   align_bottom: bool = False) -> Image.Image:
    """Crop to bounding box, then position with target fill ratio.

    The sprite will occupy ~target_fill of the canvas (longest dimension).
    Canvas is always square.

    align_bottom: If True, sprite bottom edge touches canvas bottom (feet on ground).
                  If False, sprite is centered vertically (legacy behavior).
    """
    if img.mode != "RGBA":
        img = img.convert("RGBA")

    bbox = img.getbbox()
    if bbox is None:
        return img

    # Crop to sprite bounds
    sprite = img.crop(bbox)
    sprite_w, sprite_h = sprite.size

    # Calculate square canvas size based on target fill
    max_dim = max(sprite_w, sprite_h)
    canvas_size = int(max_dim / target_fill)

    # Create transparent canvas and position sprite
    canvas = Image.new("RGBA", (canvas_size, canvas_size), (0, 0, 0, 0))
    x_offset = (canvas_size - sprite_w) // 2
    if align_bottom:
        y_offset = canvas_size - sprite_h  # feet on ground
    else:
        y_offset = (canvas_size - sprite_h) // 2  # centered
    canvas.paste(sprite, (x_offset, y_offset), sprite)

    return canvas


# ============================================================================
# Stage 3: Resize
# ============================================================================

def resize_to_production(img: Image.Image, size: int = PRODUCTION_SIZE) -> Image.Image:
    """Resize image to production standard (square)."""
    return img.resize((size, size), Image.LANCZOS)


# ============================================================================
# Stage 4: Flip
# ============================================================================

def flip_horizontal(img: Image.Image) -> Image.Image:
    """Flip image horizontally (left-right)."""
    return img.transpose(Image.FLIP_LEFT_RIGHT)


# ============================================================================
# Full Pipeline
# ============================================================================

def process_single(input_path: str, output_path: str,
                   flip: bool = False, size: int = PRODUCTION_SIZE,
                   padding: int = 85, align_bottom: bool = False) -> dict:
    """Run the full processing pipeline on a single image.

    Returns a report dict with processing details.
    """
    report = {
        "file": os.path.basename(input_path),
        "input": input_path,
        "output": output_path,
        "status": "pending",
        "original_size": None,
        "sprite_bounds": None,
        "sprite_fill_ratio": None,
        "flipped": flip,
    }

    if not os.path.exists(input_path):
        report["status"] = "error"
        report["error"] = f"Input file not found: {input_path}"
        return report

    # Get original info
    try:
        original = Image.open(input_path)
        report["original_size"] = list(original.size)
        report["original_mode"] = original.mode
    except Exception as e:
        report["status"] = "error"
        report["error"] = f"Cannot open image: {e}"
        return report

    # Stage 1: Background removal
    with tempfile.TemporaryDirectory() as tmpdir:
        bg_removed_path = os.path.join(tmpdir, "bg_removed.png")

        # Check if image already has alpha (might not need bg removal)
        needs_bg_removal = original.mode != "RGBA" or _has_green_background(original)

        if needs_bg_removal:
            result = remove_background(input_path, bg_removed_path)
            if result is None:
                report["status"] = "error"
                report["error"] = "Background removal failed"
                return report
            img = Image.open(bg_removed_path).convert("RGBA")
        else:
            img = original.convert("RGBA")

        # Stage 2: Margin adjustment
        target_fill = padding / 100.0
        info = analyze_sprite(img)
        report["sprite_bounds"] = info["bbox"]

        img = adjust_margins(img, target_fill=target_fill, align_bottom=align_bottom)

        info_after = analyze_sprite(img)
        report["sprite_fill_ratio"] = info_after["fill_ratio"]

        # Stage 3: Resize
        img = resize_to_production(img, size)

        # Stage 4: Flip (optional)
        if flip:
            img = flip_horizontal(img)

        # Save
        os.makedirs(os.path.dirname(output_path) or ".", exist_ok=True)
        img.save(output_path, "PNG")
        report["status"] = "success"
        report["final_size"] = list(img.size)

    return report


def _has_green_background(img: Image.Image) -> bool:
    """Check if image has a green (#00FF00) background."""
    if img.mode == "RGBA":
        pixels = img.load()
        w, h = img.size
        # Sample corners
        corners = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
        green_count = 0
        for x, y in corners:
            r, g, b, a = pixels[x, y]
            if g > 200 and r < 100 and b < 100 and a > 200:
                green_count += 1
        return green_count >= 3
    elif img.mode == "RGB":
        pixels = img.load()
        w, h = img.size
        corners = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
        green_count = 0
        for x, y in corners:
            r, g, b = pixels[x, y]
            if g > 200 and r < 100 and b < 100:
                green_count += 1
        return green_count >= 3
    return False


def process_batch(input_dir: str, output_dir: str,
                  flip: bool = False, size: int = PRODUCTION_SIZE,
                  padding: int = 85, force: bool = False,
                  align_bottom: bool = False) -> list:
    """Process all PNG/JPG files in input_dir.

    Returns list of report dicts.
    """
    if not os.path.isdir(input_dir):
        print(f"ERROR: Input directory not found: {input_dir}")
        return []

    os.makedirs(output_dir, exist_ok=True)

    extensions = {".png", ".jpg", ".jpeg"}
    files = sorted([
        f for f in os.listdir(input_dir)
        if os.path.splitext(f)[1].lower() in extensions
    ])

    if not files:
        print(f"No image files found in {input_dir}")
        return []

    reports = []
    for i, filename in enumerate(files, 1):
        input_path = os.path.join(input_dir, filename)
        # Output always as PNG
        output_name = os.path.splitext(filename)[0] + ".png"
        output_path = os.path.join(output_dir, output_name)

        # Skip if already exists (unless force)
        if os.path.exists(output_path) and not force:
            reports.append({
                "file": filename,
                "status": "skipped",
                "reason": "already exists",
            })
            print(f"[{i}/{len(files)}] SKIP: {filename} (already exists)")
            continue

        print(f"[{i}/{len(files)}] Processing: {filename}")
        report = process_single(input_path, output_path,
                                flip=flip, size=size, padding=padding,
                                align_bottom=align_bottom)
        reports.append(report)

        if report["status"] == "success":
            print(f"  -> OK: {output_path}")
        else:
            print(f"  -> FAIL: {report.get('error', 'unknown')}")

    return reports


# ============================================================================
# CLI: analyze command
# ============================================================================

def cmd_analyze(args):
    """Analyze a single image without processing."""
    input_path = args.input

    if not os.path.exists(input_path):
        print(f"ERROR: File not found: {input_path}")
        sys.exit(1)

    img = Image.open(input_path)
    info = {
        "file": os.path.basename(input_path),
        "path": input_path,
        "size": list(img.size),
        "mode": img.mode,
        "has_green_bg": _has_green_background(img),
    }

    if img.mode == "RGBA":
        sprite_info = analyze_sprite(img)
        info.update(sprite_info)

    print(json.dumps(info, indent=2, ensure_ascii=False))


# ============================================================================
# CLI: process command
# ============================================================================

def cmd_process(args):
    """Process a single image through the full pipeline."""
    report = process_single(
        args.input, args.output,
        flip=args.flip,
        size=args.size,
        padding=args.padding,
        align_bottom=args.align_bottom,
    )
    print(json.dumps(report, indent=2, ensure_ascii=False))

    if report["status"] != "success":
        sys.exit(1)


# ============================================================================
# CLI: batch command
# ============================================================================

def cmd_batch(args):
    """Process all images in a directory."""
    reports = process_batch(
        args.input_dir, args.output_dir,
        flip=args.flip,
        size=args.size,
        padding=args.padding,
        force=args.force,
        align_bottom=args.align_bottom,
    )

    # Summary
    success = sum(1 for r in reports if r["status"] == "success")
    failed = sum(1 for r in reports if r["status"] == "error")
    skipped = sum(1 for r in reports if r["status"] == "skipped")

    print(f"\n--- Batch Complete ---")
    print(f"Success: {success}, Failed: {failed}, Skipped: {skipped}")
    print(json.dumps(reports, indent=2, ensure_ascii=False))


# ============================================================================
# Main
# ============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="DeskWarrior Sprite Processor",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    subparsers = parser.add_subparsers(dest="command", help="Commands")

    # analyze
    p_analyze = subparsers.add_parser("analyze", help="Analyze image without processing")
    p_analyze.add_argument("input", help="Input image path")
    p_analyze.set_defaults(func=cmd_analyze)

    # process
    p_process = subparsers.add_parser("process", help="Process single image")
    p_process.add_argument("input", help="Input image path")
    p_process.add_argument("output", help="Output image path")
    p_process.add_argument("--flip", action="store_true", help="Flip horizontally")
    p_process.add_argument("--size", type=int, default=PRODUCTION_SIZE, help="Output size (default: 256)")
    p_process.add_argument("--padding", type=int, default=85, help="Sprite fill ratio %% (default: 85)")
    p_process.add_argument("--align-bottom", action="store_true", help="Align sprite to bottom (feet on ground)")
    p_process.set_defaults(func=cmd_process)

    # batch
    p_batch = subparsers.add_parser("batch", help="Process all images in directory")
    p_batch.add_argument("input_dir", help="Input directory")
    p_batch.add_argument("output_dir", help="Output directory")
    p_batch.add_argument("--flip", action="store_true", help="Flip all horizontally")
    p_batch.add_argument("--size", type=int, default=PRODUCTION_SIZE, help="Output size (default: 256)")
    p_batch.add_argument("--padding", type=int, default=85, help="Sprite fill ratio %% (default: 85)")
    p_batch.add_argument("--force", action="store_true", help="Overwrite existing files")
    p_batch.add_argument("--align-bottom", action="store_true", help="Align sprite to bottom (feet on ground)")
    p_batch.set_defaults(func=cmd_batch)

    args = parser.parse_args()

    if args.command is None:
        parser.print_help()
        sys.exit(1)

    try:
        args.func(args)
    except KeyboardInterrupt:
        print("\nInterrupted.")
        sys.exit(1)
    except Exception as e:
        print(f"ERROR: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
