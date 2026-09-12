# -*- coding: utf-8 -*-
"""解析 LieRenTVmod.pck 目录区原始字节，弄清条目结构。"""
import struct

PCK = r"D:\SteamLibrary\steamapps\workshop\content\2868840\3787753911\LieRenTVmod.pck"
data = open(PCK, "rb").read()
print("size", len(data), "magic", data[:4])
# 头部逐字段
p = 4
print("pack_ver", struct.unpack_from("<I", data, p)[0]); p += 4
print("major", struct.unpack_from("<I", data, p)[0]); p += 4
print("minor", struct.unpack_from("<I", data, p)[0]); p += 4
print("patch", struct.unpack_from("<I", data, p)[0]); p += 4
print("flags", struct.unpack_from("<I", data, p)[0]); p += 4
print("file_base", struct.unpack_from("<Q", data, p)[0]); p += 8
print("dir_off", struct.unpack_from("<Q", data, p)[0]); p += 8
file_base, dir_off = struct.unpack_from("<QQ", data, 24)
n = struct.unpack_from("<I", data, dir_off)[0]
print("entries", n)
pos = dir_off + 4
# 第一个条目原始十六进制
print("first entry raw:", " ".join("%02x" % b for b in data[pos:pos + 96]))
# 试解析：sl + path + pad4 + ofs + size + md5 + flags
sl = struct.unpack_from("<I", data, pos)[0]
print("sl", sl, "path", data[pos + 4:pos + 4 + sl])
pos2 = pos + 4 + sl
while pos2 % 4:
    pos2 += 1
print("after pad:", " ".join("%02x" % b for b in data[pos2:pos2 + 48]))
# 尝试不同布局：可能 md5 在 ofs/size 之前？
off, size = struct.unpack_from("<QQ", data, pos2)
print("ofs", off, "size", size, "next16", data[pos2 + 16:pos2 + 32].hex(), "flags", struct.unpack_from("<I", data, pos2 + 32)[0])
