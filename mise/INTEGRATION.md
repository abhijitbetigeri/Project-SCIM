# Mise → Project-SCIM integration surface

**What this directory is.** A verbatim copy of all 90 git-tracked files from the Mise repo
(`~/projects/agisummit`, GitHub `SC-Intelligence`) as of 2026-07-25, vendored so the world build can
read the real contracts without a second checkout. It is a **snapshot, not a submodule** — nothing
here is wired into a build, and edits here do not flow back to Mise.

Everything below was read out of these files, not restated from the plan.

## The one thing that matters: the task ticket

Mise's agent layer emits **records**. Two record types are physical work, and those are the ones the
world consumes. Shapes verified in [`provision/server/fixtures/mise-records.json`](provision/server/fixtures/mise-records.json):

```jsonc
// type: "transfer"  → a CARRY task (branch → branch). The Track-2 social/handoff behavior.
{ "type": "transfer", "name": "t-tomato-marina-downtown",
  "metadata": { "sku": "TOM-ROMA", "product": "Roma tomatoes", "unit": "kg",
                "from_branch": "marina", "to_branch": "downtown", "qty": 10,
                "status": "proposed",          // proposed|approved|in_transit|done|rejected
                "reason": "Marina holds surplus above par with 2 days to expiry; Downtown is
                           below reorder point. Nearest branch, nearest-expiry stock first." } }

// type: "po"        → a DOCK RECEIVE task (supplier → branch). The second half of the loop.
{ "type": "po", "name": "po-tomato-downtown",
  "metadata": { "sku": "TOM-ROMA", "to_branch": "downtown", "qty": 26,
                "supplier": "Bay Foods Wholesale", "unit_price": 2.05, "total": 53.3,
                "eta_days": 2, "status": "pending" } }
```

**Mapping to the world:**

| Mise record | Project-SCIM object | Player action |
|---|---|---|
| `transfer.from_branch` | Marina surplus shelf (spawns the crate) | pick |
| `transfer.to_branch` | Downtown shortage shelf (red glow) | carry → place |
| `transfer.qty` | crate label — `10 kg` | HUD readout on resolve |
| `transfer.reason` | the objective text / "why this crate" hint | rewards picking the near-expiry crate |
| `po.qty` + `po.total` | dock delivery, `26 kg`, `$53.30` | place → resolved |
| `status` transitions | task state machine | `proposed → in_transit` on pick, `done` on place |

`status` is the natural telemetry hook: the timestamps on those transitions *are* the pick/place
event log, and the same vocabulary is already in Postgres (`rebalance_transfers.status`, in
[`db/schema.sql:121`](db/schema.sql)).

## Ground truth for the scenario (don't re-derive these)

From the `branch-state` fixtures — the PLAN.md numbers are all consistent with them:

| Branch | Roma tomatoes on hand | par | reorder | earliest expiry | daily burn |
|---|---|---|---|---|---|
| **Downtown** | 4.0 kg | 40.0 | 12.0 | 4 d | 14.16 kg/d |
| **Marina** | 34.0 kg | 24.0 | 8.0 | **2 d** | 10.1 kg/d |
| Mission | 16.0 kg | 20.0 | 6.0 | 5 d | 8.1 kg/d |

Downtown is **36 kg short** of par (40 − 4). Marina is **10 kg over** par (34 − 24) and its stock
expires in 2 days — hence transfer 10, buy the net 26 at $2.05/kg = **$53.30**. Mission is *also*
below par, which is why it can't be the donor; that detail is worth a sentence to judges because it
shows the decision was non-trivial.

Branch UUIDs (from [`cotal.yaml`](cotal.yaml), needed only if you write to Postgres):
Downtown `b0000000-…-00000000000a` · Marina `…b` · Mission `…c`.

Franchise: **Trattoria Verde**, San Francisco. Product `TOM-ROMA`, shelf life 6 days.
Real lat/lon per branch are in the catalog fixture if you want map-accurate zone placement.

## Two integration modes

The pattern is already built into [`provision/server/mise.js`](provision/server/mise.js) — reuse it
rather than inventing one:

- **Fixture mode** (`MISE_RUNTYPE_TOKEN` unset) — reads the bundled JSON. Renders offline, always.
- **Live mode** (`MISE_RUNTYPE_TOKEN` set) — pulls records from the Runtype API.

**This is the demo-safety pattern for the hackathon.** Build the world against the fixture shape;
if the live agent is reachable at demo time, flip the token. If the network dies at 18:00, the world
still plays. Note `mise.js` is deliberately **read-only** — it renders what agents decided and never
writes. The world should follow the same rule: consume tickets, emit telemetry, never mutate Mise state.

MCP surface (from `cotal.yaml`) — three capabilities: `weekly_forecast`, `inventory_admin`,
`rebalance_and_procure`. The last one is the showpiece that emits `transfer/*` and `po/*`:

```
claude mcp add --transport http mise \
  https://api.runtype.com/v1/products/prod_01kxvr3fcneskr35jfxqhekaj0/surfaces/surf_01kxvzxg98fqb8x1ngbe2a38q4/mcp \
  --header "Authorization: Bearer $MISE_MCP_KEY"
```

Shared state (Postgres, InsForge): `https://k3trn3a2.us-east.insforge.app` —
`inventory`, `forecasts`, `rebalance_transfers`, `rfqs`, `bids`, `purchase_orders`.

## What to disclose on the submission form

External assets / prior work: **Mise is pre-existing work** (AGI Summit 2026), reused here as the
decision layer. The hackathon-built artifact is the world, the embodiment of the ticket, and the
telemetry. Say this plainly — the handbook judges "work built or meaningfully integrated during the
hackathon", and the integration is the meaningful part. Also disclose: VLGE/V-CTRL, any supplied map
or template, Cotal, Runtype, InsForge, and AI coding tools.
