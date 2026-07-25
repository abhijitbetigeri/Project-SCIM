# Project-SCIM — Unity simulation

A procedurally generated supply-chain simulation. **No art assets, no prefabs, no scene setup.**
Create an empty Unity project, drop these scripts in, press Play.

## Setup (~5 minutes once Unity is installed)

1. **Unity Hub → New Project → 3D (Core)**. Unity **6000.x LTS** or **2022.3 LTS**. Built-in or URP
   both work — the scripts only set `Renderer.material.color`, which is pipeline-agnostic.
2. **Critical:** `Edit → Project Settings → Player → Other Settings → Active Input Handling` must be
   **"Input Manager (Old)"** or **"Both"**. The scripts use `Input.GetKey`; if this is set to the new
   Input System only, nothing responds and Unity throws at runtime.
3. Copy `unity/Assets/Scripts/` into your project's `Assets/` folder.
4. **Press Play.** The world bootstraps itself via `[RuntimeInitializeOnLoadMethod]` — there is
   nothing to drag into the scene.

## What a judge sees

| Key | View |
|---|---|
| **1** | **Restaurant branches** — Downtown, Marina, Mission. Shelf colour is stock state: red below reorder, amber below par, green at par |
| **2** | **Distributor warehouse** — racking, pallets, the loading dock the purchase order ships from |
| **3** | **Follow the robot** — close on the pick, the carry through the pass, and the handoff to the cook |

Right-drag to look · WASD to fly · scroll to zoom · **T** exports telemetry · **R** restarts.

The scenario runs itself, unattended, in about 45 seconds: agents decide → robot transfers 10 kg
Marina → Downtown → robot collects the 26 kg purchase order from the warehouse → Downtown resolves at
$53.30. No builder intervention, which is the handbook's definition of done.

## Why the numbers are what they are

Straight from the Mise fixtures (`mise/INTEGRATION.md`), not invented: Downtown holds 4.0 kg against a
par of 40.0 (**36 short**); Marina holds 34.0 against a par of 24.0 (**10 surplus, 2 days to
expiry**); so transfer 10 and buy the net 26 at $2.05/kg = **$53.30**. Mission holds 16.0 but is
*itself* below par, which is why it cannot donate — worth saying aloud, because it shows the decision
was non-trivial.

## The telemetry — why this is still a Track-2 project

`SimTelemetry` records the same channels VLGE emits (`docs/telemetry.md`), including the **six that
were empty in every supplied VLGE session**:

| Channel | Filled by |
|---|---|
| `zoneIdentifierAccesses` | robot entering/leaving branch and dock zones |
| `interactionEventRecords` | `pick_begin`, `pick_complete`, `place_begin`, `place_complete` |
| `gamePlayEventRecords` | ticket assigned, task complete, scenario resolved |
| `handoffEvents` | approach distance and release delay at the cook |

Press **T** to write `scim_trajectory_*.csv` and `scim_events_*.csv` to
`Application.persistentDataPath` (on macOS, `~/Library/Application Support/<company>/<product>/`).
The trajectory CSV carries pose, speed and an action label per sample — the same shape
`scripts/extract_trajectory.py` produces from VLGE sessions, so the analysis pipeline is unchanged.

**Handoff timing is the point.** `HandoffPause` and the logged approach distance are the collaboration
quantities the data-collection plan argues are missing from existing datasets. Here they are
instrumented rather than asserted.

## Files

| File | Role |
|---|---|
| `MiseScenario.cs` | the supply-chain state and the real fixture numbers |
| `SupplyChainSim.cs` | builds all three environments procedurally; runs the scenario |
| `RobotAgent.cs` | the physical-AI agent — steering, whisker avoidance, pick/carry/place, handoff |
| `SimTelemetry.cs` | records trajectories and the discrete event channels; CSV export |
| `SimCameraRig.cs` | presets and free look |
| `SimHUD.cs` | judge-facing overlay (IMGUI — no Canvas or TextMeshPro dependency) |

## Shipping a judge-accessible build

**WebGL** gives a link, which is what the submission form wants:

`File → Build Settings → WebGL → Switch Platform → Build`

Two warnings, both from experience: switching platform re-imports the whole project, and the build
itself takes 10–30 minutes. **Start it early.** Host the output folder on Netlify Drop, Vercel, or
GitHub Pages — it's static files.

**If WebGL will not finish in time**, build a **Mac/Windows standalone** instead (2–3 minutes), record
the 60–90 s demo video from that, and submit the video plus this repo. A working local build with a
video beats a broken link.

## Known limitations

- Avoidance is three whisker raycasts, not a planner. It produces plausible clearance behavior; it is
  not a navigation policy.
- The robot follows a scripted task sequence. Nothing is learned here — this is a data *generation*
  and demonstration environment, which is exactly the claim in the data-collection plan. Don't
  overclaim it in Q&A.
- Geometry is primitives. It reads as a diagram of a restaurant, not a photoreal one.
