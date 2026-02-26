#!/usr/bin/env python3
"""
스프라이트 경로 제거 도구

batch_XX.json 파일에서 sprite 필드를 제거하여 파일 크기를 줄입니다.
제거 후에는 ResourceManager가 동적으로 경로를 생성합니다.

사용법:
    python tools/remove_sprite_paths.py config/monsters/batch_01.json
    python tools/remove_sprite_paths.py config/monsters/batch_01.json --dry-run
"""

import json
import sys
from pathlib import Path
from typing import Dict, Any


def count_sprite_fields(data: Dict[str, Any]) -> int:
    """sprite 필드 개수 세기"""
    count = 0

    for monster in data.get('monsters', []):
        for variation in monster.get('variations', {}).values():
            if 'sprite' in variation:
                count += 1

    for boss in data.get('bosses', []):
        for variation in boss.get('variations', {}).values():
            if 'sprite' in variation:
                count += 1

    return count


def remove_sprite_paths(data: Dict[str, Any]) -> Dict[str, Any]:
    """sprite 필드 제거"""
    removed = 0

    for monster in data.get('monsters', []):
        for variation in monster.get('variations', {}).values():
            if 'sprite' in variation:
                del variation['sprite']
                removed += 1

    for boss in data.get('bosses', []):
        for variation in boss.get('variations', {}).values():
            if 'sprite' in variation:
                del variation['sprite']
                removed += 1

    return data, removed


def main():
    if len(sys.argv) < 2:
        print("Usage: python remove_sprite_paths.py <batch_file> [--dry-run]")
        print("Example: python remove_sprite_paths.py config/monsters/batch_01.json")
        sys.exit(1)

    batch_file = Path(sys.argv[1])
    dry_run = '--dry-run' in sys.argv

    if not batch_file.exists():
        print(f"❌ File not found: {batch_file}")
        sys.exit(1)

    # 원본 파일 크기
    original_size = batch_file.stat().st_size

    # JSON 로드
    with open(batch_file, 'r', encoding='utf-8') as f:
        data = json.load(f)

    # sprite 필드 개수 확인
    sprite_count = count_sprite_fields(data)
    print(f"[INFO] Found {sprite_count} sprite fields in {batch_file.name}")

    if sprite_count == 0:
        print("[OK] No sprite fields to remove. File already clean.")
        return

    if dry_run:
        print("[DRY RUN] No changes will be made")
        print(f"[DRY RUN] Would remove {sprite_count} sprite fields")
        return

    # sprite 필드 제거
    data, removed = remove_sprite_paths(data)

    # 백업 생성
    backup_file = batch_file.with_suffix('.json.bak')
    with open(backup_file, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    # 파일 저장
    with open(batch_file, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    # 새 파일 크기
    new_size = batch_file.stat().st_size
    saved = original_size - new_size
    saved_percent = (saved / original_size) * 100

    print(f"[OK] Removed {removed} sprite fields from {batch_file.name}")
    print(f"[SIZE] File size: {original_size:,} bytes -> {new_size:,} bytes")
    print(f"[SAVED] {saved:,} bytes ({saved_percent:.1f}%)")
    print(f"[BACKUP] Created: {backup_file.name}")
    print()
    print("[INFO] ResourceManager will now generate sprite paths dynamically")


if __name__ == '__main__':
    main()
