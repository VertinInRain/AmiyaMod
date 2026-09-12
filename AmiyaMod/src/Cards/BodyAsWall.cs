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

/// <summary>以身铸墙（普通）：获得 12/15 点格挡；若不处于医疗形态，失去 4/3 点生命（失去生命不可格挡）。</summary>
public sealed class BodyAsWall : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(12m, ValueProp.Move) };

    public BodyAsWall() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "以身铸墙"),
        ("description", "#获得 !B! 点格挡。若不处于医疗形态，失去 {IfUpgraded:show:3|4} 点生命。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);

        if (FormManagerPower.Current?.Form != AmiyaForm.Medic)
        {
            decimal hpLoss = IsUpgraded ? 3m : 4m;
            // 失去生命：不可格挡、不吃力量（Unblockable|Unpowered）
            await CreatureCmd.Damage(choiceContext, Owner!.Creature, hpLoss, ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature, null, null);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m); // 12 → 15
    }
}
