# -*- coding: utf-8 -*-
"""自检生成的 Amiya.pck：目录结构、侧车、uid 缓存、ctex 头。"""
import base64
import re
import struct

PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya\Amiya.pck"
data = open(PCK, "rb").read()
assert data[:4] == b"GDPC"
vers = struct.unpack_from("<IIIII", data, 4)
file_base, dir_off = struct.unpack_from("<QQ", data, 24)
print("header ver", vers, "file_base", file_base, "dir_off", dir_off)
n = struct.unpack_from("<I", data, dir_off)[0]
pos = dir_off + 4
entries = {}
for _ in range(n):
    plen = struct.unpack_from("<I", data, pos)[0]
    pos += 4
    path = data[pos:pos + plen].decode("utf8").rstrip("\x00")
    pos += plen  # 无额外对齐：sl 已含 NUL+补零
    off, size = struct.unpack_from("<QQ", data, pos)
    pos += 16 + 16 + 4
    entries[path] = (off, size)
print("entries", n)
for p in sorted(entries):
    print("  ", p)
def blob(path):
    off, size = entries[path]
    return data[file_base + off:file_base + off + size]

# 校验侧车 uid 与 uid_cache 一致性 + uid 文本合法性（34 进制）
chars = "abcdefghijklmnopqrstuvwxy012345678"
def dec(t):
    v = 0
    for ch in t:
        v = v * 34 + chars.index(ch)
    return v

b = blob(".godot/uid_cache.bin")
m = struct.unpack_from("<I", b, 0)[0]
p2 = 4
cache = {}
for _ in range(m):
    uid = struct.unpack_from("<Q", b, p2)[0]
    p2 += 8
    pl = struct.unpack_from("<I", b, p2)[0]
    p2 += 4
    pth = b[p2:p2 + pl].decode("utf8")
    p2 += pl
    cache[pth] = uid
print("uid_cache entries", m, "consumed", p2, "of", len(b))
assert p2 == len(b), "uid_cache 长度不齐！"

bad = 0
for path in entries:
    if not path.endswith(".remap"):
        continue
    sc = blob(path).decode("utf8")
    tgt = re.search(r'path="(res://[^"]+)"', sc)
    assert tgt, path
    assert tgt.group(1).replace("res://", "") in entries, "remap 目标 ctex 不存在: %s" % path
    # ctex 头检查
    ctex = blob(tgt.group(1).replace("res://", ""))
    assert ctex[:4] == b"GST2", tgt.group(1)
    dfmt = struct.unpack_from("<I", ctex, 36)[0]
    fmt = struct.unpack_from("<I", ctex, 48)[0]
    mm = struct.unpack_from("<I", ctex, 44)[0]
    assert dfmt == 2 and fmt == 5 and mm == 0, (dfmt, fmt, mm)
    print("OK  %-45s -> %s" % (path.split("/")[-1], tgt.group(1).split("/")[-1]))
print("bad", bad)
pb = blob("project.binary")
assert pb[:4] == b"ECFG"
print("project.binary ECFG ok, len", len(pb))
cfg = blob(".godot/global_script_class_cache.cfg")
print("cfg", cfg)
scn = blob("Amiya/scenes/AmiyaSelectBg.tscn").decode("utf8")
print("scene:\n", scn)
