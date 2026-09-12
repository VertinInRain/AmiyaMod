#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""从 minidump 的 ThreadListStream 取异常线程上下文，做 x64 栈回溯。"""
import struct
import sys

dmp = sys.argv[1]
data = open(dmp, "rb").read()
num_streams = struct.unpack_from("<I", data, 8)[0]
dir_off = struct.unpack_from("<I", data, 12)[0]
streams = {}
for i in range(num_streams):
    off = dir_off + i * 12
    stype, size, rva = struct.unpack_from("<III", data, off)
    streams[stype] = (rva, size)

# 模块表
mods = []
mrva, msize = streams[4]
n = struct.unpack_from("<I", data, mrva)[0]
for i in range(n):
    base = struct.unpack_from("<Q", data, mrva + 4 + i * 108)[0]
    msz = struct.unpack_from("<I", data, mrva + 4 + i * 108 + 8)[0]
    mods.append((base, msz))

def resolve(addr):
    for base, msz in mods:
        if base <= addr < base + msz:
            return "+%x" % (addr - base)
    return "?"

# 异常线程 id
exc_rva, _ = streams[6]
thread_id = struct.unpack_from("<I", data, exc_rva)[0]
print("exception thread:", thread_id)

# ThreadListStream: [count u32][MINIDUMP_THREAD 48B each]
trva, tsize = streams[3]
tcount = struct.unpack_from("<I", data, trva)[0]
print("threads:", tcount)
ctx = None
stack_lo = stack_hi = 0
for i in range(tcount):
    base = trva + 4 + i * 48
    tid = struct.unpack_from("<I", data, base)[0]
    teb = struct.unpack_from("<Q", data, base + 16)[0]
    stack_start = struct.unpack_from("<Q", data, base + 24)[0]
    stack_mem = struct.unpack_from("<Q", data, base + 32)[0]
    loc_rva, loc_size = struct.unpack_from("<II", data, base + 40)
    if tid == thread_id:
        print("thread found at", i, "stack", hex(stack_start), "ctx", hex(loc_rva), loc_size)
        ctx = loc_rva
        stack_lo, stack_hi = stack_start, stack_start + 0x100000
        break

if ctx is None:
    print("no context for exception thread")
    sys.exit(1)

# CONTEXT_AMD64（minidump 带 P1Home..P6Home 前缀 48B）
def read_ctx(off):
    regs = {}
    for name, o in (("rip", 0xF8), ("rsp", 0x98), ("rbp", 0xA0)):
        regs[name] = struct.unpack_from("<Q", data, ctx + o)[0]
    return regs

regs = read_ctx(ctx)
# 若 rip 不在任何模块且 rsp 不像栈，尝试无 home 前缀布局（-48）
def is_module(addr):
    return any(b <= addr < b + s for b, s in mods)

if not is_module(regs["rip"]) and not (0x70000000000 <= regs["rsp"] <= 0x7FFFFFFFFFF):
    regs2 = {}
    for name, o in (("rip", 0xF8 - 48), ("rsp", 0x98 - 48), ("rbp", 0xA0 - 48)):
        regs2[name] = struct.unpack_from("<Q", data, ctx + o)[0]
    print("using no-home layout")
    regs = regs2
rip, rsp, rbp = regs["rip"], regs["rsp"], regs["rbp"]
print("rip:", hex(rip), resolve(rip))
print("rsp:", hex(rsp), "rbp:", hex(rbp))

# MemoryList = stream 5
mem = []
lmrva, lmsize = streams[5]
mn = struct.unpack_from("<I", data, lmrva)[0]
print("memory ranges:", mn)
for i in range(mn):
    start = struct.unpack_from("<Q", data, lmrva + 4 + i * 16)[0]
    m = struct.unpack_from("<Q", data, lmrva + 4 + i * 16 + 8)[0]
    mem.append((start, m))

def read64(addr):
    for idx, (start, m) in enumerate(mem):
        if not (m & 1):
            continue
        off = m & ~1
        nxt = mem[idx + 1][0] if idx + 1 < len(mem) else start + 0x4000
        seg_len = min(nxt - start, len(data) - off)
        if start <= addr < start + seg_len and off + (addr - start) + 8 <= len(data):
            return struct.unpack_from("<Q", data, off + (addr - start))[0]
    return None

print("stack walk (rsp chain):")
cur = rsp
for depth in range(30):
    v = read64(cur)
    if v is None:
        print("  [unreadable @%s]" % hex(cur))
        break
    print("  %s: %s  %s" % (hex(cur), hex(v), resolve(v)))
    cur += 8
