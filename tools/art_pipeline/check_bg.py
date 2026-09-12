# -*- coding: utf-8 -*-
"""从生成的 pck 里取出 bg ctex，解码 WebP，验证：四边为黑、中心有内容、尺寸正确。"""
import io
import struct

from PIL import Image

PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya\Amiya.pck"
data = open(PCK, "rb").read()
fb, do = struct.unpack_from("<QQ", data, 24)
n = struct.unpack_from("<I", data, do)[0]
pos = do + 4
blob = None
for _ in range(n):
    sl = struct.unpack_from("<I", data, pos)[0]
    pos += 4
    p = data[pos:pos + sl].decode("utf8", "replace").rstrip("\x00")
    pos += sl
    off, size = struct.unpack_from("<QQ", data, pos)
    pos += 16 + 16 + 4
    if p == ".godot/imported/bg.png-5523c88dd347d1b7cc617f632b7efdb7.ctex":
        blob = data[fb + off:fb + off + size]
w, h = struct.unpack_from("<II", blob, 8)
fmt = struct.unpack_from("<I", blob, 48)[0]
webp_size = struct.unpack_from("<I", blob, 52)[0]
print("ctex w/h", w, h, "format", fmt, "webp bytes", webp_size)
img = Image.open(io.BytesIO(blob[56:56 + webp_size])).convert("RGB")
print("decoded size", img.size)
px = img.load()
W, H = img.size
print("四角:", px[0, 0], px[W - 1, 0], px[0, H - 1], px[W - 1, H - 1])
print("四边中点:", px[W // 2, 0], px[W // 2, H - 1], px[0, H // 2], px[W - 1, H // 2])
# 中央采样：非黑像素占比 + 平均色
import numpy as np
a = np.asarray(img)
center = a[H // 4:3 * H // 4, W // 4:3 * W // 4]
nonblack = (center.max(axis=2) > 30).mean()
print("中央区域非黑像素占比: %.1f%%" % (nonblack * 100))
print("中央区域平均色:", center.reshape(-1, 3).mean(axis=0).round(1))
# 顶部/底部被裁区域外的左右黑边宽度
row = a[H // 2]
left_black = np.where(row.max(axis=1) > 30)[0]
print("水平中线第一个非黑像素 x =", left_black[0], "最后一个 =", left_black[-1], "(共", len(left_black), "px)")
