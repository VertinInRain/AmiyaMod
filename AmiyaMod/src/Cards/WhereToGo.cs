using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>去往何方（稀有，领袖）：获得 2/3 点能量（费用 1/1）。</summary>
public sealed class WhereToGo : BaseAmiyaCard
{
    public override AmiyaTag AmiyaTags => AmiyaTag.Leader;

    public WhereToGo() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "去往何方"),
        ("description", "#获得 {IfUpgraded:show:3|2} 点能量。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner != null)
        {
            decimal energy = IsUpgraded ? 3m : 2m;
            await PlayerCmd.GainEnergy(energy, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // 能量 2 → 3（费用保持 1）
    }
}
