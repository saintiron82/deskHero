
import json
import os

DB_FILE = os.path.join(os.path.dirname(__file__), "monster_db.json")

def main():
    with open(DB_FILE, 'r', encoding='utf-8') as f:
        data = json.load(f)

    for entry in data:
        base_file = entry['base_file']
        # monster_slime.png -> monster_slime
        core_name = os.path.splitext(base_file)[0]

        for var in entry['variations']:
            suffix = var['suffix']
            # Explicitly define the filename
            var['filename'] = f"{core_name}{suffix}.png"

    with open(DB_FILE, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=4, ensure_ascii=False)
    
    print(f"Updated {DB_FILE} with explicit filenames.")

if __name__ == "__main__":
    main()
