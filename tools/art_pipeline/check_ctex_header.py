# -*- coding: utf-8 -*-
"""对比自造 ctex 头与参考 pck 中真实 Godot ctex 头。"""
import struct

PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\RegentFemPortraits\RegentFemPortraits.pck"
data = open(PCK, "rb").read()
file_base, dir_off = struct.unpack_from("<QQ", data, 24)
n = struct.unpack_from("<I", data, dir_off)[0]
pos = dir_off + 4
ctex = None
for _ in range(n):
    plen = struct.unpack_from("<I", data, pos)[0]
    pos += 4
    path = data[pos:pos + plen].decode("utf8")
    pos += plen
    while pos % 4:
        pos += 1
    off, size = struct.unpack_from("<QQ", data, pos)
    pos += 16 + 16 + 4
    if path.endswith(".ctex"):
        ctex = (off, size)
        print("found", path)
        break

off, size = ctex
blob = data[file_base + off:file_base + off + 160]
print("magic", blob[:4], "ver", struct.unpack_from("<I", blob, 4)[0])
print("w h", struct.unpack_from("<II", blob, 8))
print("df mipmap_limit", struct.unpack_from("<II", blob, 16))
print("reserved", struct.unpack_from("<III", blob, 24))
print("data_format", struct.unpack_from("<I", blob, 36)[0])
print("w2 h2", struct.unpack_from("<HH", blob, 40))
print("mipmaps", struct.unpack_from("<I", blob, 44)[0])
print("format", struct.unpack_from("<I", blob, 48)[0])
print("png_size", struct.unpack_from("<I", blob, 52)[0])
png = data[file_base + off + 56:file_base + off + 56 + 8]
print("png sig", png[:8])
