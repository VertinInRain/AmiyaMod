#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""PNG → Godot 4 .ctex（GST2 格式，DATA_FORMAT_PNG 内嵌）+ .png.import 侧车。
参照 Godot 4.5 compressed_texture.cpp 的读取逻辑逐字段写出。"""
import hashlib
import os
import struct


def png_to_ctex(png_bytes):
    sig = png_bytes[:8]
    assert sig == b"\x89PNG\r\n\x1a\n", "not a png"
    w = struct.unpack(">I", png_bytes[16:20])[0]
    h = struct.unpack(">I", png_bytes[20:24])[0]
    out = bytearray()
    out += b"GST2"
    out += struct.pack("<I", 1)          # version
    out += struct.pack("<I", w)
    out += struct.pack("<I", h)
    out += struct.pack("<I", 0)          # df（无 detect 位）
    out += struct.pack("<I", 0)          # mipmap_limit
    out += struct.pack("<III", 0, 0, 0)  # reserved
    # load_image_from_file: DATA_FORMAT_PNG
    out += struct.pack("<I", 1)          # data_format = PNG
    out += struct.pack("<HH", w, h)
    out += struct.pack("<I", 0)          # mipmaps
    out += struct.pack("<I", 0)          # format（PNG 分支忽略）
    out += struct.pack("<I", len(png_bytes))
    out += png_bytes
    return bytes(out)


def make_sidecar(source_res_path, ctex_res_path):
    uid = "uid://amiya" + hashlib.md5(source_res_path.encode("utf-8")).hexdigest()[:20]
    return ('[remap]\n\nimporter="texture"\ntype="CompressedTexture2D"\nuid="%s"\npath="%s"\n'
            'metadata={\n"vram_texture": false\n}\n' % (uid, ctex_res_path))


def build_texture(png_path, out_ctex_path, out_sidecar_path, source_res_path):
    with open(png_path, "rb") as f:
        png = f.read()
    ctex = png_to_ctex(png)
    os.makedirs(os.path.dirname(out_ctex_path), exist_ok=True)
    os.makedirs(os.path.dirname(out_sidecar_path), exist_ok=True)
    with open(out_ctex_path, "wb") as f:
        f.write(ctex)
    ctex_res = "res://" + out_ctex_path.replace("\\", "/").split("/", 1)[-1] if False else None
    # ctex_res 由调用方给出（.godot/imported 相对 res:// 根）
    with open(out_sidecar_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(make_sidecar(source_res_path, "res://" + "/".join(out_ctex_path.split(os.sep))))
    return len(ctex)


if __name__ == "__main__":
    import sys
    src, dst = sys.argv[1], sys.argv[2]
    data = png_to_ctex(open(src, "rb").read())
    with open(dst, "wb") as f:
        f.write(data)
    print("ctex: %s (%d 字节)" % (dst, len(data)))
