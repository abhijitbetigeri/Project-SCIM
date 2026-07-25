# Submission answers — paste-ready

Form: https://forms.gle/AgyhxvcXJGFNj9PC8 · **Submit by 17:50**, not 18:20. Buffer is not optional.

---

## Project title
**Project-SCIM — Supply Chain Intelligence Management**

## Track
**Track 2 — VLGE Together (Social + Behavior)**

## Engine
**Unity 6 (6000.x LTS)**

## One-sentence pitch
> Our agents already decide the optimal restaurant restock; Project-SCIM is the simulated world where
> a robot physically executes that decision — and every run emits the demonstration data a real
> back-of-house logistics robot would train on.

## Setup / controls / expected outcome

**Setup:** open the link, nothing to install. Runs unattended.

**Controls:**
- `1` restaurant branches · `2` distributor warehouse · `3` follow the robot
- right-drag to look · `WASD` to fly · scroll to zoom
- `T` export telemetry CSV · `R` restart

**Expected outcome:** the scenario plays itself in about 45 seconds. Downtown is 36 kg short of Roma
tomatoes. The agents transfer 10 kg from Marina's near-expiry surplus, then buy the net 26 kg from the
distributor at $2.05/kg. A robot executes both moves — pick, carry through the service pass, hand off
to the cook, place. The shelf turns from red to green and the live telemetry panel shows the
trajectory and event counts accumulating. Press `T` at any point to export the dataset.

---

## Track 2: future data-collection plan

*(Short version — the full reasoning is in [data-collection-plan.md](data-collection-plan.md))*

**Physical-AI use case.** A back-of-house restock and inter-branch logistics robot for restaurants.
Our agent system Mise (AGI Summit 2026 winner) already decides *what* stock should move between
branches; what does not exist is a policy that can *execute* that decision in a cramped working
kitchen. Project-SCIM generates the demonstration data that policy needs.

**What the world captures.** Per session: trajectories with pose, speed and a discrete action label;
zone entry and exit at each branch and the warehouse dock; pick and place events with begin/complete
timestamps; and handoff events recording approach distance and release delay when the robot passes a
crate to a human. Exported as CSV.

**Behaviors of value.** Route choice through a congested service pass; pick-target selection when a
near-expiry crate competes with a fresher one (a policy-quality signal, not just a path); approach
distance and pause duration when handing an object to a busy human; and clearance behavior around
moving people. **Handoff timing is the specific gap** — plenty of prior work treats humans as
obstacles to avoid, far less treats them as collaborators receiving an object, and a kitchen robot
cannot be safely tuned without those distributions.

**Scaling it.** Scenarios come from live Mise tickets, so task variety is generated rather than
hand-authored, and every run is directly comparable because the scene layout is known. Many runs
across varied congestion yields a distribution over strategies — including suboptimal ones, which is
what makes a learned policy robust rather than brittle.

**Consent, provenance, identifiers.** Collected only in simulation. We capture no real kitchens and no
identifiable people — a working kitchen is a private space full of staff who cannot meaningfully
consent frame by frame, which the rules prohibit capturing, so simulation removes the problem rather
than managing it. Records carry a per-session pseudonymous actor ID with no account linkage; no audio,
no video of people. Each session records scenario ticket, scene version and timestamp, so any
contributor's data can be located and deleted on request.

---

## External assets / datasets / AI tools used

- **Mise** — our own **pre-existing work** (AGI Summit 2026 winner). Reused here as the decision layer
  that emits the restock ticket. **The hackathon-built artifact is the simulation, the robot's
  execution of that ticket, and the telemetry pipeline.**
- **Unity 6** (6000.x LTS). All geometry is generated procedurally from primitives at runtime — no
  imported models, textures, or third-party assets.
- **VLGE / V-CTRL** — evaluated as the build target; we analyzed the supplied session recordings to
  design the telemetry schema. The shipped build is Unity.
- Mise's own stack, disclosed for completeness: Cotal (agent mesh), Runtype (MCP surface), InsForge
  (Postgres).
- **AI coding tools:** Claude (Claude Code) used for the simulation code, telemetry pipeline, and
  analysis scripts.

## Repo / notes
`https://github.com/abhijitbetigeri/Project-SCIM` — includes the telemetry schema analysis, the Mise
integration contract, and the extraction scripts.

## Known device requirements
Any modern desktop browser with WebGL 2. Keyboard and mouse. No headset, no install.

---

## The 3-minute demo script

**0:00 — the problem (20s).**
> "A restaurant franchise stocks out at one branch while another throws away surplus. There's no
> coordination layer. We solved the *decision* half at AGI Summit — our agents negotiate the transfer.
> But deciding isn't doing. Nobody has solved executing it in a cramped kitchen."

**0:20 — view 1, the branches (40s).** Press `1`.
> "Three branches. Shelf colour is stock state. Downtown is red — 36 kg short of tomatoes. Marina is
> green, 10 kg above par, expiring in two days. Mission is *also* short, which is why it can't donate.
> The agents transfer the near-expiry stock first, then buy only the net 26 kg. $53.30."

**1:00 — view 2, the warehouse (20s).** Press `2`.
> "The distributor. This is where the purchased 26 kg ships from — the supply chain as physical space,
> not rows in a database."

**1:20 — view 3, the robot (60s).** Press `3`.
> "Here's the part nobody has data for. The robot picks the near-expiry crate, carries it through the
> service pass — the tightest point in the room — and hands it to the cook. Watch the pause before it
> releases. That pause is the product."

**2:20 — the data (30s).** Point at the telemetry panel, press `T`.
> "Every run emits trajectories, zone transitions, pick and place events, and handoff timing with
> approach distance and release delay. That's an imitation-learning dataset for a restock robot.
> Agents decide, worlds do, robots learn."

**2:50 — close (10s).**
> "Supply chain has a brain. It doesn't have a body. This is where we grow one."

## Q&A — the three questions you will get

**"Is anything actually learned here?"**
> No, and we won't claim otherwise. The robot follows a scripted task. This is a data *generation* and
> demonstration environment — the pipeline from execution to robot-consumable trajectory is real and
> working end to end. Training a policy on it is the next step, not this one.

**"Why simulation instead of a real kitchen?"**
> Because we can't lawfully record a real one. Staff mid-shift can't consent frame by frame and a
> commercial kitchen is a private space — the rules prohibit exactly that capture. Simulation also
> makes congestion and near-misses safe and repeatable.

**"How is this Track 2 and not Track 1?"**
> The interaction that matters is between two actors — a robot handing a physical object to a human
> who isn't looking at it. Approach distance, standoff, release delay. That collaboration timing is
> the behavioral data, and it's what's missing from existing robot datasets.
