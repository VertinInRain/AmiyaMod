#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""从已构建的 Amiya.pck 里反解出「源图已被删除」的那批资源，存成仓库内的打包源。

背景：build_art_pck.py 依赖「补充卡图」里的一批原图（9 个遗物图标、4 张形态死亡立绘、
商店立绘、尘霾之冠图标），那些图在前面的清理里被删掉了，导致主 pck 无法重建。
这里把 pck 里已经生成好的成品（ctex）解码回 PNG 存到 tools/art_pipeline/sources/，
打包脚本再优先用「补充卡图」，缺失时回退到这些恢复出来的源图。

用法：
  python tools/recover_pck_sources.py --list        # 只列 pck 内所有资源
  python tools/recover_pck_sources.py               # 执行恢复
"""
import io
import os
import struct
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya\Amiya.pck"
OUT_DIR = os.path.join(HERE, "art_pipeline", "sources")
# 与 build_art_pck.py 的 FILE_BASE 一致：目录里的 offset 是相对数据区起点的
FILE_BASE = 112

# 需要恢复的源图：目标文件名 -> pck 内的资源路径（.remap 里指向的源路径）
TARGETS = {
    # 9 个专属遗物图标
    "LongevityProofRelic.png": "res://Amiya/images/relic/LongevityProofRelic.png",
    "HuntRelic.png": "res://Amiya/images/relic/HuntRelic.png",
    "NamelessTotemRelic.png": "res://Amiya/images/relic/NamelessTotemRelic.png",
    "GreatSilenceRelic.png": "res://Amiya/images/relic/GreatSilenceRelic.png",
    "UndyingEndRelic.png": "res://Amiya/images/relic/UndyingEndRelic.png",
    "KingArmorRelic.png": "res://Amiya/images/relic/KingArmorRelic.png",
    "KingExtensionRelic.png": "res://Amiya/images/relic/KingExtensionRelic.png",
    "KingNewGunRelic.png": "res://Amiya/images/relic/KingNewGunRelic.png",
    "BunnyDollRelic.png": "res://Amiya/images/relic/BunnyDollRelic.png",
    # 4 张形态死亡立绘
    "death_guard.png": "res://Amiya/images/death/guard.png",
    "death_caster.png": "res://Amiya/images/death/caster.png",
    "death_medic.png": "res://Amiya/images/death/medic.png",
    "death_demon.png": "res://Amiya/images/death/demon.png",
    # 商店立绘 + 尘霾之冠力量图标
    "merchant.png": "res://Amiya/images/merchant/merchant.png",
    "DustHazeCrownPower.png": "res://Amiya/images/powers/DustHazeCrownPower.png",
}


def u32(b, off):
    return struct.unpack_from("<I", b, off)[0]


def u64(b, off):
    return struct.unpack_from("<Q", b, off)[0]


def read_entries(pck_path):
    with open(pck_path, "rb") as f:
        data = f.read()
    if data[:4] != b"GDPC":
        raise SystemExit("不是 GDPC pck：%r" % data[:4])
    dir_offset = u64(data, 32)
    count = u32(data, dir_offset)
    cursor = dir_offset + 4
    entries = []
    for _ in range(count):
        plen = u32(data, cursor)
        path = data[cursor + 4:cursor + 4 + plen].decode("utf-8", "replace").rstrip("\x00")
        cursor += 4 + plen
        cursor += (4 - (cursor % 4)) % 4
        offset = u64(data, cursor)
        cursor += 8
        size = u64(data, cursor)
        cursor += 8
        cursor += 16  # md5
        cursor += 4   # flags
        entries.append((path, offset + FILE_BASE, size))
    return data, entries


def remap_target(data, entries, source_res):
    """读取 <源路径>.remap，取出它指向的 ctex 内部路径。"""
    remap_name = source_res[len("res://"):] + ".remap"
    for path, offset, size in entries:
        if path == remap_name:
            blob = data[offset:offset + size].decode("utf-8", "replace")
            for line in blob.splitlines():
                if line.startswith("path="):
                    return line.split("=", 1)[1].strip().strip('"')
    return None


def ctex_to_png(blob):
    """GST2 容器（内嵌 WebP）→ PIL Image。"""
    if blob[:4] != b"GST2":
        raise ValueError("不是 GST2 ctex")
    length = u32(blob, 52)
    payload = blob[56:56 + length]
    return Image.open(io.BytesIO(payload)).copy()


def main():
    args = sys.argv[1:]
    data, entries = read_entries(PCK)
    by_path = {p: (o, s) for p, o, s in entries}

    if "--list" in args:
        print("pck 内共 %d 个文件：" % len(entries))
        for p, _, s in sorted(entries):
            print("  %-70s %d" % (p, s))
        return 0

    os.makedirs(OUT_DIR, exist_ok=True)
    ok = 0
    for target_name, source_res in TARGETS.items():
        ctex_inner = remap_target(data, entries, source_res)
        if ctex_inner is None:
            print("跳过（pck 内没有 %s 的 remap）: %s" % (source_res, target_name))
            continue
        inner = ctex_inner[len("res://"):] if ctex_inner.startswith("res://") else ctex_inner
        if inner not in by_path:
            print("跳过（找不到 %s）: %s" % (inner, target_name))
            continue
        offset, size = by_path[inner]
        try:
            img = ctex_to_png(data[offset:offset + size])
        except Exception as exc:  # noqa: BLE001
            print("解码失败 %s: %s" % (target_name, exc))
            continue
        out = os.path.join(OUT_DIR, target_name)
        img.save(out)
        print("恢复 %-28s <- %s (%dx%d, %d bytes)" % (target_name, inner, img.width, img.height, os.path.getsize(out)))
        ok += 1
    print("共恢复 %d / %d 个源图到 %s" % (ok, len(TARGETS), os.path.relpath(OUT_DIR, ROOT)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
