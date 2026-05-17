using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>灵魂堡垒 - 形态转换后�?/4格挡</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class SoulFortress : BaseAmiyaCard
{
    public SoulFortress() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        decimal amount = IsUpgraded ? 4m : 3m;
        await PowerCmd.Apply<SoulFortressPower>(cc, new[] { Owner.Creature }, amount, Owner.Creature, this);
    }
    protected override void OnUpgrade() { }
}