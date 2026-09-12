# -*- coding: utf-8 -*-
"""完整十六进制 + 逐字段解码 NB 的 project.binary。"""
import struct

PCK = r"D:\SteamLibrary\steamapps\workshop\content\2868840\3749627968\NecrobinderCardPortraits.pck"
data = open(PCK, "rb").read()
fb, do = struct.unpack_from("<QQ", data, 24)
n = struct.unpack_from("<I", data, do)[0]
pos = do + 4
blob = None
for _ in range(n):
    sl = struct.unpack_from("<I", data, pos)[0]; pos += 4
    p = data[pos:pos + sl].decode("utf8", "replace"); pos += sl
    while pos % 4: pos += 1
    off, size = struct.unpack_from("<QQ", data, pos); pos += 36
    if p.rstrip("\x00") == "project.binary":
        blob = data[fb + off:fb + off + size]
print("len", len(blob))
for i in range(0, len(blob), 16):
    chunk = blob[i:i + 16]
    hexs = " ".join("%02x" % b for b in chunk)
    ascii_s = "".join(chr(b) if 32 <= b < 127 else "." for b in chunk)
    print("%04d: %-47s  %s" % (i, hexs, ascii_s))
