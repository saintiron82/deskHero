import json
import os
import re

# Relative paths
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
PLANNING_FILE = os.path.join(BASE_DIR, '..', 'docs', 'monster_planning.md')
DB_FILE = os.path.join(BASE_DIR, 'monster_db.json')

def load_json(filepath):
    if not os.path.exists(filepath):
        return []
    with open(filepath, 'r', encoding='utf-8') as f:
        return json.load(f)

def save_json(filepath, data):
    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=4, ensure_ascii=False)

def parse_planning_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    monsters = []
    is_boss_section = False
    
    lines = content.split('\n')
    for line in lines:
        line = line.strip()
        
        # Detect Section Headers
        if line.startswith('###'):
            if 'Boss' in line or '보스' in line:
                is_boss_section = True
            elif '일반' in line or 'Standard' in line:
                is_boss_section = False
            continue
            
        if not line.startswith('|') or '---' in line or 'No.' in line:
            continue
            
        parts = [p.strip() for p in line.split('|')]
        # Parts usually: ['', 'No', 'Species', 'Normal', 'Fire', 'Ice', 'Wind', 'Holy', 'Dark', '']
        if len(parts) < 9:
            continue
            
        # Parse Species column: "**Name (English)**"
        species_col = parts[2]
        match = re.search(r'\((.*?)\)', species_col)
        if not match:
            # Try to handle cases without parentheses if any, or skip
            continue
            
        species_eng = match.group(1).strip()
        species_key = re.sub(r'[^a-zA-Z0-9]', '', species_eng)
        
        # Attributes
        normal_name = parts[3]
        fire_name = parts[4]
        ice_name = parts[5]
        wind_name = parts[6]
        holy_name = parts[7]
        dark_name = parts[8]
        
        monsters.append({
            'species': species_key,
            'is_boss': is_boss_section,
            'names': {
                'Normal': normal_name,
                'Fire': fire_name,
                'Ice': ice_name,
                'Wind': wind_name,
                'Holy': holy_name,
                'Dark': dark_name
            }
        })
        
    return monsters

def update_db():
    print(f"Loading DB from {DB_FILE}...")
    db_data = load_json(DB_FILE)
    
    existing_ids = {item['id'] for item in db_data}
    print(f"Found {len(existing_ids)} existing monsters.")
    
    print(f"Parsing planning file from {PLANNING_FILE}...")
    new_monsters = parse_planning_file(PLANNING_FILE)
    print(f"Parsed {len(new_monsters)} monsters from plan.")
    
    added_count = 0
    
    for m in new_monsters:
        species_lower = m['species'].lower()
        if species_lower == "mermaidf": species_lower = "female_mermaid"
        if species_lower == "mermaidm": species_lower = "male_mermaid"
        
        # Prefix logic
        prefix = "boss" if m['is_boss'] else "monster"
        monster_id = f"{prefix}_{species_lower}"
        base_filename = f"{prefix}_{species_lower}A.png"
        
        if monster_id in existing_ids:
            # print(f"Skipping existing ID: {monster_id}")
            continue
            
        # Create new entry
        variations = []
        attrs = [
            ("A", "Normal", m['names']['Normal'], None, "normal"),
            ("B", "Fire", m['names']['Fire'], 358, "fire"),
            ("C", "Ice", m['names']['Ice'], 210, "ice"),
            ("D", "Wind", m['names']['Wind'], 60, "wind"),
            ("E", "Holy", m['names']['Holy'], 50, "holy"),
            ("F", "Dark", m['names']['Dark'], 270, "dark"),
        ]
        
        for suffix, attr, name, hue, var_suffix in attrs:
            variations.append({
                "suffix": suffix,
                "attribute": attr,
                "name": name,
                "hue": hue,
                "filename": f"{prefix}_{species_lower}_{var_suffix}.png"
            })
            
        new_entry = {
            "id": monster_id,
            "base_file": base_filename,
            "variations": variations
        }
        
        db_data.append(new_entry)
        existing_ids.add(monster_id) # Prevent duplicates if list has same items
        added_count += 1
        
    print(f"Added {added_count} new monsters to DB.")
    save_json(DB_FILE, db_data)

if __name__ == "__main__":
    update_db()
