"""打包克雷松的地图节点/血条图标为独立小 pck（Amiya_boss.pck），由模组运行时挂载。

为什么不放进主 pck：主 pck 的构建依赖「补充卡图」里那批原图（遗物图标、形态死亡立绘等），
那些图已在前面的清理里删掉了，整包重建会丢资源。这里只打两个新图标，互不影响。

为什么需要 pck：地图节点图 / 血条头像是引擎按 res:// *预加载* 的资源，
单纯用 TakeOverPath 注册进资源缓存不够（预加载走磁盘，读不到就抛 AssetLoadException，
整个开图流程会崩），必须是真实可加载的资源。
"""
import importlib.util
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PIPELINE = os.path.join(ROOT, "tools", "art_pipeline", "build_art_pck.py")
OUT_PCK = r"D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya\Amiya_boss.pck"
NAMES = ("kreson_icon", "kreson_icon_outline")


def load_pipeline():
    spec = importlib.util.spec_from_file_location("art_pck_pipeline", PIPELINE)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    bap = load_pipeline()
    files, uid_cache = [], []
    for name in NAMES:
        src = os.path.join(ROOT, "images", "boss", name + ".png")
        if not os.path.exists(src):
            print("缺失：%s" % src)
            return 1
        res = bap.add_texture(files, uid_cache, src, "boss", name)
        print("打包 %s <- %s" % (res, os.path.relpath(src, ROOT)))
    data = bap.build_pck(files)
    with open(OUT_PCK, "wb") as f:
        f.write(data)
    print("已写出 %s（%d 字节，%d 个文件）" % (OUT_PCK, len(data), len(files)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
