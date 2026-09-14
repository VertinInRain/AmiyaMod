using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Amiya.Relics;

/// <summary>
/// 无垠花：事件「在冰原之上」任务链的终点奖励，由任务牌【路网】完成时发放。
/// 稀有度 Event 才是"不进常规奖励"的关键（RelicGrabBag 只收 Common/Uncommon/Rare/Shop），
/// 但 [Pool] 仍然必须有：BaseLib 注册会强制要求，且 RelicModel.Pool 找不到归属会抛异常（悬停即崩）。
/// 效果：克雷松【育苗】给予你的污染层数由 4 层降为 2 层。
/// </summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class BoundlessFlowerRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override string PackedIconPath => "res://Amiya/images/relic/BoundlessFlowerRelic.png";

    protected override string BigIconPath => "res://Amiya/images/relic/BoundlessFlowerRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "无垠花"),
        ("description", "克雷松【育苗】给予你的污染层数由 4 层降为 2 层。"),
        ("flavor", "在无垠的回响里，它安静地开了。")
    };

    public BoundlessFlowerRelic()
        : base(autoAdd: true)
    {
    }
}
