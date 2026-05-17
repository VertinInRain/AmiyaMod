using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class DanceInFlowers : BaseAmiyaCard
{
    protected override bool HasEnergyCostX => true;
    public DanceInFlowers() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        int x = ResolveEnergyXValue();
        var fm = Owner.Creature.GetPower<FormManagerPower>();
        if (fm != null)
        {
            for (int i = 0; i < x; i++)
                await fm.TriggerFormSwitch(cc);
            if (IsUpgraded && fm != null)
                await fm.TriggerFormSwitch(cc);
        }
    }
    protected override void OnUpgrade() { }
}