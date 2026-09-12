#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""解析 Sentry/Crashpad 产生的 minidump：异常码/崩溃地址/故障模块/寄存器/栈顶。"""
import struct
import sys

dmp = sys.argv[1]
data = open(dmp, "rb").read()

# MINIDUMP_HEADER: sig 'MDMP' + 4B streams
sig = data[:4]
print("sig:", sig)
num_streams = struct.unpack_from("<I", data, 8)[0]
dir_off = struct.unpack_from("<I", data, 12)[0]
print("streams:", num_streams)

streams = {}
for i in range(num_streams):
    off = dir_off + i * 12
    stype, size, rva = struct.unpack_from("<III", data, off)
    streams[stype] = (rva, size)
print("stream types:", {k: hex(v[0]) for k, v in sorted(streams.items())})

# 4 = ModuleList, 6 = Exception, 7 = SystemInfo, 9 = MemoryList, 3 = ThreadList
mods = []
if 4 in streams:
    rva, size = streams[4]
    n = struct.unpack_from("<I", data, rva)[0]
    print(f"modules: {n}")
    for i in range(n):
        base = struct.unpack_from("<Q", data, rva + 4 + i * 108)[0]
        msize = struct.unpack_from("<I", data, rva + 4 + i * 108 + 8)[0]
        name_rva = struct.unpack_from("<I", data, rva + 4 + i * 108 + 24)[0]
        raw = data[rva + name_rva: rva + name_rva + 512]
        end = raw.find(b"\x00\x00")
        if end >= 0:
            raw = raw[:end + 1]
        name = raw.decode("utf-16-le", "replace").split("\x00")[0]
        mods.append((base, msize, name))
    for m in mods:
        print(f"  {m[0]:016x} +{m[1]:08x} {m[2]}")

if 6 in streams:
    rva, size = streams[6]
    thread_id = struct.unpack_from("<I", data, rva)[0]
    code = struct.unpack_from("<I", data, rva + 8)[0]
    flags = struct.unpack_from("<I", data, rva + 12)[0]
    addr = struct.unpack_from("<Q", data, rva + 24)[0]
    params = struct.unpack_from("<QQ", data, rva + 32)
    print(f"exception: thread={thread_id} code={code:08x} flags={flags:08x} addr={addr:016x}")
    print(f"  params: {[hex(p) for p in params]}")
    # MINIDUMP_EXCEPTION_STREAM = [u32 ThreadId][u32 pad][EXCEPTION_RECORD 152B]
    # [MINIDUMP_LOCATION_DESCRIPTOR ThreadContext = rva + size]
    loc_rva, loc_size = struct.unpack_from("<II", data, rva + 4 + 152)
    ctx_off = loc_rva
    print(f"  context loc: rva={hex(loc_rva)} size={loc_size}")
    rip = struct.unpack_from("<Q", data, ctx_off + 0xF8)[0]
    rax = struct.unpack_from("<Q", data, ctx_off + 0x78)[0]
    rbx = struct.unpack_from("<Q", data, ctx_off + 0x80)[0]
    rcx = struct.unpack_from("<Q", data, ctx_off + 0x88)[0]
    rdx = struct.unpack_from("<Q", data, ctx_off + 0x90)[0]
    rsp = struct.unpack_from("<Q", data, ctx_off + 0x98)[0]
    rbp = struct.unpack_from("<Q", data, ctx_off + 0xA0)[0]
    rsi = struct.unpack_from("<Q", data, ctx_off + 0xA8)[0]
    rdi = struct.unpack_from("<Q", data, ctx_off + 0xB0)[0]
    r8 = struct.unpack_from("<Q", data, ctx_off + 0xB8)[0]
    r9 = struct.unpack_from("<Q", data, ctx_off + 0xC0)[0]
    r10 = struct.unpack_from("<Q", data, ctx_off + 0xC8)[0]
    r11 = struct.unpack_from("<Q", data, ctx_off + 0xD0)[0]
    print(f"  rip={rip:016x} rsp={rsp:016x} rbp={rbp:016x}")
    print(f"  rax={rax:016x} rbx={rbx:016x} rcx={rcx:016x} rdx={rdx:016x}")
    print(f"  rsi={rsi:016x} rdi={rdi:016x} r8={r8:016x} r9={r9:016x} r10={r10:016x} r11={r11:016x}")
    # 定位崩溃地址/rip 所在模块
    for target, label in ((addr, "crash_addr"), (rip, "rip")):
        for base, msize, name in mods:
            if base <= target < base + msize:
                print(f"  {label} in {name} @+{target - base:x}")
    # 栈顶：从 MemoryList 读 rsp 处的值
    if 9 in streams:
        mrva, msize = streams[9]
        n = struct.unpack_from("<I", data, mrva)[0]
        print(f"  memory ranges: {n}")
        for i in range(n):
            start = struct.unpack_from("<Q", data, mrva + 4 + i * 16)[0]
            m = struct.unpack_from("<Q", data, mrva + 4 + i * 16 + 8)[0]
            if m & 1:
                m &= ~1
                seg = data[m:m + 0x20000]
                if start <= rsp < start + len(seg):
                    off = rsp - start
                    vals = struct.unpack_from("<24Q", seg, off)
                    print(f"  stack @rsp: {[hex(v) for v in vals]}")
                    break
