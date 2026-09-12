using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>血刃高悬（稀有，墓园，消耗）：造成 32/42 点伤害；斩杀时永久获得 3/4 点最大生命。</summary>
public sealed class BloodBladeSuspended : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(32m, ValueProp.Move) };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public override AmiyaTag AmiyaTags => AmiyaTag.Graveyard;

    public BloodBladeSuspended() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "血刃高悬"),
        // -3-+4+ = 升级前显示3，升级后显示4（BaseLib SimpleLoc 升级交换语法）
        ("description", "#造成 !D! 点伤害。斩杀时，永久获得 -3-+4+ 点最大生命。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        var cmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (cmd.Results.SelectMany(r => r).Any(r => r.WasTargetKilled))
        {
            decimal maxHp = IsUpgraded ? 4m : 3m;
            await CreatureCmd.GainMaxHp(Owner!.Creature, maxHp);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m); // 32 → 42
    }
}
