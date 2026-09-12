using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Cards;

/// <summary>慈悲愿景（稀有，消耗）：若手牌中打击和防御不少于 4 张，获得 1/2 层无实体。</summary>
public sealed class MercyVision : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public MercyVision() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "慈悲愿景"),
        ("description", "#若手牌中打击和防御不少于 4 张，获得 {IfUpgraded:show:2|1} 层无实体。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = PileType.Hand.GetPile(Owner!).Cards
            .Count(c => c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend));
        if (count >= 4)
        {
            decimal stacks = IsUpgraded ? 2m : 1m;
            await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner!.Creature, stacks, Owner.Creature, null, silent: true);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
