#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""列出 Special(attack) 在 t=7.5-9.5s 的事件：附件切换/deform 帧/曲线/IK/变换。"""
import struct
import sys

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]

sd = SkeletonBinary().read_skeleton_data(open(sys.argv[1], "rb").read())
anim = {a.name: a for a in sd.animations}["Special"]
LO, HI = 7.5, 9.5

def att_class(name):
    if name is None:
        return None
    for skin in [sd.default_skin] + list(sd.skins):
        if not skin:
            continue
        for e in skin.attachments.values():
            if e.name == name:
                return e.attachment.__class__.__name__
    return "?"

STRIDE = {
    "ColorTimeline": 5, "RotateTimeline": 2, "TranslateTimeline": 3,
    "ScaleTimeline": 3, "ShearTimeline": 3, "IkConstraintTimeline": 6,
    "TransformConstraintTimeline": 5, "AttachmentTimeline": 1,
    "DeformTimeline": 1, "DrawOrderTimeline": 1,
}

print("=== Special t in [%.1f, %.1f] 的事件 ===" % (LO, HI))
for t in anim.timelines:
    cls = t.__class__.__name__
    stride = STRIDE.get(cls, 1)
    frames = t.frames
    times = [frames[i] for i in range(0, len(frames), stride)]
    for i, ft in enumerate(times):
        if LO <= ft <= HI:
            if cls == "AttachmentTimeline":
                nm = t.attachment_names[i]
                print("  t=%.3f Attachment slot %d -> %r (%s)" % (ft, t.slot_index, nm, att_class(nm)))
            elif cls == "DeformTimeline":
                nv = len(t.frame_vertices[i]) if i < len(t.frame_vertices) else -1
                print("  t=%.3f Deform slot %d att=%s nverts=%d" % (ft, t.slot_index, t.attachment.name, nv))
            elif cls == "IkConstraintTimeline":
                print("  t=%.3f IK %d mix=%.2f soft=%.2f bend=%s" % (ft, t.ik_constraint_index,
                      frames[i * stride + 1], frames[i * stride + 2], frames[i * stride + 3]))
            elif cls == "TransformConstraintTimeline":
                print("  t=%.3f TC %d mixes=%s" % (ft, t.transform_constraint_index,
                      [round(x, 2) for x in frames[i * stride + 1: i * stride + 5]]))
            elif cls in ("ColorTimeline", "RotateTimeline", "TranslateTimeline", "ScaleTimeline", "ShearTimeline"):
                pass  # 太多，不打印
            else:
                print("  t=%.3f %s idx=%s" % (ft, cls, getattr(t, "slot_index", "?")))
