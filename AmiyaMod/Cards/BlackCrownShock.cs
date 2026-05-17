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

/// <summary>
/// 黑冠震荡 - 稀有攻击牌
/// 魔王 若处于魔王形态，对所有敌人造成20/24点伤�?
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class BlackCrownShock : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(20m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.DemonLord };
    public BlackCrownShock() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 魔王关键�?
        var dlc = Owner.Creature.GetPower<DemonLordCallPower>();
        if (dlc != null) await dlc.AddStack(choiceContext);

        // 只在魔王形态下造成伤害
        if (Owner.Creature.HasPower<BlackCrownPower>())
        {
            foreach (var enemy in CombatState.HittableEnemies)
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(enemy).Execute(choiceContext);
        }
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4m); }
}