using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>痛觉相连（罕见，魔王）：去除自己和敌人身上所有格挡，给予双方一层易伤，造成 20/28 点伤害。</summary>
public sealed class PainConnection : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(20m, ValueProp.Move) };

    public override AmiyaTag AmiyaTags => AmiyaTag.DemonLord;

    public PainConnection() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "痛觉相连"),
        ("description", "#去除自己和敌人身上所有格挡，给予双方一层易伤，造成 !D! 点伤害。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        var self = Owner!.Creature;

        // 去除双方格挡（Q8 顺序：去格挡 → 双方易伤 → 伤害）
        await CreatureCmd.LoseBlock(choiceContext, self, self.Block, self);
        await CreatureCmd.LoseBlock(choiceContext, cardPlay.Target, cardPlay.Target.Block, self);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, self, 1m, self, null, silent: true);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, 1m, self, null, silent: true);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m); // 20 → 28
    }
}
