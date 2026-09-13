using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>终焉之影（稀有，虚无，升级后取消虚无且费用 2→1）：每消耗或变化一张牌，获得一层魔王之唤。</summary>
public sealed class FinalShadow : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            if (IsUpgraded)
            {
                return Array.Empty<CardKeyword>();
            }
            return new CardKeyword[] { CardKeyword.Ethereal };
        }
    }

    public FinalShadow() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "终焉之影"),
        ("description", "#每消耗或变化一张牌，获得一层魔王之唤。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FinalShadowPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 → 1
    }
}

/// <summary>终焉之影 Power：我方每消耗一张牌 → 魔王之唤 +1（复用 FormManagerPower.GainDemonLordCall）。变化（变形）由 AmiyaTransformNotifyPatch 结算。</summary>
public sealed class FinalShadowPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/FinalShadowPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "终焉之影"),
        ("description", "每消耗一张牌，获得一层魔王之唤。")
    };

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        await base.AfterCardExhausted(choiceContext, card, causedByEthereal);
        if (card.Owner?.Creature == Owner && FormManagerPower.Of(Owner) != null)
        {
            // 传入被消耗的卡作为攻击来源卡（魔王的祭器随机伤害需要 attacker）
            await FormManagerPower.Of(Owner)!.GainDemonLordCall(choiceContext, card, null);
        }
    }
}
