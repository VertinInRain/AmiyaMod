using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>披甲在身（普通）：获得 8/12 点格挡；若处于近卫或魔王形态，获得 4/6 点活力。</summary>
public sealed class ArmorUp : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(8m, ValueProp.Move) };

    public ArmorUp() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "披甲在身"),
        ("description", "#获得 !B! 点格挡。若处于近卫或魔王形态，获得 {IfUpgraded:show:6|4} 点活力。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);

        var form = FormManagerPower.Current?.Form ?? AmiyaForm.Guard;
        if (form == AmiyaForm.Guard || form == AmiyaForm.DemonLord)
        {
            decimal vigor = IsUpgraded ? 6m : 4m;
            await PowerCmd.Apply<VigorPower>(choiceContext, Owner!.Creature, vigor, Owner.Creature, null, silent: true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m); // 8 → 12
    }
}
