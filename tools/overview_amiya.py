#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""概览阿米娅骨架数据的 fork 写出相关特征。"""
import sys

from spine_asset.v38 import SkeletonBinary

path = sys.argv[1] if len(sys.argv) > 1 else r"阿米娅spine资源\1037_amiya3_sale#13\build_char_1037_amiya3_sale#13.skel"
data = open(path, "rb").read()
sd = SkeletonBinary().read_skeleton_data(data)

print("bounds x,y,w,h:", getattr(sd, "x", None), getattr(sd, "y", None),
      getattr(sd, "width", None), getattr(sd, "height", None))
print("bones:", len(sd.bones), "slots:", len(sd.slots))
print("ik constraints:", len(sd.ik_constraints))
print("transform constraints:", len(sd.transform_constraints))
print("path constraints:", len(sd.path_constraints))
print("events:", [(e.name, e.int_value, e.float_value, repr(e.string_value)) for e in sd.events])
print("default skin:", sd.default_skin.name if sd.default_skin else None)
print("skins:", [s.name for s in sd.skins])

# 附件类型统计
from collections import Counter
c = Counter()
skin_entries = {}
for skin in ([sd.default_skin] + list(sd.skins)):
    if not skin:
        continue
    for e in skin.attachments.values():
        c[e.attachment.__class__.__name__] += 1
print("attachments by type:", dict(c))

# 动画时间线统计
for anim in sd.animations:
    kinds = Counter(t.__class__.__name__ for t in anim.timelines)
    print(f"anim {anim.name!r}: {dict(kinds)}")
