using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>燎原 - 每次形态转换时�?/2临时力量</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class Wildfire : BaseAmiyaCard
{
    public Wildfire() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        decimal amount = IsUpgraded ? 2m : 1m;
        await PowerCmd.Apply<WildfirePower>(cc, new[] { Owner.Creature }, amount, Owner.Creature, this);
    }
    protected override void OnUpgrade() { }
}