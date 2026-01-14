
import json
import os

DB_FILE = os.path.join(os.path.dirname(__file__), "monster_db.json")

new_monsters = [
    {
        "id": "monster_bee",
        "base_file": "monster_beeA.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "일벌", "hue": None, "filename": "monster_bee_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "불침 벌", "hue": 358, "filename": "monster_bee_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "아이스 비", "hue": 210, "filename": "monster_bee_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "스톰 비", "hue": 60, "filename": "monster_bee_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "로얄 젤리 비", "hue": 50, "filename": "monster_bee_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "킬러 비", "hue": 270, "filename": "monster_bee_dark.png"}
        ]
    },
    {
        "id": "monster_crab",
        "base_file": "monster_crabA.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "갯벌 게", "hue": None, "filename": "monster_crab_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "용암 게", "hue": 358, "filename": "monster_crab_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "아이스 크랩", "hue": 210, "filename": "monster_crab_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "샌드 크랩", "hue": 60, "filename": "monster_crab_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "황금 게", "hue": 50, "filename": "monster_crab_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "심해 게", "hue": 270, "filename": "monster_crab_dark.png"}
        ]
    },
    {
        "id": "monster_turtle",
        "base_file": "monster_turtleA.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "숲 거북", "hue": None, "filename": "monster_turtle_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "마그마 터틀", "hue": 358, "filename": "monster_turtle_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "순백 거북", "hue": 210, "filename": "monster_turtle_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "날쌘 거북", "hue": 60, "filename": "monster_turtle_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "신수 거북", "hue": 50, "filename": "monster_turtle_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "본 터틀", "hue": 270, "filename": "monster_turtle_dark.png"}
        ]
    },
    {
        "id": "monster_plant",
        "base_file": "monster_plantA.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "식인초", "hue": None, "filename": "monster_plant_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "불꽃 덩굴", "hue": 358, "filename": "monster_plant_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "얼음 꽃", "hue": 210, "filename": "monster_plant_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "독바람 풀", "hue": 60, "filename": "monster_plant_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "생명의 나무", "hue": 50, "filename": "monster_plant_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "마계 식물", "hue": 270, "filename": "monster_plant_dark.png"}
        ]
    },
    {
        "id": "monster_mimic",
        "base_file": "monster_mimicA.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "나무 상자", "hue": None, "filename": "monster_mimic_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "화약 상자", "hue": 358, "filename": "monster_mimic_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "냉동 상자", "hue": 210, "filename": "monster_mimic_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "낡은 상자", "hue": 60, "filename": "monster_mimic_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "보물 상자", "hue": 50, "filename": "monster_mimic_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "저주받은 상자", "hue": 270, "filename": "monster_mimic_dark.png"}
        ]
    },
    {
        "id": "monster_eyeball",
        "base_file": "monster_eyeballA.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "감시자", "hue": None, "filename": "monster_eyeball_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "작열하는 눈", "hue": 358, "filename": "monster_eyeball_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "냉혹한 시선", "hue": 210, "filename": "monster_eyeball_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "폭풍의 눈", "hue": 60, "filename": "monster_eyeball_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "천리안", "hue": 50, "filename": "monster_eyeball_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "사우론의 눈", "hue": 270, "filename": "monster_eyeball_dark.png"}
        ]
    },
    {
        "id": "monster_elemental",
        "base_file": "monster_elementalA.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "자연의 정령", "hue": None, "filename": "monster_elemental_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "불의 정령", "hue": 358, "filename": "monster_elemental_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "물의 정령", "hue": 210, "filename": "monster_elemental_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "바람의 정령", "hue": 60, "filename": "monster_elemental_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "빛의 정령", "hue": 50, "filename": "monster_elemental_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "어둠의 정령", "hue": 270, "filename": "monster_elemental_dark.png"}
        ]
    },
    {
        "id": "monster_rat",
        "base_file": "monster_ratA.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "시궁창 쥐", "hue": None, "filename": "monster_rat_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "역병 쥐", "hue": 358, "filename": "monster_rat_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "실험실 쥐", "hue": 210, "filename": "monster_rat_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "날쌘 쥐", "hue": 60, "filename": "monster_rat_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "황금 쥐", "hue": 50, "filename": "monster_rat_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "좀비 쥐", "hue": 270, "filename": "monster_rat_dark.png"}
        ]
    }
]

def main():
    print(f"Loading {DB_FILE}...")
    try:
        with open(DB_FILE, 'r', encoding='utf-8') as f:
            data = json.load(f)
    except FileNotFoundError:
        print("DB file not found, creating new.")
        data = []

    existing_ids = {entry['id'] for entry in data}
    
    added_count = 0
    for monster in new_monsters:
        if monster['id'] not in existing_ids:
            data.append(monster)
            existing_ids.add(monster['id'])
            added_count += 1
            print(f"Added {monster['id']}")
        else:
            print(f"Skipping {monster['id']} (already exists)")

    if added_count > 0:
        with open(DB_FILE, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=4, ensure_ascii=False)
        print(f"Successfully added {added_count} new monsters to DB.")
    else:
        print("No new monsters added.")

if __name__ == "__main__":
    main()
