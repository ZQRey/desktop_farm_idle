import os
import glob
from PIL import Image, ImageFilter

def make_transparent_from_border(img_path, out_path, tolerance=25):
    img = Image.open(img_path).convert("RGBA")
    w, h = img.size
    pixels = img.load()

    # Determine background from corners
    corners = [pixels[0, 0], pixels[w - 1, 0], pixels[0, h - 1], pixels[w - 1, h - 1]]
    
    # We can detect whether background is white or checkerboard (alternating white/grey)
    # Check if pixel is close to white or checkerboard grey (approx 200-255 in R,G,B with R~=G~=B)
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            # Check if neutral grey/white (checkerboard pattern or solid white)
            is_neutral = abs(r - g) < 12 and abs(g - b) < 12 and abs(r - b) < 12
            is_light = (r > 195 and g > 195 and b > 195)
            
            # If at or near border and light neutral
            if is_neutral and is_light:
                # Calculate distance to corners/edges
                pixels[x, y] = (r, g, b, 0)

    # Clean isolated floating alpha and crop bounding box
    bbox = img.split()[-1].getbbox()
    if bbox:
        img = img.crop(bbox)
        
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    img.save(out_path, "PNG")
    print(f"Processed: {img_path} -> {out_path} (size: {img.size})")

def main():
    brain_dir = r"C:\Users\guita\.gemini\antigravity-ide\brain\dc16b58c-5327-45e8-bd04-bd62b96b7bdf"
    
    mapping = [
        ("tractor_body_*.jpg", r"d:\Project\idle_game\assets\vehicles\tractor_classic\body.png"),
        ("tractor_wheel_rear_*.jpg", r"d:\Project\idle_game\assets\vehicles\tractor_classic\wheel_rear.png"),
        ("tractor_wheel_front_*.jpg", r"d:\Project\idle_game\assets\vehicles\tractor_classic\wheel_front.png"),
        ("tractor_plow_*.jpg", r"d:\Project\idle_game\assets\equipment\plow\plow.png"),
        ("farm_silo_*.jpg", r"d:\Project\idle_game\assets\environment\buildings\silo.png"),
        ("farm_cat_*.jpg", r"d:\Project\idle_game\assets\environment\animals\cat.png"),
    ]

    for pattern, dest in mapping:
        matches = glob.glob(os.path.join(brain_dir, pattern))
        if matches:
            src = matches[-1]
            make_transparent_from_border(src, dest)
        else:
            print(f"Not found: {pattern}")

if __name__ == "__main__":
    main()
