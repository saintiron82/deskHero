
import json
import os

MAIN_CONFIG = r"c:\Users\saintiron\deskHero\config\CharacterData.json"
NEW_DATA = r"c:\Users\saintiron\deskHero\tools\new_character_data.json"

DEFAULT_STATS = {
    "BaseHp": 20,
    "HpGrowth": 5,
    "BaseGold": 10,
    "GoldGrowth": 2,
    "Emoji": "👾"
}

# Emoji map for base types
EMOJI_MAP = {
    "slime": "🟢",
    "bat": "🦇",
    "skeleton": "💀",
    "goblin": "👺",
    "orc": "👹",
    "ghost": "👻",
    "golem": "🗿",
    "mushroom": "🍄",
    "spider": "🕷️",
    "wolf": "🐺",
    "snake": "🐍",
    "boar": "🐗",
    "bee": "🐝",
    "crab": "🦀",
    "turtle": "🐢",
    "plant": "🌱",
    "mimic": "📦",
    "eyeball": "👁️",
    "elemental": "✨",
    "rat": "🐀"
}

def main():
    with open(MAIN_CONFIG, 'r', encoding='utf-8') as f:
        main_data = json.load(f)
        
    with open(NEW_DATA, 'r', encoding='utf-8') as f:
        new_monsters = json.load(f)

    # We will replace the 'Monsters' list entirely with the new expanded list
    # preserving any existing stats if ID matches (though IDs changed format, so mostly new)
    
    final_monster_list = []
    
    for i, m in enumerate(new_monsters):
        # m has Id, Name, Sprite
        
        # Determine stats based on index (difficulty curve) or defaults
        # Simple curve: stronger monsters come later in the species list? 
        # Actually structure is Species A-F, next Species.
        # So we want base stats to increase per Species, and maybe slightly per Variation (Elite?)
        
        # Parse species from ID: monster_slime_fire -> slime
        try:
            parts = m['Id'].split('_')
            # parts[0] = monster
            species = parts[1] # slime
            attr = parts[2] # fire
        except:
            species = "unknown"
            attr = "normal"

        # Determine stats based on index
        
        monster_entry = {
            "Id": m['Id'],
            "Name": m['Name'],
            "Sprite": m['Sprite'], 
            "BaseHp": DEFAULT_STATS['BaseHp'] + (i * 2),
            "HpGrowth": DEFAULT_STATS['HpGrowth'],
            "BaseGold": DEFAULT_STATS['BaseGold'] + int(i * 0.5),
            "GoldGrowth": DEFAULT_STATS['GoldGrowth'],
            "Emoji": EMOJI_MAP.get(species, "👾")
        }
        
        # Attribute bonuses
        if attr == 'holy': # Gold bonus
             monster_entry['BaseGold'] += 20
        if attr == 'fire': # HP/Attack bonus suggestion
             monster_entry['BaseHp'] += 10
        if attr == 'dark': 
             monster_entry['BaseHp'] += 15


             
        final_monster_list.append(monster_entry)

    main_data['Monsters'] = final_monster_list
    
    with open(MAIN_CONFIG, 'w', encoding='utf-8') as f:
        json.dump(main_data, f, indent=4, ensure_ascii=False)
        
    print(f"Merged {len(final_monster_list)} monsters into {MAIN_CONFIG}")

if __name__ == "__main__":
    main()
