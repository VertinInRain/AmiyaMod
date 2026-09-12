using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Amiya.Powers;

/// <summary>
/// 裁决（魔王形态）：获得魔王之唤时，对所有敌人造成 9 点伤害。
/// 由 FormManagerPower 在魔王之唤 +1 后检查本 Power 并结算。
/// </summary>
public sealed class JudgmentPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/JudgmentPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "裁决"),
        ("description", "获得魔王之唤时，对所有敌人造成 9 点伤害。")
    };
}
