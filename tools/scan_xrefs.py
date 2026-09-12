#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""逐字节暴力反汇编扫描 .text 中对目标字符串的 RIP 引用（容忍跳表打断）。"""
import struct

import capstone

DLL = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\libspine_godot.windows.template_release.x86_64.dll"
data = open(DLL, "rb").read()
e_lfanew = struct.unpack_from("<I", data, 0x3C)[0]
num_sections = struct.unpack_from("<H", data, e_lfanew + 6)[0]
opt_size = struct.unpack_from("<H", data, e_lfanew + 20)[0]
sec_off = e_lfanew + 24 + opt_size
sections = []
for i in range(num_sections):
    h = sec_off + i * 40
    name = data[h:h + 8].rstrip(b"\x00").decode()
    vsz = struct.unpack_from("<I", data, h + 8)[0]
    va = struct.unpack_from("<I", data, h + 12)[0]
    rsz = struct.unpack_from("<I", data, h + 16)[0]
    raw = struct.unpack_from("<I", data, h + 20)[0]
    sections.append((name, va, vsz, raw, rsz))

def va_to_off(va):
    for n, sva, vsz, raw, rsz in sections:
        if sva <= va < sva + max(vsz, rsz):
            return raw + (va - sva)
    return None

def cstr_at(va):
    off = va_to_off(va)
    if off is None:
        return None
    end = data.find(b"\x00", off)
    if end < 0:
        return None
    return data[off:end]

text_name, text_va, text_vs, text_raw, text_rsz = sections[0]
code = data[text_raw:text_raw + text_rsz]
md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
md.detail = True

targets = {
    0x101FF8: "Skeleton version...",
}
# 也扫 "load_from_file" 名串与 "4.2" 字符串的引用
extra = {
    0x101FD8: "4.2 version string",
    0x101FD0: "%x format",
}
for k, v in extra.items():
    targets[k] = v

found = {}
for off in range(0, len(code)):
    ins = next(md.disasm(code[off:off + 15], text_va + off), None)
    if ins is None:
        continue
    # 只接受边界整齐的指令
    for op in ins.operands:
        if op.type == capstone.x86.X86_OP_MEM and op.mem.base == capstone.x86.X86_REG_RIP:
            tgt = op.mem.disp + ins.address + ins.size
            if tgt in targets:
                found.setdefault(tgt, []).append(ins.address)
print("found refs:")
for tgt, addrs in found.items():
    print(f"  {hex(tgt)} ({targets[tgt]}) <- {[hex(a) for a in addrs]}")
