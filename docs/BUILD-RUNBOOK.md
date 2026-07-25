# Build runbook — the MVP, step by step

Written 15:17 against an 18:30 deadline. **Do the phases in order and do not skip ahead.** Phase 1
is the only thing that scores; everything after it is upside.

| Time | Phase |
|---|---|
| → 16:10 | **1. The loop, one trip.** Playable start to end |
| → 16:30 | **2. Verify telemetry.** The pitch's one assumption |
| → 17:00 | 3. Polish: HUD, glow, second trip |
| → 17:30 | 4. Freeze. Test the exact judge path in a clean session |
| → 18:00 | 5. Publish + video + screenshots |
| → 18:20 | 6. Form |

---

## Phase 0 — world setup (10 min)

1. **Explore templates** → pick the restaurant/dining one (tiled walls, bar counter, bar stools —
   that's `SM_Restaurant_Room_01`). New world from it.
2. Upload the six HUD PNGs from `build/hud/` as assets (`Upload Asset` on the dashboard).
   Generate them with `python3 scripts/make_hud_images.py -o build/hud` if they aren't there.
3. Place a **Spawn Point** where you want the judge to start — with clear sight of the Downtown shelf.

**Layout:** put **Downtown behind the bar**, so the player must pass the tightest part of the room.
That pinch is where handoff-timing and congestion data come from; a wide-open route produces nothing
interesting.

```
   [MARINA]  surplus, far corner
       |
       |  ← the route (past the bar = the congestion point)
       v
   ==== BAR ====
   [DOWNTOWN] shortage shelf, behind the pass
   [DOCK]     supplier delivery, side entrance
```

---

## Phase 1 — the loop, one trip (target 16:10)

**Ship this before anything else.** One trip: Marina → Downtown. The dock delivery resolves
automatically as part of the finish, which is honest to the Mise story (10 transferred, 26 bought).

### Objects to place

| Name it | What | Start state |
|---|---|---|
| `Shelf_Downtown` | kitchen island or shelving | visible, **SetColor red** |
| `Crate_Marina` | scaled box or `NewYork Pots 01` | visible |
| `Crate_Downtown` | same mesh, on the Downtown shelf | **hidden** |
| `Crate_Dock` | same mesh, at the dock | **hidden** |
| `HUD_01_objective` … `HUD_05_resolved` | ImageMedia, the six PNGs | only `01` visible |
| `Zone_Marina`, `Zone_Downtown` | **Event Volume** | `View Trigger In Play` **off** |

Set `EntityCustomInteractPrompt` on `Crate_Marina`:
> `Pick up — 10 kg Roma tomatoes (expires 2d)`

### Wiring

**`GlobalSettingsPrefab` → `On Start`** (sets the opening state, so a replay is clean):

| Target | Function | Value |
|---|---|---|
| `Shelf_Downtown` | `SetColor 0` | red |
| `Crate_Marina` | `SetVisible` | true |
| `Crate_Downtown` | `SetVisible` | false |
| `HUD_01_objective` | `SetVisible` | true |
| `HUD_02`…`HUD_05` | `SetVisible` | false |

**`Zone_Marina` → `On Character Enter`:**

| Target | Function | Value |
|---|---|---|
| `Crate_Marina` | `SetVisible` | false |
| `HUD_01_objective` | `SetVisible` | false |
| `HUD_02_carrying` | `SetVisible` | true |

**`Zone_Downtown` → `On Character Enter`:**

| Target | Function | Value |
|---|---|---|
| `Crate_Downtown` | `SetVisible` | true |
| `Shelf_Downtown` | `SetColor 0` | normal/green |
| `HUD_02_carrying` | `SetVisible` | false |
| `HUD_05_resolved` | `SetVisible` | true |

**That's the whole loop.** Enter → objective → walk to Marina → crate vanishes, "carrying" → walk
past the bar → Downtown → crate lands on the shelf, glow clears, "36 kg restocked · $53.30".

### Test it

Play Mode, from spawn, without touching anything. If it runs start to finish, **you have a
submittable project.** Publish World right now — before any polish.

> If `On Character Enter` doesn't fire, try `On Character Interact` instead and tell the player to
> press the interact key. Same wiring, one extra instruction in the controls text.

---

## Phase 2 — verify telemetry (target 16:30)

The single assumption the Track-2 pitch rests on. Do it as soon as one volume exists.

1. Play Mode, walk through both zones, complete the loop.
2. Get that session's JSON (**ask a VLGE person how to export your own session** — this is the
   unresolved question).
3. Check it:

```bash
python3 - <<'PY'
import json,sys
d=json.load(open(sys.argv[1] if len(sys.argv)>1 else 'session.json'))
for k in ['zoneIdentifierAccesses','interactionSnapshots','interactionEventRecords',
          'gamePlayEventRecords','chatMessages','audioMessages']:
    print(f"{len(d.get(k) or []):>6}  {k}")
PY
```

- **Non-empty →** the pitch is proven. Say so on stage; it's your strongest technical claim.
- **Still empty →** fall back to deriving zone events from `characterSnapshots` against known volume
  bounds. `scripts/extract_trajectory.py` already emits everything needed. Say it honestly — a
  derived event stream is still a real dataset.

Either way you have an answer, and either way the data story survives.

---

## Phase 3 — polish, only if Phase 1 is solid (target 17:00)

In this order, stopping whenever the clock says:

1. **The decoy crate.** Place a second crate at Marina — fresher, no expiry warning — with prompt
   `Pick up — 10 kg Roma tomatoes (expires 6d)`. Give it its own small volume that shows a "wrong
   crate — that one isn't near expiry" HUD. **This is worth more than it looks:** expiry-aware
   choice is the policy-quality signal in the data-collection plan, and a wrong option is what makes
   the right choice measurable.
2. **The second trip.** Dock → Downtown for the 26 kg, using `HUD_03_delivered` and `HUD_04_dock`.
   Re-entering `Zone_Downtown` would re-fire, so gate it: have `Zone_Dock`'s action call
   `SetVisible(false)` on `Zone_Downtown` and `SetVisible(true)` on a second `Zone_Downtown_B`.
   *Test whether hiding a volume disables its trigger — if it doesn't, skip the second trip.*
3. **The NPC cook.** A character asset behind the bar with a volume around it, so the delivery is a
   handoff to someone rather than a drop on a shelf. This is the Track-2 *social* half and the
   human-robot collaboration framing — the highest-value item here if time allows.
4. `View Trigger In Play` **off** on every volume.

---

## Phase 4 — freeze (17:00–17:30)

**Stop building.** Open the published link in a private window, as a stranger would, and run the
judge path start to finish. Fix only what's broken. Do not add anything.

Write the controls text now: *WASD to move, mouse to look, follow the red glow.*

---

## Phase 5 — capture (17:30–18:00)

- **Publish World**, confirm the link works logged-out.
- **Video, 60–90s:** objective → walk to Marina → pick → carry past the bar → deliver → resolved.
  One clean take, no narration needed if the HUD carries it.
- **3 screenshots:** the red shortage glow; carrying past the bar; the resolved `$53.30` panel.

---

## Phase 6 — submit (18:00–18:20)

Form: https://forms.gle/AgyhxvcXJGFNj9PC8

- Track: **VLGE Together (Social + Behavior)** · Engine: **VLGE / V-CTRL**
- Pitch: *"Mise's agents decide the optimal restock; Project-SCIM is the world where a human executes
  it — and every run becomes demonstration data for a restaurant logistics robot."*
- **Data-collection plan:** paste the short version from [data-collection-plan.md](data-collection-plan.md)
- **Disclosures:** VLGE/V-CTRL, supplied restaurant template, Mise as **pre-existing work** (AGI
  Summit 2026) reused as the decision layer, Cotal/Runtype/InsForge, AI coding tools

**Submit by 18:20.** Save the confirmation.
