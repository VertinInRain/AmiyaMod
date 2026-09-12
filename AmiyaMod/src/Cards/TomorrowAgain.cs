using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>明日再来（罕见，消耗）：给予自身 2 层仪式，给予全体敌人 1 层仪式（费用 1/0）。</summary>
public sealed class TomorrowAgain : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public TomorrowAgain() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "明日再来"),
        ("description", "#给予自身 2 层仪式，给予全体敌人 1 层仪式。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RitualPower>(choiceContext, Owner!.Creature, 2m, Owner.Creature, null, silent: true);
        await PowerCmd.Apply<RitualPower>(choiceContext, Owner.Creature.CombatState.Enemies, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}
