#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""列出 hurt(Interact) 所有附件切换的时刻与附件类型。"""
import struct
import sys
from collections import Counter

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]

sd = SkeletonBinary().read_skeleton_data(open(sys.argv[1], "rb").read())
anim = {a.name: a for a in sd.animations}[sys.argv[2] if len(sys.argv) > 2 else "Interact"]

def att_class(name):
    if name is None:
        return "None"
    for skin in [sd.default_skin] + list(sd.skins):
        if not skin:
            continue
        for e in skin.attachments.values():
            if e.name == name:
                return e.attachment.__class__.__name__
    return "?"

switches = []
for t in anim.timelines:
    if t.__class__.__name__ != "AttachmentTimeline":
        continue
    for i in range(len(t.frames)):
        switches.append((t.frames[i], t.slot_index, t.attachment_names[i], att_class(t.attachment_names[i])))
switches.sort()
by_time = Counter(round(s[0], 2) for s in switches)
print("切换时刻分布:", dict(by_time))
print("0.5s 内的切换:")
for s in switches:
    if s[0] <= 0.5:
        print("  t=%.3f slot %d: %r (%s)" % s)
# 网格切换
mesh_switches = [s for s in switches if s[3] == "MeshAttachment"]
print("网格附件切换:", len(mesh_switches), "个")
for s in mesh_switches[:8]:
    print("  t=%.3f slot %d: %s" % (s[0], s[1], s[2]))
