"""对比两个 pck 的内容清单（路径 / 大小 / md5），用于确认重建没有丢资源或改坏资源。

用法: python tools/pck_compare.py <a.pck> <b.pck>
"""
import struct
import sys

FILE_BASE = 112


def u32(b, off):
    return struct.unpack_from("<I", b, off)[0]


def u64(b, off):
    return struct.unpack_from("<Q", b, off)[0]


def read_entries(pck_path):
    with open(pck_path, "rb") as f:
        data = f.read()
    if data[:4] != b"GDPC":
        raise SystemExit("不是 GDPC pck：%s" % pck_path)
    dir_offset = u64(data, 32)
    count = u32(data, dir_offset)
    cursor = dir_offset + 4
    out = {}
    for _ in range(count):
        plen = u32(data, cursor)
        path = data[cursor + 4:cursor + 4 + plen].decode("utf-8", "replace").rstrip("\x00")
        cursor += 4 + plen
        cursor += (4 - (cursor % 4)) % 4
        offset = u64(data, cursor) + FILE_BASE
        cursor += 8
        size = u64(data, cursor)
        cursor += 8
        md5 = data[cursor:cursor + 16].hex()
        cursor += 16
        cursor += 4
        out[path] = (size, md5)
    return out


def main(a_path, b_path):
    a = read_entries(a_path)
    b = read_entries(b_path)
    only_a = sorted(set(a) - set(b))
    only_b = sorted(set(b) - set(a))
    changed = sorted(p for p in set(a) & set(b) if a[p] != b[p])
    print("A=%s (%d 个文件)" % (a_path, len(a)))
    print("B=%s (%d 个文件)" % (b_path, len(b)))
    print("仅 A 有（%d）:" % len(only_a))
    for p in only_a:
        print("   -", p)
    print("仅 B 有（%d）:" % len(only_b))
    for p in only_b:
        print("   +", p)
    print("内容不同（%d）:" % len(changed))
    for p in changed:
        print("   ~ %-64s %s -> %s" % (p, a[p][1][:8], b[p][1][:8]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1], sys.argv[2]))
