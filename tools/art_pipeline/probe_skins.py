# -*- coding: utf-8 -*-
"""对比三个皮肤 pck 的头部原始字节（V2 or V3）。"""
import struct

for name in ["regentSkin", "neowSkin", "necrobinderSkin"]:
    pck = r"D:\SteamLibrary\steamapps\workshop\content\2868840\%s\%s.pck" % (
        {"regentSkin": "3747589574", "neowSkin": "3747611326", "necrobinderSkin": "3747597614"}[name], name)
    data = open(pck, "rb").read()
    print("=== %s (size %d) ===" % (name, len(data)))
    print(" ".join("%02x" % b for b in data[:112]))
    print("pack_ver", struct.unpack_from("<I", data, 4)[0],
          "major", struct.unpack_from("<I", data, 8)[0],
          "minor", struct.unpack_from("<I", data, 12)[0],
          "patch", struct.unpack_from("<I", data, 16)[0],
          "flags", struct.unpack_from("<I", data, 20)[0],
          "file_base", struct.unpack_from("<Q", data, 24)[0],
          "dir_off", struct.unpack_from("<Q", data, 32)[0])
