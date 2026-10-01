import os
import sys
from PIL import Image, ImageFilter, ImageDraw

ARTIFACTS_DIR = r"C:\Users\guita\.gemini\antigravity-ide\brain\dc16b58c-5327-45e8-bd04-bd62b96b7bdf"
PROJECT_DIR = r"D:\Project\idle_game"

TARGETS = [
    {
        "src": os.path.join(ARTIFACTS_DIR, "clean_tractor_body_1790824641337.jpg"),
        "dest": os.path.join(PROJECT_DIR, "assets", "vehicles", "tractor_classic", "body.png"),
        "tolerance": 35
    },
    {
        "src": os.path.join(ARTIFACTS_DIR, "clean_tractor_wheel_rear_1790824654508.jpg"),
        "dest": os.path.join(PROJECT_DIR, "assets", "vehicles", "tractor_classic", "wheel_rear.png"),
        "tolerance": 30
    },
    {
        "src": os.path.join(ARTIFACTS_DIR, "clean_tractor_wheel_front_1790824668702.jpg"),
        "dest": os.path.join(PROJECT_DIR, "assets", "vehicles", "tractor_classic", "wheel_front.png"),
        "tolerance": 30
    },
    {
        "src": os.path.join(ARTIFACTS_DIR, "clean_tractor_plow_1790824687389.jpg"),
        "dest": os.path.join(PROJECT_DIR, "assets", "vehicles", "tractor_classic", "plow.png"),
        "tolerance": 35
    },
    {
        "src": os.path.join(ARTIFACTS_DIR, "clean_farm_cat_1790824705407.jpg"),
        "dest": os.path.join(PROJECT_DIR, "assets", "environment", "animals", "cat.png"),
        "tolerance": 30
    }
]

def make_transparent(src_path, dest_path, tolerance=30):
    orig = Image.open(src_path).convert("RGBA")
    w, h = orig.size
    
    # We create a mask image: 255 for background, 0 for foreground
    mask = Image.new("L", (w, h), 0)
    
    # Fast pure-Python floodfill from 4 corners and borders
    # Target color: white (255, 255, 255)
    # Using ImageDraw.floodfill
    for seed in [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1), 
                 (w // 2, 0), (w // 2, h - 1), (0, h // 2), (w - 1, h // 2)]:
        ImageDraw.floodfill(orig, seed, (0, 0, 0, 0), thresh=tolerance)

    # Any pixel with alpha == 0 in orig is background
    # Let's inspect orig alpha
    datas = orig.getdata()
    new_data = []
    for item in datas:
        # If item was flood-filled (a == 0) or is virtually pure white (R>245, G>245, B>245)
        if item[3] == 0:
            new_data.append((0, 0, 0, 0))
        elif item[0] >= 248 and item[1] >= 248 and item[2] >= 248:
            new_data.append((0, 0, 0, 0))
        elif item[0] >= 238 and item[1] >= 238 and item[2] >= 238 and abs(item[0]-item[1]) < 10 and abs(item[1]-item[2]) < 10:
            # Soft edge
            new_data.append((item[0], item[1], item[2], 80))
        else:
            new_data.append(item)
            
    res_img = Image.new("RGBA", (w, h))
    res_img.putdata(new_data)
    
    # Crop to content with 4px margin
    bbox = res_img.getbbox()
    if bbox:
        left = max(0, bbox[0] - 4)
        top = max(0, bbox[1] - 4)
        right = min(w, bbox[2] + 4)
        bottom = min(h, bbox[3] + 4)
        res_img = res_img.crop((left, top, right, bottom))
        
    os.makedirs(os.path.dirname(dest_path), exist_ok=True)
    res_img.save(dest_path, "PNG")
    print(f"Successfully processed: {os.path.basename(dest_path)} (Size: {res_img.size})")

def main():
    for target in TARGETS:
        if os.path.exists(target["src"]):
            make_transparent(target["src"], target["dest"], target["tolerance"])
        else:
            print(f"ERROR: Not found: {target['src']}")

if __name__ == "__main__":
    main()
