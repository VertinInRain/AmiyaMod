#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""列出 card_template 图集里所有精灵的 region 尺寸（普通 vs 远古卡面模板）。"""
import re
import struct
import sys
import zstandard

PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"


def u32(b, off):
    return struct.unpack_from("<I", b, off)[0]


def u64(b, off):
    return struct.unpack_from("<Q", b, off)[0]


def main():
    with open(PCK, "rb") as f:
        data = f.read()
    dir_offset = u64(data, 32)
    count = u32(data, dir_offset)
    cursor = dir_offset + 4
    entries = {}
    for _ in range(count):
        plen = u32(data, cursor)
        path = data[cursor + 4: cursor + 4 + plen].decode("utf-8", "replace").rstrip("\x00")
        cursor += 4 + plen
        cursor += (4 - (cursor % 4)) % 4
        offset = u64(data, cursor)
        cursor += 8
        size = u64(data, cursor)
        cursor += 8
        cursor += 20
        entries[path] = (offset, size)

    def blob_of(p):
        off, size = entries[p]
        b = data[off:off + size]
        if b[:4] == b"\x28\xb5\x2f\xfd":
            b = zstandard.ZstdDecompressor().decompress(b)
        return b

    prefix = "images/atlases/compressed.sprites/card_template/"
    for p in sorted(entries):
        if p.startswith(prefix) and p.endswith(".tres"):
            txt = blob_of(p).decode("utf-8", "replace")
            m = re.search(r"region = Rect2\((\d+), (\d+), (\d+), (\d+)\)", txt)
            name = p[len(prefix):-5]
            if m:
                x, y, w, h = map(int, m.groups())
                print("%-46s %4dx%-4d (%.3f)" % (name, w, h, w / h if h else 0))
            else:
                print("%-46s (无 region)" % name)
    return 0


if __name__ == "__main__":
    sys.exit(main())
