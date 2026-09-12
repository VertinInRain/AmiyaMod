# -*- coding: utf-8 -*-
"""读取 Typhon 的 ancients.json 结构（先古对话 loc 表格式）。"""
import struct

PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Typhon\Typhon.pck"
data = open(PCK, "rb").read()
fb, do = struct.unpack_from("<QQ", data, 24)
n = struct.unpack_from("<I", data, do)[0]
pos = do + 4
for _ in range(n):
    sl = struct.unpack_from("<I", data, pos)[0]
    pos += 4
    p = data[pos:pos + sl].decode("utf8", "replace").rstrip("\x00")
    pos += sl
    ofs, size = struct.unpack_from("<QQ", data, pos)
    pos += 36
    if "ancients.json" in p:
        blob = data[fb + ofs:fb + ofs + size]
        print("=====", p, "=====")
        print(blob.decode("utf8", "replace"))
        break
