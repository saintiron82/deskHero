
import os
from PIL import Image

def shift_hue(input_path: str, output_path: str = None, hue_shift: int = 0) -> str:
    if output_path is None:
        base, ext = os.path.splitext(input_path)
        output_path = f"{base}_shifted{ext}"
    
    if not os.path.exists(input_path):
        print(f"❌ Input file not found: {input_path}")
        return None

    try:
        img = Image.open(input_path).convert("RGBA")
        
        # Split channels
        r, g, b, a = img.split()
        rgb_img = Image.merge("RGB", (r, g, b))
        hsv_img = rgb_img.convert("HSV")
        
        h, s, v = hsv_img.split()
        
        # Hue shift logic
        shift_val = int(hue_shift * 255 / 360)
        
        def apply_shift(p):
            # Only shift if strict green (background) is NOT the pixel? 
            # Actually, standard goblin is green skin. 
            # We want to shift Green (Skin) to Gold (Yellow/Orange).
            # Green is ~120 deg. Gold is ~45 deg.
            # Shift = 45 - 120 = -75 deg.
            return (p + shift_val) % 255

        h = h.point(apply_shift)
        
        # Merge back
        new_hsv = Image.merge("HSV", (h, s, v))
        new_rgb = new_hsv.convert("RGB")
        final_img = Image.merge("RGBA", (*new_rgb.split(), a))
        
        final_img.save(output_path)
        print(f"✅ Generated Golden Goblin at: {output_path}")
        return output_path
    except Exception as e:
        print(f"❌ Error: {e}")
        return None


if __name__ == "__main__":
    import sys
    
    if len(sys.argv) < 3:
        print("Usage: python create_gold_goblin.py <input_path> <output_path> [hue_shift]")
        sys.exit(1)

    input_file = sys.argv[1]
    output_file = sys.argv[2]
    shift = int(sys.argv[3]) if len(sys.argv) > 3 else -70
    
    shift_hue(input_file, output_file, shift)
