# What's in VLGE_Sessions — full inventory and what to use

All four zips extracted (6.4 GB, plus the two loose `.ply` at the folder root). Everything stays
local and gitignored. Contents: **97 session JSON**, **13 mp4** screen recordings, **3 `.ply`**
Gaussian splats, **2 docx** guides.

## The headline: Teleport Share IDs for all three splat maps

The recordings contain the `TeleportShareId` field values for the supplied Gaussian splat worlds:

| Map | Teleport Share ID | Explored extent |
|---|---|---|
| **GS Map 1** | `c75e43d02a514c52ada16518139c2fbe` | 27.6 × 42.3 m |
| **GS Map 2** | `db748dc3e30e49f0b4443b003b483f8e` | 11.7 × 13.0 m |
| **GS Map 3** | `7f962ca076d046279d12b219f9d1a352` | 12.2 × 11.8 m |

Per `How to import the gaussian splat into VLGE.docx`, that ID is the **only** thing you need to load
a splat world:

1. Create a world from the black template **"World 50"**
2. In the editor click **"Add Splat"**
3. Paste the ID into **Teleport Share ID** in the object options
4. Wait for it to resolve, then click **"Generate Collider from splat"**

**This eliminates the splat risk entirely.** PLAN.md timeboxed splat capture because it takes 2–4
hours of Teleport processing. That's irrelevant now — these are already processed, and the IDs are in
hand. A photoreal real-world base is minutes of work, not an afternoon.

(The `.ply` files are the raw splat data. You don't need them for VLGE — the share ID is the route.
They're only useful if you want to inspect the geometry offline.)

## Map-by-map, inferred from sensor data

Derived from `spatialSensorFrames.nearestDisplayName` / `nearestMaterialTag` and `floorType`, since
no video tooling was available locally to watch the mp4s.

| Map | Type | Extent | What's actually in it |
|---|---|---|---|
| **3D Map 1** | built | 13.8 × 7.3 m | Wood floor, `NewYork Kitchen Island 01`, Roman statue, bed, chairs — a mixed interior |
| **3D Map 2** | built | 23.6 × 30.0 m | Concrete/marble/**grass**, stairs, windows — part outdoor, large |
| **3D Map 3** | built | **17.2 × 10.2 m** | **`SM_Restaurant_Room_01` + `SM_Restaurant_Room_01_bar`**, bar chairs, wall tiles ← |
| GS Map 1 | splat | 27.6 × 42.3 m | real capture; club sofas, hangers — reads retail/showroom, and large |
| GS Map 2 | splat | 11.7 × 13.0 m | real capture, compact |
| GS Map 3 | splat | 12.2 × 11.8 m | real capture, compact; tables, armchairs, vases |

## Recommendation: build on the restaurant room

**3D Map 3 is a restaurant.** Its base mesh is `SM_Restaurant_Room_01`, it has a dedicated bar mesh
(`SM_Restaurant_Room_01_bar`), bar chairs and wall tiles. At 17 × 10 m it is tight enough that the
congestion point — the thing that generates handoff-timing data — actually exists.

That beats a splat for this project, despite splats being flashier:

- **Thematic fit is free.** A restaurant supply-chain pitch inside an actual restaurant needs no
  explaining. A splat of a retail showroom would need constant hand-waving.
- **No load risk.** No splat resolution, no collider generation, nothing to fail in front of a judge.
- **The bar is the pass.** It's the natural handoff point between runner and cook.

It's a *dining room* rather than back-of-house — but the asset library has what's missing:
`NewYork Kitchen Island 01`, `NewYork Pots 01`, shelving and crates. Dress a corner as the prep/store
area and the read is fine. Judges will accept "back-of-house" from a restaurant interior far more
readily than from a data centre.

**Your current `supply-chain` world reads as server racks / data-centre aisles.** Wide aisles kill the
congestion behavior your dataset is about. Find the restaurant template in "Explore templates" (58
available, and this map was built from one of them).

**If you want the splat anyway**, GS Map 2 or 3 — both ~12 m, compact enough for congestion. GS Map 1
at 27 × 42 m is far too open; a player would never be forced near anyone.

## Asset library — confirmed available

Named assets seen across the recordings, so these exist in the browser:

- **Kitchen/restaurant:** `NewYork Kitchen Island 01`, `NewYork Pots 01`, `SM_Restaurant_Room_01_bar`,
  `MI_BarChairA01`
- **Seating/tables:** `NewYork Chair 01/02/03`, `NewYork Pouf 01`, `Agaria Table 02`, `Club Sofa 02`
- **Dressing:** `Agaria Vase 01–06`, `Flower in a pot 01`, `FlowerPot3`, `Hanger`, `Roman Statue 02`
- **Functional:** `EventVolume`, `SpawnPoint`, `GlobalSettingsPrefab`
- **Media:** `ImageMedia` (arbitrary image files — this is the HUD workaround), `GaussianSplat`

Asset categories are `Interior`, `Retail`, `FunctionalAsset`. No crate asset was seen — use a scaled
box or `NewYork Pots 01`, with the identity carried by `EntityCustomInteractPrompt` text rather than
the mesh.

## The two guides

- **`How To record a gaussian splat.docx`** — phone capture spec: 4K/30fps (not 60), 15 min per
  location, lock AE/AF/WB, three heights (1 m / 1.5 m / 2 m), slow continuous loops, never pivot in
  place, one unbroken clip, 50–70% frame overlap. *Only relevant if you capture your own — you
  shouldn't, given the share IDs above.*
- **`How to import the gaussian splat into VLGE.docx`** — the Teleport → World 50 → Add Splat →
  Generate Collider flow described above.

## Odds and ends

- **`GS Map 2/egocentric_view.mp4`** (277 MB) — an egocentric render, distinct from the screen
  recordings. Worth watching: if VLGE can export first-person video aligned to the telemetry, that's
  a strong visual for the data-collection story (paired observation + action, the classic imitation
  learning format).
- 13 mp4s total, one Edit Mode and one Play Mode per map. These are reference recordings of how the
  supplied worlds were built — the fastest way to learn the editor UI is to watch an Edit Mode video.
- No `ffmpeg`/`ffprobe` locally, so video contents were not inspected. Map descriptions above come
  from the sensor and object records instead.
- **There is no world-definition JSON anywhere in this folder.** All 97 files are session telemetry
  (`*_improved.json`, 2 distinct top-level schemas). The format `Update JSON` expects is still
  unknown — `Export` from the world menu remains the way to learn it.
