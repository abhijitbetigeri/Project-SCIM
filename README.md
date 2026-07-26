# Project-SCIM — Supply Chain Intelligence Management

*A spatial behavior lab for restaurant logistics — the physical-AI extension of [Mise](https://github.com/abhijitbetigeri/SC-Intelligence).*

**Open World Hackathon · 25 Jul 2026 · San Francisco · Powered by VLGE** · Track 2 (VLGE Together — Social + Behavior)

### ▶ Live

| | |
|---|---|
| **Site + playable simulation** | **https://abhijitbetigeri.github.io/Project-SCIM/** |
| Simulation direct | https://abhijitbetigeri.github.io/Project-SCIM/sim/ |
| Demo video (35s) | https://abhijitbetigeri.github.io/Project-SCIM/scim-robot-demo.mov |
| Mise — the decision layer | https://k3trn3a2.insforge.site |

Runs in any desktop browser, no install. It plays itself in ~45 seconds.
**`1`** branches · **`2`** warehouse · **`3`** follow the robot · right-drag look · WASD fly ·
**`T`** export telemetry · **`R`** restart.

> *Mise (software) decides **what** should move. Project-SCIM is the world where it **gets done** —
> and every human demonstration becomes training data for a restaurant-logistics robot.*

**Reimagining supply chain with world models and physical AI.** The long arc: the branches that
Mise rebalances are stocked by *robots*, not runners — and a robot can only learn that job inside a
world model of the space it works in. Project-SCIM is that world model in miniature: a simulated
back-of-house where stock rebalancing is performed physically, human-robot collaboration timing is
measured rather than assumed, and every session emits the demonstration data a real restock robot
would train on. Supply chain stops being rows in a database and becomes a spatial, embodied problem.

Project-SCIM is the **embodied / physical-AI extension** of [Mise](https://github.com/abhijitbetigeri/SC-Intelligence)
— our multi-agent restaurant supply chain. That system proved agents can
*decide* the optimal restock (transfer surplus between branches, buy only the net shortage, one
approval). This project tackles the unsolved half: **executing that decision in real 3D space**, and
generating the **spatial + behavioral demonstration data** an embodied restock/logistics robot needs
to learn it.

## The core interaction (one sentence)

A shortage lights up on the Downtown shelf; a robot drives to the branch holding near-expiry surplus,
picks that crate specifically, carries it back through the service pass, and hands it to the cook —
while the world logs every path, pick, and handoff as demonstration data for a restaurant-logistics
robot.

## The loop, and why it matters for physical AI

**Coordination (decided) → embodied execution (done) → behavioral dataset (learned).** The agents
emit a restock ticket; a robot executes it physically in a back-of-house; the world captures
trajectories, pick/place events, handoff timing and congestion — the imitation-learning dataset for a
back-of-house restock robot and for human-robot collaboration in a kitchen.

The scenario is not illustrative. Downtown holds 4.0 kg against a par of 40 (**36 short**); Marina
holds 34.0 against a par of 24 (**10 surplus, 2 days to expiry**); Mission holds 16.0 but is *itself*
below par, so it cannot donate. Transfer 10, buy the net 26 at $2.05/kg — **$53.30**.

## Where to start

- **The shipped simulation (setup + how it works):** [unity/README.md](unity/README.md) ·
  source in [unity/Assets/Scripts/](unity/Assets/Scripts/)
- **Landing page source:** [web/index.html](web/index.html)
- **The plan (build from this):** [docs/PLAN.md](docs/PLAN.md)
- **Track-2 data-collection plan (submission field, paste-ready):** [docs/data-collection-plan.md](docs/data-collection-plan.md)
- **What VLGE already records (telemetry schema analysis):** [docs/telemetry.md](docs/telemetry.md)
- **V-CTRL capabilities (triggers, actions, what's buildable):** [docs/vctrl-capabilities.md](docs/vctrl-capabilities.md)
- **Supplied maps, assets, splat share IDs:** [docs/vlge-assets-and-maps.md](docs/vlge-assets-and-maps.md)
- **Unity robot-replay demo beat (stretch):** [docs/unity-replay.md](docs/unity-replay.md)
- **Mise integration contract (task-ticket shape + real numbers):** [mise/INTEGRATION.md](mise/INTEGRATION.md)
- **Vendored Mise snapshot:** [mise/](mise/) — full copy of the Mise system, 2026-07-25
- **Hackathon brief + rules + judging + links:** [docs/hackathon-brief.md](docs/hackathon-brief.md)
- **Build in V-CTRL:** https://vctrl.vlge.com · **VLGE guide:** https://world.vlge.com/vlge-guide
- **Supplied maps / shared resources:** https://drive.google.com/drive/folders/1UG9KbSpQp-ntFher2geb4RfO0hTfezuV

## Status

**Shipped.** Built in **Unity 6**, hosted at the link above, runs unattended in a browser.

We started in VLGE and moved to Unity when V-CTRL's project export turned out to be broken. The VLGE
work was not wasted: analysing the supplied session recordings is what produced the telemetry schema
this simulation implements — see [docs/telemetry.md](docs/telemetry.md) and
[docs/vctrl-capabilities.md](docs/vctrl-capabilities.md). Those docs describe the VLGE investigation
and are kept as the record of it, not as a description of what shipped.

The simulation records the six channels that were **empty in every supplied VLGE session** —
`zoneIdentifierAccesses`, `interactionEventRecords`, `gamePlayEventRecords`, handoff timing, chat and
audio. Filling those is the Track-2 argument.

**Honest limits.** Nothing is learned here: the robot follows a scripted task. What works end to end
is the pipeline from execution to robot-consumable trajectory data. Avoidance is whisker raycasts,
not a planner. Geometry is primitives — it reads as a diagram of a restaurant, not a photoreal one.
