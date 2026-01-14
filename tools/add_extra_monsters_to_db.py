
import json
import os

DB_FILE = os.path.join(os.path.dirname(__file__), "monster_db.json")

# Temporary naming convention: Attribute + Name
new_monsters = [
    {
        "id": "monster_dullahan",
        "base_file": "monster_dullahanA.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "듀라한", "hue": None, "filename": "monster_dullahan_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "화염 듀라한", "hue": 358, "filename": "monster_dullahan_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "냉기 듀라한", "hue": 210, "filename": "monster_dullahan_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "질풍 듀라한", "hue": 60, "filename": "monster_dullahan_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "신성 듀라한", "hue": 50, "filename": "monster_dullahan_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "암흑 듀라한", "hue": 270, "filename": "monster_dullahan_dark.png"}
        ]
    },
    {
        "id": "monster_harpy",
        "base_file": "monster_harpyA.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "하피", "hue": None, "filename": "monster_harpy_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "불꽃 하피", "hue": 358, "filename": "monster_harpy_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "얼음 하피", "hue": 210, "filename": "monster_harpy_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "바람 하피", "hue": 60, "filename": "monster_harpy_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "엔젤 하피", "hue": 50, "filename": "monster_harpy_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "다크 하피", "hue": 270, "filename": "monster_harpy_dark.png"}
        ]
    },
    {
        "id": "monster_female_mermaid",
        "base_file": "monster_female_mermaid.png",
        "variations": [
            # Base file has no suffix 'A' in the progress doc, but code expects A for base? 
            # generate_monsters.py logic: if name_part.endswith("A")...
            # We should probably stick to the pattern and rename the base file to monster_female_mermaidA.png if consistent.
            # But the progress doc said `monster_female_mermaid.png`.
            # I will use 'monster_female_mermaidA.png' in DB to be consistent with script, and save the image as such.
            {"suffix": "A", "attribute": "Normal", "name": "인어(여)", "hue": None, "filename": "monster_female_mermaid_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "화염 인어(여)", "hue": 358, "filename": "monster_female_mermaid_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "얼음 인어(여)", "hue": 210, "filename": "monster_female_mermaid_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "바람 인어(여)", "hue": 60, "filename": "monster_female_mermaid_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "신성 인어(여)", "hue": 50, "filename": "monster_female_mermaid_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "암흑 인어(여)", "hue": 270, "filename": "monster_female_mermaid_dark.png"}
        ]
    },
    {
        "id": "monster_male_mermaid",
        "base_file": "monster_male_mermaid.png",
        "variations": [
            {"suffix": "A", "attribute": "Normal", "name": "인어(남)", "hue": None, "filename": "monster_male_mermaid_normal.png"},
            {"suffix": "B", "attribute": "Fire", "name": "화염 인어(남)", "hue": 358, "filename": "monster_male_mermaid_fire.png"},
            {"suffix": "C", "attribute": "Ice", "name": "얼음 인어(남)", "hue": 210, "filename": "monster_male_mermaid_ice.png"},
            {"suffix": "D", "attribute": "Wind", "name": "바람 인어(남)", "hue": 60, "filename": "monster_male_mermaid_wind.png"},
            {"suffix": "E", "attribute": "Holy", "name": "신성 인어(남)", "hue": 50, "filename": "monster_male_mermaid_holy.png"},
            {"suffix": "F", "attribute": "Dark", "name": "암흑 인어(남)", "hue": 270, "filename": "monster_male_mermaid_dark.png"}
        ]
    }
]

def main():
    print(f"Loading {DB_FILE}...")
    try:
        with open(DB_FILE, 'r', encoding='utf-8') as f:
            data = json.load(f)
    except FileNotFoundError:
        print("DB file not found.")
        return

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
