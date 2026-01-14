
import json
import os

DB_FILE = r"c:\Users\saintiron\.gemini\antigravity\brain\44f97050-c090-47af-88b1-5194bbd9293b\monster_db.json"

def main():
    with open(DB_FILE, 'r', encoding='utf-8') as f:
        data = json.load(f)

    for entry in data:
        base_file = entry['base_file']
        # monster_slimeA.png -> monster_slime
        base_name_no_ext = os.path.splitext(base_file)[0]
        if base_name_no_ext.endswith('A'):
            core_name = base_name_no_ext[:-1]
        else:
            core_name = base_name_no_ext

        for var in entry['variations']:
            suffix = var['suffix']
            # Explicitly define the filename
            var['filename'] = f"{core_name}{suffix}.png"

    with open(DB_FILE, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=4, ensure_ascii=False)
    
    print(f"Updated {DB_FILE} with explicit filenames.")

if __name__ == "__main__":
    main()
