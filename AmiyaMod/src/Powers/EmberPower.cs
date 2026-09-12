using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Amiya.Powers;

/// <summary>
/// 燃烬（魔王形态）：触发形态转换时，不再进入其他形态，改为选择并消耗一张手牌。
/// 逻辑由 FormManagerPower.Trigger 检查本 Power 后执行。
/// </summary>
public sealed class EmberPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/EmberPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "燃烬"),
        ("description", "触发形态转换时，改为选择并消耗一张手牌。")
    };
}
