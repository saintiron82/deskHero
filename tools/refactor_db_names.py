
import json
import os

DB_FILE = os.path.join(os.path.dirname(__file__), "monster_db.json")

def main():
    with open(DB_FILE, 'r', encoding='utf-8') as f:
        data = json.load(f)

    for entry in data:
        # entry['id'] is like "monster_slime"
        # We want filenames like "monster_slime_fire.png"
        
        species_id = entry['id'] # monster_slime
        
        for var in entry['variations']:
            attr = var['attribute'].lower() # Fire -> fire
            
            # New Filename: monster_slime_fire.png
            new_filename = f"{species_id}_{attr}.png"
            var['filename'] = new_filename
            
    with open(DB_FILE, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=4, ensure_ascii=False)
    
    print(f"Refactored filenames in {DB_FILE}")

if __name__ == "__main__":
    main()
