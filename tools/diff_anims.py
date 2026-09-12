#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""对比各动画的时间线帧数/曲线/约束帧，定位 hurt(Interact) 的异常数据。"""
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

def tl_stats(anim):
    out = {}
    for t in anim.timelines:
        name = t.__class__.__name__
        fc = len(t.frames)
        ncurve = 0
        if hasattr(t, "curves"):
            ncurve = len(t.curves) // 19
        out.setdefault(name, []).append((fc, ncurve))
    return out

def first_frames(anim, cls, n=1, k=14):
    for t in anim.timelines:
        if t.__class__.__name__ == cls:
            return t.frames[:k]
    return None

for anim in sd.animations:
    print("== %s (dur=%.3f) ==" % (anim.name, anim.duration))
    st = tl_stats(anim)
    for k, v in sorted(st.items()):
        fcs = Counter(x[0] for x in v)
        ncs = Counter(x[1] for x in v)
        print("   %s x%d: frames=%s curves=%s" % (k, len(v), dict(fcs), dict(ncs)))
    tf = first_frames(anim, "TransformConstraintTimeline")
    ik = first_frames(anim, "IkConstraintTimeline")
    if tf:
        print("   transformTL first frames:", tf[:12])
    if ik:
        print("   ikTL first frames:", ik[:12])
    # deform 帧的偏移量分布
    dl = [len(fv) for t in anim.timelines if t.__class__.__name__ == "DeformTimeline" for fv in t.frame_vertices]
    if dl:
        print("   deform offsets: min=%d max=%d" % (min(dl), max(dl)))
