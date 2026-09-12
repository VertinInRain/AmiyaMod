#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""对比各动画 attachment 时间线首帧的附件名（None vs 实际名）。"""
import struct
import sys

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]

sd = SkeletonBinary().read_skeleton_data(open(sys.argv[1], "rb").read())
for anim in sd.animations:
    none_first = 0
    named_first = 0
    multi = 0
    for t in anim.timelines:
        if t.__class__.__name__ != "AttachmentTimeline":
            continue
        if len(t.frames) > 1:
            multi += 1
        if t.attachment_names[0] is None:
            none_first += 1
        else:
            named_first += 1
    print("%s: 首帧 None=%d 首帧有名=%d 多帧时间线=%d" % (anim.name, none_first, named_first, multi))
