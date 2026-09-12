#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""fork(4.2.43) 骨架的骨骼世界包围盒 —— 仅骨骼段。用法: fork_bbox.py <spskel>"""
import math
import os
import sys
import importlib.util

spec = importlib.util.spec_from_file_location("fork_read", os.path.join(os.path.dirname(os.path.abspath(__file__)), "fork_read.py"))
fr = importlib.util.module_from_spec(spec)
spec.loader.exec_module(fr)
P = fr.P


def fork_bone_bbox(path):
    d = open(path, "rb").read()
    vidx = d.find(b"4.2")
    if vidx < 0:
        raise ValueError("version not found")
    p = P(d, vidx - 8)  # 8 字节零哈希在版本串之前
    for _ in range(8):
        p.byte()
    ver = p.string()
    b_x, b_y, b_w, b_h = p.f32(), p.f32(), p.f32(), p.f32()
    ref = p.f32()
    p.byte()  # nonessential
    n = p.varint()
    pool = [p.string() for _ in range(n)]
    n = p.varint()
    names = []
    parent = []
    rot = []
    bx = []
    by = []
    sx = []
    sy = []
    shx = []
    shy = []
    for i in range(n):
        names.append(p.string())
        parent.append(p.varint() if i > 0 else None)
        rot.append(p.f32())
        bx.append(p.f32())
        by.append(p.f32())
        sx.append(p.f32())
        sy.append(p.f32())
        shx.append(p.f32())
        shy.append(p.f32())
        p.f32()          # length
        p.varint()       # transformMode
        p.byte()         # skinRequired
        # nonessential 时还有 color——此处 noness 通常 0，若为 1 会有偏差
    wx = [0.0] * n
    wy = [0.0] * n
    pa = [1.0] * n
    pb = [0.0] * n
    pc = [0.0] * n
    pd = [1.0] * n
    for i in range(n):
        r = math.radians(rot[i])
        la = math.cos(r + math.radians(shx[i])) * sx[i]
        lb = math.sin(r + math.radians(shx[i])) * sx[i]
        lc = -math.sin(r - math.radians(shy[i])) * sy[i]
        ld = math.cos(r - math.radians(shy[i])) * sy[i]
        if parent[i] is not None:
            q = parent[i]
            wx[i] = pa[q] * bx[i] + pb[q] * by[i] + wx[q]
            wy[i] = pc[q] * bx[i] + pd[q] * by[i] + wy[q]
            pa[i] = pa[q] * la + pb[q] * lc
            pb[i] = pa[q] * lb + pb[q] * ld
            pc[i] = pc[q] * la + pd[q] * lc
            pd[i] = pc[q] * lb + pd[q] * ld
        else:
            wx[i] = bx[i]
            wy[i] = by[i]
            pa[i], pb[i], pc[i], pd[i] = la, lb, lc, ld
    ys = sorted(wy)
    xs = sorted(wx)

    def pct(a, q):
        return a[min(len(a) - 1, int(len(a) * q))]

    print("%s" % path)
    print("  header x,y,w,h,ref = %.0f %.0f %.0f %.0f %.3f" % (b_x, b_y, b_w, b_h, ref))
    print("  x: min=%.0f p5=%.0f med=%.0f p95=%.0f max=%.0f" %
          (xs[0], pct(xs, 0.05), pct(xs, 0.5), pct(xs, 0.95), xs[-1]))
    print("  y: min=%.0f p5=%.0f med=%.0f p95=%.0f max=%.0f" %
          (ys[0], pct(ys, 0.05), pct(ys, 0.5), pct(ys, 0.95), ys[-1]))
    return names, wx, wy


if __name__ == "__main__":
    for path in sys.argv[1:]:
        fork_bone_bbox(path)
