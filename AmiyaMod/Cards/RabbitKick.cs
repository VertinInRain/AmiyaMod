using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class RabbitKick : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(18m, ValueProp.Move) };
    public RabbitKick() : base(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this) return false;
        var fm = Owner?.Creature?.GetPower<FormManagerPower>();
        if (fm == null) return false;
        int discount = fm.TotalFormSwitches;
        if (discount <= 0) return false;
        modifiedCost = Math.Max(0, originalCost - discount);
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(cc);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(6m); }
}