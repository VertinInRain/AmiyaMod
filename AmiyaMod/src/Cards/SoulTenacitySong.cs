using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 神魂坚韧之歌（稀有，保留）：造成 8 点伤害；本场战斗中每成功格挡过一次伤害，命中次数 +1。
/// 成功格挡=伤害被完全格挡（0 穿过，裁定②）。描述末尾实时显示当前格挡次数。
/// </summary>
public sealed class SoulTenacitySong : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            var vars = new List<DynamicVar> { new DamageVar(8m, ValueProp.Move) };
            vars.AddRange(MakeCalculatedVar("FB", 0,
                (CardModel card, Creature? _) => FormManagerPower.Of(card.Owner)?.FullyBlockedCount ?? 0));
            return vars;
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Retain };

    public SoulTenacitySong() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "神魂坚韧之歌"),
        ("description", "#造成 !D! 点伤害。本场战斗中每成功格挡过一次伤害，命中次数 +1。（当前已格挡 !FB! 次）")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int hits = 1 + (FormManagerPower.Of(Owner)?.FullyBlockedCount ?? 0);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 → 1
    }
}
