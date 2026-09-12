# -*- coding: utf-8 -*-
"""1) 游戏自带选人背景场景结构  2) Typhon 背景图尺寸  3) 我方 bg 图构图统计。"""
import struct

# ---- 游戏 pck 里找选人背景场景 ----
PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"
f = open(PCK, "rb")
head = f.read(40)
file_base, dir_off = struct.unpack_from("<QQ", head, 24)
f.seek(dir_off)
n = struct.unpack("<I", f.read(4))[0]
entries = {}
for _ in range(n):
    sl = struct.unpack("<I", f.read(4))[0]
    path = f.read(sl).decode("utf8", "replace").rstrip("\x00")
    ofs, size = struct.unpack("<QQ", f.read(16))
    f.read(16)
    f.read(4)
    entries[path] = (ofs, size)
f.close()

hits = [p for p in entries if "select" in p.lower() and ("bg" in p.lower() or "background" in p.lower())]
print("=== 游戏选人背景相关文件 ===")
for p in sorted(hits):
    print(" ", p, entries[p])

def blob(p):
    ofs, size = entries[p]
    with open(PCK, "rb") as fp:
        fp.seek(file_base + ofs)
        return fp.read(size)


for p in sorted(hits):
    if p.endswith((".tscn", ".scn", ".remap", ".tres", ".res")):
        b = blob(p)
        print("=====", p, "=====")
        if b[:4] == b"RSRC":
            print("binary RSRC len", len(b))
        else:
            print(b.decode("utf8", "replace")[:1200])
        break

# ---- Typhon select.png 尺寸 ----
TY = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Typhon\Typhon.pck"
data = open(TY, "rb").read()
fb, do = struct.unpack_from("<QQ", data, 24)
nn = struct.unpack_from("<I", data, do)[0]
pos = do + 4
for _ in range(nn):
    sl = struct.unpack_from("<I", data, pos)[0]
    pos += 4
    p = data[pos:pos + sl].decode("utf8", "replace").rstrip("\x00")
    pos += sl
    ofs, size = struct.unpack_from("<QQ", data, pos)
    pos += 16 + 16 + 4
    if p == ".godot/imported/select.png-41e74f23817c184bd804586d02424fa1.ctex":
        b = data[fb + ofs:fb + ofs + 56]
        print("=== Typhon select.ctex ===")
        print("w/h", struct.unpack_from("<II", b, 8), "data_format", struct.unpack_from("<I", b, 36)[0],
              "mipmaps", struct.unpack_from("<I", b, 44)[0], "format", struct.unpack_from("<I", b, 48)[0])
        break

# ---- 我方 bg 构图统计（明度/边缘分布，判断主体所在区域）----
from PIL import Image, ImageFilter
import io

img = Image.open(r"C:\Users\33761\Desktop\mod2\images\bg\bg.png").convert("RGB")
img.thumbnail((1024, 1024), Image.LANCZOS)
g = img.convert("L")
edges = g.filter(ImageFilter.FIND_EDGES)
W, H = img.size
rows = 8
cols = 8
print("=== bg 构图：每 8x8 区块的边缘密度（%%，越高越有内容）===")
edge_vals = []
for r in range(rows):
    line = []
    for c in range(cols):
        box = (c * W // cols, r * H // rows, (c + 1) * W // cols, (r + 1) * H // rows)
        e = edges.crop(box)
        px = list(e.getdata())
        line.append(round(sum(px) / len(px) / 255 * 100))
    edge_vals.append(line)
for line in edge_vals:
    print("  " + " ".join("%3d" % v for v in line))
lum = list(g.resize((cols, rows)).getdata())
print("亮度(每区块平均):")
for r in range(rows):
    print("  " + " ".join("%3d" % lum[r * cols + c] for c in range(cols)))
