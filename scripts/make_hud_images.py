#!/usr/bin/env python3
"""
Generate the HUD panels for the Project-SCIM world.

V-CTRL's EventVolume actions can set visibility and colour but there is no
SetText — so on-screen text has to be pre-rendered as images and toggled with
SetVisible. This bakes each line of the restock loop into its own PNG.

    python3 scripts/make_hud_images.py -o build/hud

Upload the PNGs as assets in V-CTRL, place each as an ImageMedia entity, then
switch them with SetVisible per docs/BUILD-RUNBOOK.md.
"""
import argparse
import os

from PIL import Image, ImageDraw, ImageFont

W, H = 1400, 340          # 2:0.5-ish; readable as a floating billboard
PAD = 64

# Transparent background so the panel reads as an overlay rather than a poster.
CARD = (14, 16, 20, 225)
ACCENT_RED = (232, 74, 62)
ACCENT_AMBER = (232, 168, 62)
ACCENT_GREEN = (86, 196, 128)
WHITE = (245, 246, 248)
MUTED = (150, 158, 170)

PANELS = [
    # (filename, accent, kicker, headline, subline)
    ("01_objective", ACCENT_RED, "DOWNTOWN — SHORTAGE",
     "Short 36 kg Roma tomatoes",
     "Find the surplus at Marina. Nearest-expiry crate first."),
    ("02_carrying", ACCENT_AMBER, "CARRYING",
     "10 kg Roma tomatoes  ·  expires in 2 days",
     "Take it to the Downtown shelf."),
    ("03_delivered", ACCENT_AMBER, "TRANSFER COMPLETE",
     "10 kg delivered  ·  waste avoided",
     "Net still short 26 kg — supplier delivery at the dock."),
    ("04_dock", ACCENT_AMBER, "CARRYING — SUPPLIER DELIVERY",
     "26 kg Roma tomatoes  ·  Bay Foods Wholesale",
     "Take it to the Downtown shelf."),
    ("05_resolved", ACCENT_GREEN, "RESOLVED",
     "36 kg restocked  ·  $53.30",
     "10 kg transferred, 26 kg purchased. One approval."),
    ("06_marina", ACCENT_GREEN, "MARINA — SURPLUS",
     "34 kg on hand  ·  par 24 kg",
     "10 kg above par, 2 days to expiry. Move this first."),
]


def load_fonts():
    """Best-effort system fonts; PIL's default is a last resort."""
    candidates = [
        "/System/Library/Fonts/Supplemental/HelveticaNeue.ttc",
        "/System/Library/Fonts/Helvetica.ttc",
        "/System/Library/Fonts/SFNS.ttf",
        "/Library/Fonts/Arial.ttf",
    ]
    for path in candidates:
        if os.path.exists(path):
            try:
                return (ImageFont.truetype(path, 30),
                        ImageFont.truetype(path, 62),
                        ImageFont.truetype(path, 32))
            except OSError:
                continue
    d = ImageFont.load_default()
    return d, d, d


def rounded_card(draw):
    draw.rounded_rectangle([0, 0, W - 1, H - 1], radius=28, fill=CARD)


def render(name, accent, kicker, headline, subline, fonts, outdir):
    f_kick, f_head, f_sub = fonts
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    rounded_card(d)
    # Accent bar down the left edge — the only colour cue that survives at distance.
    d.rounded_rectangle([0, 0, 14, H - 1], radius=7, fill=accent)

    y = PAD - 8
    d.text((PAD, y), kicker, font=f_kick, fill=accent)
    y += 52
    d.text((PAD, y), headline, font=f_head, fill=WHITE)
    y += 84
    d.text((PAD, y), subline, font=f_sub, fill=MUTED)

    path = os.path.join(outdir, f"{name}.png")
    img.save(path)
    return path


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("-o", "--out", default="build/hud")
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)
    fonts = load_fonts()
    for spec in PANELS:
        print("wrote", render(*spec, fonts=fonts, outdir=args.out))


if __name__ == "__main__":
    main()
