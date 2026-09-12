#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""反汇编指定区间（skipdata 容忍数据块）。用法: python disasm_range.py <start_hex> <end_hex> [outfile]"""
import struct
import sys

import capstone

DLL = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\libspine_godot.windows.template_release.x86_64.dll"
data = open(DLL, "rb").read()
text_va = 0x1000
text_raw = 0x400
text_rsz = 0xFEC00
code = data[text_raw:text_raw + text_rsz]

start = int(sys.argv[1], 16)
end = int(sys.argv[2], 16)
out = sys.argv[3] if len(sys.argv) > 3 else None

md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
md.detail = True

lines = []
slice_ = code[start - text_va:end - text_va]
print("slice len:", len(slice_), "first bytes:", slice_[:8].hex())
print("addr:", hex(start))
try:
    for ins in md.disasm(slice_, start):
        mark = "  <<<" if ins.mnemonic == "db" else ""
        lines.append(f"{ins.address:08x}: {ins.mnemonic} {ins.op_str}{mark}")
except Exception as e:
    print("disasm error:", type(e).__name__, e)
print("instructions:", len(lines))
if out:
    open(out, "w", encoding="utf-8").write("\n".join(lines))
else:
    print("\n".join(lines[:400]))
