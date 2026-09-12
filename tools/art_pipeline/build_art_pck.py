#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""美术资源管线 v4：完全镜像 RegentFemPortraits.pck（参考模组，实际可用）的机制。
  - .godot/imported/<name>.png-<md5>.ctex   （GST2 内嵌 WebP，FORMAT_RGBA8）
  - <ModId>/images/<类>/<name>.png.import    （导入侧车，base-34 uid + 重映射到 ctex）
  - .godot/uid_cache.bin                    （uid → res:// 源路径 缓存）
  - .godot/global_script_class_cache.cfg
C# 侧引用源路径 res://Amiya/images/<类>/<name>.png（游戏经侧车重映射到 ctex）。
"""
import hashlib
import io
import json
import os
import random
import struct
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
IMAGES = os.path.join(ROOT, "images")
OUT_PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya\Amiya.pck"
FILE_BASE = 112
MOD = "Amiya"

POWER_CLASSES = [
    "ArrivedPower", "BlackCrownPower", "DemonLordBannerPower", "DemonLordCallPower",
    "DemonLordVesselPower", "EmberPower", "FinalShadowPower", "FormManagerPower",
    "IAmYoursPower", "IronGuardPower", "JudgmentPower",
    "RefuseMournPower", "SoulFortressPower", "SpreadPower",
    "SwordNeverSleepsPower", "TodayTomorrowDeviationPower", "TurbulencePower",
    "VoidFragmentPower", "WildfirePower", "WildRoarPower",
]
RELIC_CLASSES = ["PaleBlessingRelic", "PaleCrownRelic"]


def ctex_of(img, w, h, lossy):
    # GST2 容器，内嵌 WebP（与参考 pck / 游戏自带资源一致，游戏必带 webp_unpacker）。
    buf = io.BytesIO()
    img.save(buf, "WEBP", lossless=not lossy, quality=90, method=6)
    webp = buf.getvalue()
    out = bytearray(b"GST2")
    out += struct.pack("<I", 1)             # version
    out += struct.pack("<I", w)
    out += struct.pack("<I", h)
    out += struct.pack("<I", 0x0D000000)    # df（STREAM|DETECT_3D|DETECT_ROUGHNESS，与参考 pck 相同）
    out += struct.pack("<I", 0xFFFFFFFF)    # mipmap_limit（与参考 pck 相同）
    out += struct.pack("<III", 0, 0, 0)     # reserved
    out += struct.pack("<I", 2)             # DATA_FORMAT_WEBP
    out += struct.pack("<HH", w, h)
    out += struct.pack("<I", 0)             # mipmaps
    out += struct.pack("<I", 5)             # FORMAT_RGBA8
    out += struct.pack("<I", len(webp))
    out += webp
    return bytes(out)


UID_CHARS = "abcdefghijklmnopqrstuvwxy012345678"  # 34 进制（Godot 4.5 官方算法，无 z/9）


def new_uid():
    orig = random.getrandbits(63)
    uid = orig
    tmp = []
    while True:
        tmp.append(UID_CHARS[uid % 34])
        uid //= 34
        if uid == 0:
            break
    text = "".join(reversed(tmp))
    return orig, "uid://" + text


def add_texture(files, uid_cache, src_path, category, name, png_override=None):
    if png_override is not None:
        img, lossy = png_override
    else:
        img = Image.open(src_path)
        if img.mode not in ("RGB", "RGBA"):
            img = img.convert("RGBA" if "A" in img.getbands() else "RGB")
        lossy = False
    w, h = img.size
    ctex = ctex_of(img, w, h, lossy)
    hashpart = hashlib.md5(name.encode("utf-8")).hexdigest()
    ctex_path = ".godot/imported/%s.png-%s.ctex" % (name, hashpart)
    source_res = "res://%s/images/%s/%s.png" % (MOD, category, name)
    uid, uid_text = new_uid()
    # 运行时唯一被读取的重映射 = <源路径>.remap（resource_loader.cpp _path_remap 第1282行）。
    # .import 侧车在导出版游戏里从不被读取（ResourceFormatImporter 仅编辑器存在）。
    remap = '[remap]\n\npath="res://%s"\n' % ctex_path
    files.append((ctex_path, ctex))
    files.append(("%s/images/%s/%s.png.remap" % (MOD, category, name), remap.encode("utf-8")))
    uid_cache.append((uid, source_res))
    return source_res


def build_uid_cache(entries):
    data = bytearray(struct.pack("<I", len(entries)))
    for uid, path in entries:
        pb = path.encode("utf-8")
        data += struct.pack("<Q", uid)
        data += struct.pack("<I", len(pb))
        data += pb
    return bytes(data)


def ecfg_entry(key, type_code, payload):
    out = bytearray()
    kb = key.encode("utf-8")
    out += struct.pack("<I", len(kb))
    out += kb
    while len(out) % 4:
        out += b"\x00"
    out += struct.pack("<I", type_code)
    out += payload
    return bytes(out)


def pascal(b):
    out = bytearray(struct.pack("<I", len(b)))
    out += b
    while len(out) % 4:
        out += b"\x00"
    return bytes(out)


def build_project_binary():
    # ECFG（ConfigFile 二进制），键结构逐字节对照 NecrobinderCardPortraits.pck 的 project.binary。
    # 类型码：16=单字符串数组, 32=字符串, 36=字符串数组, 8=整数（实测编码）。
    out = bytearray(b"ECFG")
    out += struct.pack("<I", 7)
    out += ecfg_entry("_custom_features", 16, struct.pack("<I", 4) + pascal(b"dotnet"))
    out += ecfg_entry("application/config/name", 32, struct.pack("<I", 4) + pascal(MOD.encode("utf-8")))
    out += ecfg_entry("dotnet/project/assembly_name", 32, struct.pack("<I", 4) + pascal(MOD.encode("utf-8")))
    return bytes(out)


def build_pck(files):
    data = bytearray(b"GDPC")
    data += struct.pack("<IIIII", 3, 4, 5, 1, 2)
    data += struct.pack("<QQ", FILE_BASE, 0)
    while len(data) < FILE_BASE:
        data += b"\x00"
    offsets, sizes, md5s = [], [], []
    for _, blob in files:
        offsets.append(len(data) - FILE_BASE)
        sizes.append(len(blob))
        md5s.append(hashlib.md5(blob).digest())
        data += blob
        while len(data) % 4:
            data += b"\x00"
    dir_off = len(data)
    data += struct.pack("<I", len(files))
    for (path, _), off, size, md5 in zip(files, offsets, sizes, md5s):
        pb = path.encode("utf-8")
        # 官方 PCKPacker：sl = 路径长度+1(NUL) 再补到 4 的倍数；路径后不再有额外对齐。
        sl = len(pb) + 1
        pad = (4 - sl % 4) % 4
        data += struct.pack("<I", sl + pad)
        data += pb + b"\x00" + b"\x00" * pad
        data += struct.pack("<QQ", off, size)
        data += md5
        data += struct.pack("<I", 0)
    struct.pack_into("<QQ", data, 24, FILE_BASE, dir_off)
    return bytes(data)


def main():
    files = []
    uid_cache = []
    mapping = {}

    import numpy as np
    from scipy import ndimage as ndi

    def white_to_transparent(img, flood=True):
        """白底 → 透明底。
        flood=True：只移除与四边连通的近白色区域（适合图标截图，保留内部白色细节）；
        flood=False：移除所有近白色像素（适合白底插画/线稿，内容非白色）。"""
        if img.mode != "RGBA":
            img = img.convert("RGBA")
        arr = np.asarray(img).astype(np.float32)
        rgb = arr[..., :3]
        b = rgb.mean(axis=2)
        s = rgb.max(axis=2) - rgb.min(axis=2)
        nearwhite = (b > 228) & (s < 28)
        if flood:
            labels, _ = ndi.label(nearwhite)
            border = (set(np.unique(labels[[0, -1], :]).tolist()) | set(np.unique(labels[:, [0, -1]]).tolist())) - {0}
            mask_src = np.isin(labels, list(border))
        else:
            mask_src = nearwhite
        mask = ndi.gaussian_filter(mask_src.astype(np.float32), sigma=1.0)
        arr[..., :3] = arr[..., :3] * (1.0 - mask)[..., None]
        arr[..., 3] = arr[..., 3] * (1.0 - mask)
        return Image.fromarray(arr.clip(0, 255).astype(np.uint8), "RGBA")

    icon_dir = os.path.join(IMAGES, "powers")
    icons = sorted(f for f in os.listdir(icon_dir) if f.lower().endswith((".png", ".jpg", ".webp")))
    for cls, fn in zip(POWER_CLASSES, icons):
        img = white_to_transparent(Image.open(os.path.join(icon_dir, fn)))
        res = add_texture(files, uid_cache, None, "powers", cls, png_override=(img, False))
        mapping[cls] = {"icon": fn, "res": res}
        print("power %-26s <- %s" % (cls, fn))

    # 尘霾之冠力量图标：截图已无剩余（20/20 用尽），从补充卡图裁取顶部方形区域生成。
    # 该图为白底灰阶插画（内容贴着边），用全局抠白（flood=False）。
    crown = Image.open(os.path.join(ROOT, "补充卡图", "尘霾之冠.png"))
    if crown.mode != "RGB":
        crown = crown.convert("RGB")
    crown = crown.crop((30, 30, min(crown.width, crown.height) - 30, min(crown.width, crown.height) - 30))
    crown = white_to_transparent(crown, flood=False).resize((128, 128), Image.LANCZOS)
    add_texture(files, uid_cache, None, "powers", "DustHazeCrownPower", png_override=(crown, False))
    print("power %-26s <- 补充卡图/尘霾之冠.png（方形裁切）" % "DustHazeCrownPower")

    # 自制黑白几何图标（透明底）：溢流消耗 / 祈愿 / 打击防御数值增加
    from PIL import ImageDraw
    def geometric_icon(kind):
        img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        BLACK = (12, 12, 12, 255)
        WHITE = (245, 245, 245, 255)
        if kind == "up":  # 数值增加：粗上箭头（白芯）
            d.polygon([(64, 10), (114, 86), (82, 86), (82, 118), (46, 118), (46, 86), (14, 86)], fill=BLACK)
            d.polygon([(64, 36), (92, 86), (72, 86), (72, 100), (56, 100), (56, 86), (36, 86)], fill=WHITE)
        elif kind == "star":  # 祈愿：四角星
            d.polygon([(64, 5), (76, 52), (123, 64), (76, 76), (64, 123), (52, 76), (5, 64), (52, 52)], fill=BLACK)
            d.polygon([(64, 33), (70, 58), (95, 64), (70, 70), (64, 95), (58, 70), (33, 64), (58, 58)], fill=WHITE)
        else:  # overflow：方框 + 右上溢出箭头
            d.rectangle([22, 44, 82, 104], outline=BLACK, width=9)
            d.rectangle([34, 56, 70, 92], outline=WHITE, width=4)
            d.polygon([(86, 42), (116, 12), (124, 20), (106, 32), (124, 32), (124, 44), (98, 44)], fill=BLACK)
        return img
    for name, kind in (("DrawOverflowExhaustPower", "overflow"), ("WishPower", "star"), ("OrdinaryJoyPower", "up")):
        icon = geometric_icon(kind)
        add_texture(files, uid_cache, None, "powers", name, png_override=(icon, False))
        print("power %-26s <- 自制黑白几何图标(%s)" % (name, kind))

    # 诸王的冠冕：单独设计的王冠几何图标（独立伤害翻倍乘区的专属标识）
    crown_icon = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    cd = ImageDraw.Draw(crown_icon)
    CB = (12, 12, 12, 255)
    CW = (245, 245, 245, 255)
    cd.polygon([(28, 88), (28, 44), (50, 44), (50, 88)], fill=CB)   # 左尖
    cd.polygon([(50, 88), (50, 26), (78, 26), (78, 88)], fill=CB)   # 中尖（更高）
    cd.polygon([(78, 88), (78, 44), (100, 44), (100, 88)], fill=CB) # 右尖
    cd.rectangle([18, 88, 110, 108], fill=CB)                        # 底座
    cd.ellipse([28, 94, 40, 106], fill=CW)                           # 底座白珠 ×3
    cd.ellipse([54, 94, 66, 106], fill=CW)
    cd.ellipse([80, 94, 92, 106], fill=CW)
    add_texture(files, uid_cache, None, "relic", "CrownOfKingsRelic", png_override=(crown_icon, False))
    print("relic %-26s <- 自制王冠几何图标" % "CrownOfKingsRelic")

    relic_dir = os.path.join(IMAGES, "relic")
    relic_icons = sorted(f for f in os.listdir(relic_dir) if f.lower().endswith((".png", ".jpg", ".webp")))
    for cls, fn in zip(RELIC_CLASSES, relic_icons):
        img = white_to_transparent(Image.open(os.path.join(relic_dir, fn)))
        res = add_texture(files, uid_cache, None, "relic", cls, png_override=(img, False))
        mapping[cls] = {"icon": fn, "res": res}
        print("relic %-26s <- %s" % (cls, fn))

    # 专属遗物（补充卡图，白底转透明）
    extra_relics = {
        "LongevityProofRelic": "长生者之证.png",
        "HuntRelic": "追猎.png",
        "NamelessTotemRelic": "无名图腾.png",
        "GreatSilenceRelic": "大静谧.png",
        "UndyingEndRelic": "不死的終結.png",
        "KingArmorRelic": "国王的铠甲.png",
        "KingExtensionRelic": "国王的延伸.png",
        "KingNewGunRelic": "国王的新枪.png",
        "BunnyDollRelic": "兔兔玩偶.png",
    }
    for cls, fn in extra_relics.items():
        img = white_to_transparent(Image.open(os.path.join(ROOT, "补充卡图", fn)))
        add_texture(files, uid_cache, None, "relic", cls, png_override=(img, False))
        print("relic %-26s <- 补充卡图/%s" % (cls, fn))

    for name in ("character_icon_amiya", "char_select_amiya", "char_select_amiya_locked"):
        img = Image.open(os.path.join(IMAGES, "icon", name + ".png"))
        add_texture(files, uid_cache, None, "icon", name, png_override=(img, False))
        print("icon  %-26s <- 原图（不做抠白）" % name)

    # 死亡立绘（4 形态，暗色成品图，无需处理）
    death_map = {
        "guard": "近卫死亡.png",
        "caster": "术士死亡.png",
        "medic": "医疗死亡.png",
        "demon": "魔王死亡.png",
    }
    for name, fn in death_map.items():
        add_texture(files, uid_cache, os.path.join(ROOT, "补充卡图", fn), "death", name)
        print("death %-26s <- %s" % (name, fn))

    # 商店人物形象
    add_texture(files, uid_cache, os.path.join(ROOT, "补充卡图", "商店.png"), "merchant", "merchant")
    print("merchant merchant <- 补充卡图/商店.png")

    # 背景：原图 10752×10752 方形、白底插画。
    # 1) 白底转黑底：从四边做"近白色连通域"抠底（scipy label，只把连到边框的白色区域变黑，
    #    主体内部的白色不受影响），边缘高斯羽化防锯齿。
    # 2) 放大 20%：整幅缩放为 1728×1728（=1440×1.2），居中贴到 2560×1440 黑色画布，
    #    中心位置不变（上下各裁掉约 8%，主体在纵向 25%~78% 区间内，不受影响）。
    import numpy as np
    from scipy import ndimage as ndi
    bg = Image.open(os.path.join(IMAGES, "bg", "bg.png"))
    if bg.mode != "RGB":
        bg = bg.convert("RGB")
    work = bg.resize((3840, 3840), Image.LANCZOS)
    arr = np.asarray(work).astype(np.float32)
    b = arr.mean(axis=2)
    s = arr.max(axis=2) - arr.min(axis=2)
    nearwhite = (b > 228) & (s < 28)
    labels, nlab = ndi.label(nearwhite)
    border = (set(np.unique(labels[[0, -1], :]).tolist()) | set(np.unique(labels[:, [0, -1]]).tolist())) - {0}
    flood = np.isin(labels, list(border))
    mask = ndi.gaussian_filter(flood.astype(np.float32), sigma=1.5)
    keyed = (arr * (1.0 - mask)[..., None]).clip(0, 255).astype(np.uint8)
    art = Image.fromarray(keyed, "RGB").resize((1728, 1728), Image.LANCZOS)
    canvas = Image.new("RGB", (2560, 1440), (0, 0, 0))
    canvas.paste(art, ((2560 - 1728) // 2, (1440 - 1728) // 2))
    add_texture(files, uid_cache, None, "bg", "bg", png_override=(canvas, True))

    # 选人背景场景：TextureRect 全屏锚定 + 保持比例覆盖（stretch_mode=6）。
    # 之前用 Sprite2D：默认 centered=true 且位置(0,0)，只显示了贴图右下象限。
    # 注意 type 必须写 CompressedTexture2D：带 "Texture2D" 类型提示时 Godot 4.5 的
    # ctex 加载器无法识别（get_recognized_extensions_for_type 按 handles_type 过滤）。
    scene = ('[gd_scene load_steps=2 format=3]\n\n'
             '[ext_resource type="CompressedTexture2D" path="res://%s/images/bg/bg.png" id="1_bg"]\n\n'
             '[node name="AmiyaSelectBg" type="TextureRect"]\n'
             'anchors_preset = 15\n'
             'anchor_right = 1.0\n'
             'anchor_bottom = 1.0\n'
             'grow_horizontal = 2\n'
             'grow_vertical = 2\n'
             'mouse_filter = 2\n'
             'texture = ExtResource("1_bg")\n'
             'expand_mode = 1\n'
             'stretch_mode = 6\n' % MOD)
    files.append(("%s/scenes/AmiyaSelectBg.tscn" % MOD, scene.encode("utf-8")))

    # 战斗左上角角色头像场景（镜像游戏自带 ironclad_icon.tscn 的结构，换阿米娅贴图）
    icon_scene = ('[gd_scene load_steps=2 format=3]\n\n'
                  '[ext_resource type="CompressedTexture2D" path="res://%s/images/icon/character_icon_amiya.png" id="1_icon"]\n\n'
                  '[node name="AmiyaIcon" type="TextureRect"]\n'
                  'anchors_preset = 15\n'
                  'anchor_right = 1.0\n'
                  'anchor_bottom = 1.0\n'
                  'grow_horizontal = 2\n'
                  'grow_vertical = 2\n'
                  'mouse_filter = 2\n'
                  'texture = ExtResource("1_icon")\n'
                  'expand_mode = 1\n'
                  'stretch_mode = 5\n' % MOD)
    files.append(("%s/scenes/AmiyaIcon.tscn" % MOD, icon_scene.encode("utf-8")))

    # 商店人物形象：Node2D + Sprite2D 静态立绘（游戏商店场景只把该场景实例化进 CharacterContainer，
    # 原版 spine 脚本不挂，纯静态展示）
    merchant_scene = ('[gd_scene load_steps=2 format=3]\n\n'
                      '[ext_resource type="CompressedTexture2D" path="res://%s/images/merchant/merchant.png" id="1_m"]\n\n'
                      '[node name="AmiyaMerchant" type="Node2D"]\n\n'
                      '[node name="Sprite" type="Sprite2D" parent="."]\n'
                      'texture = ExtResource("1_m")\n'
                      'scale = Vector2(0.47, 0.47)\n' % MOD)
    files.append(("%s/scenes/AmiyaMerchant.tscn" % MOD, merchant_scene.encode("utf-8")))

    # 建筑师胜利结算对话（先古对话 loc 表）。
    # 缺少自定义对话时 TheArchitect 的 GetValidDialogues 返回空 → Rng.NextItem 抛异常 → 结算卡死。
    # 键格式参考 Typhon/localization/zhs/ancients.json：{古物ID}.talk.{角色Entry}.{对话序号}-{行}r.{char|ancient}
    ancients = {
        "THE_ARCHITECT.CONTINUE": "继续",
        "THE_ARCHITECT.RESPOND": "回应",
    }
    for i in range(4):  # 胜利次数 0~3 各一份，防止多次通关后无可用对话
        ancients["THE_ARCHITECT.talk.AMIYA-AMIYA_CHARACTER.%d-0r.char" % i] = "…………？"
        ancients["THE_ARCHITECT.talk.AMIYA-AMIYA_CHARACTER.%d-0r.next" % i] = "继续"
        ancients["THE_ARCHITECT.talk.AMIYA-AMIYA_CHARACTER.%d-1r.ancient" % i] = "…………？"
        ancients["THE_ARCHITECT.talk.AMIYA-AMIYA_CHARACTER.%d-1r.next" % i] = "继续"
    ancients_json = json.dumps(ancients, ensure_ascii=False, indent=1)
    files.append(("%s/localization/zhs/ancients.json" % MOD, ancients_json.encode("utf-8")))

    files.append((".godot/global_script_class_cache.cfg", b"list=[]\n"))
    files.append((".godot/uid_cache.bin", build_uid_cache(uid_cache)))
    files.append(("project.binary", build_project_binary()))

    data = build_pck(files)
    with open(OUT_PCK, "wb") as f:
        f.write(data)
    print("pck 已写出: %s (%d 字节, %d 文件, uid 缓存 %d 条)" % (OUT_PCK, len(data), len(files), len(uid_cache)))
    with open(os.path.join(HERE, "power_icon_map.json"), "w", encoding="utf-8") as f:
        json.dump(mapping, f, ensure_ascii=False, indent=1)
    return 0


if __name__ == "__main__":
    sys.exit(main())
