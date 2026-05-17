using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]

public partial class PhaseChange : BaseAmiyaCard
{
    public PhaseChange() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        await PowerCmd.Apply<PhaseChangePower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() { }
}