#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""解剖参考 mod 的 pck（NecrobinderCardPortraits.pck）结构。"""
import struct
import sys

PCK = r"D:\SteamLibrary\steamapps\workshop\content\2868840\3749627968\NecrobinderCardPortraits.pck"


def u32(b, off):
    return struct.unpack_from("<I", b, off)[0]


def u64(b, off):
    return struct.unpack_from("<Q", b, off)[0]


def main():
    data = open(PCK, "rb").read()
    print("magic:", data[:4], "pack_ver:", u32(data, 4), "ver: %d.%d.%d" % (u32(data, 8), u32(data, 12), u32(data, 16)),
          "flags:", u32(data, 20), "file_base:", u64(data, 24), "dir:", u64(data, 32))
    dir_off = u64(data, 32)
    count = u32(data, dir_off)
    print("文件数:", count)
    cursor = dir_off + 4
    for i in range(min(count, 6)):
        plen = u32(data, cursor)
        path = data[cursor + 4: cursor + 4 + plen].decode("utf-8", "replace").rstrip("\x00")
        cursor += 4 + plen
        cursor += (4 - (cursor % 4)) % 4
        off = u64(data, cursor)
        size = u64(data, cursor + 8)
        flags = u32(data, cursor + 32)
        cursor += 36
        blob = data[off:off + min(size, 16)]
        print("  %-60s off=%d size=%d flags=%d 头=%r" % (path, off, size, flags, blob[:12]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
