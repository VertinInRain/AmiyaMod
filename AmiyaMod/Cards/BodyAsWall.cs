using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class BodyAsWall : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new BlockVar(12m, ValueProp.Move) };
    public BodyAsWall() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cp);
        var fm = Owner.Creature.GetPower<FormManagerPower>();
        if (fm == null || fm.CurrentForm != 2)
        {
            decimal hpLoss = IsUpgraded ? 3m : 4m;
            await CreatureCmd.Damage(cc, Owner.Creature, hpLoss, ValueProp.Unblockable | ValueProp.Unpowered, this);
        }
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3m); }
}