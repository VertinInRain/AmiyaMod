#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""对比眼部网格附件的数据特征（顶点数/hull/加权/三角形）。"""
import struct
import sys

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]

sd = SkeletonBinary().read_skeleton_data(open(sys.argv[1], "rb").read())
att = {}
for skin in [sd.default_skin] + list(sd.skins):
    if not skin:
        continue
    for e in skin.attachments.values():
        att.setdefault(e.name, e.attachment)

for name in ["F_L_Eye_A", "F_L_Eye_A_1", "F_L_Eye_A_2", "F_L_Eye_B", "F_L_Eye_D",
             "F_R_Eye_A", "F_R_Eye_A_1", "F_R_Eye_A_2", "F_R_Eye_B", "F_R_Eye_D",
             "F_Mouth_4"]:
    a = att.get(name)
    if a is None:
        print("%s: 不存在" % name)
        continue
    cls = a.__class__.__name__
    if cls == "MeshAttachment":
        vc = len(a.region_uvs) // 2
        weighted = a.bones is not None
        n_bones = sum(1 for x in a.bones if x != 0) if weighted else 0
        print("%s: Mesh vc=%d hull=%d tri=%d weighted=%s bonesListLen=%d uvs0=%.3f,%.3f" % (
            name, vc, a.hull_length // 2, len(a.triangles) // 3, weighted,
            len(a.bones) if weighted else 0, a.region_uvs[0], a.region_uvs[1]))
    else:
        print("%s: %s w=%.1f h=%.1f" % (name, cls, getattr(a, "width", 0), getattr(a, "height", 0)))
