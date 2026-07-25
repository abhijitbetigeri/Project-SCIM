# V-CTRL capabilities — what the session recordings prove

The two open questions (zones? interactables?) are mostly answered **without opening the editor**.
The Edit Mode recordings in `VLGE_Sessions/` capture `entitySnapshot.fields` — the complete inspector
surface of every object someone touched. That's a schema dump of V-CTRL, sitting on disk.

Reproduce with:

```bash
python3 scripts/dump_vctrl_schema.py "VLGE_Sessions/VLGE Sessions - Hackathon"
```

## Proven: V-CTRL has a trigger → action system

There is a `FunctionalAsset` category, distinct from decor. Two prefabs appear in the sessions:
**`EventVolume`** (11 records) and **`SpawnPoint`** (127 records).

`EventVolume`'s inspector fields:

| Field | Type | Meaning |
|---|---|---|
| `OnCharacterEnter` | **List** | actions fired when a player enters the volume |
| `OnCharacterInteract` | **List** | actions fired when a player interacts inside it |
| `ViewTriggerInPlay` | Bool | show/hide the volume during Play Mode |
| `Position` / `Rotation` / `Scale` | Vector3 | the volume's extent |
| `AnimationToPlay` / `AnimationLoop` / `AnimationStartingCondition` | String/Bool | animation hooks |

Each entry in those Lists is an **Action** with three slots, decoded from the raw records:

```
Action_0:
  Target      (entity reference — pick another object in the scene)
  Function    (dropdown; possibleValues populated from the chosen Target)
  Parameters  (nested variable bag passed to the Function)
```

That is a visual scripting system: *when a character enters this volume, call `Function` on
`Target` with `Parameters`.* It is exactly the primitive the restock loop needs.

**Answer to Q1 (zones): yes.** Marina, Downtown and Dock are three `EventVolume`s.

## Proven: every object is interactable

Ordinary decor meshes (e.g. `SM_NewYork_Bed_01`) carry these fields:

- **`EntityCustomInteractPrompt` (String)** — the on-screen prompt text for interacting with it
- `EntityUrl`, `OpenInSmallWindow`, `OpenInTheSameTab` — an object can open a URL
- `ColorOverride_0`, `MaterialTextureOverride_0` — recolor/retexture at runtime
- `IsAutoRotating`, `RotationSpeed`, `RotateXAxis/Y/Z` — spin an object

`EntityCustomInteractPrompt` is the important one: **any mesh can present a custom interact prompt.**
So a crate can read "Pick up — 10 kg Roma tomatoes (expires 2d)" without any special prefab.

`ColorOverride_0` is how the Downtown shelf glows red and then stops glowing on resolve — no scripting
needed, just a field change driven from an Action.

## RESOLVED: the complete Function list

21 additional JSON files sat unextracted inside the supplied zips, mostly Edit Mode. In one of them
someone **did** select a Target, which populated the `Function` dropdown's `possibleValues`. The full
list, for a `StaticMesh` target:

| Function | Use in the restock loop |
|---|---|
| **`SetVisible`** | the carry mechanic — hide the crate at Marina, show it at Downtown |
| **`SetColor 0`–`SetColor 4`** | the shortage glow on the Downtown shelf, cleared on resolve |
| **`MoveTo`** | slide a crate onto a shelf; move the dock delivery into place |
| `Rotate` / `Scale` | emphasis, attention cues |
| `SetTexture 0`–`SetTexture 4` | swap a crate label / signage |
| `SetAutoRotating` | idle spin to draw the eye to the objective |
| `SetExternalURL` | link out — not useful here |

There is also a third event hook: **`GlobalSettingsPrefab.OnStart` (List)** — a world-level init
action, which is where the scenario should be set up (glow the shelf, show the objective).

And `AnimationStartingCondition` accepts `Start` / `On Trigger` / `On Interact` / `Never`, so placed
assets can animate on the same triggers.

### What this settles

**There is no attach-to-player function — true carry does not exist.** Don't spend a minute looking
for it. The loop is built from `SetVisible`:

1. `OnStart` → Downtown shelf `SetColor` red, objective visible.
2. Enter Marina volume → crate `SetVisible(false)` at Marina, carried-crate `SetVisible(true)`.
3. Enter Downtown volume → carried crate off, shelf crate on, shelf `SetColor` back to normal.
4. Enter Dock volume → the 26 kg delivery appears, place it, resolved.

Visually identical to carrying for a 60-second demo.

**There is no text/HUD function either** — no `SetText`, no message box. Work around it with the
`ImageMedia` entity type (present in the recordings, 44 records): pre-render each objective and
readout line as an image, place them all, and toggle with `SetVisible`. That covers "Short 36 kg
tomatoes", "10 kg delivered", and "$53.30, resolved" without any text API.

**Caveat:** this list is for a `StaticMesh` target. Other target types (`GlobalSettings`,
`ImageMedia`) may expose different functions — worth 30 seconds checking in the editor, since a
`GlobalSettings` target might offer something better for HUD.

## Unknown: multiplayer and NPCs

Nothing in the recordings speaks to this — all supplied sessions are single-player, and `chatMessages`
/ `audioMessages` are empty everywhere. `SpawnPoint` existing (127 records) is weakly suggestive of
multiplayer support, since a single-player world needs only one. Worth one question at the mentor
check-in rather than an hour of experimentation.

Per [PLAN.md](PLAN.md), the NPC-cook fallback matters more than true multiplayer anyway: it gives the
Track-2 social behavior *and* works when a judge enters alone.

## The remaining risk

Everything above proves the *world* can be built. What it does **not** prove is that firing these
triggers populates `zoneIdentifierAccesses`, `interactionSnapshots` and `gamePlayEventRecords` in the
session recording — the six empty channels that [telemetry.md](telemetry.md) makes the Track-2
argument out of.

That link is an assumption. It is a reasonable one — the engine defines those arrays and defines
these triggers, so they're likely wired together — but it is unverified.

**Verify it early and cheaply:** build one `EventVolume`, walk through it in Play Mode, download the
session JSON, and check whether the arrays are non-empty. Twenty minutes, and it de-risks the entire
pitch. If they stay empty, the fallback is to derive zone entry and interaction events from
`characterSnapshots` positions against known volume bounds — the extractor already emits everything
needed for that, so the data story survives either way. Say so honestly if asked; a derived event
stream is still a real dataset.
