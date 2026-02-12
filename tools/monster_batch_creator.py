#!/usr/bin/env python3
"""
Monster Batch Creator Tool
DeskWarrior 프로젝트용 몬스터 배치 데이터 자동 생성 도구

사용법:
    python tools/monster_batch_creator.py --batch 2 --theme "Volcanic Wasteland"

또는 Claude Code 서브에이전트로:
    Task(subagent_type="monster-batch-creator", prompt="Batch 2 생성")
"""

import json
import sys
from pathlib import Path
from typing import Dict, List, Any

# 프로젝트 루트 경로
PROJECT_ROOT = Path(__file__).parent.parent
CONFIG_DIR = PROJECT_ROOT / "config"
MONSTERS_DIR = CONFIG_DIR / "monsters"

# 속성별 기본 Modifier (balance_reference.md 기준)
ELEMENT_MODIFIERS = {
    "normal": {
        "hp_modifier": 1.0,
        "gold_modifier": 1.0,
        "hue_shift": 0,
        "emoji": "🟢"
    },
    "fire": {
        "hp_modifier": 1.2,
        "gold_modifier": 1.0,
        "hue_shift": 20,
        "emoji": "🔥"
    },
    "ice": {
        "hp_modifier": 1.1,
        "gold_modifier": 1.05,
        "hue_shift": 200,
        "emoji": "❄️"
    },
    "wind": {
        "hp_modifier": 1.1,
        "gold_modifier": 1.05,
        "hue_shift": 90,
        "emoji": "💨"
    },
    "holy": {
        "hp_modifier": 1.3,
        "gold_modifier": 2.0,
        "hue_shift": 50,
        "emoji": "✨"
    },
    "dark": {
        "hp_modifier": 1.5,
        "gold_modifier": 1.15,
        "hue_shift": 280,
        "emoji": "💀"
    }
}

# 속성별 접두사 패턴 (로컬라이제이션 템플릿용)
ELEMENT_PREFIXES = {
    "ko-KR": {
        "normal": ["숲", "들판", "평원"],
        "fire": ["마그마", "화염", "불타는"],
        "ice": ["아이스", "서리", "프로즌"],
        "wind": ["윈드", "질풍", "스톰"],
        "holy": ["황금", "엔젤", "고대"],
        "dark": ["커스드", "다크", "맹독"]
    },
    "en-US": {
        "normal": ["Forest", "Plains", "Wild"],
        "fire": ["Magma", "Fire", "Burning"],
        "ice": ["Ice", "Frost", "Frozen"],
        "wind": ["Wind", "Storm", "Gale"],
        "holy": ["Golden", "Angel", "Ancient"],
        "dark": ["Cursed", "Dark", "Venom"]
    }
}

# 종족 이름 매핑 (영어 → 한국어)
SPECIES_NAMES = {
    "slime": "슬라임",
    "bat": "박쥐",
    "snake": "뱀",
    "wolf": "늑대",
    "spider": "거미",
    "skeleton": "스켈레톤",
    "zombie": "좀비",
    "goblin": "고블린",
    "orc": "오크",
    "troll": "트롤",
    "golem": "골렘",
    "dragon": "드래곤",
    "demon": "데몬",
    "elemental": "엘리멘탈",
    "wraith": "망령",
    "gargoyle": "가고일",
    "imp": "임프",
    "ghoul": "구울",
    "wyvern": "와이번",
    "harpy": "하피"
}


def calculate_base_gold(batch_id: int, index: int) -> int:
    """
    배치 및 인덱스에 따른 기본 골드 계산

    공식: 10 + (batch_id - 1) * 40 + index * 3

    예:
        Batch 1, index 0: 10 + 0 + 0 = 10
        Batch 1, index 12: 10 + 0 + 36 = 46
        Batch 2, index 0: 10 + 40 + 0 = 50
        Batch 2, index 12: 10 + 40 + 36 = 86
    """
    return 10 + (batch_id - 1) * 40 + index * 3


def generate_variation(
    species: str,
    element: str,
    batch_theme: str = ""
) -> Dict[str, Any]:
    """
    특정 종족 + 속성 조합의 variation 데이터 생성
    """
    modifier = ELEMENT_MODIFIERS[element]
    ko_species = SPECIES_NAMES.get(species, species.capitalize())

    # 속성별 접두사 선택 (첫 번째 것 사용)
    ko_prefix = ELEMENT_PREFIXES["ko-KR"][element][0]
    en_prefix = ELEMENT_PREFIXES["en-US"][element][0]

    # 이름 생성
    if element == "normal":
        ko_name = f"{ko_prefix} {ko_species}"
        en_name = f"{en_prefix} {species.capitalize()}"
    else:
        ko_name = f"{ko_prefix} {ko_species}"
        en_name = f"{en_prefix} {species.capitalize()}"

    # 설명 템플릿
    ko_desc = f"{batch_theme}에서 발견된 {element} 속성의 {ko_species}."
    en_desc = f"A {element}-elemental {species} found in {batch_theme}."

    return {
        "localization": {
            "ko-KR": {
                "name": ko_name,
                "description": ko_desc
            },
            "en-US": {
                "name": en_name,
                "description": en_desc
            }
        },
        "sprite": f"Production/monster_{species}_{element}.png",
        "emoji": modifier["emoji"],
        "hue_shift": modifier["hue_shift"],
        "hp_modifier": modifier["hp_modifier"],
        "gold_modifier": modifier["gold_modifier"]
    }


def generate_monster(
    species: str,
    batch_id: int,
    index: int,
    batch_theme: str = "",
    is_boss: bool = False
) -> Dict[str, Any]:
    """
    단일 몬스터 데이터 생성 (6가지 속성 변형 포함)
    """
    base_gold = calculate_base_gold(batch_id, index)

    monster_data = {
        "id": f"monster_{species}",
        "species": species,
        "is_boss": is_boss,
        "base_stats": {
            "base_hp": 40,  # Tier 시스템 활성화로 고정
            "hp_growth": 10,
            "base_gold": base_gold if not is_boss else base_gold * 10,
            "gold_growth": 2
        },
        "spawn_weight": 100 if not is_boss else 10,
        "display_options": {
            "needs_flip": True,
            "rotation": 0
        },
        "variations": {}
    }

    # 6가지 속성 변형 자동 생성
    for element in ["normal", "fire", "ice", "wind", "holy", "dark"]:
        variation = generate_variation(species, element, batch_theme)

        # 보스는 HP/Gold modifier 증폭
        if is_boss:
            variation["hp_modifier"] *= 5
            variation["gold_modifier"] *= 10

        monster_data["variations"][element] = variation

    return monster_data


def generate_batch(
    batch_id: int,
    species_list: List[str],
    batch_name_ko: str,
    batch_name_en: str,
    theme: str,
    boss_species: List[str] = None
) -> Dict[str, Any]:
    """
    전체 배치 데이터 생성
    """
    batch_data = {
        "batch_id": batch_id,
        "name": {
            "ko-KR": batch_name_ko,
            "en-US": batch_name_en
        },
        "theme": theme,
        "unlock_condition": None,
        "monsters": [],
        "bosses": []
    }

    # 일반 몬스터 생성
    for idx, species in enumerate(species_list):
        monster = generate_monster(species, batch_id, idx, theme, is_boss=False)
        batch_data["monsters"].append(monster)

    # 보스 몬스터 생성 (옵션)
    if boss_species:
        for idx, species in enumerate(boss_species):
            boss = generate_monster(species, batch_id, len(species_list) + idx, theme, is_boss=True)
            batch_data["bosses"].append(boss)

    return batch_data


def save_batch_file(batch_data: Dict[str, Any], batch_id: int) -> Path:
    """
    배치 데이터를 JSON 파일로 저장
    """
    output_file = MONSTERS_DIR / f"batch_{batch_id:02d}.json"

    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(batch_data, f, ensure_ascii=False, indent=2)

    print(f"✅ Batch {batch_id} 파일 생성: {output_file}")
    return output_file


def update_index_file(batch_id: int, enable: bool = False) -> None:
    """
    _index.json의 enabled 플래그 업데이트
    """
    index_file = MONSTERS_DIR / "_index.json"

    with open(index_file, 'r', encoding='utf-8') as f:
        index_data = json.load(f)

    # 해당 배치의 enabled 플래그 업데이트
    for batch in index_data["batches"]:
        if batch["batch_id"] == batch_id:
            batch["enabled"] = enable
            print(f"✅ _index.json 업데이트: Batch {batch_id} enabled={enable}")
            break

    with open(index_file, 'w', encoding='utf-8') as f:
        json.dump(index_data, f, ensure_ascii=False, indent=2)


def validate_batch_file(batch_file: Path) -> bool:
    """
    생성된 배치 파일의 유효성 검증
    """
    try:
        with open(batch_file, 'r', encoding='utf-8') as f:
            data = json.load(f)

        # 필수 필드 확인
        assert "batch_id" in data
        assert "monsters" in data

        # 각 몬스터가 6가지 속성을 가지는지 확인
        for monster in data["monsters"]:
            variations = monster.get("variations", {})
            required_elements = {"normal", "fire", "ice", "wind", "holy", "dark"}
            assert set(variations.keys()) == required_elements, \
                f"Monster {monster['id']} missing elements: {required_elements - set(variations.keys())}"

        print(f"✅ 배치 파일 검증 성공: {len(data['monsters'])} monsters × 6 elements")
        return True

    except Exception as e:
        print(f"❌ 배치 파일 검증 실패: {e}")
        return False


def main():
    """
    CLI 사용 예시 (실제로는 Claude Code 서브에이전트가 호출)
    """
    import argparse

    parser = argparse.ArgumentParser(description="Monster Batch Creator")
    parser.add_argument("--batch", type=int, required=True, help="Batch ID (2-9)")
    parser.add_argument("--theme", type=str, required=True, help="Batch theme")
    parser.add_argument("--name-ko", type=str, help="Korean batch name")
    parser.add_argument("--name-en", type=str, help="English batch name")
    parser.add_argument("--species", type=str, nargs="+", help="Monster species (space separated)")
    parser.add_argument("--enable", action="store_true", help="Enable batch in _index.json")

    args = parser.parse_args()

    # 기본값 설정
    if not args.species:
        print("❌ --species required (e.g., --species dragon troll imp)")
        sys.exit(1)

    name_ko = args.name_ko or f"배치 {args.batch}"
    name_en = args.name_en or f"Batch {args.batch}"

    # 배치 생성
    batch_data = generate_batch(
        batch_id=args.batch,
        species_list=args.species,
        batch_name_ko=name_ko,
        batch_name_en=name_en,
        theme=args.theme
    )

    # 파일 저장
    batch_file = save_batch_file(batch_data, args.batch)

    # 검증
    if validate_batch_file(batch_file):
        # _index.json 업데이트
        update_index_file(args.batch, enable=args.enable)
        print(f"\n🎉 Batch {args.batch} 생성 완료!")
        print(f"   - {len(args.species)} species × 6 elements = {len(args.species) * 6} monsters")
        print(f"   - Theme: {args.theme}")
        print(f"   - File: {batch_file}")


if __name__ == "__main__":
    main()
