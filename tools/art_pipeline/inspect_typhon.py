# -*- coding: utf-8 -*-
"""分析 Typhon.pck（用户游戏里实际可用的模组）的内部结构与 DLL 引用方式。"""
import struct
import sys

PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Typhon\Typhon.pck"
data = open(PCK, "rb").read()
print("size", len(data), "head", data[:4])
file_base, dir_off = struct.unpack_from("<QQ", data, 24)
print("file_base", file_base, "dir_off", dir_off)
n = struct.unpack_from("<I", data, dir_off)[0]
pos = dir_off + 4
entries = []
for _ in range(n):
    plen = struct.unpack_from("<I", data, pos)[0]
    pos += 4
    path = data[pos:pos + plen].decode("utf8", "replace")
    pos += plen
    while pos % 4:
        pos += 1
    off, size = struct.unpack_from("<QQ", data, pos)
    pos += 16 + 16 + 4
    entries.append((path, off, size))
print("entries", n)
from collections import Counter
exts = Counter()
prefixes = Counter()
for p, o, s in entries:
    ext = p.rsplit(".", 1)[-1] if "." in p else "(none)"
    exts[ext] += 1
    if "/" in p:
        prefixes[p.split("/")[0]] += 1
print("exts", dict(exts))
print("prefixes", dict(prefixes))
# 打印全部条目（Typhon 应该条目不多）
for p, o, s in entries:
    print("%10d  %s" % (s, p))
# uid_cache
for p, o, s in entries:
    if p.endswith("uid_cache.bin"):
        b = data[file_base + o:file_base + o + s]
        m = struct.unpack_from("<I", b, 0)[0]
        print("uid_cache entries", m)
        p2 = 4
        for j in range(min(m, 6)):
            uid = struct.unpack_from("<Q", b, p2)[0]
            p2 += 8
            pl = struct.unpack_from("<I", b, p2)[0]
            p2 += 4
            pth = b[p2:p2 + pl].decode("utf8", "replace")
            p2 += pl
            print("  ", uid, repr(pth[:80]))
# 第一个 ctex 头
for p, o, s in entries:
    if p.endswith(".ctex"):
        b = data[file_base + o:file_base + o + 64]
        print("ctex", p, "magic", b[:4], "ver", struct.unpack_from("<I", b, 4)[0],
              "w/h", struct.unpack_from("<II", b, 8),
              "data_format", struct.unpack_from("<I", b, 36)[0],
              "format", struct.unpack_from("<I", b, 48)[0])
        break
# 是否有裸 PNG
pngs = [p for p, o, s in entries if p.lower().endswith((".png", ".webp", ".jpg"))]
print("raw images in pck:", len(pngs), pngs[:5])
sys.exit(0)
