# -*- coding: utf-8 -*-
"""把 C# 里错误的直连 ctex 路径恢复为源路径 res://Amiya/images/<类>/<名>.png。"""
import os
import re
import sys

SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "AmiyaMod", "src")

RELIC = {"PaleCrownRelic", "PaleBlessingRelic"}
ICON = {"character_icon_amiya", "char_select_amiya", "char_select_amiya_locked"}

PAT = re.compile(r'res://Amiya/\.godot/imported/([A-Za-z0-9_]+)\.png-[0-9a-f]{12}\.ctex')


def category(name):
    if name in RELIC:
        return "relic"
    if name in ICON:
        return "icon"
    return "powers"


changed = 0
for root, _dirs, files in os.walk(SRC):
    for fn in files:
        if not fn.endswith(".cs"):
            continue
        path = os.path.join(root, fn)
        with open(path, "r", encoding="utf-8") as f:
            text = f.read()
        new_text, n = PAT.subn(
            lambda m: "res://Amiya/images/%s/%s.png" % (category(m.group(1)), m.group(1)),
            text)
        if n:
            with open(path, "w", encoding="utf-8") as f:
                f.write(new_text)
            changed += n
            print("%3d  %s" % (n, os.path.relpath(path, SRC)))
print("total %d replacements" % changed)
sys.exit(0 if changed else 1)
