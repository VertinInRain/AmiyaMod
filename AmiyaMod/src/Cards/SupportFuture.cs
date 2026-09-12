using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>支援未来（罕见，感染）：造成 16/20 点伤害，获得所造成伤害值等量的金币（官方版税，战斗结束时到账，Q28）。</summary>
public sealed class SupportFuture : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(16m, ValueProp.Move) };

    public override AmiyaTag AmiyaTags => AmiyaTag.Infection;

    public SupportFuture() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "支援未来"),
        ("description", "#造成 !D! 点伤害，获得所造成伤害值等量的金币。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        var cmd = await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        decimal dealt = cmd.Results.SelectMany(r => r).Sum(r => (decimal)r.UnblockedDamage);
        if (dealt > 0)
        {
            await PowerCmd.Apply<RoyaltiesPower>(choiceContext, Owner!.Creature, dealt, Owner.Creature, null, silent: true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 16 → 20
    }
}
