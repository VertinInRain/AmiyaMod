#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""审计 fork 输出：每条时间线比较写出的 bezierCount 与实际 bezier 曲线数（运行时
setBezier 按实际曲线递增，若 bezierCount 偏小会越界写堆 → 延迟崩溃）。"""
import struct
import sys

sys.path.insert(0, "tools")
from fork_read import P, read_bezier


def audit(path):
    data = open(path, "rb").read()
    p = P(data, 0)
    p.int_be(); p.int_be()
    ver = p.string()
    assert ver == "4.2.43", ver
    for _ in range(5):
        p.f32()
    noness = p.boolean()
    if noness:
        p.f32(); p.string(); p.string()
    n = p.varint()
    pool = [p.string() for _ in range(n)]
    skel = {"pool": pool, "ev_audio": []}
    nb = p.varint()
    for i in range(nb):
        p.string()
        if i > 0:
            p.varint()
        for _ in range(8):
            p.f32()
        p.varint()
        p.boolean()
        if noness:
            p.color(); p.string(); p.boolean()
    ns = p.varint()
    for _ in range(ns):
        p.string(); p.varint(); p.color()
        p.byte(); p.byte(); p.byte(); p.byte()
        p.string_ref(pool); p.varint()
        if noness:
            p.boolean()
    for _ in range(p.varint()):
        p.string(); p.varint()
        n2 = p.varint()
        for _ in range(n2):
            p.varint()
        p.varint()
        fl = p.byte()
        if fl & 32:
            if fl & 64:
                p.f32()
        if fl & 128:
            p.f32()
    for _ in range(p.varint()):
        p.string(); p.varint()
        n2 = p.varint()
        for _ in range(n2):
            p.varint()
        p.varint()
        fl = p.byte()
        for bit in (8, 16, 32, 64, 128):
            if fl & bit:
                p.f32()
        fl = p.byte()
        for bit in (1, 2, 4, 8, 16, 32, 64):
            if fl & bit:
                p.f32()
    for _ in range(p.varint()):
        p.string(); p.varint(); p.boolean()
        n2 = p.varint()
        for _ in range(n2):
            p.varint()
        p.varint()
        fl = p.byte()
        if fl & 128:
            p.f32()
        for _ in range(5):
            p.f32()
    for _ in range(p.varint()):
        p.string(); p.varint(); p.varint()
        fl = p.byte()
        for bit in (2, 4, 8, 16, 32):
            if fl & bit:
                p.f32()
        if fl & 64:
            p.f32()
        p.byte()
        p.f32(); p.f32(); p.f32()
        if fl & 128:
            p.f32()
        p.f32(); p.f32()
        fl = p.byte()
        if fl & 128:
            p.f32()
    # default skin + skins（与 fork_read 相同逻辑）
    import fork_read as fr
    fr.read_skin(p, True, skel, noness)
    nsk = p.varint()
    for _ in range(nsk):
        fr.read_skin(p, False, skel, noness)
    ne = p.varint()
    for _ in range(ne):
        p.string(); p.svarint(); p.f32(); p.string()
        ap = p.string()
        skel["ev_audio"].append(bool(ap))
        if ap:
            p.f32(); p.f32()
    na = p.varint()
    problems = []
    for ai in range(na):
        aname = p.string()
        p.varint()
        # slot timelines
        for _ in range(p.varint()):
            slot_index = p.varint()
            for _ in range(p.varint()):
                ttype = p.byte()
                fc = p.varint()
                if ttype == 0:
                    for _ in range(fc):
                        p.f32(); p.string_ref(pool)
                    continue
                bezier_count = p.varint()
                nvals = {1: 4, 2: 3, 3: 7, 4: 6, 5: 1}[ttype]
                actual = 0
                p.f32()
                for _ in range(nvals):
                    p.byte()
                for f in range(fc - 1):
                    p.f32()
                    for _ in range(nvals):
                        p.byte()
                    c = p.byte()
                    if c == 2:
                        read_bezier(p, nvals)
                        actual += 1
                if actual > bezier_count:
                    problems.append((aname, "slot", slot_index, ttype, fc, bezier_count, actual))
        # bone timelines
        for _ in range(p.varint()):
            bone_index = p.varint()
            for _ in range(p.varint()):
                ttype = p.byte()
                fc = p.varint()
                if ttype == 10:
                    for _ in range(fc):
                        p.f32(); p.byte()
                    continue
                bezier_count = p.varint()
                nvals = 2 if ttype in (1, 4, 7) else 1
                actual = 0
                for _ in range(nvals + 1):
                    p.f32()
                for f in range(fc - 1):
                    for _ in range(nvals + 1):
                        p.f32()
                    c = p.byte()
                    if c == 2:
                        read_bezier(p, nvals)
                        actual += 1
                if actual > bezier_count:
                    problems.append((aname, "bone", bone_index, ttype, fc, bezier_count, actual))
        # ik
        for _ in range(p.varint()):
            p.varint()
            fc = p.varint()
            bezier_count = p.varint()
            actual = 0
            flags = p.byte()
            p.f32()
            if (flags & 1) and (flags & 2):
                p.f32()
            if flags & 4:
                p.f32()
            for f in range(fc - 1):
                flags = p.byte()
                p.f32()
                if (flags & 1) and (flags & 2):
                    p.f32()
                if flags & 4:
                    p.f32()
                if flags & 128:
                    read_bezier(p, 2)
                    actual += 1
            if actual > bezier_count:
                problems.append((aname, "ik", 0, 0, fc, bezier_count, actual))
        # transform
        for _ in range(p.varint()):
            p.varint()
            fc = p.varint()
            bezier_count = p.varint()
            actual = 0
            p.f32()
            for _ in range(6):
                p.f32()
            for f in range(fc - 1):
                p.f32()
                for _ in range(6):
                    p.f32()
                c = p.byte()
                if c == 2:
                    read_bezier(p, 6)
                    actual += 1
            if actual > bezier_count:
                problems.append((aname, "transform", 0, 0, fc, bezier_count, actual))
        # path
        for _ in range(p.varint()):
            p.varint()
            for _ in range(p.varint()):
                ttype = p.byte()
                fc = p.varint()
                bezier_count = p.varint()
                nvals = 1 if ttype in (0, 1) else 3
                actual = 0
                for _ in range(nvals + 1):
                    p.f32()
                for f in range(fc - 1):
                    for _ in range(nvals + 1):
                        p.f32()
                    c = p.byte()
                    if c == 2:
                        read_bezier(p, nvals)
                        actual += 1
                if actual > bezier_count:
                    problems.append((aname, "path", 0, ttype, fc, bezier_count, actual))
        # physics
        for _ in range(p.varint()):
            p.varint()
            for _ in range(p.varint()):
                ttype = p.byte()
                fc = p.varint()
                if ttype == 8:
                    for _ in range(fc):
                        p.f32()
                    continue
                bezier_count = p.varint()
                actual = 0
                p.f32(); p.f32()
                for f in range(fc - 1):
                    p.f32(); p.f32()
                    c = p.byte()
                    if c == 2:
                        read_bezier(p, 1)
                        actual += 1
                if actual > bezier_count:
                    problems.append((aname, "physics", 0, ttype, fc, bezier_count, actual))
        # attachment
        for _ in range(p.varint()):
            p.varint()
            for _ in range(p.varint()):
                p.varint()
                for _ in range(p.varint()):
                    p.string_ref(pool)
                    ttype = p.byte()
                    fc = p.varint()
                    if ttype == 0:
                        bezier_count = p.varint()
                        actual = 0
                        p.f32()
                        for f in range(fc):
                            end = p.varint()
                            if end:
                                start = p.varint()
                                for _ in range(end):
                                    p.f32()
                            if f < fc - 1:
                                p.f32()
                                c = p.byte()
                                if c == 2:
                                    read_bezier(p, 1)
                                    actual += 1
                        if actual > bezier_count:
                            problems.append((aname, "deform", 0, 0, fc, bezier_count, actual))
                    else:
                        for _ in range(fc):
                            p.f32(); p.int_be(); p.f32()
        dc = p.varint()
        if dc > 0:
            for _ in range(dc):
                p.f32()
                oc = p.varint()
                for _ in range(oc):
                    p.varint(); p.varint()
        ec = p.varint()
        if ec > 0:
            for _ in range(ec):
                p.f32()
                eidx = p.varint()
                p.svarint(); p.f32(); p.string()
                if skel["ev_audio"][eidx]:
                    p.f32(); p.f32()
    print(f"walked to {p.o}/{len(data)} remaining={len(data) - p.o}")
    print(f"bezierCount < actual 的问题数: {len(problems)}")
    for pr in problems[:10]:
        print("  ", pr)


if __name__ == "__main__":
    audit(sys.argv[1])
