
import os
import json
import colorsys
import sys
# Try to import PIL, if fails, user needs to install it
try:
    from PIL import Image
except ImportError:
    print("Pillow (PIL) is not installed. Please run: pip install Pillow")
    sys.exit(1)

# Configuration
BASE_DIR = r"c:\Users\saintiron\deskHero\Assets\Images\Raw_Green"
DB_FILE = r"monster_db.json" # Relative path, assuming run from tools dir
OUTPUT_JSON_FILE = r"new_character_data.json" # Output in same dir

# Green Background Color to mask (Approximate)
BG_COLOR_RGB = (0, 255, 0)
TOLERANCE = 50  # Tolerance for background detection

def is_background(r, g, b):
    # Simple green detection: G is high, R and B are low
    if g > 200 and r < 100 and b < 100:
        return True
    return False

def shift_hue(image_path, target_hue_deg, output_filename):
    """
    Shifts the hue of the non-background pixels to match the target hue.
    target_hue_deg: 0-360
    """
    if not os.path.exists(image_path):
        print(f"File not found: {image_path}")
        return None

    img = Image.open(image_path).convert("RGBA")
    pixels = img.load()
    width, height = img.size

    # 1. Calculate Average Hue of the object
    total_hue = 0
    count = 0
    
    # Sample pixels to find average hue
    for y in range(0, height, 5): # Optimization: Skip pixels
        for x in range(0, width, 5):
            r, g, b, a = pixels[x, y]
            if a < 10: continue # Transparent
            if is_background(r, g, b): continue

            h, s, v = colorsys.rgb_to_hsv(r/255.0, g/255.0, b/255.0)
            total_hue += h
            count += 1

    if count == 0:
        print(f"Warning: No object pixels found in {image_path}")
        return None

    avg_hue = total_hue / count
    target_hue = target_hue_deg / 360.0
    hue_shift = target_hue - avg_hue

    # 2. Apply Hue Shift
    new_img = Image.new("RGBA", (width, height))
    new_pixels = new_img.load()

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            
            # Copy background or transparent pixels directly
            if a < 10 or is_background(r, g, b):
                new_pixels[x, y] = (r, g, b, a)
                continue

            # Shift Hue
            h, s, v = colorsys.rgb_to_hsv(r/255.0, g/255.0, b/255.0)
            
            # Apply shift
            new_h = (h + hue_shift) % 1.0
            
            # Optional: Tweak Saturation/Value based on attribute?
            # For now, keep simple hue shift
            
            new_r, new_g, new_b = colorsys.hsv_to_rgb(new_h, s, v)
            new_pixels[x, y] = (int(new_r*255), int(new_g*255), int(new_b*255), a)

    # Save
    output_path = os.path.join(BASE_DIR, output_filename)
    new_img.save(output_path)
    print(f"Generated: {output_filename}")
    return output_filename

def main():
    print("Loading DB...")
    with open(DB_FILE, 'r', encoding='utf-8') as f:
        data = json.load(f)

    generated_configs = []

    for entry in data:
        base_filename = entry['base_file']
        base_path = os.path.join(BASE_DIR, base_filename)
        
        # Check if Base File Exists (Skip if not generated yet)
        if not os.path.exists(base_path):
             # print(f"Skipping {base_filename} (Not found)")
             continue
        
        print(f"Processing {base_filename}...")

        for var in entry['variations']:
            suffix = var['suffix']
            target_hue = var['hue']
            name = var['name']
            
            # Use explicit filename from DB
            output_filename = var.get('filename')
            if not output_filename:
                # Fallback if config is missing filename
                base_name = os.path.basename(base_filename)
                name_part = os.path.splitext(base_name)[0]
                if name_part.endswith("A"):
                    core_name = name_part[:-1]
                else:
                    core_name = name_part
                output_filename = f"{core_name}{suffix}.png"

            # Check logic
            if suffix == "A":
                # If output filename != base filename, we must copy it so it exists with the new name
                if output_filename != base_filename:
                    final_path = os.path.join(BASE_DIR, output_filename)
                    # Use shutil or simple open/write to copy
                    try:
                        # Simple copy via PIL since we have it open-ish or just load save
                        # actually better to just save existing image to new path to ensure format
                        with Image.open(base_path) as img:
                            img.save(final_path)
                        print(f"Copied Base: {output_filename}")
                        final_filename = output_filename
                    except Exception as e:
                        print(f"Error copying base file: {e}")
                        final_filename = None
                else:
                    final_filename = base_filename
            else:
                # Generate Variation
                if target_hue is not None:
                    # Pass the explicit output filename to the function
                    final_filename = shift_hue(base_path, target_hue, output_filename)
                else:
                    final_filename = None

            if final_filename:
                # Add to JSON config list
                # Construct ID: monster_slime_fire
                attr_lower = var['attribute'].lower()
                mob_id = f"{entry['id']}_{attr_lower}"
                
                config_entry = {
                    "Id": mob_id,
                    "Name": name,
                    "Sprite": f"Raw_Green/{final_filename}"
                }
                generated_configs.append(config_entry)

    # Save Config Snippet
    print("Saving JSON config...")
    with open(OUTPUT_JSON_FILE, 'w', encoding='utf-8') as f:
        json.dump(generated_configs, f, indent=4, ensure_ascii=False)
    
    print(f"Done! Config saved to {OUTPUT_JSON_FILE}")

if __name__ == "__main__":
    main()
