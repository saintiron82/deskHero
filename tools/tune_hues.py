
import json
import random

DB_FILE = r"c:\Users\saintiron\deskHero\tools\monster_db.json"

# Base Hues Mapping
BASE_HUES = {
    "Fire": 0,    # Red
    "Ice": 210,   # Azure
    "Wind": 60,   # Yellow
    "Holy": 50,   # Gold
    "Dark": 270   # Purple
}

# Species specific tweaks (optional example)
# Maybe Plant monsters have different "Fire" (Orange leaves) than Rock monsters (Magma)?
SPECIES_TWEAKS = {
    "monster_plant": { "Fire": 30 }, # Orange
    "monster_golem": { "Ice": 190 }, # Cyan-ish
}

def main():
    with open(DB_FILE, 'r', encoding='utf-8') as f:
        data = json.load(f)

    for entry in data:
        species_id = entry['id']
        
        for var in entry['variations']:
            attr = var['attribute']
            
            # Skip Normal
            if attr == "Normal":
                continue
                
            # Determine base hue
            base_hue = BASE_HUES.get(attr)
            
            if base_hue is not None:
                # 1. Check for specific tweaks
                if species_id in SPECIES_TWEAKS and attr in SPECIES_TWEAKS[species_id]:
                    final_hue = SPECIES_TWEAKS[species_id][attr]
                else:
                    # 2. Add random jitter (-10 to +10) for natural variety
                    jitter = random.randint(-5, 5) 
                    final_hue = base_hue + jitter
                    
                    # Wrap around 360
                    final_hue = final_hue % 360
                    
                var['hue'] = final_hue

    with open(DB_FILE, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=4, ensure_ascii=False)
        
    print(f"Tuned hues with natural variety in {DB_FILE}")

if __name__ == "__main__":
    main()
