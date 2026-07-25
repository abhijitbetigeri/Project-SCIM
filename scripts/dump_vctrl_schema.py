#!/usr/bin/env python3
"""
Dump V-CTRL's inspector schema out of VLGE session recordings.

Edit Mode recordings embed `entitySnapshot.fields` for every object the builder
touched — the full inspector surface of that prefab. Aggregating those across a
session set reverse-engineers what the editor can actually do, without opening it.

    python3 scripts/dump_vctrl_schema.py "VLGE_Sessions/VLGE Sessions - Hackathon"
    python3 scripts/dump_vctrl_schema.py <dir> --prefab EventVolume

See docs/vctrl-capabilities.md for the findings.
"""
import argparse
import collections
import glob
import json
import os
import sys

RECORD_KEYS = [
    "placedObjectRecords",
    "mapEnvironmentObjectRecords",
    "modifiedEntityFieldRecords",
    "modifiedObjectRecords",
]

# Fields every prefab carries; noise when you're looking for capabilities.
COMMON = {"UUID", "DisplayName", "PrefabName", "Preview", "Position", "Rotation",
          "Scale", "ShouldBeAffectedByPP", "AnimationToPlay", "AnimationLoop",
          "AnimationStartingCondition"}


def collect(root):
    fields = collections.defaultdict(dict)      # prefab -> {field: type}
    meta = {}                                   # prefab -> (category, entityType)
    counts = collections.Counter()

    pattern = os.path.join(root, "**", "*.json")
    for path in glob.glob(pattern, recursive=True):
        try:
            with open(path) as fh:
                data = json.load(fh)
        except (json.JSONDecodeError, OSError):
            continue
        for key in RECORD_KEYS:
            for rec in data.get(key) or []:
                snap = rec.get("entitySnapshot") or {}
                prefab = snap.get("prefabName")
                if not prefab:
                    continue
                counts[prefab] += 1
                meta[prefab] = (snap.get("assetCategory", ""), snap.get("entityType", ""))
                for f in snap.get("fields") or []:
                    fields[prefab][f.get("identifier")] = f.get("valueType")
    return fields, meta, counts


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("sessions_dir")
    ap.add_argument("--prefab", help="show only this prefab")
    ap.add_argument("--all-fields", action="store_true",
                    help="include common transform/animation fields")
    args = ap.parse_args()

    fields, meta, counts = collect(args.sessions_dir)
    if not fields:
        sys.exit(f"no entity snapshots found under {args.sessions_dir}")

    # Functional assets first — they're the ones with behavior.
    order = sorted(fields, key=lambda p: (meta[p][0] != "FunctionalAsset", -counts[p]))

    for prefab in order:
        if args.prefab and prefab != args.prefab:
            continue
        category, etype = meta[prefab]
        print(f"\n=== {prefab}  [{category or 'uncategorized'} / {etype}]  "
              f"{counts[prefab]} records ===")
        for ident, vtype in fields[prefab].items():
            if not args.all_fields and ident in COMMON:
                continue
            # Lists are the event hooks — the interesting surface.
            mark = "  <-- event hook" if vtype == "List" else ""
            print(f"  {ident}  ({vtype}){mark}")

    print(f"\n{len(fields)} distinct prefabs. "
          f"Functional: {[p for p in fields if meta[p][0] == 'FunctionalAsset']}")


if __name__ == "__main__":
    main()
