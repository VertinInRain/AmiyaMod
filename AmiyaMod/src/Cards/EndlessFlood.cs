using System;
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

/// <summary>洪流不息（罕见，魔王）：造成 16/20 点伤害，获得 X 点能量（X=打出本牌前本回合已打出的攻击牌数，Q27）。</summary>
public sealed class EndlessFlood : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(16m, ValueProp.Move) };

    public override AmiyaTag AmiyaTags => AmiyaTag.DemonLord;

    public EndlessFlood() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "洪流不息"),
        ("description", "#造成 !D! 点伤害。获得 X 点能量，X 为本回合已打出的攻击牌数量。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        int x = Math.Max(0, (FormManagerPower.Of(Owner)?.TurnAttackPlayedCount ?? 1) - 1);
        await PlayerCmd.GainEnergy(x, Owner!);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 16 → 20
    }
}
