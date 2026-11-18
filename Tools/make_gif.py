"""Turn recorded clip frames (Temp/capture/<clip>/f_0000.jpg ...) into one looping animated GIF.

    python Tools/make_gif.py out.gif clipA:start:count clipB:start:count ... [--fps 15] [--width 640]

Frames are recorded at 30 fps; every second frame is kept for a 15 fps GIF. A shared adaptive palette keeps the
pixel art crisp and the file small.
"""
import os
import sys
from PIL import Image

CAP = os.path.join(os.path.dirname(__file__), "..", "Temp", "capture")


def main():
    args = sys.argv[1:]
    fps, width = 15, 640
    if "--fps" in args:
        i = args.index("--fps"); fps = int(args[i + 1]); del args[i:i + 2]
    if "--width" in args:
        i = args.index("--width"); width = int(args[i + 1]); del args[i:i + 2]
    out, specs = args[0], args[1:]
    step = max(1, round(30 / fps))
    frames = []
    for spec in specs:
        name, start, count = spec.split(":")
        for f in range(int(start), int(start) + int(count), step):
            path = os.path.join(CAP, name, f"f_{f:04d}.jpg")
            if not os.path.exists(path):
                break
            im = Image.open(path).convert("RGB")
            if im.width != width:
                im = im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)
            frames.append(im)
    if not frames:
        sys.exit("no frames found")
    # one palette for the whole animation, built from a sample of frames
    sample = Image.new("RGB", (width, frames[0].height * 4))
    for k in range(4):
        sample.paste(frames[k * (len(frames) - 1) // 3], (0, k * frames[0].height))
    palette = sample.quantize(colors=255, method=Image.Quantize.MEDIANCUT)
    quant = [im.quantize(palette=palette, dither=Image.Dither.NONE) for im in frames]
    quant[0].save(out, save_all=True, append_images=quant[1:], duration=round(1000 / fps), loop=0, optimize=True, disposal=1)
    print(f"{out}: {len(quant)} frames, {os.path.getsize(out) / 1048576:.1f} MB")


if __name__ == "__main__":
    main()
