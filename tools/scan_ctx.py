#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""在 CONTEXT 区域扫描 rip/rsp 候选（模块内=代码地址，栈区=栈指针）。"""
import struct
import sys

dmp = sys.argv[1]
data = open(dmp, "rb").read()

mrva = 0x19B90
n = struct.unpack_from("<I", data, mrva)[0]
mods = []
for i in range(n):
    base = struct.unpack_from("<Q", data, mrva + 4 + i * 108)[0]
    msz = struct.unpack_from("<I", data, mrva + 4 + i * 108 + 8)[0]
    mods.append((base, msz))

def in_mod(a):
    return any(b <= a < b + s for b, s in mods)

def mod_name(a):
    for b, s in mods:
        if b <= a < b + s:
            return "+%x" % (a - b)
    return "?"

ctx = 0x4D0
st_start, st_size = 0x76FA3E3000, 0x8E9E0
print("context qwords (module/stack hits):")
for off in range(0, 0x220, 8):
    v = struct.unpack_from("<Q", data, ctx + off)[0]
    tags = []
    if in_mod(v):
        tags.append("MOD")
    if st_start <= v < st_start + st_size:
        tags.append("STACK")
    if tags:
        print("  +%03x: %016x %s" % (off, v, " ".join(tags)))

# 栈内存块（rva 0x1D0, size 0x8E9E0）
print("stack memory return-address candidates:")
st = data[0x1D0: 0x1D0 + 0x8E9E0]
hits = []
for off in range(0, len(st) - 8, 8):
    v = struct.unpack_from("<Q", st, off)[0]
    if in_mod(v):
        hits.append((off, v))
for off, v in hits[:25]:
    print("  stack+%06x: %016x %s" % (off, v, mod_name(v)))
print("total hits:", len(hits))
