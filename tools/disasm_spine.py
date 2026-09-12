#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""反汇编引用版本错误串的两个函数，还原 fork-4.2.43 readSkeletonData 头部布局。"""
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

text_name, text_va, text_vs, text_raw, text_rsz = sections[0]
code = data[text_raw:text_raw + text_rsz]
md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
md.detail = True

def find_func_start(addr):
    for back in range(0, 0x1000):
        p = addr - back
        if p < text_va:
            return None
        off = p - text_va
        if code[off] in (0xC3, 0xC2):
            start = p + (3 if code[off] == 0xC2 else 1)
            while start < addr and code[start - text_va] in (0xCC, 0x90):
                start += 1
            return start
    return None

for anchor in (0x29CE1, 0x3A2B8):
    start = find_func_start(anchor)
    print(f"\n===== anchor {hex(anchor)} func start {hex(start)} =====")
    end = min(start + 0x700, text_va + text_rsz)
    insns = list(md.disasm(code[start - text_va:end - start], start))
    for ins in insns[:500]:
        print(f"{ins.address:08x}: {ins.mnemonic} {ins.op_str}")
