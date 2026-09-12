#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把 cropped/*.png 打包为 Amiya.pck（Godot 4 pck v3，与游戏 pck 同版本头）。
内部路径: images/amiya_cards/<卡牌类名>.png → res://images/amiya_cards/..."""
import json
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
CROPPED = os.path.join(HERE, "cropped")
CARDS_JSON = os.path.join(HERE, "cards.json")
OUT = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya\Amiya.pck"


def main():
    cards = json.load(open(CARDS_JSON, encoding="utf-8"))
    files = []
    missing = []
    for c in cards:
        p = os.path.join(CROPPED, c["id"] + ".png")
        if os.path.exists(p):
            with open(p, "rb") as f:
                files.append((c["id"], f.read()))
        else:
            missing.append(c["id"])
    if missing:
        print("缺少裁剪文件:", missing)
        return 1

    data = bytearray()
    # 头：与参考 mod pck 相同（flags=2 + file_base=112；目录偏移=相对 file_base）
    data += b"GDPC"
    data += struct.pack("<IIIII", 3, 4, 5, 1, 2)  # pack_ver, major, minor, patch, flags
    FILE_BASE = 112
    data += struct.pack("<QQ", FILE_BASE, 0)      # file_base, dir_offset（回填）
    while len(data) < FILE_BASE:
        data += b"\x00"

    offsets = []
    sizes = []
    for cid, blob in files:
        offsets.append(len(data) - FILE_BASE)     # 相对 file_base
        sizes.append(len(blob))
        data += blob
        while len(data) % 4:
            data += b"\x00"

    dir_off = len(data)  # 目录偏移 = 绝对地址（仅文件偏移相对 file_base）
    data += struct.pack("<I", len(files))
    for cid, off, size in zip([cid for cid, _ in files], offsets, sizes):
        pb = ("images/amiya_cards/%s.png" % cid).encode("utf-8")
        data += struct.pack("<I", len(pb))
        data += pb
        while len(data) % 4:
            data += b"\x00"
        data += struct.pack("<QQ", off, size)
        data += b"\x00" * 16          # md5
        data += struct.pack("<I", 0)  # flags（未压缩）

    struct.pack_into("<QQ", data, 24, FILE_BASE, dir_off)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "wb") as f:
        f.write(data)
    print("已打包 %d 张卡图 -> %s (%d 字节)" % (len(files), OUT, len(data)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
