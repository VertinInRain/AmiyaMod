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

/// <summary>赤霄拔刀（罕见）：造成 9/11 点伤害，抽牌至抽到第一张打击/防御（抽空或手牌满即停，Q25）。</summary>
public sealed class RedDawnDraw : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(9m, ValueProp.Move) };

    public RedDawnDraw() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "赤霄拔刀"),
        ("description", "#造成 !D! 点伤害。抽牌至抽到第一张打击/防御。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        var hand = PileType.Hand.GetPile(Owner!);
        for (int i = 0; i < 20; i++)
        {
            if (hand.Cards.Count >= CardPile.MaxCardsInHand)
            {
                break;
            }
            var drawn = (await CardPileCmd.Draw(choiceContext, 1m, Owner)).ToList();
            if (drawn.Count == 0)
            {
                break;
            }
            if (drawn.Any(c => c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend)))
            {
                break;
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 9 → 11
    }
}
