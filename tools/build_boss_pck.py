"""打包模组的「附加资源小 pck」（Amiya_boss.pck），由模组运行时挂载。

内容：
  images/boss/*.png            → res://Amiya/images/boss/*    （克雷松地图节点图标 / 血条头像）
  images/custom_powers/*.png   → res://Amiya/images/powers/*  （自制黑色几何状态图标）

为什么要独立小 pck：主 pck 的构建依赖「补充卡图」里那批原图（遗物图标、形态死亡立绘等），
那些图已在前面的清理里删掉，整包重建会丢资源；这里只打新增资源，互不影响。

为什么必须进 pck（而不是运行时 TakeOverPath 注册）：地图节点图 / 血条头像是引擎按
res:// 路径「预加载」的资源，预加载走磁盘，读不到就抛 AssetLoadException，整个开图流程会崩。
"""
import importlib.util
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PIPELINE = os.path.join(ROOT, "tools", "art_pipeline", "build_art_pck.py")
OUT_PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya\Amiya_boss.pck"

# (本地子目录, pck 内的 images 分类)
SOURCES = (
    (os.path.join("images", "boss"), "boss"),
    (os.path.join("images", "custom_powers"), "powers"),
    (os.path.join("images", "custom_relics"), "relic"),
)


def load_pipeline():
    spec = importlib.util.spec_from_file_location("art_pck_pipeline", PIPELINE)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    bap = load_pipeline()
    files, uid_cache = [], []
    count = 0
    for subdir, category in SOURCES:
        directory = os.path.join(ROOT, subdir)
        if not os.path.isdir(directory):
            print("跳过（目录不存在）：%s" % subdir)
            continue
        for entry in sorted(os.listdir(directory)):
            if not entry.lower().endswith(".png"):
                continue
            name = os.path.splitext(entry)[0]
            src = os.path.join(directory, entry)
            res = bap.add_texture(files, uid_cache, src, category, name)
            print("打包 %s <- %s" % (res, os.path.relpath(src, ROOT)))
            count += 1
    if count == 0:
        print("没有任何资源可打包")
        return 1
    data = bap.build_pck(files)
    with open(OUT_PCK, "wb") as f:
        f.write(data)
    print("已写出 %s（%d 字节，%d 个资源）" % (OUT_PCK, len(data), count))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
