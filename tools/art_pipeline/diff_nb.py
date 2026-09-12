# -*- coding: utf-8 -*-
"""全面对比 Amiya.pck vs 可用的 NecrobinderCardPortraits.pck。"""
import struct

NB = r"D:\SteamLibrary\steamapps\workshop\content\2868840\3749627968\NecrobinderCardPortraits.pck"
MY = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya\Amiya.pck"


def parse(pck):
    data = open(pck, "rb").read()
    raw_ver = struct.unpack_from("<IIIII", data, 4)
    file_base, dir_off = struct.unpack_from("<QQ", data, 24)
    n = struct.unpack_from("<I", data, dir_off)[0]
    pos = dir_off + 4
    entries = {}
    for i in range(n):
        plen = struct.unpack_from("<I", data, pos)[0]
        pos += 4
        path = data[pos:pos + plen].decode("utf8", "replace")
        pos += plen
        p1 = pos
        while pos % 4:
            pos += 1
        off, size = struct.unpack_from("<QQ", data, pos)
        pos += 16
        md5 = data[pos:pos + 16]
        pos += 16
        flags = struct.unpack_from("<I", data, pos)[0]
        pos += 4
        entries[path] = (off, size, md5, flags, p1)
    return data, raw_ver, file_base, dir_off, entries


nb_data, nb_ver, nb_base, nb_dir, nb_ent = parse(NB)
my_data, my_ver, my_base, my_dir, my_ent = parse(MY)
print("NB ver", nb_ver, "base", nb_base, "dir", nb_dir)
print("MY ver", my_ver, "base", my_base, "dir", my_dir)

print("\n=== NB full dir ===")
for p, (o, s, m, f, p1) in sorted(nb_ent.items()):
    print("%10d %-75s md5=%s flags=%d" % (s, p.rstrip("\x00"), m.hex()[:8], f))
print("\n=== MY full dir ===")
for p, (o, s, m, f, p1) in sorted(my_ent.items()):
    print("%10d %-75s md5=%s flags=%d" % (s, p.rstrip("\x00"), m.hex()[:8], f))


def nb_blob(path):
    key = path
    if key not in nb_ent:
        key = [k for k in nb_ent if k.rstrip("\x00") == path][0]
    o, s, m, f, p1 = nb_ent[key]
    return nb_data[nb_base + o:nb_base + o + s]


# 找到 NB 的一个侧车（人物图标类），与我们的并排对比
print("\n=== NB sidecar sample ===")
for p in nb_ent:
    if p.rstrip("\x00").endswith(".png.import"):
        print(repr(p), "->")
        print(repr(nb_blob(p).decode("utf8", "replace")))
        break
print("\n=== MY sidecar sample ===")
for p in my_ent:
    if p.rstrip("\x00").endswith(".png.import"):
        print(repr(p), "->")
        print(repr(my_data[my_base + my_ent[p][0]:my_base + my_ent[p][0] + my_ent[p][1]].decode("utf8", "replace")))
        break

# NB ctex 头
print("\n=== NB ctex head ===")
for p in nb_ent:
    if p.rstrip("\x00").endswith(".ctex"):
        b = nb_blob(p)
        print(p.rstrip("\x00"))
        print("df", hex(struct.unpack_from("<I", b, 16)[0]),
              "mipmap_limit", hex(struct.unpack_from("<I", b, 20)[0]),
              "data_format", struct.unpack_from("<I", b, 36)[0],
              "mipmaps", struct.unpack_from("<I", b, 44)[0],
              "format", struct.unpack_from("<I", b, 48)[0],
              "size", struct.unpack_from("<I", b, 52)[0])
        break

# NB project.binary
print("\n=== NB project.binary ===")
print(nb_blob("project.binary")[:120].hex(" "))
# NB uid_cache 前几条 + 总量
print("\n=== NB uid_cache ===")
b = nb_blob(".godot/uid_cache.bin")
m = struct.unpack_from("<I", b, 0)[0]
print("entries", m, "bytes", len(b))
# NB global_script_class_cache
print("NB cfg:", nb_blob(".godot/global_script_class_cache.cfg"))
# NB 有没有 res:// 前缀的条目？
print("NB entries with res:// prefix:", sum(1 for k in nb_ent if k.startswith("res://")))
