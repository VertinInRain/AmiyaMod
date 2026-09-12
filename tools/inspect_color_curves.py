#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""分析 hurt(Interact) 的颜色时间线曲线：帧时间/曲线类型/值。"""
import struct
import sys

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]

sd = SkeletonBinary().read_skeleton_data(open(sys.argv[1], "rb").read())
anim = {a.name: a for a in sd.animations}[sys.argv[2] if len(sys.argv) > 2 else "Interact"]

# 各时间线类型里"有曲线"的帧时间分布
for t in anim.timelines:
    if t.__class__.__name__ != "ColorTimeline":
        continue
    fc = len(t.frames)
    ncurves = (len(t.curves) // 19) if hasattr(t, "curves") else 0
    if ncurves == 0:
        continue
    # 曲线类型
    types = []
    for i in range(min(ncurves, len(t.curves) // 19)):
        types.append(int(t.curves[i * 19]))
    # 帧时间
    times = t.frames
    vals = []
    stride = len(t.frames) // fc if fc else 0
    print("slot %d: frames=%d curves=%d types=%s firstTimes=%s" % (
        t.slot_index, fc, ncurves, types[:8], [round(x, 3) for x in times[:6]]))
    # 若 frames 扁平 [time,r,g,b,a,...] 则展示前 15 个
    print("   raw frames[:15]:", [round(x, 3) for x in t.frames[:15]])
    break
