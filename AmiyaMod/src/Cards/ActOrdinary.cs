using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>演绎平凡（罕见）：获得等同于手牌中打击、防御数量的能量（费用 1/0）。</summary>
public sealed class ActOrdinary : BaseAmiyaCard
{
    public ActOrdinary() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "演绎平凡"),
        ("description", "#获得等同于手牌中打击、防御数量的能量。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int n = PileType.Hand.GetPile(Owner!).Cards
            .Count(c => c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend));
        await PlayerCmd.GainEnergy(n, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}
