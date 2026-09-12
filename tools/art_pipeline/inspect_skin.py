# -*- coding: utf-8 -*-
"""分析已启用的皮肤模组 pck（用户游戏里真实工作的参考）。"""
import struct
import sys

PCK = sys.argv[1]
data = open(PCK, "rb").read()
print("file", PCK, "size", len(data), "head", data[:4])
file_base, dir_off = struct.unpack_from("<QQ", data, 24)
print("file_base", file_base, "dir_off", dir_off)
n = struct.unpack_from("<I", data, dir_off)[0]
pos = dir_off + 4
entries = []
for _ in range(n):
    plen = struct.unpack_from("<I", data, pos)[0]
    pos += 4
    path = data[pos:pos + plen].decode("utf8", "replace").rstrip("\x00")
    pos += plen
    while pos % 4:
        pos += 1
    off, size = struct.unpack_from("<QQ", data, pos)
    pos += 16 + 16 + 4
    entries.append((path, off, size))
print("entries", n)
for p, o, s in entries:
    print("%10d  %s" % (s, p))
