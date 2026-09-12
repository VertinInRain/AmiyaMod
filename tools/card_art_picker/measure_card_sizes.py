#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""测量远古卡图与远古卡模板的精确尺寸。"""
import re
import struct
import sys
import zstandard
from collections import Counter

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

    print("===== 远古卡模板 =====")
    for t in ["images/atlases/compressed.sprites/card_template/ancient_card_border.tres",
              "images/atlases/compressed.sprites/card_template/ancient_card_text_bg_attack.tres"]:
        if t in entries:
            txt = blob_of(t).decode("utf-8", "replace")
            m = re.search(r"region = Rect2\(([\d, ]+)\)", txt)
            print("  ", t, "-> region", m.group(1) if m else "?")

    print("===== 普通卡模板（对照） =====")
    for p in entries:
        if p.startswith("images/atlases/compressed.sprites/card_template/") and p.endswith(".tres") and "ancient" not in p:
            txt = blob_of(p).decode("utf-8", "replace")
            m = re.search(r"region = Rect2\(([\d, ]+)\)", txt)
            if m:
                print("  ", p, "->", m.group(1))
            break

    print("===== 远古卡图精灵（引用 card_atlas_1.png）=====")
    sprites = [p for p in entries if p.startswith("images/atlases/card_atlas.sprites/") and p.endswith(".tres")]
    sizes = Counter()
    samples = []
    for p in sprites:
        txt = blob_of(p).decode("utf-8", "replace")
        if "card_atlas_1.png" in txt:
            m = re.search(r"region = Rect2\(([\d, ]+)\)", txt)
            if m:
                parts = [int(x) for x in m.group(1).split(",")]
                sizes[(parts[2], parts[3])] += 1
                if len(samples) < 10:
                    samples.append((p.split("/")[-1], parts[2], parts[3]))
    print("远古卡图精灵数:", sum(sizes.values()))
    print("区域尺寸分布:", dict(sizes))
    for s in samples:
        print("  ", s)
    return 0


if __name__ == "__main__":
    sys.exit(main())
