from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "src" / "QuickPlay.WinUI" / "Assets"
SCALE = 4
SIZE = 1024


def point(value: int) -> int:
    return value * SCALE


canvas = Image.new("RGBA", (point(SIZE), point(SIZE)), (0, 0, 0, 0))
draw = ImageDraw.Draw(canvas)

draw.rounded_rectangle(
    (point(40), point(40), point(984), point(984)),
    radius=point(224),
    fill="#171717",
)
draw.ellipse(
    (point(198), point(184), point(786), point(772)),
    outline="#079cf5",
    width=point(88),
)
draw.line(
    [(point(690), point(690)), (point(826), point(826))],
    fill="#0078d4",
    width=point(88),
)

waveform = [
    (230, 506), (302, 506), (344, 424), (398, 598), (455, 353),
    (513, 655), (571, 426), (624, 584), (669, 506), (753, 506),
]
draw.line(
    [(point(x), point(y)) for x, y in waveform],
    fill="#20b7ff",
    width=point(30),
    joint="curve",
)

triangle = [(433, 368), (433, 592), (620, 480)]
draw.polygon(
    [(point(x), point(y)) for x, y in triangle],
    fill="#ffffff",
    outline="#171717",
    width=point(26),
)

icon = canvas.resize((SIZE, SIZE), Image.Resampling.LANCZOS)
icon.resize((512, 512), Image.Resampling.LANCZOS).save(
    ASSETS / "QuickPlay.png",
    optimize=True,
)
icon.save(
    ASSETS / "QuickPlay.ico",
    format="ICO",
    sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (128, 128), (256, 256)],
)
