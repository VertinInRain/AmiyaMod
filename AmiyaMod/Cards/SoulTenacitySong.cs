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
/// <summary>神魂坚韧之歌 - 保留 造成8伤害，每格挡成功一次命中次�?1</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class SoulTenacitySong : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(8m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };
    public SoulTenacitySong() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        // 确保计数器Power已挂�?
        if (!Owner.Creature.HasPower<SoulTenacityCounterPower>())
            await PowerCmd.Apply<SoulTenacityCounterPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
        var counter = Owner.Creature.GetPower<SoulTenacityCounterPower>();
        int extraHits = counter?.BlocksPerformedThisCombat ?? 0;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).WithHitCount(1 + extraHits).Execute(cc);
    }
    protected override void OnUpgrade() { UpgradeStarCostBy(-1); }
}