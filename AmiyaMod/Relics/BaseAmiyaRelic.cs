using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Amiya.Relics;

/// <summary>
/// 阿米娅遗物基类 - 自动映射图标路径
/// Id.Entry 格式: AMIYA-PALE_BLESSING_RELIC → 图片: pale_blessing.png
/// </summary>
public abstract partial class BaseAmiyaRelic : CustomRelicModel
{
    public override string? PackedIconPath =>
        "res://Amiya/images/relic/" + ReplaceId(base.Id.Entry.ToLowerInvariant()) + ".png";

    protected override string? BigIconPath =>
        "res://Amiya/images/relic/" + ReplaceId(base.Id.Entry.ToLowerInvariant()) + ".png";

    private static string ReplaceId(string entry)
    {
        return entry.Replace("amiya-", "").Replace("_relic", "");
    }
}
