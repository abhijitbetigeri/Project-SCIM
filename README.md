# Project-SCIM — Supply Chain Intelligence Management

*A spatial behavior lab for restaurant logistics — the physical-AI extension of [Mise](https://github.com/abhijitbetigeri/SC-Intelligence).*

**Open World Hackathon · 25 Jul 2026 · San Francisco · Powered by VLGE** · Track 2 (VLGE Together — Social + Behavior)

> *Mise (software) decides **what** should move. Project-SCIM is the world where it **gets done** —
> and every human demonstration becomes training data for a restaurant-logistics robot.*

Project-SCIM is the **embodied / physical-AI extension** of [Mise](https://github.com/abhijitbetigeri/SC-Intelligence)
— our AGI-Summit-2026-winning multi-agent restaurant supply chain. That system proved agents can
*decide* the optimal restock (transfer surplus between branches, buy only the net shortage, one
approval). This project tackles the unsolved half: **executing that decision in real 3D space**, and
generating the **spatial + behavioral demonstration data** an embodied restock/logistics robot needs
to learn it.

## The core interaction (one sentence)

A shortage lights up on a shelf; a player must find the surplus, carry it along the fastest route to
the branch that needs it, and hand it off — while the world logs every path, pick, and handoff as
demonstration data for a restaurant-logistics robot.

## The loop, and why it matters for physical AI

**Coordination (decided) → embodied execution (done) → behavioral dataset (learned).** Humans play
out the restock/handoff task in a believable back-of-house world; the world captures trajectories,
pick/place events, handoffs, and congestion — the imitation-learning dataset for a back-of-house
restock robot and for human-robot collaboration in a kitchen.

## Where to start

- **The plan (build from this):** [docs/PLAN.md](docs/PLAN.md)
- **Hackathon brief + rules + judging + links:** [docs/hackathon-brief.md](docs/hackathon-brief.md)
- **Build in V-CTRL:** https://vctrl.vlge.com · **VLGE guide:** https://world.vlge.com/vlge-guide
- **Supplied maps / shared resources:** https://drive.google.com/drive/folders/1UG9KbSpQp-ntFher2geb4RfO0hTfezuV

## Status

Planning → build. Track 2, VLGE (V-CTRL / Unity if needed). MVP first: a single-player
pick→carry→place restock loop that runs in Play Mode from a shareable link; then a behavioral
telemetry overlay, multiplayer handoff, and a live tie-in to the Mise agents.
