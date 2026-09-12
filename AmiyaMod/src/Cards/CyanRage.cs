using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Cards;

/// <summary>青色怒火（稀有，领袖）：打出抽牌堆和手牌中所有打击和防御（升级后：先升级再打出，永久，Q18）。</summary>
public sealed class CyanRage : BaseAmiyaCard
{
    public override AmiyaTag AmiyaTags => AmiyaTag.Leader;

    public CyanRage() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "青色怒火"),
        ("description", "#{IfUpgraded:show:升级并| }打出抽牌堆和手牌中所有打击和防御。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool isStrikeOrDefend(CardModel c) => c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend);

        // 快照：先手牌后抽牌堆（Q42）
        var handCards = PileType.Hand.GetPile(Owner!).Cards.Where(isStrikeOrDefend).ToList();
        var drawCards = PileType.Draw.GetPile(Owner).Cards.Where(isStrikeOrDefend).ToList();

        foreach (var card in handCards.Concat(drawCards))
        {
            if (IsUpgraded)
            {
                CardCmd.Upgrade(card);
            }
            var target = card.Type == CardType.Attack
                ? Owner.Creature.CombatState.Enemies.FirstOrDefault(e => e.IsAlive)
                : null;
            await CardCmd.AutoPlay(choiceContext, card, target);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
