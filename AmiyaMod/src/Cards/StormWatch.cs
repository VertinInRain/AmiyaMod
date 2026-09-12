using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Cards;

/// <summary>
/// 风暴瞭望（稀有，消耗）：抽牌直到抽满手牌（上限10，Q37），本回合不能再抽牌，且打击、防御可以免费打出。
/// </summary>
public sealed class StormWatch : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public StormWatch() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "风暴瞭望"),
        ("description", "#抽牌直到抽满手牌，本回合不能再抽牌，且打击、防御可以免费打出。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner!);
        for (int i = 0; i < CardPile.MaxCardsInHand; i++)
        {
            if (hand.Cards.Count >= CardPile.MaxCardsInHand)
            {
                break;
            }
            var drawn = await CardPileCmd.Draw(choiceContext, 1m, Owner);
            if (!System.Linq.Enumerable.Any(drawn))
            {
                break;
            }
        }

        // 本回合不能再抽牌（官方 NoDraw 状态）
        await PowerCmd.Apply<NoDrawPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
        // 本回合打击/防御免费（由 FormManagerPower 费用钩子结算）
        if (FormManagerPower.Current != null)
        {
            FormManagerPower.Current.StrikesDefendsFreeThisTurn = true;
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}
