from PIL import Image
import sys
import os

try:
    src = sys.argv[1]
    dst = sys.argv[2]
    
    print(f"Opening: {src}")
    img = Image.open(src)
    
    print("Flipping...")
    flipped = img.transpose(Image.FLIP_LEFT_RIGHT)
    
    print(f"Saving to: {dst}")
    flipped.save(dst)
    print("Done.")
except ImportError:
    print("Error: PIL (Pillow) not installed.")
    sys.exit(1)
except Exception as e:
    print(f"Error: {e}")
    sys.exit(1)
