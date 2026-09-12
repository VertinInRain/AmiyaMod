using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Cards;

/// <summary>示我以真？（稀有，消耗）：获得 2 层无实体，将一张呼唤分别加入你的抽牌堆、手牌和弃牌堆（升级后获得保留）。</summary>
public sealed class ShowMeTruth : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public ShowMeTruth() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "示我以真？"),
        ("description", "#获得 2 层无实体，将一张呼唤分别加入你的抽牌堆、手牌和弃牌堆。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner!.Creature, 2m, Owner.Creature, null, silent: true);

        foreach (var pileType in new[] { PileType.Draw, PileType.Hand, PileType.Discard })
        {
            var beckon = Owner.Creature.CombatState.CreateCard<Beckon>(Owner);
            await CardPileCmd.Add(beckon, pileType, CardPilePosition.Bottom);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
