using Godot;

namespace Amiya;

/// <summary>
/// 便携路径：所有"运行时从 mods/Amiya 读 PNG"的地方都从这里取目录，
/// 相对游戏可执行文件（SlayTheSpire2.exe）所在目录推导，不写死盘符，
/// 这样别人把游戏装在别的盘符/路径也能读到卡图与形态立绘。
/// </summary>
public static class AmiyaPaths
{
    /// <summary>游戏安装根目录（SlayTheSpire2.exe 所在目录）。</summary>
    public static readonly string GameDir = OS.GetExecutablePath().GetBaseDir();

    /// <summary>mods/Amiya 目录。</summary>
    public static readonly string ModDir = GameDir + "/mods/Amiya";

    /// <summary>mods/Amiya/spine（形态立绘 PNG 所在）。</summary>
    public static readonly string SpineDir = ModDir + "/spine";

    /// <summary>mods/Amiya/spine/card_art（卡图 PNG 所在）。</summary>
    public static readonly string CardArtDir = SpineDir + "/card_art";
}
