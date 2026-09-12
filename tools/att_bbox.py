#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""附件级包围盒：把 skin 里所有 region/mesh 附件变换到世界坐标，统计角点分布。
用法: att_bbox.py <skel...>"""
import math
import struct
import sys

from spine_asset.utils import SkeletonBinaryReader
from spine_asset.v38 import SkeletonBinary

SkeletonBinaryReader.read_float32 = lambda self: struct.unpack(">f", self._stream.read(4))[0]
SkeletonBinaryReader.read_int32 = lambda self: struct.unpack(">i", self._stream.read(4))[0]
SkeletonBinaryReader.read_int16 = lambda self: struct.unpack(">h", self._stream.read(2))[0]
SkeletonBinaryReader.read_uint32 = lambda self: struct.unpack(">I", self._stream.read(4))[0]


def analyze(path):
    sd = SkeletonBinary().read_skeleton_data(open(path, "rb").read())
    n = len(sd.bones)
    pa = [1.0] * n; pb = [0.0] * n; pc = [0.0] * n; pd = [1.0] * n
    wx = [0.0] * n; wy = [0.0] * n
    for i, b in enumerate(sd.bones):
        r = math.radians(b.rotation)
        la = math.cos(r) * b.scale_x
        lb = math.sin(r) * b.scale_x
        lc = -math.sin(r) * b.scale_y
        ld = math.cos(r) * b.scale_y
        if b.parent is not None:
            q = b.parent.index
            wx[i] = pa[q] * b.x + pb[q] * b.y + wx[q]
            wy[i] = pc[q] * b.x + pd[q] * b.y + wy[q]
            pa[i] = pa[q] * la + pb[q] * lc
            pb[i] = pa[q] * lb + pb[q] * ld
            pc[i] = pc[q] * la + pd[q] * lc
            pd[i] = pc[q] * lb + pd[q] * ld
        else:
            wx[i] = b.x
            wy[i] = b.y
            pa[i], pb[i], pc[i], pd[i] = la, lb, lc, ld

    xs = []
    ys = []
    for skin in sd.skins:
        for entry in skin.attachments:
            att = entry.attachment
            bone = att.bone if hasattr(att, "bone") and att.bone is not None else None
            bi = bone.index if bone is not None else 0
            if att.__class__.__name__ == "RegionAttachment":
                cx = [att.x, att.x + att.width, att.x, att.x + att.width]
                cy = [att.y, att.y, att.y + att.height, att.y + att.height]
                r = math.radians(att.rotation)
                ca, sa = math.cos(r), math.sin(r)
                pts = []
                for k in range(4):
                    lx = cx[k] * ca - cy[k] * sa
                    ly = cx[k] * sa + cy[k] * ca
                    pts.append((lx * att.scale_x, ly * att.scale_y))
            elif att.__class__.__name__ == "MeshAttachment":
                pts = []
                verts = att.vertices
                for k in range(0, len(verts) // 2 * 2, 2):
                    pts.append((verts[k], verts[k + 1]))
            else:
                continue
            for lx, ly in pts:
                gx = pa[bi] * lx + pb[bi] * ly + wx[bi]
                gy = pc[bi] * lx + pd[bi] * ly + wy[bi]
                xs.append(gx)
                ys.append(gy)
    xs.sort()
    ys.sort()

    def pct(a, q):
        return a[min(len(a) - 1, int(len(a) * q))]

    print("%s" % path.split("\\")[-1])
    print("  角点数=%d" % len(xs))
    print("  x: min=%.0f p5=%.0f med=%.0f p95=%.0f max=%.0f" %
          (xs[0], pct(xs, 0.05), pct(xs, 0.5), pct(xs, 0.95), xs[-1]))
    print("  y: min=%.0f p5=%.0f med=%.0f p95=%.0f max=%.0f" %
          (ys[0], pct(ys, 0.05), pct(ys, 0.5), pct(ys, 0.95), ys[-1]))
    print("  H(p5..p95)=%.0f  脚底(p5 y)=%.0f  中心=(%.0f, %.0f)" %
          (pct(ys, 0.95) - pct(ys, 0.05), pct(ys, 0.05),
           (pct(xs, 0.05) + pct(xs, 0.95)) / 2, (pct(ys, 0.05) + pct(ys, 0.95)) / 2))


if __name__ == "__main__":
    for path in sys.argv[1:]:
        try:
            analyze(path)
        except Exception as e:
            print("%s: ERROR %r" % (path, e))
