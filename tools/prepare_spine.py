#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""准备阿米娅 Spine 资源：去除 skel 的 28 字节哈希包装，改名无 # 的文件，改 atlas 引用。"""
import os
import shutil
import sys

SRC = r"阿米娅spine资源\1037_amiya3_sale#13"
OUT = r"AmiyaMod\assets\spine\amiya3"

def main():
    os.makedirs(OUT, exist_ok=True)
    src_skel = os.path.join(SRC, "build_char_1037_amiya3_sale#13.skel")
    src_atlas = os.path.join(SRC, "build_char_1037_amiya3_sale#13.atlas")
    src_png = os.path.join(SRC, "build_char_1037_amiya3_sale#13.png")
    with open(src_skel, "rb") as f:
        data = f.read()
    prefix_len = data[0]
    body = data[prefix_len:]
    print("skel 原长:", len(data), "包装长度:", prefix_len, "去除后:", len(body))
    print("包装串:", data[1:prefix_len].decode("utf-8", "replace"))
    with open(os.path.join(OUT, "amiya3.skel"), "wb") as f:
        f.write(body)
    # atlas：替换 png 名称为 amiya3.png
    with open(src_atlas, "r", encoding="utf-8") as f:
        atlas = f.read()
    atlas = atlas.replace("build_char_1037_amiya3_sale#13.png", "amiya3.png")
    with open(os.path.join(OUT, "amiya3.atlas"), "w", encoding="utf-8", newline="\n") as f:
        f.write(atlas)
    shutil.copyfile(src_png, os.path.join(OUT, "amiya3.png"))
    print("已输出到:", OUT)

if __name__ == "__main__":
    main()
