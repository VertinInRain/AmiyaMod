"""为「效果是自定义、却在借用原版图标」的状态生成黑色几何图标（128×128，透明底）。

风格沿用之前的手绘图标：黑色主体 + 白色内芯，形状尽量能被一眼看懂。
产出到 images/custom_powers/<力量类名>.png（由 tools/build_boss_pck.py 打进 Amiya_boss.pck）。
"""
import os
from PIL import Image, ImageDraw

OUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "images", "custom_powers")

BLACK = (12, 12, 12, 255)
WHITE = (245, 245, 245, 255)
S = 128


def canvas():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def invincible():
    """无敌：六边形护盾（中空六边形）。"""
    img, d = canvas()
    hexa = [(64, 4), (118, 36), (118, 92), (64, 124), (10, 92), (10, 36)]
    inner = [(64, 26), (99, 47), (99, 81), (64, 102), (29, 81), (29, 47)]
    d.polygon(hexa, fill=BLACK)
    d.polygon(inner, fill=WHITE)
    return img


def fade():
    """飘忽：向下的箭头落进方框（牌加入手牌）。"""
    img, d = canvas()
    d.polygon([(64, 6), (98, 46), (76, 46), (76, 66), (52, 66), (52, 46), (30, 46)], fill=BLACK)
    d.rectangle([18, 76, 110, 122], outline=BLACK, width=10)
    d.rectangle([36, 92, 92, 110], fill=WHITE)
    return img


def terminus():
    """终点：沙漏（上下两个三角）。"""
    img, d = canvas()
    d.polygon([(18, 8), (110, 8), (64, 64)], fill=BLACK)
    d.polygon([(18, 120), (110, 120), (64, 64)], fill=BLACK)
    d.polygon([(38, 20), (90, 20), (64, 52)], fill=WHITE)
    d.polygon([(38, 108), (90, 108), (64, 76)], fill=WHITE)
    return img


def nursing():
    """育苗：种子 + 两片新芽。"""
    img, d = canvas()
    d.ellipse([44, 82, 84, 122], fill=BLACK)
    d.ellipse([54, 92, 74, 112], fill=WHITE)
    d.line([(64, 84), (64, 44)], fill=BLACK, width=10)
    d.ellipse([24, 24, 62, 54], fill=BLACK)
    d.ellipse([66, 24, 104, 54], fill=BLACK)
    return img


def hand_limit():
    """手牌上限变化：手牌方框 + 上下双向箭头。"""
    img, d = canvas()
    d.rectangle([10, 34, 118, 118], outline=BLACK, width=10)
    d.polygon([(42, 44), (60, 26), (42, 8)], fill=BLACK)
    d.polygon([(42, 44), (60, 26), (42, 8)], fill=BLACK)
    d.polygon([(86, 84), (104, 102), (86, 120)], fill=BLACK)
    d.polygon([(86, 84), (104, 102), (86, 120)], fill=BLACK)
    d.rectangle([52, 62, 76, 78], fill=BLACK)
    return img


def buried():
    """埋于地下：地面横线 + 向下箭头。"""
    img, d = canvas()
    d.polygon([(64, 8), (104, 58), (80, 58), (80, 88), (48, 88), (48, 58), (24, 58)], fill=BLACK)
    d.rectangle([8, 100, 120, 118], fill=BLACK)
    d.rectangle([36, 100, 92, 106], fill=WHITE)
    return img


def horizon_flame():
    """地平线火焰云：地平线 + 火焰三角。"""
    img, d = canvas()
    d.polygon([(64, 8), (112, 92), (16, 92)], fill=BLACK)
    d.polygon([(64, 38), (92, 84), (36, 84)], fill=WHITE)
    d.rectangle([6, 100, 122, 116], fill=BLACK)
    return img


def prophecy():
    """预言影像：菱形外框 + 内部圆（镜头/幻象）。"""
    img, d = canvas()
    d.polygon([(64, 4), (124, 64), (64, 124), (4, 64)], fill=BLACK)
    d.ellipse([34, 34, 94, 94], fill=WHITE)
    d.ellipse([52, 52, 76, 76], fill=BLACK)
    return img


def see_light():
    """见光：太阳（圆 + 射线）。"""
    img, d = canvas()
    for angle in ((0, 8, 22, 8), (0, 120, 22, 120), (8, 0, 8, 22), (120, 0, 120, 22),
                  (14, 14, 28, 28), (114, 14, 100, 28), (14, 114, 28, 100), (114, 114, 100, 100)):
        d.line([(angle[0] + 0, angle[1] + 0), (angle[2] + 0, angle[3] + 0)], fill=BLACK, width=9)
    d.ellipse([30, 30, 98, 98], fill=BLACK)
    d.ellipse([46, 46, 82, 82], fill=WHITE)
    return img


ICONS = {
    "KresonInvinciblePower": invincible,
    "KresonFadePower": fade,
    "KresonTerminusPower": terminus,
    "KresonNursingPower": nursing,
    "HandLimitReductionPower": hand_limit,
    "BuriedUndergroundPower": buried,
    "HorizonFireCloudPower": horizon_flame,
    "ProphecyImagePower": prophecy,
    "SeeLightPower": see_light,
}


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for name, maker in ICONS.items():
        path = os.path.join(OUT_DIR, name + ".png")
        maker().save(path)
        print("icon %-26s -> %s (%d bytes)" % (name, os.path.relpath(path), os.path.getsize(path)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
