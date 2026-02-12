#!/usr/bin/env python3
"""
ResourceManager Integration Test

테스트 시나리오:
1. JSON에서 sprite 필드 제거
2. 게임 실행
3. 스프라이트 경로가 동적으로 생성되는지 확인
4. 롤백

사용법:
    python tools/test_resource_manager_integration.py
"""

import json
import shutil
from pathlib import Path


def main():
    print("=" * 60)
    print("ResourceManager Integration Test")
    print("=" * 60)
    print()

    batch_file = Path("config/monsters/batch_01.json")

    if not batch_file.exists():
        print(f"[ERROR] {batch_file} not found")
        return

    # Step 1: 백업 생성
    backup_file = batch_file.with_suffix('.json.test_backup')
    shutil.copy(batch_file, backup_file)
    print(f"[1/5] Backup created: {backup_file.name}")

    # Step 2: 원본 분석
    with open(batch_file, 'r', encoding='utf-8') as f:
        original_data = json.load(f)

    sprite_count = 0
    for monster in original_data.get('monsters', []):
        for variation in monster.get('variations', {}).values():
            if 'sprite' in variation:
                sprite_count += 1

    print(f"[2/5] Found {sprite_count} sprite fields in original")

    # Step 3: sprite 필드 제거
    test_data = json.loads(json.dumps(original_data))  # Deep copy
    removed = 0

    for monster in test_data.get('monsters', []):
        for variation in monster.get('variations', {}).values():
            if 'sprite' in variation:
                del variation['sprite']
                removed += 1

    print(f"[3/5] Removed {removed} sprite fields")

    # Step 4: 테스트 파일 저장
    test_file = batch_file.parent / "batch_01_test.json"
    with open(test_file, 'w', encoding='utf-8') as f:
        json.dump(test_data, f, indent=2, ensure_ascii=False)

    print(f"[4/5] Test file created: {test_file.name}")

    # Step 5: 검증 안내
    print(f"[5/5] Verification steps:")
    print()
    print("  Manual Test:")
    print(f"  1. Rename {batch_file.name} to {batch_file.stem}_original{batch_file.suffix}")
    print(f"  2. Rename {test_file.name} to {batch_file.name}")
    print("  3. Run: dotnet run")
    print("  4. Check if all sprites load correctly")
    print("  5. Check console for: 'Warning: Failed to get sprite path'")
    print()
    print("  Rollback:")
    print(f"  - Restore from: {backup_file.name}")
    print()
    print("  Expected Result:")
    print("  - All monsters/bosses display normally")
    print("  - No errors in console")
    print("  - ResourceManager generates paths dynamically")
    print()

    # 파일 크기 비교
    original_size = batch_file.stat().st_size
    test_size = test_file.stat().st_size
    saved = original_size - test_size
    saved_percent = (saved / original_size) * 100

    print("  File Size Comparison:")
    print(f"  - Original: {original_size:,} bytes")
    print(f"  - Test:     {test_size:,} bytes")
    print(f"  - Saved:    {saved:,} bytes ({saved_percent:.1f}%)")
    print()
    print("[OK] Test preparation complete!")


if __name__ == '__main__':
    main()
