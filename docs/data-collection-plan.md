# Future data-collection plan (Track 2 submission field)

Two versions below: a short one to paste into the form, and the full reasoning to draw on in Q&A.
Every schema name is real — read from the supplied VLGE sessions, see [telemetry.md](telemetry.md).

---

## Short version — paste into the form

**Physical-AI use case.** A back-of-house restock and inter-branch logistics robot for restaurants.
Our agent system Mise already decides *what* stock should move between branches;
what does not exist is a policy that can *execute* that decision in a cramped working kitchen.
Project-SCIM generates the demonstration data that policy needs.

**What the world captures.** VLGE already records a demonstration-grade stream: body and camera pose
at ~195 Hz, discretized action tokens (`dm_action_token_id`) and an 11-dimensional latent action
vector, a 240-ray simulated depth sensor (`HumanDepthViewV0`, 90°×60°, 40 m), gaze raycasts resolved
to named objects, and 32 derived session metrics including path straightness and spatial entropy. Our
task adds the channels a walkthrough leaves empty: crate pick/carry/place interactions, zone
entry/exit at the surplus, shortage and dock locations, task-resolution gameplay events, and
runner↔cook coordination during the handoff.

**Behaviors of value.** Route choice under congestion; pick-target selection when a near-expiry crate
competes with a fresher one (a policy-quality signal, not just a path); approach distance and pause
duration when handing an object to another actor; and avoidance behavior around moving people. The
handoff timing distribution is the specific quantity we think is missing from existing datasets and
that a collaborative kitchen robot cannot be safely tuned without.

**Scaling it.** Each session is already a self-contained, machine-readable record including the scene
object layout, so sessions are directly comparable across players. Many players running the same
seeded scenario yields a distribution over strategies rather than one expert trace — which is what
imitation learning actually needs. Scenarios come from live Mise tickets, so task variety is
generated rather than hand-authored.

**Consent, provenance, identifiers.** Data is collected only in simulation — we capture no real
kitchens and no identifiable people, which is why we chose a built world over a site capture.
Collection would be opt-in per session with a visible recording indicator, and players would be told
what is captured before they play. Records carry a per-session pseudonymous ID with no account
linkage; there is no audio, no video of players, and no free-text beyond in-task coordination.
Provenance is preserved per session (map version, scenario ticket, engine build, timestamp) so any
contributor's data can be located and deleted on request. Any release would be scenario-level
aggregates plus trajectory files under a stated licence.

---

## Full reasoning

### Why this data doesn't exist yet

Robot learning datasets for manipulation are mostly tabletop and mostly single-agent. Warehouse
logistics datasets exist but describe wide aisles, structured racking, and no humans underfoot.
A restaurant back-of-house is the opposite: sub-metre clearances, a moving crew, time pressure,
and objects whose *identity matters* (the near-expiry crate is not interchangeable with the fresh one).

The specific gap is **collaboration timing**. Plenty of work covers a robot navigating around humans
as obstacles. Much less covers a robot handing a physical object to a busy human who is not looking
at it — approach speed, standoff distance, when to release, how long to wait before retrying. Those
are distributions you can only get by watching many humans do it many times.

### What the task generates, channel by channel

| Behavior | Channel | Robot-learning use |
|---|---|---|
| Route through the service line | `characterSnapshots`, `frameSignalRecords` | navigation policy, human-aware path cost |
| What the player looked at before choosing | `lookAtSnapshots` | attention prior; proves deliberation |
| Near-expiry vs fresh crate choice | `interactionSnapshots` | policy-quality label, not just a trajectory |
| Pick / carry / place | `interactionEventRecords` | manipulation segmentation |
| Marina / Downtown / Dock entry + exit | `zoneIdentifierAccesses` | task phase boundaries |
| Shortage raised, transfer resolved | `gamePlayEventRecords` | reward / success labelling |
| Runner ↔ cook coordination | `chatMessages`, handoff events | collaboration timing |
| Clearance around moving actors | `spatialSensorFrames` | the observation space itself |

The six channels a walkthrough leaves empty are exactly the top of that list. That is the whole
design argument: the recorder is proven, the payload is what's missing.

### Why simulation is the right call, not a compromise

The hackathon rules prohibit capturing private spaces, identifiable people, or behavioral data
without permission. A real working kitchen is all three at once — staff mid-shift cannot meaningfully
consent frame by frame, and a commercial kitchen is a private space.

Simulation removes the problem rather than managing it: no faces, no premises, no bystanders. It is
also repeatable (same scenario, many players), cheap (no cameras, no downtime), and safe (congestion
and near-misses can be studied without anyone carrying 20 kg into a moving colleague). The
sim-to-real gap is real and we don't wave it away — see limitations.

### From one session to a dataset

1. **Seeded scenarios.** Mise emits the ticket, so every player runs a *comparable* task with
   controlled variation in quantity, distance and time pressure.
2. **Scene ground truth.** `placedObjectRecords` and `mapEnvironmentObjectRecords` capture every
   object's pose and prefab, so trajectories are interpretable against a known layout — sessions
   remain comparable even if the map changes.
3. **Many players, not one expert.** The valuable artifact is the *distribution* — including
   suboptimal routes, which is what makes a policy robust rather than brittle.
4. **Adversarial congestion.** A second player working the line generates the dynamic obstacles that
   a static world cannot.

### Honest limitations

- **Sim-to-real.** Simulated depth is clean; real depth is noisy. These trajectories pretrain and
  shape a policy — they do not deploy one.
- **No force or grasp data.** Mass, friction and grasp stability are absent. This dataset addresses
  navigation, target selection and collaboration timing, not dexterous manipulation.
- **Keyboard control is not embodiment.** Human motion through a keyboard has different dynamics than
  human walking. `dm_latent_action_vec` captures the intent structure better than raw velocity does.
- **Small n.** A hackathon produces a proof of pipeline, not a dataset. The claim is that the
  collection mechanism works end to end — verifiable, since
  [`scripts/extract_trajectory.py`](../scripts/extract_trajectory.py) already turns raw sessions into
  training-ready CSV.
