using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>以血还血（罕见，魔王）：受到 2 点伤害 3/4 次，随机造成 6 点伤害 3/4 次（每轮先自伤后输出）。</summary>
public sealed class BloodForBlood : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(6m, ValueProp.Move) };

    public override AmiyaTag AmiyaTags => AmiyaTag.DemonLord;

    public BloodForBlood() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "以血还血"),
        ("description", "#受到 2 点伤害 -3-+4+ 次，随机造成 !D! 点伤害 -3-+4+ 次。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int rounds = IsUpgraded ? 4 : 3;
        for (int i = 0; i < rounds; i++)
        {
            // 自伤 2（可被格挡，Q5 已裁）
            await CreatureCmd.Damage(choiceContext, Owner!.Creature, 2m, ValueProp.Move, Owner.Creature, null, null);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingRandomOpponents(CombatState)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        // 3 → 4 轮
    }
}
