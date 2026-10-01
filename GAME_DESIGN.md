# Desktop Farm Idle — Game Design Document (GDD)

## 1. High Concept & Vision
**Desktop Farm Idle** is a serene, ambient idle farm simulation designed to sit unobtrusively at the bottom edge of a user's Windows desktop, directly atop the taskbar. In its standard operating state, the game window is fully click-through, transparent, and tranquil, allowing the user to seamlessly click underlying desktop icons, work applications, and browser windows without disturbance.

When desired, the player can interact via the Windows System Tray or toggle **Interactive Mode** to directly inspect machinery, dispatch tractors, hire specialists, or adjust crop allocations.

---

## 2. Core Gameplay Loop

```
  [ Soil Preparation ] ──> [ Sowing ] ──> [ Growth & Weather ] ──> [ Harvesting ]
          ▲                                                              │
          │                                                              ▼
  [ Maintenance & Upgrades ] <── [ Market / Contracts / Profit ] <── [ Transport & Storage ]
```

1. **Soil Preparation**:
   - The field begins in `Stubble` state after a harvest.
   - Tractor equipped with a **Plow** tills the soil, turning stubble into dark rich furrows (`Plowed`).
   - Tractor equipped with a **Cultivator** levels and aerates the soil (`Cultivated`), optimizing seed bed moisture.
2. **Sowing**:
   - Tractor with a **Seeder** implants selected crop seeds (`Seeded`).
3. **Germination & Maturation**:
   - Seeds germinate (`Germinating`) and advance through visual growth phases:
     - `Stage 1` (Sprouts emerge from soil)
     - `Stage 2` (Vibrant green vegetative canopy)
     - `Stage 3` (Flowering / ripening head formation)
     - `Ready to Harvest` (Golden wheat, plump ears of corn, bright sunflowers)
   - Influenced by ambient weather (rain speeds growth; heat wave demands irrigation; hail risks minor damage).
4. **Harvesting**:
   - Combine harvester cuts through mature crops, leaving behind neat rows of stubble while filling its internal grain bunker.
   - Ground changes directly behind the combine header in real time.
5. **Transport & Discharge**:
   - When combine bunker nears capacity, a grain truck aligns adjacent to the combine.
   - The auger pipe deploys and pours streams of golden grain into the truck bed.
   - The truck journeys to the on-farm Silo / Storage facility.
6. **Economics & Reinvestment**:
   - Harvested grain is stored in silos or sold immediately at fluctuating market rates.
   - Profits finance more powerful tractors, wider implements, experienced farm hands, and automation controllers.

---

## 3. Crop Cycle & Balance

A complete natural crop cycle spans **15 to 20 real minutes** (target default: **18 minutes**).

### Initial Crop Roster:
| Crop | Base Cycle Time | Seed Cost | Yield / Cell | Base Price / Ton | Water Demand | Heat Res. | Cold Res. |
|---|---|---|---|---|---|---|---|
| **Wheat** | 18 min | 50 Coins | 1.2 t | 120 Coins | Moderate | High | Moderate |
| **Barley** | 16 min | 45 Coins | 1.1 t | 110 Coins | Low | High | High |
| **Corn** | 22 min | 75 Coins | 2.0 t | 145 Coins | High | High | Low |
| **Sunflower** | 24 min | 90 Coins | 1.5 t | 180 Coins | Moderate | Very High | Low |
| **Potato** | 20 min | 60 Coins | 2.5 t | 95 Coins | High | Low | Moderate |

---

## 4. Machinery & Equipment

### 4.1 Tractors
1. **Old Utility Tractor ("Rusty Classic 25")**:
   - Compact 2-cylinder tractor, slow but dependable starter machine.
   - Work speed: 1.0x, Fuel tank: 40L, Reliability: 75%.
2. **Universal Medium Tractor ("AgroSteed 70")**:
   - Versatile workhorse capable of pulling 3-furrow plows and medium seeders.
   - Work speed: 1.4x, Fuel tank: 80L, Reliability: 88%.
3. **Heavy Field Tractor ("Titan 150")**:
   - High torque, dual rear tires, ideal for heavy cultivation and large attachments.
   - Work speed: 1.8x, Fuel tank: 160L, Reliability: 92%.
4. **Modern Power Tractor ("Apex Pro 280")**:
   - Advanced GPS-guided tractor, ultra-efficient fuel injection.
   - Work speed: 2.3x, Fuel tank: 260L, Reliability: 97%.

### 4.2 Harvesters
1. **Compact Harvester ("CerealCut 3")**:
   - 3-meter cutterbar, 3000L bunker.
2. **Modern Mega Combine ("GrainMaster 9000")**:
   - 7-meter cutterbar, high capacity 9500L bunker with fast discharge auger.

### 4.3 Support Vehicles
- **Grain Truck**: Hauls crops between combine in the field and the storage silo.
- **Service & Repair Van**: Dispatches mobile mechanic to disabled vehicles on-field.
- **Fuel Bowser / Tanker**: Refuels machinery directly at field edge before on-site fuel stations are unlocked.

### 4.4 Implements
- **Plow**: Converts stubble into furrowed soil.
- **Cultivator**: Prepares smooth seedbed.
- **Precision Seeder**: Sows designated crop seeds with fertilizer integration.
- **Fertilizer Spreader**: Boosts soil nutrient levels for +25% crop yield.
- **Crop Sprayer**: Neutralizes weed and fungal threats.
- **Tipping Trailer**: Transports bulk agricultural goods.

---

## 5. Specialists & Worker System

1. **Tractor Driver**: Navigates tractors, maintains optimal implement depth.
2. **Combine Operator**: Maximizes crop intake without header clogging.
3. **Mechanic**: Diagnoses worn components, performs preventative oil/filter maintenance, repairs breakdowns.
4. **Agronomist**: Scans fields for moisture deficits, soil disease, and weed infiltration.
5. **Farm Manager**: Unlocks hands-free automation, auto-assigning available equipment and workers to pending field tasks.

### Stamina & Morale
- Workers experience gradual fatigue during active shifts (0-100%).
- At >70% fatigue, efficiency tapers down. At >90%, error rates rise (slower turns, minor implement misalignment).
- When fatigue peaks or scheduled breaks occur, workers stroll to the cozy Farmhouse / Break room to rest and replenish stamina.

---

## 6. Weather & Environmental Ecosystem

Markov-chain weather dynamics provide both visual ambiance and gameplay modifiers:
- **Clear Sky**: Standard drying and growth rate.
- **Light / Gentle Rain**: Naturally hydrates soil; provides free water without requiring irrigation.
- **Heavy Rain / Storm**: Increases soil mud; field operations slow down; combine harvesting temporarily halted to prevent grain spoiling.
- **Hail**: Rare event; damages 10-25% of mature standing crops unless harvested promptly.
- **Heat Wave**: Accelerates moisture loss; unwatered crops risk health penalty.

### Wildlife & Visual Life
- A relaxed farm cat prowling fence posts, dozing on tractor hoods when idle, or chasing butterflies.
- Sparrows, crows, and swallows hopping across fresh stubble picking stray seeds.
- Swaying birch and willow trees responding to real-time wind speed.

---

## 7. Progression Milestones

- **Level 1**: 1 Field, Old Classic Tractor, 2-furrow Plow, Seeder, 1 Worker.
- **Level 3**: Cultivator implement, unlocks Barley.
- **Level 5**: Second field expansion.
- **Level 7**: Entry-level Combine Harvester ("CerealCut 3").
- **Level 10**: On-site Workshop (faster vehicle repair, preventative maintenance).
- **Level 12**: Field Fuel Station (eliminates mobile tanker delivery fees).
- **Level 15**: Agro-Meteorological Weather Station (alerts 3 minutes ahead of severe storms).
- **Level 20**: Grain Silo complex (storage up to 100 tons, market price arbitration).
- **Level 25**: Farm Manager AI (full end-to-end task automation).

---

## 8. Offline Progression Model
When the game is closed, the simulation deterministically projects progress up to an 8-hour cap (upgradable):
- Calculates number of full harvest cycles feasible given current fuel, storage, and worker stamina.
- Deducts fuel, calculates wear-and-tear and seed purchases.
- Upon relaunch, presents a warm **"While You Were Away"** summary modal with total grain produced, coins earned, and equipment status.
