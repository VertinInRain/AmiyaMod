# -*- coding: utf-8 -*-
"""逐个文件对比 cache uid 与侧车 uid 文本，暴力测试编码。"""
import base64
import re
import struct

PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\RegentFemPortraits\RegentFemPortraits.pck"
data = open(PCK, "rb").read()
file_base, dir_off = struct.unpack_from("<QQ", data, 24)
n = struct.unpack_from("<I", data, dir_off)[0]
pos = dir_off + 4
entries = {}
for _ in range(n):
    plen = struct.unpack_from("<I", data, pos)[0]
    pos += 4
    path = data[pos:pos + plen].decode("utf8")
    pos += plen
    while pos % 4:
        pos += 1
    off, size = struct.unpack_from("<QQ", data, pos)
    pos += 16 + 16 + 4
    entries[path] = (off, size)


def blob(path):
    off, size = entries[path]
    return data[file_base + off:file_base + off + size]


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

d36 = "0123456789abcdefghijklmnopqrstuvwxyz"
d62 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"

for path in entries:
    if not path.endswith(".import"):
        continue
    src = "res://" + path[:-len(".import")]
    uid = cache.get(src)
    if uid is None:
        continue
    sc = blob(path).decode("utf8")
    text = re.search(r'uid="(uid://[^"]+)"', sc).group(1)[6:]
    cur = uid
    b36 = ""
    while cur:
        b36 += d36[cur % 36]
        cur //= 36
    cur = uid
    b62 = ""
    while cur:
        b62 += d62[cur % 62]
        cur //= 62
    raw = struct.pack("<Q", uid)
    b64le = base64.b64encode(raw).decode().rstrip("=")
    print("%-18s uid=%-20d text=%-14s b36rev=%-14s b62rev=%-12s b64le=%s" % (
        src.split("/")[-1][:-4], uid, text, b36, b62, b64le))
