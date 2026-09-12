using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>勘破虚妄（罕见，消耗）：选择任意张手牌，变为防御+（至少 1 张，Q35）。升级后获得保留。</summary>
public sealed class SeeThrough : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public SeeThrough() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "勘破虚妄"),
        ("description", "#选择任意张手牌，变为防御+。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner!);
        if (hand.Cards.Count == 0)
        {
            return;
        }
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1, hand.Cards.Count), null, this);
        foreach (var card in selected)
        {
            var target = Owner.Creature.CombatState.CreateCard<AmiyaDefend>(Owner);
            CardCmd.Upgrade(target);
            await CardCmd.Transform(card, target);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
