using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class SupportFuture : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(16m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Infection };
    public SupportFuture() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(cc);
        // 从战斗历史中获取本次攻击造成的实际未格挡伤害
        var dmg = (int)CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Where(e => e.HappenedThisTurn(CombatState) && e.Receiver == target)
            .Sum(e => e.Result.UnblockedDamage);
        if (!Owner.Creature.HasPower<SupportFuturePower>())
            await PowerCmd.Apply<SupportFuturePower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
        var sfp = Owner.Creature.GetPower<SupportFuturePower>();
        if (sfp != null) sfp.GoldToGain += dmg;
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4m); }
}