using BaseLib.Abstracts;

namespace Amiya.Powers;

/// <summary>
/// 阿米娅能力基类 — 自动映射图标路径
/// Id.Entry 格式: AMIYA-FORM_MANAGER_POWER → 图片: form_manager.png
/// </summary>
public abstract partial class BaseAmiyaPower : CustomPowerModel
{
    public override string? CustomPackedIconPath =>
        "res://Amiya/images/powers/" + ReplaceId(base.Id.Entry.ToLowerInvariant()) + ".png";

    public override string? CustomBigIconPath =>
        "res://Amiya/images/powers/" + ReplaceId(base.Id.Entry.ToLowerInvariant()) + ".png";

    private static string ReplaceId(string entry)
    {
        return entry.Replace("amiya-", "").Replace("_power", "");
    }
}
