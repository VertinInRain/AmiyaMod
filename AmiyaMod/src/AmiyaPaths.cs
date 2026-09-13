using Godot;

namespace Amiya;

/// <summary>
/// 便携路径：所有"运行时从 mods/Amiya 读 PNG"的地方都从这里取目录。
/// 相对游戏可执行文件（SlayTheSpire2.exe）所在目录推导，不写死盘符；
/// 并且会自动识别 mod 文件夹的实际名字（有人解压后会得到 Amiya-v0.2.0 之类
/// 的名字，此时若仍按 mods/Amiya 去找就会读不到卡图）。
/// </summary>
public static class AmiyaPaths
{
    /// <summary>游戏安装根目录（SlayTheSpire2.exe 所在目录）。</summary>
    public static readonly string GameDir = OS.GetExecutablePath().GetBaseDir();

    /// <summary>mods 目录。</summary>
    public static readonly string ModsRoot = GameDir + "/mods";

    /// <summary>mod 实际所在目录（优先 mods/Amiya，否则扫描 mods/*/spine/card_art）。</summary>
    public static readonly string ModDir = ResolveModDir();

    /// <summary>mod 目录下的 spine（形态立绘 PNG 所在）。</summary>
    public static readonly string SpineDir = ModDir + "/spine";

    /// <summary>spine/card_art（卡图 PNG 所在）。</summary>
    public static readonly string CardArtDir = SpineDir + "/card_art";

    private static string ResolveModDir()
    {
        string standard = ModsRoot + "/Amiya";
        try
        {
            if (DirAccess.DirExistsAbsolute(standard))
            {
                return standard;
            }
            // 兼容"文件夹被改名"的情况：找出含 spine/card_art 的那个 mod 目录
            using var dir = DirAccess.Open(ModsRoot);
            if (dir != null)
            {
                foreach (string sub in dir.GetDirectories())
                {
                    string candidate = ModsRoot + "/" + sub;
                    if (DirAccess.DirExistsAbsolute(candidate + "/spine/card_art"))
                    {
                        return candidate;
                    }
                }
            }
        }
        catch
        {
            // 路径解析失败时回退到标准路径
        }
        return standard;
    }
}
