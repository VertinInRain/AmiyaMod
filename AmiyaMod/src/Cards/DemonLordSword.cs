using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 魔王的誓剑（普通，魔王）：造成 4 点伤害，牌组中每有一张魔王牌，伤害额外 +8（升级后 +12）。
/// 实现完全照搬官方 PerfectedStrike（完美打击）：
/// CalculationBaseVar + ExtraDamageVar + CalculatedDamageVar(WithMultiplier 统计魔王牌数，含自身)，
/// 检索对象由"名字含打击的牌"换成"带魔王词条的牌"。动态预览由官方管线自动处理。
/// </summary>
public sealed class DemonLordSword : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(4m),
        new ExtraDamageVar(8m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (CardModel card, Creature? _) =>
            {
                // Q24 已裁："牌组"不含消耗堆（官方 PerfectedStrike 的 AllCards 含消耗堆，这里按裁决收窄）；
                // 保留出牌堆是为了让打出中的誓剑计入自身（预览与实际伤害一致）
                var cs = card.Owner.PlayerCombatState;
                return cs.AllPiles
                    .Where(p => p.Type != PileType.Exhaust)
                    .SelectMany(p => p.Cards)
                    .Count(c => c is BaseAmiyaCard ac && ac.HasAmiyaTag(AmiyaTag.DemonLord));
            })
    };

    public override AmiyaTag AmiyaTags => AmiyaTag.DemonLord;

    public DemonLordSword() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "魔王的誓剑"),
        ("description", "#造成 !CD! 点伤害。牌组中每有一张魔王牌，伤害额外 +{IfUpgraded:show:12|8}。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(4m); // 8 → 12
    }
}
