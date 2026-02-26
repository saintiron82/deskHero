"""Resources/Org 원본 리소스를 배치별 하위폴더로 정리"""
import shutil
from pathlib import Path

PROJECT_ROOT = Path(__file__).parent.parent
ORG_DIR = PROJECT_ROOT / "Resources" / "Org"

# monster_planning.md 기준 배치 매핑
BATCH_1_MONSTERS = [
    "slime", "bat", "skeleton", "goblin", "orc", "ghost", "golem",
    "mushroom", "spider", "wolf", "snake", "boar", "bee", "crab",
    "turtle", "plant", "mimic", "eyeball", "elemental", "rat"
]
BATCH_1_BOSSES = ["dragon", "knight", "lich", "demon", "reaper"]

BATCH_2_MONSTERS = [
    "dullahan", "harpy", "female_mermaid", "male_mermaid", "nymph",
    "mummy", "zombie", "banshee", "gargoyle", "treant", "lizardman",
    "gnoll", "kobold", "troll", "ogre", "centaur", "medusa",
    "basilisk", "griffin", "chimera", "sercubus"
]
BATCH_2_BOSSES = ["slimeking", "mummylord", "orclord", "goblinKing", "skeletonking"]

BATCH_3_MONSTERS = [
    "cockatrice", "wyvern", "drake", "hippogriff", "pegasus", "cyclops",
    "ettin", "hobgoblin", "bugbear", "lamia", "arachne", "drider",
    "doppelganger", "shade", "phantom", "homunculus", "livingarmor",
    "flyingsword", "magicbook", "poltergeist", "fairy"
]

SPECIAL = ["goldengoblin"]
HERO_PREFIXES = ["hero_"]


def get_batch_folder(filename: str) -> str:
    """파일명으로 배치 폴더 결정"""
    name = filename.lower()

    # Hero
    if name.startswith("hero_"):
        return "Hero"

    # Special (golden goblin)
    for sp in SPECIAL:
        if sp in name:
            return "Special"

    # Boss
    if name.startswith("boss_"):
        boss_species = name.replace("boss_", "").replace(".png", "")
        for boss in BATCH_1_BOSSES:
            if boss in boss_species:
                return "Batch_01/Boss"
        for boss in BATCH_2_BOSSES:
            if boss.lower() in boss_species:
                return "Batch_02/Boss"
        return "Uncategorized"

    # Monster
    if name.startswith("monster_"):
        # Extract species: monster_{species}.png or monster_{species}_{element}.png
        rest = name.replace("monster_", "").replace(".png", "")

        for species in BATCH_1_MONSTERS:
            if rest == species or rest.startswith(f"{species}_"):
                return "Batch_01/Monster"

        for species in BATCH_2_MONSTERS:
            if rest == species or rest.startswith(f"{species}_"):
                return "Batch_02/Monster"

        for species in BATCH_3_MONSTERS:
            if rest == species or rest.startswith(f"{species}_"):
                return "Batch_03/Monster"

    return "Uncategorized"


def main():
    files = sorted(ORG_DIR.glob("*.png"))
    print(f"Total files: {len(files)}")
    print()

    # Categorize
    categories = {}
    for f in files:
        folder = get_batch_folder(f.name)
        categories.setdefault(folder, []).append(f)

    # Print plan
    for folder in sorted(categories.keys()):
        file_list = categories[folder]
        print(f"[{folder}] ({len(file_list)} files)")
        for f in file_list:
            print(f"  {f.name}")
        print()

    # Ask for confirmation
    total = sum(len(v) for v in categories.values())
    print(f"Total: {total} files -> {len(categories)} folders")
    print()

    confirm = input("Proceed with moving files? (y/n): ").strip().lower()
    if confirm != 'y':
        print("Cancelled.")
        return

    # Create directories and move files
    for folder, file_list in categories.items():
        dest_dir = ORG_DIR / folder
        dest_dir.mkdir(parents=True, exist_ok=True)
        for f in file_list:
            dest = dest_dir / f.name
            shutil.move(str(f), str(dest))
            print(f"  {f.name} -> {folder}/")

    print()
    print("Done!")


if __name__ == "__main__":
    main()
