#!/usr/bin/env python3
"""
Extract a replayable trajectory from a VLGE session recording.

VLGE shards a session into 30-second JSON files. This concatenates the shards in
timestamp order and emits one flat CSV — the format a Unity replay script, a
heatmap renderer, or a behavior-cloning dataloader can all read directly.

    python3 scripts/extract_trajectory.py <session_dir> -o out.csv

<session_dir> is any directory containing *_improved.json files; it is searched
recursively, so pointing it at a "Play Mode" folder or a whole map both work.

Schema notes (see docs/telemetry.md):
  - timestamps are .NET ticks: unix_s = ticks/1e7 - 62135596800
  - characterSnapshots holds camera pose (pc/rc) and body pose (pp/rp)
  - frameSignalRecords holds the dm_* demonstration signals, and links back via
    sourceCharacterSnapshotIndex (an index *within its own shard*)
"""
import argparse
import csv
import json
import pathlib
import sys

TICKS_EPOCH = 62135596800  # seconds between 0001-01-01 and 1970-01-01
TICKS_PER_S = 1e7

# dm_* signals worth carrying into a replay/training row. Scalars only — the
# 11-dim latent action vector is emitted separately as a JSON string.
DM_SCALARS = [
    "dm_speed_xz_mps",
    "dm_vel_longitudinal_mps",
    "dm_vel_lateral_mps",
    "dm_slip_angle_deg",
    "dm_cam_yaw_rate_dps",
    "dm_body_yaw_rate_dps",
    "dm_jerk_mps3",
    "dm_path_curvature",
    "dm_turn_radius_m",
    "dm_action_locomotion",
    "dm_action_turn",
    "dm_action_token_id",
    "dm_gaze_object",
    "dm_gaze_hit_distance_m",
]

COLUMNS = (
    ["t_unix", "t_rel_s", "shard", "frame",
     "cam_x", "cam_y", "cam_z", "cam_qx", "cam_qy", "cam_qz", "cam_qw",
     "body_x", "body_y", "body_z", "body_qx", "body_qy", "body_qz", "body_qw",
     "floor_type"]
    + DM_SCALARS
    + ["dm_latent_action_vec"]
)


def ticks_to_unix(ticks):
    return ticks / TICKS_PER_S - TICKS_EPOCH


def vec(d, *keys):
    """Pull a vector/quaternion dict into a flat list, tolerating nulls."""
    if not isinstance(d, dict):
        return [""] * len(keys)
    return [d.get(k, "") for k in keys]


def load_shards(session_dir):
    files = sorted(pathlib.Path(session_dir).rglob("*_improved.json"))
    if not files:
        sys.exit(f"no *_improved.json files under {session_dir}")
    shards = []
    for f in files:
        try:
            with open(f) as fh:
                shards.append((f, json.load(fh)))
        except (json.JSONDecodeError, OSError) as e:
            print(f"  ! skipping {f.name}: {e}", file=sys.stderr)
    # Order by session start, not filename — filenames sort correctly today but
    # the recorded start time is the actual authority.
    shards.sort(key=lambda s: s[1].get("startDateTime", {}).get("ticks", 0))
    return shards


def extract(session_dir, max_speed=None):
    rows, t0, dropped = [], None, 0

    for shard_path, data in load_shards(session_dir):
        snaps = data.get("characterSnapshots") or []
        # Index the dm_* signals by the snapshot they describe. This mapping is
        # shard-local, which is why it is rebuilt per file rather than globally.
        signals = {}
        for rec in data.get("frameSignalRecords") or []:
            idx = rec.get("sourceCharacterSnapshotIndex")
            if idx is not None:
                signals[idx] = rec.get("signals", {})

        for i, snap in enumerate(snaps):
            ticks = (snap.get("timestamp") or {}).get("ticks")
            if ticks is None:
                continue
            t = ticks_to_unix(ticks)
            if t0 is None:
                t0 = t

            # dm_* signals are far sparser than positions (~247 signal records per
            # 5,864 snapshots), so most rows carry pose only and blank signals.
            sig = signals.get(i, {})
            latent = sig.get("dm_latent_action_vec")
            rows.append(
                [round(t, 4), round(t - t0, 4), shard_path.name, i]
                + vec(snap.get("pc"), "x", "y", "z")
                + vec(snap.get("rc"), "x", "y", "z", "w")
                + vec(snap.get("pp"), "x", "y", "z")
                + vec(snap.get("rp"), "x", "y", "z", "w")
                + [snap.get("floorType", "")]
                + [sig.get(k, "") for k in DM_SCALARS]
                + [json.dumps(latent) if latent else ""]
            )

    rows.sort(key=lambda r: r[0])

    # Teleport/respawn spikes (sessionMetrics reports max_velocity_mps ~71 in a
    # walking session). Speed must be derived from consecutive body positions --
    # dm_speed_xz_mps is blank on most frames and would let every spike through.
    if max_speed is not None:
        kept, prev = [], None
        bx, bz, bt = COLUMNS.index("body_x"), COLUMNS.index("body_z"), 0
        for r in rows:
            if prev is not None:
                dt = r[bt] - prev[bt]
                if dt > 0 and all(isinstance(v, (int, float)) for v in
                                  (r[bx], r[bz], prev[bx], prev[bz])):
                    dist = ((r[bx] - prev[bx]) ** 2 + (r[bz] - prev[bz]) ** 2) ** 0.5
                    if dist / dt > max_speed:
                        dropped += 1
                        continue  # keep prev as the anchor; the spike is the outlier
            kept.append(r)
            prev = r
        rows = kept

    return rows, dropped


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("session_dir")
    ap.add_argument("-o", "--out", default="trajectory.csv")
    ap.add_argument("--max-speed", type=float, default=None,
                    help="drop frames above this m/s (teleport spikes); off by default")
    args = ap.parse_args()

    rows, dropped = extract(args.session_dir, args.max_speed)
    if not rows:
        sys.exit("no character snapshots found")

    with open(args.out, "w", newline="") as fh:
        w = csv.writer(fh)
        w.writerow(COLUMNS)
        w.writerows(rows)

    span = rows[-1][1]
    print(f"{len(rows):,} frames  ·  {span:.1f}s  ·  {len(rows)/span:.0f} Hz  ->  {args.out}")
    if dropped:
        print(f"dropped {dropped:,} frames above {args.max_speed} m/s")


if __name__ == "__main__":
    main()
