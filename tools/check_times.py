#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""按各类步长检查 hurt 时间线帧时间的单调性。"""
import struct
import sys
from collections import Counter

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]

STRIDE = {
    "ColorTimeline": 5,
    "RotateTimeline": 2,
    "TranslateTimeline": 3,
    "ScaleTimeline": 3,
    "ShearTimeline": 3,
    "IkConstraintTimeline": 6,
    "TransformConstraintTimeline": 5,
    "AttachmentTimeline": 1,
    "DeformTimeline": 1,
    "DrawOrderTimeline": 1,
    "EventTimeline": 1,
}

sd = SkeletonBinary().read_skeleton_data(open(sys.argv[1], "rb").read())
anim = {a.name: a for a in sd.animations}[sys.argv[2] if len(sys.argv) > 2 else "Interact"]

problems = 0
for t in anim.timelines:
    cls = t.__class__.__name__
    stride = STRIDE.get(cls, 1)
    frames = t.frames
    times = [frames[i] for i in range(0, len(frames), stride)]
    if not times:
        continue
    bad = []
    for i in range(1, len(times)):
        if times[i] < times[i - 1]:
            bad.append("non-monotonic @%d (%.4f -> %.4f)" % (i, times[i - 1], times[i]))
        if times[i] == times[i - 1]:
            bad.append("duplicate @%d (%.4f)" % (i, times[i]))
        if times[i] < 0 or times[i] > 1e6:
            bad.append("bad time @%d (%.4f)" % (i, times[i]))
    if bad:
        problems += len(bad)
        if problems <= 8:
            print("%s idx=%s: %s" % (cls, getattr(t, "slot_index", getattr(t, "bone_index", getattr(t, "transform_constraint_index", "?"))),
                                     " | ".join(bad[:2])))
print("问题数:", problems)
