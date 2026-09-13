"""克雷松素材处理：抠白底 → 裁内容 → 统一脚底/高度 → 输出到 mod 目录。

产出：
  spine/boss/kreson_invincible.png   无敌状态（战斗立绘）
  spine/boss/kreson_released.png     解除无敌（战斗立绘）
  spine/boss/kreson_dead.png         死亡（战斗立绘）
  spine/boss/kreson_icon.png         地图 boss 节点 / 顶部血条图标（256×256）
  spine/boss/kreson_icon_outline.png 同上的描边版（游戏要 _outline 变体）
  spine/card_art/Anchor.png          锚点卡图（250×190）

三张战斗立绘统一：同一画布尺寸、脚底对齐底部、内容高度 420px。
"""
import os
import shutil
from collections import deque

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SRC = "补充卡图"
REPO_BOSS = "AmiyaMod/assets/boss"
REPO_CARD = "AmiyaMod/assets/card_art"
GAME_MOD = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya"

BATTLE_HEIGHT = 420
ICON_SIZE = 256
CARD_W, CARD_H = 250, 190


def key_white(img: Image.Image, light_min: int = 200, sat_max: int = 34, feather: float = 0.8) -> Image.Image:
    """抠掉「与画面边缘连通的浅色背景」（形态学重建，而不是全局按白色抠，
    这样角色身上的白色细节不会被误伤）。背景若是渐变/带阴影也能吃掉。"""
    rgb = img.convert("RGB")
    arr = np.asarray(rgb).astype(np.int16)
    mn = arr.min(axis=2)
    mx = arr.max(axis=2)
    light = (mn >= light_min) & ((mx - mn) <= sat_max)

    bg = np.zeros_like(light)
    bg[0, :] = light[0, :]
    bg[-1, :] = light[-1, :]
    bg[:, 0] = light[:, 0]
    bg[:, -1] = light[:, -1]
    for _ in range(4000):
        grown = bg.copy()
        grown[1:, :] |= bg[:-1, :]
        grown[:-1, :] |= bg[1:, :]
        grown[:, 1:] |= bg[:, :-1]
        grown[:, :-1] |= bg[:, 1:]
        grown &= light
        if grown.sum() == bg.sum():
            break
        bg = grown

    alpha = np.where(bg, 0, 255).astype(np.uint8)
    if img.mode == "RGBA":
        alpha = np.minimum(alpha, np.asarray(img)[:, :, 3])
    out = img.convert("RGBA")
    out.putalpha(Image.fromarray(alpha))
    if feather > 0:
        soft = out.split()[3].filter(ImageFilter.GaussianBlur(feather))
        out.putalpha(soft)
    return out


def dense_bbox(img: Image.Image, thr: int = 200):
    """不透明实体的包围盒（排除淡淡的辉光/描边），用于统一各状态的实际体型。"""
    alpha = np.asarray(img)[:, :, 3]
    ys, xs = np.where(alpha > thr)
    if len(ys) == 0:
        return (0, 0, img.width, img.height)
    return (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)


def crop_dense(img: Image.Image, frac: float = 0.05) -> Image.Image:
    """裁到「最密的那块内容」（大图里常有多余的辉光/文字，只取主体方块）。"""
    dense = np.asarray(img)[:, :, 3] > 128
    h, w = dense.shape

    def biggest(arr, thr, minlen):
        best, start = None, None
        for i, v in enumerate(arr):
            if v >= thr and start is None:
                start = i
            elif v < thr and start is not None:
                if i - start >= minlen and (best is None or i - start > best[1] - best[0]):
                    best = (start, i - 1)
                start = None
        if start is not None and len(arr) - start >= minlen and (best is None or len(arr) - start > best[1] - best[0]):
            best = (start, len(arr) - 1)
        return best or (0, len(arr) - 1)

    x0, x1 = biggest(dense.sum(axis=0), h * frac, 30)
    y0, y1 = biggest(dense.sum(axis=1), w * frac, 30)
    return img.crop((x0, y0, x1 + 1, y1 + 1))


def crop_content(img: Image.Image, pad: int = 2) -> Image.Image:
    alpha = np.asarray(img)[:, :, 3]
    ys, xs = np.where(alpha > 8)
    x0, x1 = max(int(xs.min()) - pad, 0), min(int(xs.max()) + pad, img.width - 1)
    y0, y1 = max(int(ys.min()) - pad, 0), min(int(ys.max()) + pad, img.height - 1)
    return img.crop((x0, y0, x1 + 1, y1 + 1))


def scale_to_height(img: Image.Image, height: int) -> Image.Image:
    s = height / img.height
    return img.resize((max(1, round(img.width * s)), height), Image.LANCZOS)


def scale_to_dense_height(img: Image.Image, height: int) -> Image.Image:
    """按"实体"高度缩放：让各状态的怪物本体一样高，辉光/特效允许超出画布。"""
    x0, y0, x1, y1 = dense_bbox(img)
    cur = max(1, y1 - y0)
    s = height / cur
    return img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)


def compose_states(states: dict, height: int, pad: int = 24) -> dict:
    """统一画布：本体高度一致、脚底（实体底边）对齐，水平居中。"""
    scaled = {k: scale_to_dense_height(v, height) for k, v in states.items()}
    width = max(im.width for im in scaled.values()) + pad * 2
    canvas_h = height + pad * 2
    out = {}
    for key, im in scaled.items():
        canvas = Image.new("RGBA", (width, canvas_h), (0, 0, 0, 0))
        x0, y0, x1, y1 = dense_bbox(im)
        offset_x = (width - (x0 + x1)) // 2
        offset_y = canvas_h - pad - y1
        canvas.alpha_composite(im, (offset_x, offset_y))
        out[key] = canvas
    return out


def make_icon(img: Image.Image, size: int) -> Image.Image:
    s = min(size / img.width, size / img.height)
    scaled = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(scaled, ((size - scaled.width) // 2, (size - scaled.height) // 2))
    return canvas


def make_outline(icon: Image.Image) -> Image.Image:
    """描边版：本体剪影 + 外描边（地图节点用的 _outline 变体）。"""
    alpha = icon.split()[3]
    grown = alpha.filter(ImageFilter.MaxFilter(9))
    edge = Image.new("RGBA", icon.size, (0, 0, 0, 0))
    edge.putalpha(grown)
    edge = Image.composite(Image.new("RGBA", icon.size, (10, 10, 14, 255)), Image.new("RGBA", icon.size, (0, 0, 0, 0)), grown.point(lambda v: 255 if v > 40 else 0))
    body = Image.composite(Image.new("RGBA", icon.size, (240, 240, 245, 255)), Image.new("RGBA", icon.size, (0, 0, 0, 0)), alpha.point(lambda v: 255 if v > 40 else 0))
    out = Image.new("RGBA", icon.size, (0, 0, 0, 0))
    out.alpha_composite(edge)
    out.alpha_composite(body)
    return out


def save(img: Image.Image, path: str) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print(f"  -> {path}  {img.size}  {os.path.getsize(path) // 1024} KB")


def main() -> None:
    print("处理战斗立绘…")
    invincible = crop_content(key_white(Image.open(os.path.join(SRC, "克雷松-无敌状态.png"))))
    released = crop_content(key_white(Image.open(os.path.join(SRC, "克雷松-解除无敌.png"))))
    dead = crop_content(key_white(Image.open(os.path.join(SRC, "克雷松-死亡.png"))))
    print(f"  无敌 {invincible.size} / 解除无敌 {released.size} / 死亡 {dead.size}")
    states = compose_states({"kreson_invincible": invincible, "kreson_released": released, "kreson_dead": dead}, BATTLE_HEIGHT)

    print("处理图标…")
    sheet = crop_dense(key_white(Image.open(os.path.join(SRC, "克雷松-在地图上以及其他地方的显示.png"))))
    print(f"  地图素材主体 {sheet.size}")
    icon = make_icon(sheet, ICON_SIZE)
    outline = make_outline(icon)

    print("处理锚点卡图…")
    anchor_src = crop_content(key_white(Image.open(os.path.join(SRC, "锚点.png"))))
    s = min(CARD_W / anchor_src.width, CARD_H / anchor_src.height)
    anchor = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    resized = anchor_src.resize((max(1, round(anchor_src.width * s)), max(1, round(anchor_src.height * s))), Image.LANCZOS)
    anchor.alpha_composite(resized, ((CARD_W - resized.width) // 2, (CARD_H - resized.height) // 2))

    outputs = {
        "boss/kreson_invincible.png": states["kreson_invincible"],
        "boss/kreson_released.png": states["kreson_released"],
        "boss/kreson_dead.png": states["kreson_dead"],
        "boss/kreson_icon.png": icon,
        "boss/kreson_icon_outline.png": outline,
    }
    for rel, img in outputs.items():
        save(img, os.path.join(REPO_BOSS, os.path.basename(rel)))
        save(img, os.path.join(GAME_MOD, "spine", os.path.basename(os.path.dirname(rel)), os.path.basename(rel)))

    save(anchor, os.path.join(REPO_CARD, "Anchor.png"))
    save(anchor, os.path.join(GAME_MOD, "spine", "card_art", "Anchor.png"))


if __name__ == "__main__":
    main()
