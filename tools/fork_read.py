#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""spine-godot 4.2 fork 二进制格式实证读端（镜像 SkeletonBinary.cpp readSkeletonData 全流程）。
用于验证 .spskel 载荷是否与 fork-4.2.43 布局一致。用法: python fork_read.py <spskel> [payload_offset]"""
import struct
import sys


class P:
    def __init__(self, data, off):
        self.d = data
        self.o = off

    def varint(self):
        v = 0
        s = 0
        while True:
            if self.o >= len(self.d):
                raise EOFError(f"varint EOF at {self.o}")
            b = self.d[self.o]
            self.o += 1
            v |= (b & 0x7F) << s
            if not (b & 0x80):
                return v
            s += 7

    def svarint(self):
        v = self.varint()
        return (v >> 1) ^ -(v & 1)

    def byte(self):
        if self.o >= len(self.d):
            raise EOFError
        b = self.d[self.o]
        self.o += 1
        return b

    def boolean(self):
        return self.byte() != 0

    def int_be(self):
        v = 0
        for _ in range(4):
            v = (v << 8) | self.byte()
        return v

    def f32(self):
        return struct.unpack(">f", struct.pack(">I", self.int_be()))[0]

    def string(self):
        n = self.varint()
        if n == 0:
            return None
        s = self.d[self.o:self.o + n - 1]
        self.o += n - 1
        return s.decode("utf-8", "replace")

    def string_ref(self, pool):
        idx = self.varint()
        if idx == 0:
            return None
        if idx > len(pool):
            raise ValueError(f"stringRef {idx} out of pool range {len(pool)} at {self.o}")
        return pool[idx - 1]

    def color(self):
        return tuple(self.byte() for _ in range(4))


def read_vertices(p, weighted):
    vc = p.varint()
    vl = vc * 2
    if not weighted:
        vals = [p.f32() for _ in range(vl)]
        return vl, vals, []
    bones = []
    verts = []
    for _ in range(vc):
        bc = p.varint()
        bones.append(bc)
        for _ in range(bc):
            bones.append(p.varint())
            verts.append(p.f32())
            verts.append(p.f32())
            verts.append(p.f32())
    return vl, verts, bones


def read_bezier(p, nvals):
    return [p.f32() for _ in range(nvals * 4)]


def read_attachment(p, skin, slot_index, att_name, skel, noness):
    flags = p.byte()
    name = p.string_ref(skel["pool"]) if (flags & 8) else att_name
    atype = flags & 0x7
    if atype == 0:  # region
        path = p.string_ref(skel["pool"]) if (flags & 16) else name
        color = p.color() if (flags & 32) else None
        seq = None
        if flags & 64:
            p.varint(); p.varint(); p.varint(); p.varint()
        rotation = p.f32() if (flags & 128) else 0
        x, y, sx, sy, w, h = p.f32(), p.f32(), p.f32(), p.f32(), p.f32(), p.f32()
        return ("region", name, path, color, rotation)
    if atype == 1:  # bounding box
        vl, verts, bones = read_vertices(p, (flags & 16) != 0)
        color = p.color() if noness else None
        return ("bbox", name, vl, color)
    if atype == 2:  # mesh
        path = p.string_ref(skel["pool"]) if (flags & 16) else name
        color = p.color() if (flags & 32) else None
        if flags & 64:
            p.varint(); p.varint(); p.varint(); p.varint()
        hull = p.varint()
        vl, verts, bones = read_vertices(p, (flags & 128) != 0)
        uvs = [p.f32() for _ in range(vl)]
        tri_count = (vl - hull - 2) * 3
        tris = [p.varint() for _ in range(tri_count)]
        if noness:
            ec = p.varint()
            edges = [p.varint() for _ in range(ec)]
            w, h = p.f32(), p.f32()
        return ("mesh", name, path, color, hull, vl, len(tris))
    if atype == 3:  # linked mesh
        path = p.string_ref(skel["pool"]) if (flags & 16) else name
        color = p.color() if (flags & 32) else None
        if flags & 64:
            p.varint(); p.varint(); p.varint(); p.varint()
        inherit = (flags & 128) != 0
        skin_index = p.varint()
        parent = p.string_ref(skel["pool"])
        if noness:
            p.f32(); p.f32()
        return ("linkedmesh", name, path, skin_index, parent)
    if atype == 4:  # path
        closed = (flags & 16) != 0
        constant = (flags & 32) != 0
        vl, verts, bones = read_vertices(p, (flags & 64) != 0)
        lengths = [p.f32() for _ in range(vl // 6)]
        color = p.color() if noness else None
        return ("path", name, vl, closed, constant, color)
    if atype == 5:  # point
        rot, x, y = p.f32(), p.f32(), p.f32()
        color = p.color() if noness else None
        return ("point", name, rot, x, y, color)
    if atype == 6:  # clipping
        end_slot = p.varint()
        vl, verts, bones = read_vertices(p, (flags & 16) != 0)
        color = p.color() if noness else None
        return ("clipping", name, end_slot, vl, color)
    raise ValueError(f"bad attachment type {atype} at {p.o}")


def read_skin(p, default, skel, noness):
    if default:
        slot_count = p.varint()
        if slot_count == 0:
            return None
        name = "default"
    else:
        name = p.string()
        if noness:
            p.color()
        for _ in range(5):  # bones, ik, transform, path, physics
            n = p.varint()
            for _ in range(n):
                p.varint()
        slot_count = p.varint()
    slots = []
    for _ in range(slot_count):
        slot_index = p.varint()
        atts = []
        for _ in range(p.varint()):
            aname = p.string_ref(skel["pool"])
            att = read_attachment(p, None, slot_index, aname, skel, noness)
            atts.append((aname, att[0] if att else None))
        slots.append((slot_index, atts))
    return (name, slots)


def read_anim(p, skel):
    name = p.string()
    p.varint()  # numTimelines (unused)
    info = {"name": name, "start": p.o}
    # slot timelines
    for _ in range(p.varint()):
        slot_index = p.varint()
        for _ in range(p.varint()):
            ttype = p.byte()
            fc = p.varint()
            if ttype > 5:
                raise ValueError(f"anim {name!r} slot {slot_index} bad timeline type {ttype} "
                                 f"fc={fc} at {p.o}, bytes={p.d[p.o-12:p.o+16].hex()}")
            if ttype == 0:  # attachment
                for f in range(fc):
                    p.f32()
                    p.string_ref(skel["pool"])
            elif ttype in (1, 2, 3, 4, 5):
                p.varint()  # bezier count
                nvals = {1: 4, 2: 3, 3: 7, 4: 6, 5: 1}[ttype]
                # first frame
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
    print(f"  [{name}] slots done @ {p.o}")
    # bone timelines
    for _ in range(p.varint()):
        bone_index = p.varint()
        for _ in range(p.varint()):
            ttype = p.byte()
            fc = p.varint()
            if ttype == 10:  # inherit
                for _ in range(fc):
                    p.f32()
                    p.byte()
                continue
            p.varint()  # bezier count
            if ttype in (1, 4, 7):  # 2-value
                p.f32(); p.f32(); p.f32()
                for f in range(fc - 1):
                    p.f32(); p.f32(); p.f32()
                    c = p.byte()
                    if c == 2:
                        read_bezier(p, 2)
            elif ttype in (0, 2, 3, 5, 6, 8, 9):  # 1-value
                p.f32(); p.f32()
                for f in range(fc - 1):
                    p.f32(); p.f32()
                    c = p.byte()
                    if c == 2:
                        read_bezier(p, 1)
            else:
                raise ValueError(f"bad bone timeline type {ttype} at {p.o}")
    print(f"  [{name}] bones done @ {p.o}")
    # ik timelines
    for _ in range(p.varint()):
        p.varint()
        fc = p.varint()
        p.varint()  # bezier count
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
    print(f"  [{name}] ik done @ {p.o}")
    # transform timelines
    for _ in range(p.varint()):
        p.varint()
        fc = p.varint()
        p.varint()
        p.f32()  # time
        for _ in range(6):
            p.f32()
        for f in range(fc - 1):
            p.f32()  # time2
            for _ in range(6):
                p.f32()
            c = p.byte()
            if c == 2:
                read_bezier(p, 6)
    print(f"  [{name}] transform done @ {p.o}")
    # path timelines
    for _ in range(p.varint()):
        p.varint()
        for _ in range(p.varint()):
            ttype = p.byte()
            fc = p.varint()
            p.varint()
            if ttype in (0, 1):
                p.f32(); p.f32()
                for f in range(fc - 1):
                    p.f32(); p.f32()
                    c = p.byte()
                    if c == 2:
                        read_bezier(p, 1)
            elif ttype == 2:
                p.f32(); p.f32(); p.f32(); p.f32()
                for f in range(fc - 1):
                    p.f32(); p.f32(); p.f32(); p.f32()
                    c = p.byte()
                    if c == 2:
                        read_bezier(p, 3)
    print(f"  [{name}] path done @ {p.o}")
    # physics timelines
    for _ in range(p.varint()):
        p.varint()
        for _ in range(p.varint()):
            ttype = p.byte()
            fc = p.varint()
            if ttype == 8:
                for _ in range(fc):
                    p.f32()
                continue
            p.varint()
            p.f32(); p.f32()
            for f in range(fc - 1):
                p.f32(); p.f32()
                c = p.byte()
                if c == 2:
                    read_bezier(p, 1)
    print(f"  [{name}] physics done @ {p.o}")
    # attachment timelines (deform/sequence)
    seq_count = 0
    for _ in range(p.varint()):
        p.varint()  # skin index
        for _ in range(p.varint()):
            p.varint()  # slot index
            for _ in range(p.varint()):
                p.string_ref(skel["pool"])
                ttype = p.byte()
                fc = p.varint()
                if ttype == 0:  # deform
                    p.varint()  # bezier count
                    p.f32()  # 首帧 time（循环外一次）
                    for f in range(fc):
                        end = p.varint()
                        if end:
                            start = p.varint()
                            for _ in range(end):
                                p.f32()
                        if f < fc - 1:
                            p.f32()  # time2
                            c = p.byte()
                            if c == 2:
                                read_bezier(p, 1)
                elif ttype == 1:
                    seq_count += 1
                    for _ in range(fc):
                        p.f32()
                        p.int_be()
                        p.f32()
    # draw order
    print(f"  [{name}] attachment done @ {p.o} (seq={seq_count})")
    dc = p.varint()
    if dc > 0:
        for _ in range(dc):
            p.f32()
            oc = p.varint()
            for _ in range(oc):
                p.varint()
                p.varint()
    # events
    ec = p.varint()
    if ec > 0:
        for _ in range(ec):
            p.f32()
            eidx = p.varint()
            p.svarint()
            p.f32()
            p.string()
            if skel["ev_audio"][eidx]:
                p.f32()
                p.f32()
    return info


def main():
    path = sys.argv[1]
    off = int(sys.argv[2], 0) if len(sys.argv) > 2 else None
    data = open(path, "rb").read()
    if off is None:
        # 自动定位载荷：找 \x07"4.2.43" 前的 8 字节 hash
        i = data.find(b"4.2.43")
        off = i - 9  # varint 07 + hash 8
        print(f"auto offset: {off}")
    p = P(data, off)
    low = p.int_be()
    high = p.int_be()
    version = p.string()
    print(f"hash: {high:x}{low:x}  version: {version!r}")
    x, y, w, h = p.f32(), p.f32(), p.f32(), p.f32()
    ref = p.f32()
    print(f"bounds: x={x} y={y} w={w} h={h} refScale={ref}")
    noness = p.boolean()
    if noness:
        fps = p.f32()
        img = p.string()
        aud = p.string()
        print(f"noness: fps={fps} images={img!r} audio={aud!r}")
    n = p.varint()
    pool = [p.string() for _ in range(n)]
    print(f"pool: {n} strings, first={pool[:4]}")
    skel = {"pool": pool}
    nb = p.varint()
    bones = []
    for i in range(nb):
        nm = p.string()
        if i > 0:
            p.varint()
        for _ in range(8):
            p.f32()
        p.varint()
        p.boolean()
        if noness:
            p.color()
            p.string()
            p.boolean()
        bones.append(nm)
    print(f"bones: {nb}, first={bones[:4]}")
    ns = p.varint()
    slots = []
    for i in range(ns):
        nm = p.string()
        p.varint()
        p.color()
        p.byte(); p.byte(); p.byte(); p.byte()
        att = p.string_ref(pool)
        p.varint()
        if noness:
            p.boolean()
        slots.append(nm)
    print(f"slots: {ns}, first={slots[:4]}")
    # ik
    for _ in range(p.varint()):
        p.string()
        p.varint()
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
    # transform
    for _ in range(p.varint()):
        p.string()
        p.varint()
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
    # path constraints
    for _ in range(p.varint()):
        p.string()
        p.varint()
        p.boolean()
        n2 = p.varint()
        for _ in range(n2):
            p.varint()
        p.varint()
        fl = p.byte()
        if fl & 128:
            p.f32()
        p.f32()
        p.f32()
        p.f32()
        p.f32()
        p.f32()
    # physics constraints
    for _ in range(p.varint()):
        p.string()
        p.varint()
        p.varint()
        fl = p.byte()
        for bit in (2, 4, 8, 16, 32):
            if fl & bit:
                p.f32()
        if fl & 64:
            p.f32()
        p.byte()
        p.f32()
        p.f32()
        p.f32()
        if fl & 128:
            p.f32()
        p.f32()
        p.f32()
        fl = p.byte()
        if fl & 128:
            p.f32()
    print(f"constraints done @ {p.o}")
    ds = read_skin(p, True, skel, noness)
    print(f"default skin: {ds[0] if ds else None} slots={len(ds[1]) if ds else 0}")
    nsk = p.varint()
    skins = []
    for _ in range(nsk):
        skins.append(read_skin(p, False, skel, noness))
    print(f"skins: {nsk}, names={[s[0] for s in skins]}")
    # events
    ne = p.varint()
    evs = []
    ev_audio = []
    for _ in range(ne):
        nm = p.string()
        p.svarint()
        p.f32()
        sv = p.string()
        ap = p.string()
        has_audio = bool(ap)
        if has_audio:
            p.f32()
            p.f32()
        evs.append(nm)
        ev_audio.append(has_audio)
    print(f"events: {evs}")
    skel["ev_audio"] = ev_audio
    na = p.varint()
    anims = []
    for _ in range(na):
        a = read_anim(p, skel)
        anims.append(a)
        print(f"  anim {a['name']!r}: {a['start']} -> {p.o}")
    print(f"animations: {[a['name'] for a in anims]}")
    print(f"FINAL offset: {p.o} / {len(data)}  remaining={len(data) - p.o}")


if __name__ == "__main__":
    main()
