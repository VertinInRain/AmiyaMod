using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>于花丛中轻舞（罕见，X费）：触发形态转换 X 次（升级后 X+1 次），获得 1 点能量。</summary>
public sealed class DanceInFlowers : BaseAmiyaCard
{
    protected override bool HasEnergyCostX => true;

    public DanceInFlowers() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "于花丛中轻舞"),
        ("description", "#触发形态转换 X{IfUpgraded:show:+1|} 次。获得 1 点能量。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int times = EnergyCost.CapturedXValue + (IsUpgraded ? 1 : 0);
        for (int i = 0; i < times; i++)
        {
            await TriggerFormSwitch(choiceContext);
        }
        if (Owner != null)
        {
            await PlayerCmd.GainEnergy(1m, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
