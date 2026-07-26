# Six empty arrays

*How we found the gap in robot training data by reading a game engine's own session recordings —
and then built the thing that fills it.*

---

## The brain had no body

We already had the hard half working. Mise is a multi-agent system for restaurant supply chains:
branch and supplier agents negotiate peer-to-peer, and when one location runs short they resolve it
between themselves before anyone buys anything.

Here's the case it handles, using real numbers from our fixtures rather than a toy example. Downtown
holds 4.0 kg of Roma tomatoes against a par of 40 — **36 kg short**, well below its reorder point.
Marina holds 34.0 against a par of 24 — **10 kg surplus**, and that stock expires in two days.
Mission holds 16.0 kg, which *looks* like it could help, except its own par is 20. It's short too. It
can't donate.

So the agents move Marina's near-expiry stock first, then buy only the net 26 kg at $2.05/kg.
**$53.30.** One approval instead of three independent purchase orders and a bin full of spoiled
tomatoes.

That's a decision. It is not an action.

Somebody still has to walk into a kitchen mid-service and physically move 10 kg of tomatoes from one
place to another — through sub-metre clearances, around a crew that's moving, carrying an object
whose *identity matters*, because the near-expiry crate is not interchangeable with the fresh one
sitting next to it.

No robot does that today. We wanted to know why. The answer turned out to be less about actuators
than about data.

## Reading an engine from its own exhaust

We started building in VLGE, and were handed 12 GB of sample session recordings — six maps, Edit Mode
and Play Mode captures, 97 JSON files.

Most teams would treat those as example content. We treated them as documentation, because that's
what they turned out to be.

Each session file carries **17 record arrays**. Not gameplay logging — *demonstration* capture. Body
and camera pose at roughly 195 Hz. A `frameSignalRecords` block with velocity decomposed into
longitudinal, lateral and vertical, plus slip angle, yaw rate, jerk, path curvature and turn radius.
Discretised action labels. And two fields that gave the game away: **`dm_action_token_id`** and an
**11-dimensional `dm_latent_action_vec`**.

That's action tokenisation. It's the representation behaviour-cloning and world models train on.
Whoever built that recorder was not thinking about games.

There was more. `spatialSensorFrames` describes a **240-ray depth scan** — 24 azimuth by 10
elevation, 90° × 60° field of view, 40 m range, 2 Hz — reporting nearest object type and material
tag. A depth camera in all but name. And it carries an `isAgent` boolean, meaning the schema already
anticipates recording non-human actors alongside humans.

Then the editor recordings gave us something better. `entitySnapshot.fields` embeds the **complete
inspector surface** of every prefab someone touched. We could reverse-engineer what the editor could
do without opening the editor:

```
EventVolume  [FunctionalAsset]
  OnCharacterEnter      (List)   <-- event hook
  OnCharacterInteract   (List)   <-- event hook
  ViewTriggerInPlay     (Bool)
```

Each list entry is an Action of `{Target, Function, Parameters}` — a visual scripting system. And in
one file, buried inside a zip nobody had extracted, someone had actually selected a Target, which
populated the dropdown:

```
SetVisible · SetColor 0–4 · MoveTo · Rotate · Scale
SetTexture 0–4 · SetAutoRotating · SetExternalURL
```

No attach-to-player. No text API. We knew, before touching the editor, that true carrying didn't
exist and the HUD would have to be pre-rendered images toggled with `SetVisible`. That saved an hour
we didn't have.

## The finding

Then we counted what was actually *in* those 17 arrays across all 97 files.

Eleven were populated. Six were empty in **every single session**:

| Channel | Records, across 97 sessions |
|---|---|
| `zoneIdentifierAccesses` | 0 |
| `interactionSnapshots` | 0 |
| `interactionEventRecords` | 0 |
| `gamePlayEventRecords` | 0 |
| `chatMessages` | 0 |
| `audioMessages` | 0 |

They're empty because the sample sessions are people walking around looking at furniture. No zones
were defined. Nothing was interactive. There was no task and no second actor.

The engine computes a metric called `zone_convergence_density`. It reads **0.0** in every session,
because there were no zones to converge on.

That's the whole project in one number. The recorder is excellent. The *payload* is missing. Nobody
had given it a purposeful task to record — manipulation, task phases, and two actors coordinating.

Those six channels are exactly what a restock task generates.

## When the plan breaks

Mid-afternoon, VLGE's project export stopped working. Without it we couldn't author scenes
programmatically, and hand-placing everything through a browser editor wasn't going to happen in the
time left.

So we moved to Unity — with about three hours on the clock and Unity not yet installed.

The constraint shaped the design, and improved it. With no time for an art pipeline, **everything is
generated procedurally from primitives at runtime**. No prefabs, no imported models, no scene setup.
Create an empty project, drop in six C# files, press Play. A `[RuntimeInitializeOnLoadMethod]`
bootstraps three restaurant branches, a distributor warehouse, the crates, the zones and the robot.

The pivot also strengthened the argument. In VLGE a human was going to stand in for the robot. In
Unity the robot executes the ticket itself. "Robots doing the rebalancing" stopped being a roadmap
slide.

## What the robot taught us about steering

Three failures, each instructive.

**It stopped short of everything.** The agent uses whisker raycasts for obstacle avoidance. As it
closed on a shelf, the whiskers hit *that shelf* and pushed back harder than the seek force pulled
in. It stalled two metres out, oscillating, forever. Avoidance was repelling it from its own
destination. Fix: suppress avoidance inside 2.5× the arrival radius — and add a navigation timeout,
because a demo must never hang whatever the geometry does.

**It jammed against walls.** The robot spawned inside a room with side walls and had to leave to
reach the source. Whisker steering has no path planner; it ground against the wall until the timeout
rescued it. Fix: open-fronted rooms, and every remaining wall moved to Ignore Raycast so it reads as
architecture without steering the agent. Tables and shelves stayed as obstacles — that's where the
clearance behaviour worth recording comes from.

**It looked like it was delivering, not fetching.** The robot spawned at the warehouse, so its first
leg was warehouse→Marina — purely to *reach* the crate. On screen that read as goods flowing from the
distributor to Marina, the exact opposite of the ticket. Fix: start it at Downtown, the branch that's
short, so the outbound leg is a fetch and the return leg is the transfer. And surface the current leg
on the HUD: *"robot en route to collect — empty"* versus *"CARRYING to Downtown"*.

That last one wasn't a bug in the code. It was a bug in what the code *communicated*, which for a
judged demo is the same thing.

## Two hours of shipping

The build rendered magenta. Everything is created at runtime, so nothing in the saved scene
references URP's Lit shader, and the build stripper dropped it — correct in the Editor, broken in the
player. The documented fix, adding it to *Always Included Shaders*, doesn't work: that picker only
searches `Assets/`, and URP's shaders live in `Packages/`. We shipped a material asset in
`Resources/` instead and instantiate copies from it. A material in Resources is always included, and
it drags its shader along with it.

Then GitHub Pages refused to serve the build. Unity compresses Web builds to Brotli; Pages doesn't
send `Content-Encoding: br` and gives you no way to set response headers. Since we couldn't configure
the host, we removed the compression — decompressed the `.br` files in place and repointed the
loader. The wasm went from 7.9 MB to 43 MB. It loads on any static host with zero configuration,
which was the only property that mattered.

## What we actually have

A simulation you can open in a browser that plays itself in about 45 seconds. Three restaurant
branches, a distributor warehouse, shelf colour encoding stock state. A robot takes the transfer
ticket, drives to Marina empty, picks the near-expiry crate, carries it back through the service
pass, pauses at the cook, releases, places. Then it collects the purchase order from the distributor.
Shelf goes red to green. $53.30.

And every run writes a dataset: trajectories with pose, speed and action label; zone entry and exit;
pick and place events with begin/complete timestamps; and handoff events logging **approach distance
and release delay**.

That last one is the point. Plenty of prior work treats humans as obstacles to route around. Far less
treats them as *collaborators receiving an object* — a person who isn't looking at the robot, in a
space too small to wait in politely. How close do you get? How long do you hold? When do you let go?
Those are distributions, and you only get them by watching it happen many times.

## What this isn't

Nothing is learned here. The robot follows a scripted task. What works end to end is the pipeline
from execution to robot-consumable trajectory data — training a policy on it is the next step, not
this one, and we'd rather say so than let a judge discover it in Q&A.

Avoidance is three raycasts, not a planner. Simulated depth is clean; real depth is noisy. There's no
force or grasp data, so this addresses navigation, target selection and collaboration timing — not
dexterous manipulation. And a hackathon produces a proof of pipeline, not a dataset.

One choice we'd defend regardless of time: **it had to be simulated.** You cannot lawfully record a
working kitchen. Staff mid-shift can't consent frame by frame, and a commercial kitchen is a private
space. Simulation doesn't dodge the consent problem — it removes it, and makes congestion and
near-misses safe and repeatable besides.

---

Supply chain has a brain. It didn't have a body. This is where we started growing one.

**Live:** https://abhijitbetigeri.github.io/Project-SCIM/
**Source:** https://github.com/abhijitbetigeri/Project-SCIM
