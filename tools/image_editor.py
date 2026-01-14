#!/usr/bin/env python3
"""
DeskHero Image Editor CLI
이미지 반전, 회전, 중심점 이동, 색조 변경 등의 기능을 제공하는 CLI 도구

Usage:
    python image_editor.py <command> [options] <input> <output>

Commands:
    flip      - 이미지 반전 (horizontal/vertical)
    rotate    - 이미지 회전 (각도 지정)
    offset    - 중심점/피벗 이동
    hue       - 색조(Hue) 변경
    crop      - 이미지 크롭
    resize    - 이미지 리사이즈
    batch     - JSON 설정으로 일괄 처리

Examples:
    python image_editor.py flip -d horizontal monster.png monster_flipped.png
    python image_editor.py rotate -a 90 monster.png monster_rotated.png
    python image_editor.py offset -x 10 -y -5 monster.png monster_offset.png
    python image_editor.py hue -t 180 monster.png monster_ice.png
    python image_editor.py crop -l 10 -t 10 -r 90 -b 90 monster.png monster_cropped.png
    python image_editor.py batch config.json
"""

import argparse
import colorsys
import json
import os
import sys

try:
    from PIL import Image
except ImportError:
    print("Error: Pillow (PIL) is not installed.")
    print("Please run: pip install Pillow")
    sys.exit(1)


# ============================================================================
# Image Operations
# ============================================================================

def flip_image(img: Image.Image, direction: str) -> Image.Image:
    """이미지 반전"""
    if direction == "horizontal" or direction == "h":
        return img.transpose(Image.FLIP_LEFT_RIGHT)
    elif direction == "vertical" or direction == "v":
        return img.transpose(Image.FLIP_TOP_BOTTOM)
    elif direction == "both":
        return img.transpose(Image.FLIP_LEFT_RIGHT).transpose(Image.FLIP_TOP_BOTTOM)
    else:
        raise ValueError(f"Unknown flip direction: {direction}")


def rotate_image(img: Image.Image, angle: float, expand: bool = True, fill_color: tuple = (0, 0, 0, 0)) -> Image.Image:
    """이미지 회전"""
    return img.rotate(angle, expand=expand, fillcolor=fill_color, resample=Image.BICUBIC)


def offset_image(img: Image.Image, offset_x: int, offset_y: int, fill_color: tuple = (0, 0, 0, 0)) -> Image.Image:
    """이미지 중심점/피벗 이동 (캔버스 내에서 이미지 위치 조정)"""
    width, height = img.size
    new_img = Image.new("RGBA", (width, height), fill_color)
    new_img.paste(img, (offset_x, offset_y), img if img.mode == "RGBA" else None)
    return new_img


def expand_canvas(img: Image.Image, left: int, top: int, right: int, bottom: int, fill_color: tuple = (0, 0, 0, 0)) -> Image.Image:
    """캔버스 확장 (이미지 주변에 여백 추가)"""
    width, height = img.size
    new_width = width + left + right
    new_height = height + top + bottom
    new_img = Image.new("RGBA", (new_width, new_height), fill_color)
    new_img.paste(img, (left, top), img if img.mode == "RGBA" else None)
    return new_img


def crop_image(img: Image.Image, left: int, top: int, right: int, bottom: int) -> Image.Image:
    """이미지 크롭"""
    width, height = img.size
    return img.crop((left, top, width - right, height - bottom))


def resize_image(img: Image.Image, width: int = None, height: int = None, scale: float = None) -> Image.Image:
    """이미지 리사이즈"""
    orig_width, orig_height = img.size

    if scale:
        new_width = int(orig_width * scale)
        new_height = int(orig_height * scale)
    elif width and height:
        new_width, new_height = width, height
    elif width:
        ratio = width / orig_width
        new_width = width
        new_height = int(orig_height * ratio)
    elif height:
        ratio = height / orig_height
        new_height = height
        new_width = int(orig_width * ratio)
    else:
        return img

    return img.resize((new_width, new_height), Image.LANCZOS)


def shift_hue(img: Image.Image, target_hue: int, preserve_bg: bool = True) -> Image.Image:
    """색조(Hue) 변경"""
    img = img.convert("RGBA")
    pixels = img.load()
    width, height = img.size

    # 1. Calculate average hue of the object
    total_hue = 0
    count = 0

    for y in range(0, height, 5):
        for x in range(0, width, 5):
            r, g, b, a = pixels[x, y]
            if a < 10:
                continue
            # Skip green background if preserving
            if preserve_bg and g > 200 and r < 100 and b < 100:
                continue

            h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            if s > 0.1:  # Only count colored pixels
                total_hue += h
                count += 1

    if count == 0:
        print("Warning: No colored pixels found")
        return img

    avg_hue = total_hue / count
    target_hue_normalized = target_hue / 360.0
    hue_shift = target_hue_normalized - avg_hue

    # 2. Apply hue shift
    new_img = Image.new("RGBA", (width, height))
    new_pixels = new_img.load()

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]

            # Skip transparent or green background
            if a < 10:
                new_pixels[x, y] = (r, g, b, a)
                continue
            if preserve_bg and g > 200 and r < 100 and b < 100:
                new_pixels[x, y] = (r, g, b, a)
                continue

            h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            new_h = (h + hue_shift) % 1.0

            new_r, new_g, new_b = colorsys.hsv_to_rgb(new_h, s, v)
            new_pixels[x, y] = (int(new_r * 255), int(new_g * 255), int(new_b * 255), a)

    return new_img


def adjust_saturation(img: Image.Image, factor: float) -> Image.Image:
    """채도 조정 (factor: 0.0 = 무채색, 1.0 = 원본, 2.0 = 2배)"""
    img = img.convert("RGBA")
    pixels = img.load()
    width, height = img.size

    new_img = Image.new("RGBA", (width, height))
    new_pixels = new_img.load()

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a < 10:
                new_pixels[x, y] = (r, g, b, a)
                continue

            h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            new_s = min(1.0, s * factor)

            new_r, new_g, new_b = colorsys.hsv_to_rgb(h, new_s, v)
            new_pixels[x, y] = (int(new_r * 255), int(new_g * 255), int(new_b * 255), a)

    return new_img


def adjust_brightness(img: Image.Image, factor: float) -> Image.Image:
    """밝기 조정 (factor: 0.0 = 검정, 1.0 = 원본, 2.0 = 2배)"""
    img = img.convert("RGBA")
    pixels = img.load()
    width, height = img.size

    new_img = Image.new("RGBA", (width, height))
    new_pixels = new_img.load()

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a < 10:
                new_pixels[x, y] = (r, g, b, a)
                continue

            h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            new_v = min(1.0, v * factor)

            new_r, new_g, new_b = colorsys.hsv_to_rgb(h, s, new_v)
            new_pixels[x, y] = (int(new_r * 255), int(new_g * 255), int(new_b * 255), a)

    return new_img


# ============================================================================
# Batch Processing
# ============================================================================

def process_batch(config_path: str):
    """JSON 설정 파일로 일괄 처리

    Config format:
    {
        "base_dir": "./images",
        "output_dir": "./output",
        "operations": [
            {
                "input": "monster.png",
                "output": "monster_flipped.png",
                "commands": [
                    {"type": "flip", "direction": "horizontal"},
                    {"type": "hue", "target": 180}
                ]
            }
        ]
    }
    """
    with open(config_path, 'r', encoding='utf-8') as f:
        config = json.load(f)

    base_dir = config.get('base_dir', '.')
    output_dir = config.get('output_dir', base_dir)

    os.makedirs(output_dir, exist_ok=True)

    for op in config.get('operations', []):
        input_path = os.path.join(base_dir, op['input'])
        output_path = os.path.join(output_dir, op['output'])

        if not os.path.exists(input_path):
            print(f"Skip (not found): {input_path}")
            continue

        print(f"Processing: {op['input']} -> {op['output']}")
        img = Image.open(input_path).convert("RGBA")

        for cmd in op.get('commands', []):
            cmd_type = cmd['type']

            if cmd_type == 'flip':
                img = flip_image(img, cmd.get('direction', 'horizontal'))
            elif cmd_type == 'rotate':
                img = rotate_image(img, cmd.get('angle', 0), cmd.get('expand', True))
            elif cmd_type == 'offset':
                img = offset_image(img, cmd.get('x', 0), cmd.get('y', 0))
            elif cmd_type == 'expand':
                img = expand_canvas(img, cmd.get('left', 0), cmd.get('top', 0),
                                   cmd.get('right', 0), cmd.get('bottom', 0))
            elif cmd_type == 'crop':
                img = crop_image(img, cmd.get('left', 0), cmd.get('top', 0),
                                cmd.get('right', 0), cmd.get('bottom', 0))
            elif cmd_type == 'resize':
                img = resize_image(img, cmd.get('width'), cmd.get('height'), cmd.get('scale'))
            elif cmd_type == 'hue':
                img = shift_hue(img, cmd.get('target', 0), cmd.get('preserve_bg', True))
            elif cmd_type == 'saturation':
                img = adjust_saturation(img, cmd.get('factor', 1.0))
            elif cmd_type == 'brightness':
                img = adjust_brightness(img, cmd.get('factor', 1.0))
            else:
                print(f"  Unknown command: {cmd_type}")

        img.save(output_path)
        print(f"  Saved: {output_path}")


# ============================================================================
# CLI Commands
# ============================================================================

def cmd_flip(args):
    img = Image.open(args.input).convert("RGBA")
    result = flip_image(img, args.direction)
    result.save(args.output)
    print(f"Flipped ({args.direction}): {args.input} -> {args.output}")


def cmd_rotate(args):
    img = Image.open(args.input).convert("RGBA")
    result = rotate_image(img, args.angle, args.expand)
    result.save(args.output)
    print(f"Rotated ({args.angle}deg): {args.input} -> {args.output}")


def cmd_offset(args):
    img = Image.open(args.input).convert("RGBA")
    result = offset_image(img, args.x, args.y)
    result.save(args.output)
    print(f"Offset ({args.x}, {args.y}): {args.input} -> {args.output}")


def cmd_expand(args):
    img = Image.open(args.input).convert("RGBA")
    result = expand_canvas(img, args.left, args.top, args.right, args.bottom)
    result.save(args.output)
    print(f"Expanded canvas: {args.input} -> {args.output}")


def cmd_crop(args):
    img = Image.open(args.input).convert("RGBA")
    result = crop_image(img, args.left, args.top, args.right, args.bottom)
    result.save(args.output)
    print(f"Cropped: {args.input} -> {args.output}")


def cmd_resize(args):
    img = Image.open(args.input).convert("RGBA")
    result = resize_image(img, args.width, args.height, args.scale)
    result.save(args.output)
    print(f"Resized: {args.input} -> {args.output} ({result.size})")


def cmd_hue(args):
    img = Image.open(args.input).convert("RGBA")
    result = shift_hue(img, args.target, not args.include_bg)
    result.save(args.output)
    print(f"Hue shifted ({args.target}): {args.input} -> {args.output}")


def cmd_saturation(args):
    img = Image.open(args.input).convert("RGBA")
    result = adjust_saturation(img, args.factor)
    result.save(args.output)
    print(f"Saturation adjusted ({args.factor}x): {args.input} -> {args.output}")


def cmd_brightness(args):
    img = Image.open(args.input).convert("RGBA")
    result = adjust_brightness(img, args.factor)
    result.save(args.output)
    print(f"Brightness adjusted ({args.factor}x): {args.input} -> {args.output}")


def cmd_batch(args):
    process_batch(args.config)
    print("Batch processing complete.")


def cmd_info(args):
    """이미지 정보 출력"""
    img = Image.open(args.input)
    print(f"File: {args.input}")
    print(f"Format: {img.format}")
    print(f"Mode: {img.mode}")
    print(f"Size: {img.size[0]} x {img.size[1]}")
    if hasattr(img, 'info'):
        for k, v in img.info.items():
            print(f"{k}: {v}")


# ============================================================================
# Main
# ============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="DeskHero Image Editor CLI",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__
    )
    subparsers = parser.add_subparsers(dest='command', help='Commands')

    # flip
    p_flip = subparsers.add_parser('flip', help='Flip image')
    p_flip.add_argument('-d', '--direction', choices=['horizontal', 'h', 'vertical', 'v', 'both'],
                        default='horizontal', help='Flip direction')
    p_flip.add_argument('input', help='Input image path')
    p_flip.add_argument('output', help='Output image path')
    p_flip.set_defaults(func=cmd_flip)

    # rotate
    p_rotate = subparsers.add_parser('rotate', help='Rotate image')
    p_rotate.add_argument('-a', '--angle', type=float, required=True, help='Rotation angle (degrees)')
    p_rotate.add_argument('--no-expand', dest='expand', action='store_false', help='Do not expand canvas')
    p_rotate.add_argument('input', help='Input image path')
    p_rotate.add_argument('output', help='Output image path')
    p_rotate.set_defaults(func=cmd_rotate)

    # offset
    p_offset = subparsers.add_parser('offset', help='Offset image position (move pivot)')
    p_offset.add_argument('-x', type=int, default=0, help='X offset (pixels)')
    p_offset.add_argument('-y', type=int, default=0, help='Y offset (pixels)')
    p_offset.add_argument('input', help='Input image path')
    p_offset.add_argument('output', help='Output image path')
    p_offset.set_defaults(func=cmd_offset)

    # expand
    p_expand = subparsers.add_parser('expand', help='Expand canvas (add padding)')
    p_expand.add_argument('-l', '--left', type=int, default=0, help='Left padding')
    p_expand.add_argument('-t', '--top', type=int, default=0, help='Top padding')
    p_expand.add_argument('-r', '--right', type=int, default=0, help='Right padding')
    p_expand.add_argument('-b', '--bottom', type=int, default=0, help='Bottom padding')
    p_expand.add_argument('input', help='Input image path')
    p_expand.add_argument('output', help='Output image path')
    p_expand.set_defaults(func=cmd_expand)

    # crop
    p_crop = subparsers.add_parser('crop', help='Crop image')
    p_crop.add_argument('-l', '--left', type=int, default=0, help='Left crop')
    p_crop.add_argument('-t', '--top', type=int, default=0, help='Top crop')
    p_crop.add_argument('-r', '--right', type=int, default=0, help='Right crop')
    p_crop.add_argument('-b', '--bottom', type=int, default=0, help='Bottom crop')
    p_crop.add_argument('input', help='Input image path')
    p_crop.add_argument('output', help='Output image path')
    p_crop.set_defaults(func=cmd_crop)

    # resize
    p_resize = subparsers.add_parser('resize', help='Resize image')
    p_resize.add_argument('-w', '--width', type=int, help='Target width')
    p_resize.add_argument('-H', '--height', type=int, help='Target height')
    p_resize.add_argument('-s', '--scale', type=float, help='Scale factor')
    p_resize.add_argument('input', help='Input image path')
    p_resize.add_argument('output', help='Output image path')
    p_resize.set_defaults(func=cmd_resize)

    # hue
    p_hue = subparsers.add_parser('hue', help='Shift hue')
    p_hue.add_argument('-t', '--target', type=int, required=True, help='Target hue (0-360)')
    p_hue.add_argument('--include-bg', action='store_true', help='Include green background in hue shift')
    p_hue.add_argument('input', help='Input image path')
    p_hue.add_argument('output', help='Output image path')
    p_hue.set_defaults(func=cmd_hue)

    # saturation
    p_sat = subparsers.add_parser('saturation', help='Adjust saturation')
    p_sat.add_argument('-f', '--factor', type=float, required=True, help='Saturation factor (0.0-2.0)')
    p_sat.add_argument('input', help='Input image path')
    p_sat.add_argument('output', help='Output image path')
    p_sat.set_defaults(func=cmd_saturation)

    # brightness
    p_brt = subparsers.add_parser('brightness', help='Adjust brightness')
    p_brt.add_argument('-f', '--factor', type=float, required=True, help='Brightness factor (0.0-2.0)')
    p_brt.add_argument('input', help='Input image path')
    p_brt.add_argument('output', help='Output image path')
    p_brt.set_defaults(func=cmd_brightness)

    # batch
    p_batch = subparsers.add_parser('batch', help='Batch process from JSON config')
    p_batch.add_argument('config', help='JSON config file path')
    p_batch.set_defaults(func=cmd_batch)

    # info
    p_info = subparsers.add_parser('info', help='Show image info')
    p_info.add_argument('input', help='Input image path')
    p_info.set_defaults(func=cmd_info)

    args = parser.parse_args()

    if args.command is None:
        parser.print_help()
        sys.exit(1)

    try:
        args.func(args)
    except FileNotFoundError as e:
        print(f"Error: File not found - {e}")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
