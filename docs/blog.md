# Supply chain has a brain. Now it needs a body.

*Why we built a world where robots learn to run a restaurant's back-of-house — and why the data it
generates matters more than the simulation itself.*

---

## The same tomato, twice

Walk into a franchise's Downtown location on a Friday and you'll find them out of tomatoes. Walk into
their Marina location twenty minutes later and you'll find a case going soft in the walk-in.

Same company. Same product. Same week. One is turning away orders; the other is filling a bin.

This isn't negligence. It's structural. Every branch forecasts its own demand, orders from its own
supplier, and has no visibility into what the branch across town is holding. The obvious fix — move
stock between locations before buying new — requires a coordination layer that simply doesn't exist in
most operations. So it never happens.

## What we solved first

**Mise** is that coordination layer. Branch and supplier agents negotiate peer-to-peer: when a
location dips below its reorder point, the network resolves it internally before anyone raises a
purchase order.

Here's a live case from the system:

| Branch | On hand | Par | Expiry | Position |
|---|---|---|---|---|
| **Downtown** | 4.0 kg | 40 | 4 days | **36 kg short** — below reorder |
| **Marina** | 34.0 kg | 24 | **2 days** | 10 kg surplus, near expiry |
| Mission | 16.0 kg | 20 | 5 days | also short — cannot donate |

The naive answer is to buy 36 kg. The right answer is to move Marina's near-expiry stock *first* —
because in two days it's waste no matter what — then purchase only the net 26 kg.

**Result: $53.30 spent instead of a full order, 10 kg of waste avoided, one approval instead of
three.**

Multiply that across products, locations and weeks and you have the margin most operators are looking
for. This part works today.

## The half nobody has solved

Then we watched what happens after the decision.

A person walks into a kitchen mid-service and physically moves 10 kg of tomatoes. Through gaps under
a metre wide. Around a crew that's moving fast and not looking. Carrying an object whose *identity
matters* — the near-expiry crate is not interchangeable with the fresh one beside it, and picking the
wrong one silently undoes the entire optimisation.

Every operator we describe this to says the same thing: *that's the part that doesn't happen
reliably.*

It's also precisely the work the robotics industry is racing toward — and precisely where it has the
least data. Manipulation datasets are overwhelmingly tabletop. Warehouse datasets assume wide aisles,
structured racking, and no humans underfoot. A restaurant back-of-house is the opposite of all three.

You cannot train a policy on data that doesn't exist. So we built the place where it does.

## Project-SCIM

**Project-SCIM is a world model for restaurant logistics** — a simulated back-of-house where the
restock decision is carried out physically, and every execution is captured as training data.

Three branches. A distributor warehouse. A robot that receives the ticket Mise generates and performs
it: drives to the branch holding surplus, selects the near-expiry crate specifically, carries it back
through the service pass, hands it to the cook, places it on the shelf. Then collects the purchase
order from the distributor and closes the shortage.

Shelf colour tells you the state of the business at a glance — red below reorder, amber below par,
green at par. The whole cycle resolves in about 45 seconds, unattended.

**[▶ Watch it run](https://abhijitbetigeri.github.io/Project-SCIM/)** — opens in a browser, nothing
to install.

## The product is the data

The simulation is the visible part. The asset is what it emits.

Every run produces a structured record of how the task was actually performed:

- **Routes** — which path through a congested space, and how efficient it was
- **Selection** — which crate was chosen when a near-expiry and a fresh one competed. This is a
  *policy quality* signal, not just a trajectory
- **Manipulation** — pick and place events with start and completion timing
- **Handoff** — approach distance and release delay when passing an object to a person

That last category is the one we think is genuinely missing from the field.

There is substantial work on robots treating humans as obstacles to avoid. There is very little on
robots treating humans as **collaborators receiving an object** — a cook who isn't looking, in a space
too tight to wait politely in. How close does the robot come? How long does it hold before releasing?
When does it retry? These are distributions, not constants, and you only obtain them by observing the
handoff repeatedly under realistic pressure.

A kitchen robot cannot be safely tuned without them. Nobody is collecting them.

## Why simulation is the right substrate

This isn't a stopgap while we wait for real-world capture. It's the better instrument, for three
reasons.

**Consent.** You cannot lawfully record a working kitchen. Staff mid-shift cannot meaningfully
consent frame by frame, and a commercial kitchen is a private space. Simulation doesn't manage that
problem — it removes it. No faces, no premises, no bystanders.

**Repeatability.** The same scenario can be run hundreds of times with controlled variation in
quantity, distance and crowding. Real operations give you one uncontrolled sample per shift.

**Safety.** Congestion, near-misses and failed handoffs are the most valuable behaviours in the
dataset and the ones you least want to stage with a person carrying 20 kg.

And because scenarios originate from live agent tickets rather than hand-authored scripts, task
variety is *generated*. The system writes its own curriculum.

## The flywheel

This is why the two halves belong together.

**Agents decide → the world executes → the execution becomes data → the data trains a policy → the
policy executes the next decision.**

Each turn makes the next one better. The coordination layer produces an endless supply of realistic,
economically meaningful tasks. The embodied layer turns each one into demonstration data. Nothing
here depends on scraping someone else's dataset or waiting for a hardware generation to arrive.

## Where this goes

Near term, the world is a **development and evaluation environment**: a place to pretrain
pick-carry-place and human-aware navigation policies, and to test them against congestion patterns
before anything touches a real kitchen.

Beyond that, the same instrumentation becomes an operational tool. If you can measure how long a
transfer *should* take through a given floor plan, you can tell an operator that their pass is the
bottleneck — and what moving one shelf would recover.

The customers are the people already at this intersection: franchise operations teams carrying both
waste and stockout costs, and robotics teams that need back-of-house behaviour data and currently have
none.

## Honest about the stage

We'll say plainly what this is not. No policy has been trained yet — the robot follows a scripted
task. What is real and working end to end is the pipeline from decision, through embodied execution,
to robot-consumable trajectory data. That's the hard architectural problem, and it's solved.

Training on it is the next step. We'd rather state that than let anyone discover it later.

---

Restaurant supply chain has spent a decade becoming smarter software. It is still, at the last metre,
a person carrying a box through a doorway that's too narrow.

**Supply chain has a brain. Now it needs a body.**

**[See it work →](https://abhijitbetigeri.github.io/Project-SCIM/)**
