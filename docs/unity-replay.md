# Unity replay — the closing demo beat

**Gate: only start this if the VLGE build is stable and the handoff works by ~16:00.** This is a
15-second clip for the demo video, not a second world. If the MVP is shaky, skip it entirely — the
handbook is explicit that there's no extra credit for unused features.

## What it proves

The rest of the project argues that human play produces robot training data. This closes the loop by
*showing* it: a recorded human restock run, replayed driving a robot in Unity. Problem → interaction
→ outcome, with the outcome being a robot doing the job.

It also puts you in the engine Track 2 nominally prefers, without risking the build that actually
scores.

## Why it's cheap: VLGE is Unity underneath

The session schemas give it away — `Camera.main` as a sensor mount, `prefabName: "SM_NewYork_Bed_01"`,
`entityType: "StaticMesh"`, `layerName`, xyzw quaternions, y-up coordinates. VLGE is a Unity
application.

**Consequence: no coordinate conversion.** Positions and quaternions from
[`scripts/extract_trajectory.py`](../scripts/extract_trajectory.py) feed straight into
`transform.position` and `transform.rotation`. This is the single biggest reason this stretch is
achievable in an hour rather than a day.

## The path (no ROS required)

From the [Unity Robotics Hub](https://github.com/Unity-Technologies/Unity-Robotics-Hub), three
packages exist: **URDF Importer** (load a robot description), **ROS TCP Connector**, and **ROS TCP
Endpoint**. Requires **Unity 2020.2+**.

**Skip ROS.** The hub's *Articulations Robot Demo* runs robot physics on Unity's built-in solver with
no ROS integration — that's the path here. ROS is only needed for two-way control, which a replay
doesn't do. (The Pick-and-Place tutorial is the fuller path, but it pulls in ROS; too much for today.)

Cheapest viable version, in order of increasing cost:

1. **A capsule or crate mesh** following the trajectory. Proves the data drives motion. ~10 minutes.
2. **A wheeled base** (any free mobile-robot asset) following it, with the crate parented on top at
   pick time. Reads unmistakably as a restock robot. ~30 minutes.
3. **URDF Importer + an articulated arm**. Only if everything else is done.

Option 2 is the sweet spot for the video.

## Generate the input

```bash
python3 scripts/extract_trajectory.py "VLGE_Sessions/VLGE Sessions - Hackathon/3D Map 1/Play Mode" \
    -o replay.csv --max-speed 8
```

Always pass `--max-speed` for a replay — unfiltered teleport spikes make the robot jump across the
map. Verified on the supplied sessions: derived max speed 31.1 m/s against a p99 of 2.67 m/s, and the
filter removes 14 frames of 41,085.

Put `replay.csv` in `Assets/StreamingAssets/`.

## Replay script

Drop this on the robot GameObject. It resamples by wall-clock time, so the 228 Hz capture plays back
smoothly regardless of Unity's frame rate.

```csharp
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class TrajectoryReplay : MonoBehaviour
{
    public string csvFile = "replay.csv";
    public float speedMultiplier = 1f;
    public bool loop = true;
    public bool useBodyPose = true;   // body pose = feet on floor; camera pose is eye height

    struct Frame { public float t; public Vector3 pos; public Quaternion rot; }

    readonly List<Frame> frames = new List<Frame>();
    float elapsed;

    void Start()
    {
        var path = Path.Combine(Application.streamingAssetsPath, csvFile);
        var lines = File.ReadAllLines(path);
        var header = new List<string>(lines[0].Split(','));

        // Look columns up by name — column order is not a stable contract.
        int cT = header.IndexOf("t_rel_s");
        string p = useBodyPose ? "body" : "cam";
        int cX = header.IndexOf(p + "_x"), cY = header.IndexOf(p + "_y"), cZ = header.IndexOf(p + "_z");
        int cQX = header.IndexOf(p + "_qx"), cQY = header.IndexOf(p + "_qy"),
            cQZ = header.IndexOf(p + "_qz"), cQW = header.IndexOf(p + "_qw");

        var inv = CultureInfo.InvariantCulture;
        for (int i = 1; i < lines.Length; i++)
        {
            // Naive split is safe for these columns; the only quoted field
            // (dm_latent_action_vec) sits after every column we read.
            var f = lines[i].Split(',');
            if (f.Length <= cQW || f[cX].Length == 0) continue;

            frames.Add(new Frame {
                t   = float.Parse(f[cT], inv),
                pos = new Vector3(float.Parse(f[cX], inv), float.Parse(f[cY], inv), float.Parse(f[cZ], inv)),
                rot = new Quaternion(float.Parse(f[cQX], inv), float.Parse(f[cQY], inv),
                                     float.Parse(f[cQZ], inv), float.Parse(f[cQW], inv))
            });
        }
        Debug.Log($"TrajectoryReplay: {frames.Count} frames, {frames[frames.Count - 1].t:F1}s");
    }

    void Update()
    {
        if (frames.Count < 2) return;

        elapsed += Time.deltaTime * speedMultiplier;
        float dur = frames[frames.Count - 1].t;
        if (elapsed > dur) { if (!loop) return; elapsed = 0f; }

        // Advance to the bracketing pair, then interpolate between them.
        int i = 0;
        while (i < frames.Count - 2 && frames[i + 1].t < elapsed) i++;

        var a = frames[i];
        var b = frames[i + 1];
        float span = b.t - a.t;
        float u = span > 0f ? (elapsed - a.t) / span : 0f;

        transform.position = Vector3.Lerp(a.pos, b.pos, u);
        transform.rotation = Quaternion.Slerp(a.rot, b.rot, u);
    }
}
```

Use `useBodyPose = true` for a ground robot — the `pp`/`rp` pose sits at floor level, while `pc`/`rc`
is eye height (~1.84 vs −0.08 in the sample sessions).

## What to say over the clip

> "This robot isn't scripted. It's replaying a trajectory a human recorded playing the restock task —
> the same file a policy would train on."

That sentence is the whole thesis in fifteen seconds.

## Honest caveat

The trajectory is a *human's* path, replayed. This is a data-provenance demo, not a trained policy —
no learning happens. Say so if a judge asks; claiming otherwise is the kind of thing that unravels in
Q&A. The credible claim is that the pipeline from human demonstration to robot-consumable trajectory
is real and working end to end, which it is.
