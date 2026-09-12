#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""阿米娅 Spine 转换器：读 Arknights 格式 -> 改名映射 -> 写标准 Spine 3.8 二进制。

用法: python convert_spine.py <input.skel> <output.skel> [rename.json]
rename.json 可选: {"源动画名": "目标动画名", ...}；缺省用内置战斗映射（基建小人 -> 战斗动作）。
"""
import json
import sys

from spine_asset.v38 import SkeletonBinary, SkeletonJson, SkeletonData

# 基建小人动画 -> 游戏 CreatureAnimator 期望的战斗动画名
DEFAULT_RENAME = {
    "Default": "idle_loop",
    "Special": "attack",
    "Interact": "hurt",
    "Sleep": "die",
}

def main(path, out, rename_path=None):
    with open(path, "rb") as f:
        data = f.read()
    try:
        sd = SkeletonBinary().read_skeleton_data(data)
    except Exception as e:
        print("SkeletonBinary 解析失败:", e)
        try:
            sd = SkeletonJson().read_skeleton_data(data.decode("utf-8", "replace"))
            print("JSON 解析成功")
        except Exception as e2:
            print("JSON 也失败:", e2)
            sys.exit(1)

    rename = dict(DEFAULT_RENAME)
    if rename_path:
        with open(rename_path, "r", encoding="utf-8") as f:
            rename.update(json.load(f))

    print("骨骼数:", len(sd.bones), "槽位数:", len(sd.slots), "皮肤数:", len(sd.skins), "动画数:", len(sd.animations))
    for a in sd.animations:
        new_name = rename.get(a.name)
        print("  ", a.name, "->", new_name if new_name else "(保留)")
        if new_name:
            a.name = new_name

    from spine_writer import write_skeleton_data
    blob = write_skeleton_data(sd)
    if isinstance(blob, str):
        blob = blob.encode("utf-8")
    with open(out, "wb") as f:
        f.write(blob)
    print("已写出标准 JSON:", out, len(blob), "字节")

    # 往返自检：用 spine-asset 的 JSON 阅读器读回
    from spine_asset.v38 import SkeletonJson
    sd2 = SkeletonJson().read_skeleton_data(blob.decode("utf-8"))
    print("自检: 骨骼", len(sd2.bones), "槽位", len(sd2.slots), "动画", len(sd2.animations))
    if len(sd2.bones) != len(sd.bones) or len(sd2.animations) != len(sd.animations):
        print("!! 自检失败：数量不一致")
        sys.exit(1)
    print("自检通过 ✓")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None)
