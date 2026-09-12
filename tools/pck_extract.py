#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""从 Godot 4 pck 提取单个文件（逆向解析目录，支持 zstd）。用法: python pck_extract.py <pck> <res_path> <out_file>"""
import sys
import struct

def u32(b, off):
    return struct.unpack_from("<I", b, off)[0]

def u64(b, off):
    return struct.unpack_from("<Q", b, off)[0]

def printable(s):
    return all(32 <= c < 127 for c in s)

def main(pck_path, res_path, out_path):
    with open(pck_path, "rb") as f:
        data = f.read()
    if data[:4] != b"GDPC":
        print("非 GDPC:", data[:4]); sys.exit(1)
    # Godot4.5 PCK v3：头部 32 字节后是目录偏移(uint64)
    dir_offset = u64(data, 32)
    print("目录偏移:", dir_offset)
    count = u32(data, dir_offset)
    print("文件数:", count)
    cursor = dir_offset + 4
    entries = []
    for _ in range(count):
        plen = u32(data, cursor)
        path = data[cursor + 4: cursor + 4 + plen].decode("utf-8", "replace").rstrip("\x00")
        cursor += 4 + plen
        cursor += (4 - (cursor % 4)) % 4
        offset = u64(data, cursor); cursor += 8
        size = u64(data, cursor); cursor += 8
        cursor += 16  # md5
        cursor += 4   # flags
        entries.append((path, offset, size))
    target = None
    for p, o, s in entries:
        if p == res_path:
            target = (o, s)
            break
    if not target:
        print("未找到:", res_path)
        for p, o, s in entries:
            if res_path.split("/")[-1].split(".")[0] in p:
                print("  候选:", p)
        sys.exit(1)
    off, size = target
    print("offset:", off, "size:", size)
    blob = data[off: off + size]
    if blob[:4] == b"\x28\xb5\x2f\xfd":
        import zstandard
        blob = zstandard.ZstdDecompressor().decompress(blob)
        print("zstd 解压后:", len(blob))
    with open(out_path, "wb") as f:
        f.write(blob)
    print("已写出:", out_path)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
