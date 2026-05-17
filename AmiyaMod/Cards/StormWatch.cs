using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Cards;

/// <summary>风暴瞭望 - 抽牌直到满手牌，本回合不能抽牌，打击防御免费 消�?/summary>
[Pool(typeof(AmiyaCardPool))]

public partial class StormWatch : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    public StormWatch() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        // 抽牌直到满手�?
        var handPile = PileType.Hand.GetPile(Owner);
        int maxHand = 10; // CardPile.MaxCardsInHand
        while (handPile.Cards.Count() < maxHand)
        {
            var drawPile = PileType.Draw.GetPile(Owner);
            if (!drawPile.Cards.Any()) break;
            await CardPileCmd.Draw(cc, 1m, Owner);
        }

        // 本回合不能再抽牌
        await PowerCmd.Apply<NoDrawPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);

        // 手牌中所有打击防御免�?
        foreach (var card in handPile.Cards.ToList())
        {
            if (card is Strike || card is Defend)
            {
                if (!card.EnergyCost.CostsX)
                    card.SetToFreeThisTurn();
            }
        }
    }
    protected override void OnUpgrade() { }
}