#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""检查 Default 动画第 0 帧的附件时间线状态 + 皮肤每槽条目数。"""
import struct
import sys

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]

for path in sys.argv[1:]:
    sd = SkeletonBinary().read_skeleton_data(open(path, "rb").read())
    print("===== %s =====" % path.split("\\")[-1])
    # 皮肤每槽条目数分布
    skin = sd.skins[0] if sd.skins else None
    from collections import Counter, defaultdict
    by_slot = defaultdict(list)
    if skin is not None:
        for e in skin.attachments:
            by_slot[e.slot_index].append(e.name)
    multi = {i: names for i, names in by_slot.items() if len(names) > 1}
    print("皮肤: 槽总数=%d, 多条目槽=%d" % (len(by_slot), len(multi)))
    for i, names in list(multi.items())[:12]:
        print("  slot %d (%s): %s" % (i, sd.slots[i].name, names[:6]))
    # Default 动画附件时间线第 0 帧
    anim = None
    for a in sd.animations:
        if a.name == "Default" or a.name == "A_Default":
            anim = a
            break
    if anim is None:
        print("无 Default 动画"); continue
    null0 = 0
    named0 = 0
    total = 0
    hand_like = []
    for tl in anim.timelines:
        if tl.__class__.__name__ == "AttachmentTimeline":
            total += 1
            name0 = tl.attachment_names[0]
            slot = sd.slots[tl.slot_index]
            if not name0:
                null0 += 1
            else:
                named0 += 1
            if "hand" in slot.name.lower() or "arm" in slot.name.lower():
                hand_like.append((slot.name, name0, len(tl.attachment_names), len(set(tl.attachment_names))))
    print("附件时间线: 总数=%d, 第0帧=NULL 的槽=%d, 有名字的槽=%d" % (total, null0, named0))
    print("名字含 hand/arm 的槽:")
    for h in hand_like:
        print("  slot=%s 帧0=%r 帧数=%d 不同附件数=%d" % h)
