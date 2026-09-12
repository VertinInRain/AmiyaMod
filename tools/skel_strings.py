#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""扫描 .skel 尾部的 NUL 结尾字符串，提取候选动画名。用法: python skel_strings.py <file.skel>"""
import sys
import re

def main(path):
    with open(path, "rb") as f:
        data = f.read()
    # 动画区在文件后段：从 60% 处开始扫
    tail = data[int(len(data) * 0.6):]
    strings = []
    cur = bytearray()
    for b in tail:
        if b == 0:
            if len(cur) >= 3:
                s = cur.decode("utf-8", "replace")
                strings.append(s)
            cur = bytearray()
        elif 32 <= b < 127:
            cur.append(b)
        else:
            cur = bytearray()
    # 去重保序
    seen = set()
    uniq = []
    for s in strings:
        if s not in seen:
            seen.add(s)
            uniq.append(s)
    print("总字符串数:", len(uniq))
    print("=== 全部（后 200 条）===")
    for s in uniq[-200:]:
        print(repr(s))
    # 猜测常见战斗动画名
    print("=== 疑似动画名（常见战斗关键词）===")
    for s in uniq:
        if re.fullmatch(r"[A-Za-z_0-9 ]{2,30}", s) and re.search(r"idle|attack|die|death|skill|move|relax|hit|hurt|start|win|battle", s, re.I):
            print("  ", repr(s))

if __name__ == "__main__":
    main(sys.argv[1])
