import os
from PIL import Image, ImageDraw

def process_cat():
    src = r"C:\Users\guita\.gemini\antigravity-ide\brain\dc16b58c-5327-45e8-bd04-bd62b96b7bdf\cat_walk_cycle_1790825377657.jpg"
    dest_dir = r"D:\Project\idle_game\assets\environment\animals"
    os.makedirs(dest_dir, exist_ok=True)
    
    orig = Image.open(src).convert("RGBA")
    w, h = orig.size
    
    for seed in [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1), (w // 2, 0), (w // 2, h - 1)]:
        ImageDraw.floodfill(orig, seed, (0, 0, 0, 0), thresh=30)
        
    datas = orig.getdata()
    new_data = []
    for item in datas:
        if item[3] == 0:
            new_data.append((0, 0, 0, 0))
        elif item[0] >= 248 and item[1] >= 248 and item[2] >= 248:
            new_data.append((0, 0, 0, 0))
        elif item[0] >= 238 and item[1] >= 238 and item[2] >= 238 and abs(item[0]-item[1]) < 10 and abs(item[1]-item[2]) < 10:
            new_data.append((item[0], item[1], item[2], 80))
        else:
            new_data.append(item)
            
    trans = Image.new("RGBA", (w, h))
    trans.putdata(new_data)
    
    segments = [(56, 350), (378, 672), (696, 990), (1018, 1313)]
    cropped_frames = []
    
    max_frame_w = 0
    max_frame_h = 0
    
    for (x1, x2) in segments:
        sub = trans.crop((x1 - 4, 250, x2 + 4, 520))
        bbox = sub.getbbox()
        if bbox:
            c = sub.crop(bbox)
            cropped_frames.append(c)
            max_frame_w = max(max_frame_w, c.width)
            max_frame_h = max(max_frame_h, c.height)
            
    print(f"Max frame size: {max_frame_w}x{max_frame_h}")
    
    # Standardize each frame into a fixed cell: bottom-aligned (paws on floor)
    pad = 4
    cell_w = max_frame_w + pad * 2
    cell_h = max_frame_h + pad * 2
    
    sheet = Image.new("RGBA", (cell_w * 4, cell_h), (0, 0, 0, 0))
    
    for i, frame in enumerate(cropped_frames):
        # Center horizontally, align bottom (paws at bottom - pad)
        px = i * cell_w + (cell_w - frame.width) // 2
        py = cell_h - pad - frame.height
        sheet.paste(frame, (px, py), frame)
        
    sheet_path = os.path.join(dest_dir, "cat_walk_sheet.png")
    sheet.save(sheet_path, "PNG")
    print(f"Saved spritesheet: {sheet_path} (Size: {sheet.size}, 4 frames of {cell_w}x{cell_h})")
    
    # Also save frame 0 as default cat.png
    frame0 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    fx = (cell_w - cropped_frames[0].width) // 2
    fy = cell_h - pad - cropped_frames[0].height
    frame0.paste(cropped_frames[0], (fx, fy), cropped_frames[0])
    frame0.save(os.path.join(dest_dir, "cat.png"), "PNG")
    print(f"Saved default cat.png")

if __name__ == "__main__":
    process_cat()
