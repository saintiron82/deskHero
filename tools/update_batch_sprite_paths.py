#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
배치 JSON 파일의 스프라이트 경로 자동 업데이트 도구
Production/BatchN → Batches/Batch_0N 변환
"""

import json
import os
import sys
from pathlib import Path

# Windows 콘솔 UTF-8 출력 설정
if sys.platform == 'win32':
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'strict')
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer, 'strict')

def update_batch_paths(batch_file: str, old_prefix: str, new_prefix: str) -> bool:
    """
    배치 JSON 파일의 스프라이트 경로 업데이트

    Args:
        batch_file: 업데이트할 JSON 파일 경로
        old_prefix: 기존 경로 접두사 (예: "Production/Batch1")
        new_prefix: 새 경로 접두사 (예: "Batches/Batch_01")

    Returns:
        bool: 성공 여부
    """
    try:
        # JSON 읽기
        with open(batch_file, 'r', encoding='utf-8') as f:
            data = json.load(f)

        updated_count = 0

        # 몬스터 경로 업데이트
        for monster in data.get('monsters', []):
            for element, variation in monster.get('variations', {}).items():
                if 'sprite' in variation:
                    old_path = variation['sprite']
                    if old_prefix in old_path:
                        new_path = old_path.replace(old_prefix, new_prefix)
                        variation['sprite'] = new_path
                        updated_count += 1
                        print(f"  ✓ {element}: {old_path} → {new_path}")

        # 보스 경로 업데이트
        for boss in data.get('bosses', []):
            for element, variation in boss.get('variations', {}).items():
                if 'sprite' in variation:
                    old_path = variation['sprite']
                    if old_prefix in old_path:
                        new_path = old_path.replace(old_prefix, new_prefix)
                        variation['sprite'] = new_path
                        updated_count += 1
                        print(f"  ✓ {element} (Boss): {old_path} → {new_path}")

        if updated_count == 0:
            print(f"⚠️  No paths updated in {batch_file} (already correct?)")
            return True

        # JSON 저장 (포맷 유지)
        with open(batch_file, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

        print(f"✅ Updated {updated_count} sprite paths in {batch_file}")
        return True

    except FileNotFoundError:
        print(f"❌ File not found: {batch_file}")
        return False
    except json.JSONDecodeError as e:
        print(f"❌ JSON parsing error in {batch_file}: {e}")
        return False
    except Exception as e:
        print(f"❌ Unexpected error: {e}")
        return False

def main():
    """메인 실행 함수"""
    # 프로젝트 루트 경로
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    config_dir = project_root / "config" / "monsters"

    # 배치별 경로 매핑
    batch_mappings = [
        {
            "file": config_dir / "batch_01.json",
            "old_prefix": "Production/Batch1",
            "new_prefix": "Batches/Batch_01"
        },
        {
            "file": config_dir / "batch_02.json",
            "old_prefix": "Production/Batch2",
            "new_prefix": "Batches/Batch_02"
        },
        {
            "file": config_dir / "batch_03.json",
            "old_prefix": "Production/Batch3",
            "new_prefix": "Batches/Batch_03"
        }
    ]

    print("=" * 60)
    print("배치 스프라이트 경로 업데이트")
    print("=" * 60)
    print()

    success_count = 0
    skip_count = 0

    for mapping in batch_mappings:
        batch_file = mapping["file"]
        batch_name = batch_file.name

        print(f"[{batch_name}]")

        if not batch_file.exists():
            print(f"⏭️  Skipped: {batch_file} (not exists)")
            skip_count += 1
            print()
            continue

        if update_batch_paths(
            str(batch_file),
            mapping["old_prefix"],
            mapping["new_prefix"]
        ):
            success_count += 1

        print()

    # 결과 요약
    print("=" * 60)
    print(f"✅ Successfully updated: {success_count} files")
    if skip_count > 0:
        print(f"⏭️  Skipped: {skip_count} files (not found)")
    print("=" * 60)

    return 0 if success_count > 0 else 1

if __name__ == "__main__":
    sys.exit(main())
