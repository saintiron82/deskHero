"""
이미지 유틸리티 도구 (Image Utility Tools)
==========================================
몬스터 스프라이트 이미지 편집을 위한 API 함수들

사용 예시:
    from image_utils import flip_horizontal, resize_image, adjust_margin
    
    # 좌우 반전
    flip_horizontal("input.png", "output.png")
    
    # 크기 조절 (50% 축소)
    resize_image("input.png", "output.png", scale=0.5)
    
    # 여백 조절 (상하 10% 패딩 추가)
    adjust_margin("input.png", "output.png", padding_percent=10)
"""

from PIL import Image
import os
import sys


def flip_horizontal(input_path: str, output_path: str = None) -> str:
    """
    이미지를 좌우 반전합니다.
    
    Args:
        input_path: 입력 이미지 경로
        output_path: 출력 이미지 경로 (기본값: input_flipped.png)
    
    Returns:
        저장된 파일 경로
    """
    if output_path is None:
        base, ext = os.path.splitext(input_path)
        output_path = f"{base}_flipped{ext}"
    
    img = Image.open(input_path)
    flipped = img.transpose(Image.FLIP_LEFT_RIGHT)
    flipped.save(output_path)
    print(f"✅ 좌우 반전 완료: {output_path}")
    return output_path


def resize_image(input_path: str, output_path: str = None, 
                 scale: float = None, width: int = None, height: int = None) -> str:
    """
    이미지 크기를 조절합니다.
    
    Args:
        input_path: 입력 이미지 경로
        output_path: 출력 이미지 경로 (기본값: input_resized.png)
        scale: 배율 (예: 0.5 = 50%, 2.0 = 200%)
        width: 목표 너비 (높이는 비율 유지)
        height: 목표 높이 (너비는 비율 유지)
    
    Returns:
        저장된 파일 경로
    """
    if output_path is None:
        base, ext = os.path.splitext(input_path)
        output_path = f"{base}_resized{ext}"
    
    img = Image.open(input_path)
    original_width, original_height = img.size
    
    if scale is not None:
        new_width = int(original_width * scale)
        new_height = int(original_height * scale)
    elif width is not None:
        new_width = width
        new_height = int(original_height * (width / original_width))
    elif height is not None:
        new_height = height
        new_width = int(original_width * (height / original_height))
    else:
        raise ValueError("scale, width, 또는 height 중 하나는 지정해야 합니다.")
    
    resized = img.resize((new_width, new_height), Image.LANCZOS)
    resized.save(output_path)
    print(f"✅ 크기 조절 완료: {original_width}x{original_height} → {new_width}x{new_height}")
    print(f"   저장됨: {output_path}")
    return output_path


def adjust_margin(input_path: str, output_path: str = None,
                  padding_percent: float = 5,
                  background_color: tuple = (0, 255, 0),
                  target_size: tuple = None) -> str:
    """
    이미지 여백을 조절합니다. 스프라이트를 중앙에 배치하고 패딩을 추가합니다.
    
    Args:
        input_path: 입력 이미지 경로
        output_path: 출력 이미지 경로 (기본값: input_margin.png)
        padding_percent: 상하좌우 패딩 비율 (%) - 기본값 5%
        background_color: 배경색 RGB (기본값: 녹색 #00FF00)
        target_size: 목표 캔버스 크기 (width, height) - None이면 원본 비율 유지
    
    Returns:
        저장된 파일 경로
    """
    if output_path is None:
        base, ext = os.path.splitext(input_path)
        output_path = f"{base}_margin{ext}"
    
    img = Image.open(input_path).convert("RGBA")
    original_width, original_height = img.size
    
    # 투명하지 않은 영역(스프라이트) 찾기
    bbox = img.getbbox()
    if bbox is None:
        print("⚠️ 이미지에 콘텐츠가 없습니다.")
        return input_path
    
    # 스프라이트만 크롭
    sprite = img.crop(bbox)
    sprite_width, sprite_height = sprite.size
    
    # 목표 캔버스 크기 결정
    if target_size:
        canvas_width, canvas_height = target_size
    else:
        # 패딩을 고려한 캔버스 크기 계산
        padding_ratio = padding_percent / 100
        canvas_width = int(sprite_width / (1 - 2 * padding_ratio))
        canvas_height = int(sprite_height / (1 - 2 * padding_ratio))
    
    # 새 캔버스 생성 (RGBA로 생성 후 RGB로 변환)
    canvas = Image.new("RGBA", (canvas_width, canvas_height), (*background_color, 255))
    
    # 스프라이트를 중앙에 배치
    x_offset = (canvas_width - sprite_width) // 2
    y_offset = (canvas_height - sprite_height) // 2
    
    # 스프라이트 합성
    canvas.paste(sprite, (x_offset, y_offset), sprite)
    
    # RGB로 변환 (배경색 적용)
    final = Image.new("RGB", canvas.size, background_color)
    final.paste(canvas, mask=canvas.split()[3])
    
    final.save(output_path)
    print(f"✅ 여백 조절 완료:")
    print(f"   원본: {original_width}x{original_height}")
    print(f"   스프라이트: {sprite_width}x{sprite_height}")
    print(f"   캔버스: {canvas_width}x{canvas_height}")
    print(f"   패딩: 상하좌우 약 {padding_percent}%")
    print(f"   저장됨: {output_path}")
    return output_path


def remove_background(input_path: str, output_path: str = None,
                       color: str = "#00FF00", tolerance: int = 30,
                       erosion: int = 1) -> str:
    """
    AutoAlphaChannel.exe를 사용하여 배경을 투명하게 변환합니다.
    
    Args:
        input_path: 입력 이미지 경로
        output_path: 출력 폴더 경로 (기본값: 원본 위치)
        color: 제거할 배경색 Hex 코드 (기본값: #00FF00 녹색)
        tolerance: 색상 허용 오차 0~100 (기본값: 30)
        erosion: 가장자리 깎기 픽셀 0~10 (기본값: 1)
    
    Returns:
        실행 결과 메시지
    """
    import subprocess
    
    # AutoAlphaChannel.exe 경로 찾기
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.dirname(script_dir)
    exe_path = os.path.join(project_root, "AutoAlphaChannel", "AutoAlphaChannel.exe")
    
    if not os.path.exists(exe_path):
        print(f"❌ AutoAlphaChannel.exe를 찾을 수 없습니다: {exe_path}")
        return None
    
    cmd = [
        exe_path,
        "-i", input_path,
        "-mode", "1",  # Single color mode
        "-color", color,
        "-tolerance", str(tolerance),
        "-erosion", str(erosion)
    ]
    
    if output_path:
        cmd.extend(["-o", output_path])
    
    result = subprocess.run(cmd, capture_output=True, text=True)
    
    if result.returncode == 0:
        print(f"✅ 배경 제거 완료: {input_path}")
        if result.stdout:
            print(result.stdout)
    else:
        print(f"❌ 오류 발생:")
        print(result.stderr)
    
    return result.stdout


# CLI 지원
if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("""
사용법:
    python image_utils.py <명령> <입력파일> [옵션]

명령:
    flip <입력> [출력]              - 좌우 반전
    resize <입력> [출력] <배율>     - 크기 조절 (예: 0.5, 2.0)
    margin <입력> [출력] <패딩%>    - 여백 조절 (예: 5, 10)
    removebg <입력> [출력폴더]      - 녹색 배경 제거 (AutoAlphaChannel 사용)

예시:
    python image_utils.py flip monster.png
    python image_utils.py resize monster.png output.png 0.8
    python image_utils.py margin monster.png output.png 10
    python image_utils.py removebg monster.png ./output/
        """)
        sys.exit(1)
    
    command = sys.argv[1].lower()
    input_file = sys.argv[2]
    
    if command == "flip":
        output = sys.argv[3] if len(sys.argv) > 3 else None
        flip_horizontal(input_file, output)
    
    elif command == "resize":
        output = sys.argv[3] if len(sys.argv) > 3 and not sys.argv[3].replace('.', '').isdigit() else None
        scale_idx = 4 if output else 3
        scale = float(sys.argv[scale_idx]) if len(sys.argv) > scale_idx else 1.0
        resize_image(input_file, output, scale=scale)
    
    elif command == "margin":
        output = sys.argv[3] if len(sys.argv) > 3 and not sys.argv[3].replace('.', '').isdigit() else None
        padding_idx = 4 if output else 3
        padding = float(sys.argv[padding_idx]) if len(sys.argv) > padding_idx else 5.0
        adjust_margin(input_file, output, padding_percent=padding)
    
    elif command == "removebg":
        output = sys.argv[3] if len(sys.argv) > 3 else None
        remove_background(input_file, output)
    
    else:
        print(f"❌ 알 수 없는 명령: {command}")
        sys.exit(1)
