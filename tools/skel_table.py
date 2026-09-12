#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""按'字符串表'假设解析 skel：0x1C包装 + 版本 + 4浮点 + [varint计数][(varint长度,字符串)]*N，观察后续结构。"""
import io
import struct
import sys

def read_varint(f):
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

def main(path):
    data = open(path, "rb").read()
    f = io.BytesIO(data)
    # 包装：第0字节=前缀总长（含自身）
    prefix = f.read(1)[0]
    f.read(prefix - 1)
    ver_len = read_varint(f)
    ver = f.read(ver_len)
    print("version:", ver)
    floats = struct.unpack("<4f", f.read(16))
    print("floats:", floats)
    count = read_varint(f)
    print("string table count:", count)
    names = []
    for i in range(count):
        n = read_varint(f)
        s = f.read(n)
        names.append(s)
    print("=== 全部字符串（去重保留序）===")
    seen = set()
    for s in names:
        key = s.rstrip(b"\x00")
        if key not in seen:
            seen.add(key)
            print(repr(key.decode("utf-8", "replace")))
    print("=== 表后剩余字节数:", len(data) - f.tell())
    rest = data[f.tell():]
    print("表后前 80 字节 hex:")
    print(" ".join(f"{b:02X}" for b in rest[:80]))

if __name__ == "__main__":
    main(sys.argv[1])
