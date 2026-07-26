# Project-SCIM — build plan

> Verify the exact VLGE interaction features in the [VLGE guide](https://world.vlge.com/vlge-guide)
> in the first 20 minutes. Everything below is designed to degrade gracefully if a feature isn't there.

## The idea

Mise already proved **agents can decide** the optimal restock. This hackathon is about
**physical AI**, so the extension is the unsolved half: **executing** that decision in real 3D space,
and generating the **behavioral data** a robot needs to learn it.

**Mise (software) decides what should move. Project-SCIM is the world where it gets done — and every
human demonstration becomes training data for a restaurant-logistics robot.**

Through-line: **coordination (decided) → embodied execution (done) → behavioral dataset (learned).**

### The bigger frame: supply chain as a world model

State this in the pitch and the Q&A — it's what lifts the project from "a game about carrying
crates" to a physical-AI thesis:

- **World models.** A restock robot cannot be trained in a spreadsheet. It needs a *world* — shelves
  at real heights, crates with real mass, a service line that gets crowded at 19:00. Project-SCIM is
  a small world model of back-of-house, and the same world serves double duty: humans play in it to
  generate data, and a policy can later be evaluated in it.
- **Robots doing the rebalancing.** Today Mise's transfer ticket ends with a human runner. The
  endgame is the ticket dispatching to a robot: pick the near-expiry crate, navigate the line, place
  it on the Downtown shelf. The world is where that policy is taught and tested before it touches a
  real kitchen.
- **Human-robot collaboration timing.** The handoff is the crux, not the carry. *When* does a robot
  release a crate? How close does it approach a moving cook? How long does it wait? These are
  timing distributions you can only get by watching humans do it — which is exactly what the
  multiplayer handoff captures.
- **Why a game world and not a real kitchen.** Cheaper, safer, infinitely repeatable, and free of
  the consent problem — you cannot capture behavioral data in a real working kitchen without
  addressing identifiable people (the handbook is explicit about this). Simulation sidesteps it and
  scales.

**One-liner:** *"Supply chain has a brain — Mise. It doesn't have a body. Project-SCIM is the world
where we grow one."*

## Track: VLGE Together (Social + Behavior)

A bullseye — Track 2 wants *"interaction that produces useful data for robotics, physical AI, or
behavior modeling"* and to *"identify the robotics/physical-AI use case and the behaviors or
telemetry that could be valuable."* Coordination behavior + a credible data-collection use is exactly
that. **Fallback:** Track 1 (Open Narrative) as a single-player "restock-run" puzzle — same world,
no networking.

## Core loop (what a judge does in ~60s)

1. **Enter** the back-of-house world. Red glow on the **Downtown** shelf: *"Short 36 kg tomatoes."*
2. **Find the surplus** at **Marina** (a near-expiry crate, subtly marked — reward picking the right one).
3. **Carry it** along a route, past the cramped "line" during service (the congestion point).
4. **Hand off / place** on the Downtown shelf. Resolves; HUD: *"10 kg delivered · waste avoided · net 26 kg."*
5. The remaining **26 kg** arrives at the **dock** from the supplier → place it → *"$53.30, resolved."* Timer + score.

Clear start, objective, interaction, end state (the handbook's Definition of Done).

## The spatial + behavioral model (the crux)

Per session, capture: **trajectories** (pos over time → navigation/route choice), **pick/place
events** (manipulation targets), **handoff events** (two players, proximity, transfer → human-robot
collaboration timing), **congestion / near-misses** (obstacle avoidance under dynamic humans),
**expiry-aware choice** (did they grab the near-expiry crate? → policy-quality signal).

**Physical-AI use case (state it to judges):** a demonstration + data-generation environment for a
back-of-house **restock / inter-branch logistics robot**. Human plays → imitation-learning dataset →
a policy for pick-carry-place and human-aware navigation in a real kitchen. Mise supplies the *task
tickets*; Project-SCIM supplies the *embodied demonstrations*. A decision→action data flywheel.

Even if VLGE logging is optional, **show the data**: a live **path-trace / heatmap** overlay + a
session-telemetry panel. Makes the data story tangible and screenshots well.

## Tie to the winning Mise agents (originality edge)

Mise is now **vendored at [`mise/`](../mise/)** (all 90 tracked files, snapshot of 2026-07-25).
Read **[`mise/INTEGRATION.md`](../mise/INTEGRATION.md)** before building the world's task logic — it
has the verified ticket shape, the real branch inventory numbers, and the branch UUIDs.

- **MVP:** script the scenario (36 short → transfer 10 → buy 26 → $53.30) — but shape the world's
  task object like a Mise `transfer` / `po` record from the start. Same effort now, and the live
  tie-in becomes a config flip instead of a rewrite.
- **Stretch:** live tie-in — the hosted `rebalance_and_procure` agent emits the transfer decision
  over the MCP bridge, which **spawns the physical task** in the world. *"Agents that decide, worlds
  that do."*
- **Demo safety:** copy Mise's own pattern (`provision/server/mise.js`) — fixture by default, live
  when a token is set. The world must play with the network unplugged.

## Build plan in VLGE (MVP first)

**MVP — a working Play Mode build by ~15:00:**
1. **Base world:** use the **restaurant room template** — `SM_Restaurant_Room_01` with a bar mesh,
   17 × 10 m, tight enough that congestion exists. See [vlge-assets-and-maps.md](vlge-assets-and-maps.md).
   Dress a corner with `NewYork Kitchen Island 01` / `NewYork Pots 01` as the prep-and-store area.
   *Splats are no longer a gamble either — the Teleport Share IDs for all three supplied GS maps are
   in that doc, so a photoreal base is a paste-and-go if you want one.*
2. **Zones:** clear markers for **Marina (surplus)**, **Downtown (shortage, red glow)**, **Dock (supplier)**.
3. **Interactive crates:** grabbable "tomato crate" objects with pick/carry/drop (V-CTRL object +
   inspector + "game" logic if available; otherwise **proximity trigger zones**: enter surplus →
   attach crate → enter shortage → resolve).
4. **Objective + HUD:** on-screen goal text + readout (kg delivered, waste avoided, $53.30).
5. **Play Mode:** the full loop runs start-to-end **without builder intervention**, from a shareable link.

**Stretch (only after MVP is solid):**
- Path-trace / heatmap overlay (the data visual).
- Multiplayer: a Marina **runner** + a Downtown **cook** with a real **handoff** (the Track-2 social behavior).
- Real-kitchen Gaussian splat as the base (timebox: if not ready by mid-afternoon, keep the supplied map).
- Live Mise agent → task-ticket tie-in.
- Downloadable telemetry CSV — **done**, `scripts/extract_trajectory.py` (see [telemetry.md](telemetry.md)).
- The written "future data-collection plan" (a required Track-2 field).
- **Unity robot replay clip** for the demo video — gated on a stable MVP by ~16:00. See
  [unity-replay.md](unity-replay.md). No ROS needed; VLGE is Unity underneath, so poses transfer
  with no coordinate conversion.

## Day schedule (deadline 18:30 PT)

| Time | Do |
|------|-----|
| 10:00–10:30 | Lock the one-sentence loop; **read the VLGE guide** (confirm object/interaction/multiplayer features) |
| 10:30–11:30 | Base map + zones (Marina / Downtown / Dock) placed |
| 11:30–13:30 | The **pick → carry → place** interaction working single-player |
| 13:30–15:00 | Objective + HUD + resolve logic + $53.30 readout → **working Play Mode MVP** |
| 15:00–16:30 | Path-trace/heatmap **or** multiplayer handoff (pick ONE) |
| 16:30–17:00 | Mentor check-in — show the core loop, ask one focused question |
| 17:00–17:45 | **Freeze features.** Test the exact judge path in a clean session |
| 17:45–18:20 | Backup video (60–90s) + 3 screenshots + write the data-collection plan |
| 18:20–18:30 | **Submit the Google Form.** Save the confirmation |

## Judging alignment (100 pts)

- **Experience + Usability (25):** stranger enters, understands the shortage, resolves it — trivial controls.
- **Technical Execution (25):** stable Play Mode, reliable pick/place, runs from a link.
- **Track Fit + Impact (20):** Track 2 to the core — behavior → credible robot dataset.
- **Originality (20):** decision→embodiment loop + a real supply-chain behavior model in space (ideally the live agent tie-in).
- **Demo + Reproducibility (10):** judge-ready world link, controls text, disclosures (VLGE, supplied map, Mise agents / AI tools).

## Risks & the rule that wins

- **Timebox the splat and multiplayer.** *A working experience scores better than an unfinished
  capture*, and *no extra credit for unused features.* Nail the single-player loop first.
- **"Why does this matter?"** (Q&A one-liner): *"Agents can already decide the optimal restock; the
  unsolved part is doing it in a cramped kitchen — Project-SCIM turns human play into the demonstration
  data that teaches restaurant robots to execute."*

## Submission fields to prep (one form per team, by 18:30 PT)

Team + participant names · project title + **track (VLGE Together)** + engine · one-sentence pitch ·
judge-accessible world/build link · 60–90s backup video · 3 strongest screenshots · setup / controls
/ expected outcome · **Track 2 future data-collection plan** · external assets/datasets/AI tools used ·
repo/notes + device requirements.
