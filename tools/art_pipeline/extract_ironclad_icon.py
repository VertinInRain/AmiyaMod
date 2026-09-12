# -*- coding: utf-8 -*-
"""从游戏主 pck 提取 ironclad 角色图标场景，弄清图标场景结构。"""
import struct

PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"
f = open(PCK, "rb")
head = f.read(40)
print("magic", head[:4], "vers", struct.unpack_from("<IIIII", head, 4),
      "file_base", struct.unpack_from("<Q", head, 24)[0], "dir_off", struct.unpack_from("<Q", head, 32)[0])
file_base, dir_off = struct.unpack_from("<QQ", head, 24)
f.seek(dir_off)
n = struct.unpack("<I", f.read(4))[0]
print("entries", n)


def read_entry(fp):
    sl = struct.unpack("<I", fp.read(4))[0]
    path = fp.read(sl).decode("utf8", "replace").rstrip("\x00")
    ofs, size = struct.unpack("<QQ", fp.read(16))
    fp.read(16)  # md5
    fp.read(4)   # flags
    return path, ofs, size


targets = []
for _ in range(n):
    path, ofs, size = read_entry(f)
    if "character_icons" in path and path.endswith((".tscn", ".scn", ".remap")):
        targets.append((path, ofs, size))
        print("found:", path, ofs, size)
        if len(targets) >= 10:
            break

for path, ofs, size in targets[:3]:
    f.seek(file_base + ofs)
    blob = f.read(size)
    print("=====", path, "=====")
    if blob[:4] == b"RSRC":
        print("binary RSRC, first bytes:", " ".join("%02x" % b for b in blob[:64]))
    else:
        print(blob.decode("utf8", "replace")[:3000])
f.close()
