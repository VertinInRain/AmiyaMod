#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""审计 drawOrder 重建的越界/排序风险（fork 读端算法模拟）。"""
import struct
import sys

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]

sd = SkeletonBinary().read_skeleton_data(open(sys.argv[1], "rb").read())
slot_count = len(sd.slots)
total_frames = 0
bad_frames = 0
for anim in sd.animations:
    for tl in anim.timelines:
        if tl.__class__.__name__ != "DrawOrderTimeline":
            continue
        for fi in range(len(tl.frames)):
            total_frames += 1
            order = tl.draw_orders[fi]
            problems = []
            pairs = [(i, order[i] - i) for i in range(len(order))
                     if order[i] != -1 and order[i] != i]
            if pairs != sorted(pairs):
                problems.append("not-sorted")
            for i, off in pairs:
                if i + off < 0 or i + off >= slot_count:
                    problems.append("OOB slot %d off %d -> %d" % (i, off, i + off))
            if len(order) != slot_count:
                problems.append("len %d != slots %d" % (len(order), slot_count))
            if problems:
                bad_frames += 1
                if bad_frames <= 6:
                    print("%s frame %d: %s" % (anim.name, fi, " | ".join(problems)))
print("drawOrder frames: %d, bad: %d" % (total_frames, bad_frames))
