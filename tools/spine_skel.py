#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""解析 Spine 3.8 二进制 .skel，提取动画名列表。用法: python spine_skel.py <file.skel>"""
import io
import sys
import struct

def read_varint(f):
    """Spine binary optimizePositive 变长整数"""
    b = f.read(1)
    if not b:
        return 0
    b = b[0]
    result = b & 0x7F
    if (b & 0x80) != 0:
        b = f.read(1)[0]
        result |= (b & 0x7F) << 7
        if (b & 0x80) != 0:
            b = f.read(1)[0]
            result |= (b & 0x7F) << 14
            if (b & 0x80) != 0:
                b = f.read(1)[0]
                result |= (b & 0x7F) << 21
                if (b & 0x80) != 0:
                    b = f.read(1)[0]
                    result |= (b & 0x7F) << 28
    return result

def read_string(f):
    n = read_varint(f)
    if n <= 0:
        return ""
    raw = f.read(n)
    try:
        return raw.decode("utf-8")
    except Exception:
        return raw.decode("latin-1")

def read_float(f):
    return struct.unpack("<f", f.read(4))[0]

def read_color(f):
    return struct.unpack("<4B", f.read(4))

def read_cstring(f):
    """NUL 结尾字符串"""
    out = bytearray()
    while True:
        b = f.read(1)
        if not b or b == b"\x00":
            break
        out += b
    return out.decode("utf-8", "replace")

def main(path):
    with open(path, "rb") as fp:
        f = io.BytesIO(fp.read())
    magic = f.read(4)
    print("magic:", magic)
    if magic != b"skel":
        # ArkModels 包装：第0字节=总长(含自身)，随后为资产哈希串，再往后才是标准 skel 内容
        wrapper_len = magic[0]
        f.read(wrapper_len - 4)  # 已读 4 字节（1 长度 + 3 哈希开头）
        version = read_string(f)
    else:
        version = read_string(f)
    print("version:", version)
    x = read_float(f); y = read_float(f)
    width = read_float(f); height = read_float(f)
    print("xy/wh:", x, y, width, height)
    nonessential = bool(read_varint(f))
    print("nonessential:", nonessential)
    if nonessential:
        fps = read_float(f); images = read_string(f); audio = read_string(f)
        print("fps/images/audio:", fps, images, audio)

    # Bones
    n = read_varint(f)
    print("bones:", n)
    for _ in range(n):
        name = read_string(f)
        parent = read_varint(f)
        length = read_float(f)
        x = read_float(f); y = read_float(f)
        rot = read_float(f); sx = read_float(f); sy = read_float(f)
        shx = read_float(f); shy = read_float(f)
        transform = read_varint(f)

    # Slots
    n = read_varint(f)
    print("slots:", n)
    for _ in range(n):
        name = read_string(f)
        bone = read_varint(f)
        color = read_color(f)
        attachment = read_string(f)
        blend = read_varint(f)

    # IK constraints
    n = read_varint(f)
    print("ik:", n)
    for _ in range(n):
        name = read_string(f)
        order = read_varint(f)
        bones = read_varint(f)
        for _b in range(bones):
            read_varint(f)
        target = read_varint(f)
        mix = read_float(f); softness = read_float(f); bend = read_varint(f)
        compress = read_varint(f); stretch = read_varint(f)
        uniform = read_varint(f)

    # Transform constraints
    n = read_varint(f)
    print("transform constraints:", n)
    for _ in range(n):
        name = read_string(f)
        order = read_varint(f)
        bones = read_varint(f)
        for _b in range(bones):
            read_varint(f)
        target = read_varint(f)
        rotate = read_float(f); translate = read_float(f); scale = read_float(f); shear = read_float(f)
        rotmix = read_float(f); transmix = read_float(f); scalmix = read_float(f); shearmix = read_float(f)
        offsetRot = read_float(f); offsetX = read_float(f); offsetY = read_float(f); offsetScaleX = read_float(f)
        offsetScaleY = read_float(f); offsetShearY = read_float(f)
        relative = read_varint(f); local = read_varint(f)

    # Path constraints
    n = read_varint(f)
    print("path constraints:", n)
    for _ in range(n):
        name = read_string(f)
        order = read_varint(f)
        bones = read_varint(f)
        for _b in range(bones):
            read_varint(f)
        target = read_varint(f)
        positionMode = read_varint(f); spacingMode = read_varint(f); rotateMode = read_varint(f)
        offsetRotation = read_float(f)
        position = read_float(f); spacing = read_float(f); rotateMix = read_float(f); mixX = read_float(f)
        mixY = read_float(f); mixRotate = read_float(f)
        for _b in range(bones):
            curve = read_varint(f)
            if curve > 0:
                read_float(f)
        compensate = read_varint(f)

    # Skins
    n = read_varint(f)
    print("skins:", n)
    for _ in range(n):
        skin_name = read_string(f)
        slots = read_varint(f)
        for _s in range(slots):
            slot_index = read_varint(f)
            attachments = read_varint(f)
            for _a in range(attachments):
                read_string(f)  # attachment name
                typ = read_varint(f)
                if typ == 0:  # region
                    read_string(f)
                    read_float(f); read_float(f); read_float(f); read_float(f)
                    read_float(f); read_float(f); read_float(f); read_float(f)
                    color = read_color(f)
                elif typ == 1:  # mesh
                    read_string(f)
                    uvs = read_varint(f)
                    for _u in range(uvs):
                        read_float(f); read_float(f)
                    tris = read_varint(f)
                    for _t in range(tris):
                        read_varint(f)
                    hull = read_varint(f)
                    edges = read_varint(f)
                    for _e in range(edges):
                        read_varint(f)
                    read_float(f); read_float(f); read_float(f); read_float(f)
                    read_float(f); read_float(f); read_float(f); read_float(f)
                    color = read_color(f)
                    if edges == 0:
                        pass
                elif typ == 2:  # linked mesh
                    skin_index = read_varint(f)
                    read_string(f)
                    read_string(f)
                    read_float(f); read_float(f); read_float(f); read_float(f)
                    read_float(f); read_float(f); read_float(f); read_float(f)
                    color = read_color(f)
                elif typ == 3:  # bounding box
                    vertex_count = read_varint(f)
                    for _v in range(vertex_count):
                        read_float(f); read_float(f)
                elif typ == 4:  # path
                    read_string(f)
                    vertex_count = read_varint(f)
                    for _v in range(vertex_count):
                        read_float(f); read_float(f)
                        read_float(f)
                    lengths = read_varint(f)
                    for _v in range(lengths):
                        read_float(f)
                    closed = read_varint(f)
                    constant = read_varint(f)
                elif typ == 5:  # point
                    read_float(f); read_float(f); read_float(f); read_float(f)
                    color = read_color(f)
                elif typ == 6:  # clipping
                    read_string(f)
                    vertex_count = read_varint(f)
                    for _v in range(vertex_count):
                        read_float(f); read_float(f)
                    color = read_color(f)

    # Events
    n = read_varint(f)
    print("events:", n)
    for _ in range(n):
        ev = read_string(f)
        int_val = read_varint(f)
        float_val = read_float(f)
        string_val = read_string(f)
        print("  event:", ev)

    # Animations
    n = read_varint(f)
    print("animations:", n)
    names = []
    for _ in range(n):
        name = read_string(f)
        names.append(name)
        print("  anim:", repr(name))
        # timeline counts
        read_varint(f)  # bones
        read_varint(f)  # slots
        read_varint(f)  # ik
        read_varint(f)  # transform
        read_varint(f)  # paths
        read_varint(f)  # deform
        read_varint(f)  # drawOrder
        read_varint(f)  # events
    print("ANIM_NAMES_JSON:", names)

if __name__ == "__main__":
    main(sys.argv[1])
