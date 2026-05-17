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

/// <summary>
/// 魔王的誓�?- 普通攻击牌
/// 魔王 造成12点伤�?
/// 牌组中每有一�?魔王 牌，伤害�?/12
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class DemonLordSword : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(12m, ValueProp.Move)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => 
        new[] { AmiyaKeywords.DemonLord };

    public DemonLordSword() 
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        // 确保魔王之唤存在
        if (!Owner.Creature.HasPower<DemonLordCallPower>())
            await PowerCmd.Apply<DemonLordCallPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
        var dlc = Owner.Creature.GetPower<DemonLordCallPower>();
        if (dlc != null) await dlc.AddStack(cc);

        // 计算伤害：基础12 + 牌组中魔王牌数量 × 加成
        int demonLordCount = PileType.Draw.GetPile(Owner).Cards.Count(c => c.Keywords.Contains(AmiyaKeywords.DemonLord))
                           + PileType.Hand.GetPile(Owner).Cards.Count(c => c.Keywords.Contains(AmiyaKeywords.DemonLord))
                           + PileType.Discard.GetPile(Owner).Cards.Count(c => c.Keywords.Contains(AmiyaKeywords.DemonLord));
        
        decimal damageBonus = IsUpgraded ? 12m : 8m;
        decimal totalDamage = DynamicVars.Damage.BaseValue + demonLordCount * damageBonus;

        await DamageCmd.Attack(totalDamage)
            .FromCard(this)
            .Targeting(target)
            .Execute(cc);
    }

    protected override void OnUpgrade()
    {
        // 每张魔王牌加成从8提高�?2
        DynamicVars.Damage.UpgradeValueBy(0m); // 基础伤害不变，bonus在OnPlay中计�?
    }
}