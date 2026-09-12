#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""列出 hurt(Interact) 时间 0 的附件切换（对比 Default），找出 hurt 特有的网格附件。"""
import struct
import sys

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]

sd = SkeletonBinary().read_skeleton_data(open(sys.argv[1], "rb").read())
anims = {a.name: a for a in sd.animations}
default_att = {}
hurt_att = {}

def first_attachments(anim):
    out = {}
    for t in anim.timelines:
        if t.__class__.__name__ != "AttachmentTimeline":
            continue
        if len(t.frames) == 0:
            continue
        name = t.attachment_names[0]
        out[t.slot_index] = (t.frames[0], name)
    return out

def att_class(name):
    for skin in [sd.default_skin] + list(sd.skins):
        if not skin:
            continue
        for e in skin.attachments.values():
            if e.name == name:
                return e.attachment.__class__.__name__
    return None

d = first_attachments(anims["Default"])
h = first_attachments(anims["Interact"])
print("hurt 时间0 切换（与 Default 不同）:")
n = 0
for slot_idx in sorted(h):
    t, name = h[slot_idx]
    prev = d.get(slot_idx)
    if prev is None or prev[1] != name:
        cls = att_class(name)
        print("  slot %d: '%s' (%s)  [default: %s]" % (slot_idx, name, cls,
              (prev[1] + " (" + (att_class(prev[1]) or "?") + ")") if prev else "无"))
        n += 1
print("共 %d 个切换" % n)
# hurt 全部帧的附件切换总数
total_switches = 0
for t in anims["Interact"].timelines:
    if t.__class__.__name__ == "AttachmentTimeline":
        total_switches += len(t.frames)
print("hurt attachment 帧总数:", total_switches)
