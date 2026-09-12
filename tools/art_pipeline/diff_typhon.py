# -*- coding: utf-8 -*-
"""逐字节对比：Typhon 的侧车/ctex 头 vs 我们生成的。"""
import struct

TY = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Typhon\Typhon.pck"
MY = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya\Amiya.pck"


def parse(pck):
    data = open(pck, "rb").read()
    file_base, dir_off = struct.unpack_from("<QQ", data, 24)
    n = struct.unpack_from("<I", data, dir_off)[0]
    pos = dir_off + 4
    entries = {}
    for _ in range(n):
        plen = struct.unpack_from("<I", data, pos)[0]
        pos += 4
        path = data[pos:pos + plen].decode("utf8")
        pos += plen
        while pos % 4:
            pos += 1
        off, size = struct.unpack_from("<QQ", data, pos)
        pos += 16 + 16 + 4
        entries[path] = (off, size)
    return data, file_base, entries


def blob(pack, path):
    data, file_base, entries = pack
    key = path
    if key not in entries:
        cands = [k for k in entries if k.startswith(path) and k[len(path):].strip("\x00") == ""]
        if len(cands) != 1:
            raise KeyError(path)
        key = cands[0]
    off, size = entries[key]
    return data[file_base + off:file_base + off + size]


ty = parse(TY)
my = parse(MY)

# 1) 侧车对比
ty_sc = blob(ty, "Typhon/images/icon/character_icon_typhon.png.import").decode("utf8")
my_sc = blob(my, "Amiya/images/icon/character_icon_amiya.png.import").decode("utf8")
print("=== Typhon sidecar ===")
print(repr(ty_sc))
print("=== Mine sidecar ===")
print(repr(my_sc))

# 2) ctex 头对比（前 64 字节）
ty_ct = blob(ty, ".godot/imported/character_icon_typhon.png-c2f7407d70e8af56b17fe2fdc2d3fc7a.ctex")
my_ct = blob(my, ".godot/imported/character_icon_amiya.png-adf7143c0277556bc9700b9ebdedb41b.ctex")
print("=== Typhon ctex head ===")
print(" ".join("%02x" % b for b in ty_ct[:64]))
print("ver", struct.unpack_from("<I", ty_ct, 4)[0], "w/h", struct.unpack_from("<II", ty_ct, 8),
      "df", hex(struct.unpack_from("<I", ty_ct, 16)[0]),
      "mipmap_limit", hex(struct.unpack_from("<I", ty_ct, 20)[0]),
      "reserved", struct.unpack_from("<III", ty_ct, 24),
      "data_format", struct.unpack_from("<I", ty_ct, 36)[0],
      "w2/h2", struct.unpack_from("<HH", ty_ct, 40),
      "mipmaps", struct.unpack_from("<I", ty_ct, 44)[0],
      "format", struct.unpack_from("<I", ty_ct, 48)[0],
      "size", struct.unpack_from("<I", ty_ct, 52)[0])
print("=== Mine ctex head ===")
print(" ".join("%02x" % b for b in my_ct[:64]))
print("ver", struct.unpack_from("<I", my_ct, 4)[0], "w/h", struct.unpack_from("<II", my_ct, 8),
      "df", hex(struct.unpack_from("<I", my_ct, 16)[0]),
      "mipmap_limit", hex(struct.unpack_from("<I", my_ct, 20)[0]),
      "reserved", struct.unpack_from("<III", my_ct, 24),
      "data_format", struct.unpack_from("<I", my_ct, 36)[0],
      "w2/h2", struct.unpack_from("<HH", my_ct, 40),
      "mipmaps", struct.unpack_from("<I", my_ct, 44)[0],
      "format", struct.unpack_from("<I", my_ct, 48)[0],
      "size", struct.unpack_from("<I", my_ct, 52)[0])

# 3) Typhon 侧车有没有 uid？我们怎么写的？
import re
print("Typhon uid:", re.search(r'uid="([^"]*)"', ty_sc))
print("Mine  uid:", re.search(r'uid="([^"]*)"', my_sc))

# 4) 场景：Typhon 用 .tscn.remap → .godot/exported 二进制 scn
rem = blob(ty, "Typhon/scenes/TyphonSelectBg.tscn.remap").decode("utf8")
print("=== Typhon scene remap ===")
print(repr(rem))
scn = blob(ty, ".godot/exported/133200997/export-00ec3cb36b466b202918aece18399508-TyphonSelectBg.scn")
print("=== exported scn (first 80 bytes) ===")
print(" ".join("%02x" % b for b in scn[:80]))
print("text?", scn[:16])
# 5) project.binary
pb = blob(ty, "project.binary")
print("=== Typhon project.binary ===")
print(" ".join("%02x" % b for b in pb[:48]))
