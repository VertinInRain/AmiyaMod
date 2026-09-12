#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Special 的 EventTimeline 时间 + t=8.2-8.4 附近各类型帧（精确崩溃窗口 8.25-8.33）。"""
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
print("事件数据:", [(e.name, e.int_value, e.float_value, e.string_value) for e in sd.events])
for anim in sd.animations:
    evs = [t for t in anim.timelines if t.__class__.__name__ == "EventTimeline"]
    if evs:
        for t in evs:
            print("%s EventTimeline frames: %s" % (anim.name, [round(x, 3) for x in t.frames]))

anim = {a.name: a for a in sd.animations}["Special"]
LO, HI = 8.20, 8.40
STRIDE = {
    "ColorTimeline": 5, "RotateTimeline": 2, "TranslateTimeline": 3,
    "ScaleTimeline": 3, "ShearTimeline": 3, "IkConstraintTimeline": 6,
    "TransformConstraintTimeline": 5, "AttachmentTimeline": 1,
    "DeformTimeline": 1, "DrawOrderTimeline": 1,
}
print("\nt=8.2-8.4 帧:")
for t in anim.timelines:
    cls = t.__class__.__name__
    stride = STRIDE.get(cls, 1)
    frames = t.frames
    times = [frames[i] for i in range(0, len(frames), stride)]
    hits = [ft for ft in times if LO <= ft <= HI]
    if hits:
        print("  %-28s idx=%-4s times=%s" % (cls, getattr(t, "slot_index", getattr(t, "bone_index", getattr(t, "ik_constraint_index", getattr(t, "transform_constraint_index", "?")))), [round(x, 3) for x in hits]))
print("\nTC 全部时间线:")
for t in anim.timelines:
    if t.__class__.__name__ == "TransformConstraintTimeline":
        print("  TC idx=%s times=%s" % (t.transform_constraint_index, [round(x, 2) for x in t.frames[::5]]))
print("\nIK 全部时间线:")
for t in anim.timelines:
    if t.__class__.__name__ == "IkConstraintTimeline":
        print("  IK idx=%s times=%s" % (t.ik_constraint_index, [round(x, 2) for x in t.frames[::6]]))
