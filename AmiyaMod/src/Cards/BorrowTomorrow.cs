using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Amiya.Cards;

/// <summary>预借明日（罕见，消耗）：获得 2 点能量，抽 3/4 张牌，将两张虚空加入你的弃牌堆。</summary>
public sealed class BorrowTomorrow : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(3) };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public BorrowTomorrow() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "预借明日"),
        ("description", "#获得 2 点能量，抽 !C! 张牌，将两张虚空加入你的弃牌堆。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(2m, Owner!);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        // 虚空加入弃牌堆（AddGeneratedCardToCombat + PreviewCardPileAdd 保持入堆动画）
        for (int i = 0; i < 2; i++)
        {
            var voidCard = Owner.Creature.CombatState.CreateCard<Void>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(voidCard, PileType.Discard, Owner, CardPilePosition.Bottom));
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m); // 3 → 4
    }
}
