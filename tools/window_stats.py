#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""列出 Special 在 t=8.0-9.0s 的各类型时间线帧数（含曲线），决定剥离目标。"""
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
anim = {a.name: a for a in sd.animations}["Special"]
LO, HI = 8.0, 9.0

STRIDE = {
    "ColorTimeline": 5, "RotateTimeline": 2, "TranslateTimeline": 3,
    "ScaleTimeline": 3, "ShearTimeline": 3, "IkConstraintTimeline": 6,
    "TransformConstraintTimeline": 5, "AttachmentTimeline": 1,
    "DeformTimeline": 1, "DrawOrderTimeline": 1,
}

counts = Counter()
curve_hits = []
for t in anim.timelines:
    cls = t.__class__.__name__
    stride = STRIDE.get(cls, 1)
    frames = t.frames
    times = [frames[i] for i in range(0, len(frames), stride)]
    hits = [ft for ft in times if LO <= ft <= HI]
    if hits:
        counts[cls] += len(hits)
        # 曲线类型（该时间线有曲线且命中区间）
        ncurves = (len(t.curves) // 19) if hasattr(t, "curves") else 0
        if ncurves:
            curve_hits.append((cls, getattr(t, "slot_index", getattr(t, "bone_index", "?")), ncurves, hits))
print("t=8-9s 帧数统计:", dict(counts))
print("带曲线的时间线命中:")
for cls, idx, nc, hits in curve_hits[:12]:
    print("  %s idx=%s curves=%d times=%s" % (cls, idx, nc, [round(x, 3) for x in hits[:4]]))
# TC/IK 的帧时间
for t in anim.timelines:
    cls = t.__class__.__name__
    if cls in ("IkConstraintTimeline", "TransformConstraintTimeline"):
        stride = STRIDE[cls]
        times = [t.frames[i] for i in range(0, len(t.frames), stride)]
        print("%s idx=%s times=%s" % (cls, getattr(t, "ik_constraint_index", getattr(t, "transform_constraint_index", "?")),
                                      [round(x, 2) for x in times]))
