#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""计算骨架 setup 姿态的骨骼包围盒（spine 坐标系，y 向上），判断原点在脚底还是中心。"""
import math
import struct
import sys

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]

for path in sys.argv[1:]:
    sd = SkeletonBinary().read_skeleton_data(open(path, "rb").read())
    n = len(sd.bones)
    wx = [0.0] * n
    wy = [0.0] * n
    pa = [1.0] * n; pb = [0.0] * n; pc = [0.0] * n; pd = [1.0] * n
    for i, b in enumerate(sd.bones):
        r = math.radians(b.rotation)
        la = math.cos(r) * b.scale_x
        lb = math.sin(r) * b.scale_x
        lc = -math.sin(r) * b.scale_y
        ld = math.cos(r) * b.scale_y
        if b.parent is not None:
            p = b.parent.index
            wx[i] = pa[p] * b.x + pb[p] * b.y + wx[p]
            wy[i] = pc[p] * b.x + pd[p] * b.y + wy[p]
            pa[i] = pa[p] * la + pb[p] * lc
            pb[i] = pa[p] * lb + pb[p] * ld
            pc[i] = pc[p] * la + pd[p] * lc
            pd[i] = pc[p] * lb + pd[p] * ld
        else:
            wx[i] = b.x
            wy[i] = b.y
            pa[i], pb[i], pc[i], pd[i] = la, lb, lc, ld
    ys = sorted(wy)
    xs = sorted(wx)
    def pct(a, q):
        return a[min(len(a) - 1, int(len(a) * q))]
    print("%s" % path.split("\\")[-1])
    print("  x: min=%.0f p5=%.0f med=%.0f p95=%.0f max=%.0f" %
          (xs[0], pct(xs, 0.05), pct(xs, 0.5), pct(xs, 0.95), xs[-1]))
    print("  y: min=%.0f p5=%.0f med=%.0f p95=%.0f max=%.0f" %
          (ys[0], pct(ys, 0.05), pct(ys, 0.5), pct(ys, 0.95), ys[-1]))
