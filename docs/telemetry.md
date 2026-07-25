# VLGE session telemetry — what the engine already records

Analysis of the supplied `VLGE_Sessions/` capture set (kept local, gitignored — 12 GB).
**76 session JSON files** across 6 maps (`3D Map 1–3`, `GS Map 1–3`), each with Edit Mode and Play
Mode recordings, from 2026-06-29 and 2026-07-05. Sessions shard into **30-second files**
(`remoteUpdateFrequency: 30.0`). Every schema below was read from the files, not assumed.

## The headline

**We do not have to build telemetry. VLGE already emits a robot-learning dataset.**

A session file carries **17 record arrays**. Across all 76 files, 11 are populated and **6 are always
empty** — and the 6 empty ones are precisely the channels Project-SCIM's core loop generates.

| Populated by a walkthrough | Total records |
|---|---|
| `characterSnapshots` | 387,705 |
| `lookAtSnapshots` | 15,766 |
| `frameSignalRecords` | 7,169 |
| `modifiedEntityFieldRecords` | 2,264 |
| `spatialSensorFrames` | 1,842 |
| `entityVibilityRecords` *(sic — engine typo)* | 1,422 |
| `physicalInputSnapshots` | 1,218 |
| `modifiedObjectRecords` | 96 |
| `mapEnvironmentObjectRecords` | 81 |
| `placedObjectRecords` | 57 |
| `logicalInputSnapshots` | 48 |

| **Never populated in any supplied session** | Why it's empty | Project-SCIM fills it with |
|---|---|---|
| `interactionSnapshots` | nothing interactive was built | crate pick / carry / drop |
| `interactionEventRecords` | ” | discrete manipulation events |
| `zoneIdentifierAccesses` | no zones defined | Marina / Downtown / Dock entry + exit |
| `gamePlayEventRecords` | no game logic | shortage raised, transfer resolved, PO received |
| `chatMessages` | single-player | runner ↔ cook coordination |
| `audioMessages` | ” | spoken handoff cues |

**This is the pitch.** The supplied sessions are people *walking around looking at furniture*. They
prove the recorder works and leave the manipulation, zone, task, and social channels blank.
Project-SCIM is a purposeful task built to populate exactly those six. Same recorder, first real
payload.

Corroborating detail: `sessionMetrics.zone_convergence_density` is **0.0** in every session — the
engine computes a zone metric that has no zones to converge on.

## What's in the populated channels

### `characterSnapshots` — trajectory at ~195 Hz
5,864 samples per 30 s. Per sample: `pc`/`rc` (camera position + rotation quaternion), `pp`/`rp`
(player pivot pose), `floorType` (e.g. `"Wood"` — surface-aware!), `timestamp.ticks`, and `fm`, a
block of derived motion signals.

### `frameSignalRecords` — the `dm_*` signal block (the important one)
These are explicitly demonstration/world-model features, not game stats:

- **Kinematics:** `dm_vel_longitudinal_mps`, `dm_vel_lateral_mps`, `dm_vel_vertical_mps`,
  `dm_speed_xz_mps`, `dm_slip_angle_deg`, `dm_jerk_mps3`
- **Rotation:** `dm_cam_yaw_rate_dps`, `dm_cam_pitch_rate_dps`, `dm_body_yaw_rate_dps`,
  `dm_ang_accel_yaw_dps2`
- **Path shape:** `dm_path_curvature`, `dm_turn_radius_m`
- **Discretized action:** `dm_action_locomotion` (`idle`/`walk`/`running`), `dm_action_turn`,
  `dm_action_pitch`, `dm_action_vertical`, and **`dm_action_token_id`**
- **`dm_latent_action_vec`** — an 11-dimensional latent action vector
- **Gaze coupling:** `dm_gaze_object`, `dm_gaze_hit_distance_m`, `dm_gaze_movement_align_deg`
- **Raw input:** `dm_input_keys`

`dm_action_token_id` and `dm_latent_action_vec` are action *tokenization* — the representation
behavior-cloning and world models train on. VLGE is not recording gameplay; it is recording
demonstrations.

### `spatialSensorFrames` — a simulated robot sensor
`sensorId: "player_camera_human_depth"`, `sensorProfile: "HumanDepthViewV0"`, mounted on
`Camera.main`. A **240-ray depth scan** (24 azimuth × 10 elevation), 90° × 60° FOV, 40 m range, at
2 Hz. Reports `nearestHitDistance`, `averageHitDistance`, `hitRatio`, plus `nearestObjectType`,
`nearestDisplayName`, `nearestMaterialTag`.

This is a depth camera feed in all but name — the observation space a navigation policy consumes.
Note the field **`isAgent: false`**: the schema anticipates non-human actors being recorded the same
way. That is the human-robot collaboration story, already in the data model.

### `lookAtSnapshots` — gaze with raycast resolution
Per sample: `gazeOrigin`, `gazeDirection`, `hasHit`, `entityId`, `objectType`, `colliderName`,
`hitPoint`, `hitNormal`, `distance`. Attention targets, resolved to named objects — this is how you
prove a player *considered* the near-expiry crate before choosing.

### `sessionMetrics` — 32 derived metrics, free
`session_duration_s`, `total_distance_m`, `avg_velocity_mps`, `path_length_m`, `net_displacement_m`,
**`straightness_index`**, `radius_of_gyration`, **`spatial_entropy`** (+ normalized),
`dwell_cluster_count` with `cluster_centroids`, `movement_distribution`
(stationary/walk/running frame counts), `surface_distribution`, `exploration_loop_count`,
`camera_pitch_mean_deg`, `camera_yaw_std_deg`, `zone_convergence_density`.

`straightness_index` is route efficiency — the single best "did they find the fast path" number, and
it needs no work from us. Sample session: path 28.65 m, net displacement 2.49 m, straightness 0.087
(wandering). A player on a purposeful restock run should score dramatically higher, and that contrast
*is* a result worth showing a judge.

### Editor-side records
`placedObjectRecords` / `mapEnvironmentObjectRecords` capture every object with `entityId`,
`position`, `scale`, `rotation`, and an `entitySnapshot` (`displayName`, `prefabName`, `entityType`,
`assetCategory`). `modifiedObjectRecords` logs before/after transforms; `modifiedEntityFieldRecords`
logs individual inspector field edits with `previousValue` → `value`.

Practical consequence: **the world's object layout is machine-readable**, so shelf and crate poses
can be exported as scene ground truth alongside the trajectories.

## Data quality caveats (state these honestly)

- `max_velocity_mps: 71.16` in a walking session — teleport/respawn spikes. Any dataset needs an
  outlier filter; don't quote max velocity to judges.
- Timestamps are .NET `ticks` (100 ns since year 1). Convert: `unix_s = ticks/1e7 - 62135596800`.
- 30-second sharding means a full run spans several files — they must be concatenated in timestamp
  order before analysis.
- `entityVibilityRecords` is misspelled in the engine schema. Match it exactly when parsing.

## What this changes in the plan

1. **The path-trace/heatmap stretch is largely already done.** `cluster_centroids` +
   `characterSnapshots` are a heatmap's input. Rendering it is now a visualization job, not a
   capture job — which frees the 15:00–16:30 block for the **handoff**, resolving the Track-2
   social gap.
2. **The data-collection plan writes itself from real schemas**, not aspiration. Naming
   `dm_latent_action_vec`, a 240-ray depth profile, and 387,705 recorded trajectory samples is far
   stronger than "we would log trajectories."
3. **Design the world to fill the six empty arrays.** Use real zone identifiers for Marina /
   Downtown / Dock, real interactable crates, and real gameplay events — so the recorder captures
   the task rather than a walk.
4. **A GS map is a viable base.** `GS Map 1–3` are supplied, prebuilt, and already recorded against;
   `gs_map_1-002.ply` (3.3 GB) and `gs_map_3-003.ply` (2.2 GB) are on disk, with an import guide at
   `VLGE_Sessions/VLGE Sessions - Hackathon/How to import the gaussian splat into VLGE.docx`. This
   removes the reason to attempt your own capture — the timebox risk in PLAN.md is now moot.
