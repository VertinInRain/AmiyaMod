using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>洪流不息 - 魔王 造成16/20伤害，本回合每打出一张攻击牌�?能量</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class EndlessFlood : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(16m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.DemonLord };
    public EndlessFlood() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        var dlc = Owner.Creature.GetPower<DemonLordCallPower>();
        if (dlc != null) await dlc.AddStack(cc);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(cc);

        // 确保计数器Power已挂�?
        if (!Owner.Creature.HasPower<EndlessFloodCounterPower>())
            await PowerCmd.Apply<EndlessFloodCounterPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
        var counter = Owner.Creature.GetPower<EndlessFloodCounterPower>();
        if (counter != null && counter.AttacksPlayedThisTurn > 0)
            await PlayerCmd.GainEnergy(counter.AttacksPlayedThisTurn, Owner);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4m); }
}