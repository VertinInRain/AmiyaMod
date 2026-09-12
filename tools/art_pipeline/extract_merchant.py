# -*- coding: utf-8 -*-
"""提取游戏 merchant 场景与 rest_site 场景结构。"""
import struct

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

for p, (ofs, size) in entries.items():
    if "merchant" in p and p.endswith(".tscn") and "ironclad" in p:
        print("=====", p, "=====")
        with open(PCK, "rb") as fp:
            fp.seek(file_base + ofs)
            b = fp.read(size)
        if b[:4] == b"RSRC":
            print("binary RSRC, len", len(b))
        else:
            print(b.decode("utf8", "replace")[:2500])
        break
