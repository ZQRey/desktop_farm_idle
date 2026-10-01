# Desktop Farm Idle — Asset Guide & Pipeline

## 1. Visual Style & Art Direction

**Style Formula**:
> *"High-quality stylized 2D side-view farming game asset, friendly semi-realistic proportions, clean readable silhouette, soft detailed shading, consistent warm rural color palette, suitable for desktop idle game, transparent background, no text, no logos, no watermark"*

### Key Principles:
- **Perspective**: Pure side-view with slight elevated perspective (approx. 8-10 degrees elevation) to give volume to tractor hoods, tires, and furrows.
- **Lighting**: Soft warm daylight arriving from the upper-left (10 o'clock angle). Subtle ambient occlusion under wheel wells and vehicle frames.
- **Color Palette**: Warm, inviting, natural rural tones:
  - Soil: Rich loamy browns `#4a3525`, furrow shadow `#2d1e14`, tilled highlights `#6b4f39`.
  - Wheat: Golden ochre `#e6b843`, ripe amber `#c48b26`, fresh sprout green `#6db33f`.
  - Machinery: Classic farm paints (warm crimson red, meadow green, harvest yellow, slate blue), always with friendly weathered highlights.
  - Transparent Background: All game sprites MUST have crisp alpha channels (PNG format).

---

## 2. Layered Vehicle Assembly

To achieve buttery-smooth wheel rotation, implement articulation, and dynamic lighting without needing thousands of static animation frames, all vehicles are assembled from discrete modular 2D layers:

```
[Vehicle Root Node2D]
├── ShadowLayer (Soft blurred ground shadow sprite, scales with suspension travel)
├── ToolHitchRear (Mount point for Plow / Cultivator / Seeder / Trailer)
├── AttachedTool (Implement sprite with variable angle and depth)
├── RearWheels (Large agricultural tire sprite, pivots around center hub)
├── ChassisBody (Tractor engine hood, cabin with tinted glass, mudguards, steering column)
├── FrontWheels (Smaller steerable tire sprite, pivots around center hub)
├── ToolHitchFront (Mount point for front weights or cutterbar)
├── LightsLayer (Headlight / Taillight glowing sprites, toggled for dusk/night)
├── ExhaustAnchor (Marker2D position for exhaust puff GPU particles)
└── WheelDustAnchors (Marker2D positions for soil ejection particles)
```

### Motion & Animation Parameters:
- **Suspension Oscillation**: Body bobbing `sin(time * speed * 8.0) * 1.5px` vertical offset simulates bumpy field terrain.
- **Wheel Rotation**: Rotates by `dAngle = (linearVelocity * dt) / radius`.
- **Exhaust Smoke**: Low-density grey puffs emitted at idle; dark energetic puffs emitted upon acceleration or under heavy implement draft load.
- **Soil Furrow Particles**: Small earthen clumps ejected backwards when plow/cultivator blades are engaged.

---

## 3. Directory Structure

```
assets/
├── manifest.json
├── vehicles/
│   ├── tractor_classic/
│   │   ├── body.png
│   │   ├── wheel_front.png
│   │   ├── wheel_rear.png
│   │   └── shadow.png
│   ├── tractor_universal/
│   ├── tractor_heavy/
│   ├── combine_harvester/
│   └── truck_grain/
├── equipment/
│   ├── plow/
│   ├── cultivator/
│   ├── seeder/
│   └── trailer/
├── crops/
│   ├── wheat/
│   │   ├── sprout.png
│   │   ├── growing.png
│   │   ├── mature.png
│   │   └── stubble.png
│   ├── barley/
│   ├── corn/
│   └── sunflower/
├── environment/
│   ├── soil/
│   │   ├── stubble.png
│   │   ├── plowed.png
│   │   └── cultivated.png
│   ├── buildings/
│   │   ├── silo.png
│   │   ├── barn.png
│   │   ├── workshop.png
│   │   └── fuel_station.png
│   ├── trees/
│   └── animals/
│       └── cat/
├── vfx/
│   ├── smoke_puff.png
│   ├── soil_particle.png
│   ├── grain_drop.png
│   └── rain_drop.png
└── ui/
    ├── icons/
    └── panels/
```

---

## 4. Asset Manifest Specification (`manifest.json`)

Each visual asset is cataloged with metadata:

```json
{
  "id": "tractor_classic",
  "type": "vehicle",
  "name": "Classic Utility Tractor",
  "scale": 1.0,
  "pivot": { "x": 0.5, "y": 0.8 },
  "layers": {
    "body": "assets/vehicles/tractor_classic/body.png",
    "wheel_front": { "path": "assets/vehicles/tractor_classic/wheel_front.png", "offset": [-28, 14], "radius": 12 },
    "wheel_rear": { "path": "assets/vehicles/tractor_classic/wheel_rear.png", "offset": [22, 10], "radius": 20 },
    "shadow": { "path": "assets/vehicles/tractor_classic/shadow.png", "offset": [0, 24] },
    "exhaust": { "offset": [-12, -22] },
    "hitch_rear": { "offset": [34, 12] }
  }
}
```

---

## 5. Procedural Vector Fallbacks

If an image generator or texture is being generated, the rendering pipeline seamlessly falls back to procedural vector canvas drawing (`DrawColoredPolygon` / `DrawCircle` / `DrawArc` in Godot `_Draw()`). This ensures zero missing texture artifacts or pink checkered boxes at all times during development.
