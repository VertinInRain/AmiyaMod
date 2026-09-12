#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""确认 .spskel 内嵌载荷的起点与布局：用标准 3.8 语义（字符串=varint(len+1)+len字节、
hull 顶点数=推导）从候选偏移试探解析，找到能完整走通骨骼/槽位的起点。
用法: python probe_spskel.py <spskel> [version]"""
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
                raise EOFError("EOF at %d" % self.o)
            b = self.d[self.o]
            self.o += 1
            v |= (b & 0x7F) << s
            if not (b & 0x80):
                return v
            s += 7

    def f32(self):
        if self.o + 4 > len(self.d):
            raise EOFError
        v = struct.unpack_from("<f", self.d, self.o)[0]
        self.o += 4
        return v

    def string(self):
        n = self.varint()
        if n == 0:
            return ""
        n -= 1
        if self.o + n > len(self.d):
            raise EOFError("string %d > EOF at %d" % (n, self.o))
        s = self.d[self.o:self.o + n]
        self.o += n
        return s.decode("utf-8", "replace")

    def skip(self, n):
        self.o += n


def probe(path, start, version):
    data = open(path, "rb").read()
    p = P(data, start)
    try:
        ver = p.string()
        if ver != version:
            return None
        x, y, w, h = p.f32(), p.f32(), p.f32(), p.f32()
        noness = p.varint()
        n = p.varint()
        if not (1 <= n <= 2000):
            return None
        names = []
        for i in range(n):
            nm = p.string()
            if i > 0:
                p.varint()
            for _ in range(8):
                p.f32()
            p.varint()
            names.append(nm)
        ns = p.varint()
        if not (1 <= ns <= 2000):
            return None
        slotnames = []
        for _ in range(min(ns, 3)):
            nm = p.string()
            p.varint()
            p.skip(4)
            att = p.string()
            p.varint()
            slotnames.append(nm)
        return dict(ver=ver, bounds=(x, y, w, h), noness=noness,
                    bones=n, first_bones=names[:5], slots=ns, first_slots=slotnames,
                    offset=start, end=p.o)
    except Exception:
        return None


def main():
    path = sys.argv[1]
    version = sys.argv[2] if len(sys.argv) > 2 else "4.2.43"
    data = open(path, "rb").read()
    idx = data.find(version.encode())
    print(f"{version} at {idx}")
    found = []
    for start in range(max(0, idx - 20), idx + 20):
        r = probe(path, start, version)
        if r:
            found.append(r)
            print("OK", r)
    if not found:
        print("no candidate parsed")
    else:
        print("candidates:", len(found))


if __name__ == "__main__":
    main()
