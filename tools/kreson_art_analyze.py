"""分析克雷松素材的布局（非白内容区块），用于决定裁剪方案。"""
import numpy as np
from PIL import Image


def analyze(path, label):
    im = Image.open(path)
    rgb = im.convert("RGB")
    a = np.asarray(rgb).astype(np.int16)
    white = (a[:, :, 0] > 240) & (a[:, :, 1] > 240) & (a[:, :, 2] > 240)
    if im.mode == "RGBA":
        alpha = np.asarray(im)[:, :, 3]
        content = alpha > 8
    else:
        content = ~white
    print(f"--- {label} {im.size} mode={im.mode} 内容占比 {content.mean():.3f}")
    if not content.any():
        print("    (空)")
        return
    ys, xs = np.where(content)
    print(f"    内容包围盒 x[{xs.min()},{xs.max()}] y[{ys.min()},{ys.max()}]")

    def runs(arr, thr, minlen):
        out, start = [], None
        for i, v in enumerate(arr):
            if v >= thr and start is None:
                start = i
            elif v < thr and start is not None:
                out.append((start, i - 1))
                start = None
        if start is not None:
            out.append((start, len(arr) - 1))
        return [r for r in out if r[1] - r[0] >= minlen]

    print(f"    列区块(宽>=8%): {runs(content.sum(axis=0), content.shape[0] * 0.02, content.shape[1] // 12)}")
    print(f"    行区块(高>=8%): {runs(content.sum(axis=1), content.shape[1] * 0.02, content.shape[0] // 12)}")


for name in [
    "克雷松-在地图上以及其他地方的显示.png",
    "克雷松-无敌状态.png",
    "克雷松-解除无敌.png",
    "克雷松-死亡.png",
    "锚点.png",
]:
    analyze("补充卡图/" + name, name)
