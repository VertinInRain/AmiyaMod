using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Cards;

/// <summary>立体艺术装置（罕见，消耗）：若处于医疗形态，恢复 8/12 点生命；若处于魔王形态，获得 6/8 层再生。</summary>
public sealed class SculptureInstallation : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public SculptureInstallation() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "立体艺术装置"),
        ("description", "#若处于医疗形态，恢复 {IfUpgraded:show:12|8} 点生命。若处于魔王形态，获得 {IfUpgraded:show:8|6} 层再生。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var form = FormManagerPower.Current?.Form;
        if (form == AmiyaForm.Medic)
        {
            decimal heal = IsUpgraded ? 12m : 8m;
            await CreatureCmd.Heal(Owner!.Creature, heal);
        }
        else if (form == AmiyaForm.DemonLord)
        {
            decimal regen = IsUpgraded ? 8m : 6m;
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner!.Creature, regen, Owner.Creature, null, silent: true);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
