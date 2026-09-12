using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>真理残余（普通）：造成 4/5 点伤害，获得与未被格挡伤害相等的格挡，将一张此牌的复制放入弃牌堆。</summary>
public sealed class TruthRemnant : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(4m, ValueProp.Move) };

    public TruthRemnant() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "真理残余"),
        ("description", "#造成 !D! 点伤害，获得与未被格挡伤害相等的格挡，将一张此牌的复制放入弃牌堆。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        var cmd = await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        decimal unblocked = cmd.Results.SelectMany(r => r).Sum(r => (decimal)r.UnblockedDamage);
        if (unblocked > 0)
        {
            await CreatureCmd.GainBlock(Owner!.Creature, unblocked, ValueProp.Move, cardPlay);
        }

        var copy = CreateClone();
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m); // 4 → 5
    }
}
