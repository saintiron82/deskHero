"""
배치 이미지 처리 스크립트
Raw_Green 폴더의 모든 이미지를 Production 폴더로 처리
"""

import os
import subprocess
import shutil
from pathlib import Path

# 경로 설정
SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent
RAW_GREEN = PROJECT_ROOT / "Assets" / "Images" / "Raw_Green"
PRODUCTION = PROJECT_ROOT / "Assets" / "Images" / "Production"
AUTO_ALPHA = PROJECT_ROOT / "AutoAlphaChannel" / "AutoAlphaChannel.exe"

def ensure_production_folder():
    """Production 폴더가 없으면 생성"""
    PRODUCTION.mkdir(parents=True, exist_ok=True)
    print(f"✅ Production 폴더 준비됨: {PRODUCTION}")

def process_single_image(input_path: Path) -> bool:
    """단일 이미지 처리 (녹색 배경 제거 후 Production으로 이동)"""
    try:
        # AutoAlphaChannel 실행 (녹색 배경 자동 제거)
        cmd = [
            str(AUTO_ALPHA),
            "-i", str(input_path),
            "-mode", "0",  # Auto 모드
            "-erosion", "1"
        ]
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=30)
        
        # 처리된 파일 찾기 (원본명 + A.png)
        base_name = input_path.stem
        processed_name = f"{base_name}A.png"
        processed_path = input_path.parent / processed_name
        
        if processed_path.exists():
            # Production 폴더로 이동 (원래 이름으로)
            target_path = PRODUCTION / input_path.name
            shutil.move(str(processed_path), str(target_path))
            print(f"  ✅ {input_path.name} → Production/{input_path.name}")
            return True
        else:
            print(f"  ⚠️ 처리 결과를 찾을 수 없음: {processed_name}")
            return False
            
    except Exception as e:
        print(f"  ❌ 오류: {e}")
        return False

def batch_process():
    """Raw_Green 폴더의 모든 PNG 파일 처리"""
    ensure_production_folder()
    
    # 이미 Production에 있는 파일 목록
    existing = {f.name for f in PRODUCTION.glob("*.png")}
    
    # Raw_Green의 모든 PNG 파일
    all_images = list(RAW_GREEN.glob("*.png"))
    
    # 아직 처리되지 않은 파일만 필터링
    to_process = [f for f in all_images if f.name not in existing]
    
    print(f"\n📊 처리 현황:")
    print(f"   Raw_Green 총 파일: {len(all_images)}")
    print(f"   Production 기존 파일: {len(existing)}")
    print(f"   처리 대상: {len(to_process)}")
    print()
    
    if not to_process:
        print("✅ 모든 파일이 이미 처리되었습니다!")
        return
    
    success = 0
    failed = 0
    
    for i, img_path in enumerate(to_process, 1):
        print(f"[{i}/{len(to_process)}] 처리 중: {img_path.name}")
        if process_single_image(img_path):
            success += 1
        else:
            failed += 1
    
    print(f"\n📊 완료: 성공 {success}, 실패 {failed}")

if __name__ == "__main__":
    batch_process()
