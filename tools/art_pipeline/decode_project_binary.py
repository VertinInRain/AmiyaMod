# -*- coding: utf-8 -*-
"""解码 NecrobinderCardPortraits 的 project.binary（ECFG 格式）全部键值。"""
import struct

PCK = r"D:\SteamLibrary\steamapps\workshop\content\2868840\3749627968\NecrobinderCardPortraits.pck"
data = open(PCK, "rb").read()
file_base, dir_off = struct.unpack_from("<QQ", data, 24)
n = struct.unpack_from("<I", data, dir_off)[0]
pos = dir_off + 4
blob = None
for _ in range(n):
    plen = struct.unpack_from("<I", data, pos)[0]
    pos += 4
    path = data[pos:pos + plen].decode("utf8", "replace").rstrip("\x00")
    pos += plen
    while pos % 4:
        pos += 1
    off, size = struct.unpack_from("<QQ", data, pos)
    pos += 16 + 16 + 4
    if path == "project.binary":
        blob = data[file_base + off:file_base + off + size]
print("len", len(blob), blob[:8])
assert blob[:4] == b"ECFG"
ver = struct.unpack_from("<I", blob, 4)[0]
print("version", ver)
p = 8
def read_str(b, p):
    ln = struct.unpack_from("<I", b, p)[0]; p += 4
    s = b[p:p+ln].decode("utf8", "replace"); p += ln
    while p % 4: p += 1
    return s, p
TYPE_MAP = {0:"NIL",1:"BOOL",2:"INT",3:"FLOAT",4:"STRING",14:"PACKEDSTRINGARRAY", 40:"STRINGNAME"}
while p < len(blob):
    key, p = read_str(blob, p)
    t = struct.unpack_from("<I", blob, p)[0]; p += 4
    if t == 4:  # STRING
        v, p = read_str(blob, p)
        print("%-40s STRING %r" % (key, v))
    elif t == 14:  # PackedStringArray
        cnt = struct.unpack_from("<I", blob, p)[0]; p += 4
        vals = []
        for _ in range(cnt):
            v, p = read_str(blob, p)
            vals.append(v)
        print("%-40s PACKEDSTRINGARRAY(%d) %r" % (key, cnt, vals))
    elif t in (0, 1, 2, 3):
        print("%-40s TYPE%d" % (key, t))
    else:
        print("%-40s UNKNOWN TYPE %d at %d" % (key, t, p))
        break
