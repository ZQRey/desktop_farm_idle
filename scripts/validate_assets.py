import os
import sys
from PIL import Image

PROJECT_DIR = r"D:\Project\idle_game"
ASSETS_DIR = os.path.join(PROJECT_DIR, "assets")

def check_checkerboard_patch(pixels, x, y, block_size=16):
    # Samples pixel colors in a block to see if alternating light-gray / white pattern exists
    # Checkerboard usually alternates between ~#FFFFFF (255,255,255) and ~#CCCCCC (204,204,204)
    # with high frequency and square grid
    pass

def validate_png(filepath):
    errors = []
    warnings = []
    
    try:
        img = Image.open(filepath)
    except Exception as e:
        return ["FAIL"], [f"Cannot open image: {e}"]
        
    if img.format != "PNG":
        errors.append(f"Format is {img.format}, expected PNG")
        
    if img.mode != "RGBA":
        errors.append(f"Mode is {img.mode}, expected RGBA with alpha channel")
        return errors, warnings
        
    w, h = img.size
    pix = img.load()
    
    # 1. Check edges for transparency (top, bottom, left, right borders)
    non_transparent_edge_count = 0
    total_edge_pixels = 2 * w + 2 * h - 4
    
    for x in range(w):
        if pix[x, 0][3] > 10: non_transparent_edge_count += 1
        if pix[x, h - 1][3] > 10: non_transparent_edge_count += 1
    for y in range(1, h - 1):
        if pix[0, y][3] > 10: non_transparent_edge_count += 1
        if pix[w - 1, y][3] > 10: non_transparent_edge_count += 1
        
    edge_opaque_ratio = non_transparent_edge_count / total_edge_pixels
    if edge_opaque_ratio > 0.05:
        errors.append(f"Opaque borders detected! {edge_opaque_ratio*100:.1f}% of border pixels are not transparent.")
        
    # 2. Check for checkerboard artifacts in RGB channels
    # Samples visible or transparent pixels to detect baked checkerboard
    # In fake checkerboard, grey pixels (around 204,204,204) and white (255,255,255) alternate in a regular 8-16px grid
    # Let's count high-contrast neutral grey/white square patterns
    checker_pattern_matches = 0
    sample_step = 8
    samples = 0
    for y in range(0, h - sample_step, sample_step):
        for x in range(0, w - sample_step, sample_step):
            p1 = pix[x, y]
            p2 = pix[x + sample_step // 2, y]
            # If both are fully opaque
            if p1[3] > 200 and p2[3] > 200:
                # Check if one is near white (>240) and one is near gray (~200) with r=g=b
                is_gray1 = abs(p1[0]-p1[1]) < 5 and abs(p1[1]-p1[2]) < 5 and 180 <= p1[0] <= 215
                is_white2 = abs(p2[0]-p2[1]) < 5 and abs(p2[1]-p2[2]) < 5 and p2[0] >= 240
                is_white1 = abs(p1[0]-p1[1]) < 5 and abs(p1[1]-p1[2]) < 5 and p1[0] >= 240
                is_gray2 = abs(p2[0]-p2[1]) < 5 and abs(p2[1]-p2[2]) < 5 and 180 <= p2[0] <= 215
                if (is_gray1 and is_white2) or (is_white1 and is_gray2):
                    checker_pattern_matches += 1
            samples += 1
            
    if samples > 0 and (checker_pattern_matches / samples) > 0.08:
        errors.append(f"Checkerboard pattern detected! ({checker_pattern_matches}/{samples} sampled grid cells match fake checkerboard)")
        
    return errors, warnings

def main():
    print("==================================================")
    print(" DESKTOP FARM IDLE - ASSET VALIDATION TOOL")
    print("==================================================")
    
    all_passed = True
    results = []
    
    for root, dirs, files in os.walk(ASSETS_DIR):
        for f in files:
            if f.lower().endswith(".png"):
                full_path = os.path.join(root, f)
                rel_path = os.path.relpath(full_path, PROJECT_DIR)
                errors, warnings = validate_png(full_path)
                
                status = "PASS" if not errors else "FAIL"
                if errors:
                    all_passed = False
                results.append((status, rel_path, errors, warnings))
                
    for status, rel_path, errors, warnings in sorted(results, key=lambda x: (x[0] != "FAIL", x[1])):
        print(f"[{status}] {rel_path}")
        for err in errors:
            print(f"       -> ERROR: {err}")
        for warn in warnings:
            print(f"       -> WARN:  {warn}")
            
    print("==================================================")
    if all_passed:
        print("RESULT: ALL ASSETS PASSED VALIDATION!")
    else:
        print("RESULT: SOME ASSETS FAILED VALIDATION! Please fix or remove failed assets.")
    print("==================================================")
    return 0 if all_passed else 1

if __name__ == "__main__":
    sys.exit(main())
