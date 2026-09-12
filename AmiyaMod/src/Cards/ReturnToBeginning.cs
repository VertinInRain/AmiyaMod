using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 重返初识（稀有，领袖）：造成 18/24 点伤害，获得 18/24 点格挡。
/// 消耗抽牌堆、手牌与弃牌堆所有牌，将 4 张打击、4 张防御放入手牌（全新未升级基础版，Q30）。
/// </summary>
public sealed class ReturnToBeginning : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(18m, ValueProp.Move),
        new BlockVar(18m, ValueProp.Move)
    };

    public override AmiyaTag AmiyaTags => AmiyaTag.Leader;

    public ReturnToBeginning() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "重返初识"),
        ("description", "#造成 !D! 点伤害，获得 !B! 点格挡。消耗抽牌堆、手牌与弃牌堆所有牌，将 4 张打击、4 张防御放入手牌。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        await CommonActions.CardBlock(this, cardPlay);

        // 消耗抽牌堆 + 手牌 + 弃牌堆全部（不含消耗堆本身；作者 2026-09 追加抽牌堆）
        foreach (var card in PileType.Draw.GetPile(Owner!).Cards.ToList())
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
        foreach (var card in PileType.Hand.GetPile(Owner!).Cards.ToList())
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
        foreach (var card in PileType.Discard.GetPile(Owner).Cards.ToList())
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        // 生成 4 打击 + 4 防御（未升级基础版；CombatState.CreateCard 注册进战斗状态）
        for (int i = 0; i < 4; i++)
        {
            await CardPileCmd.Add(Owner.Creature.CombatState.CreateCard<AmiyaStrike>(Owner), PileType.Hand, CardPilePosition.Bottom);
        }
        for (int i = 0; i < 4; i++)
        {
            await CardPileCmd.Add(Owner.Creature.CombatState.CreateCard<AmiyaDefend>(Owner), PileType.Hand, CardPilePosition.Bottom);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);  // 18 → 24
        DynamicVars.Block.UpgradeValueBy(6m);   // 18 → 24
    }
}
