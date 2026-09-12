using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Amiya.Cards;

/// <summary>追寻（罕见，感染）：将手牌中所有攻击牌变为 灵魂（原版牌：0 费抽 2 张，Q13）。</summary>
public sealed class Pursuit : BaseAmiyaCard
{
    public override AmiyaTag AmiyaTags => AmiyaTag.Infection;

    public Pursuit() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "追寻"),
        ("description", "#将手牌中所有攻击牌变为 {IfUpgraded:show:灵魂+|灵魂}。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attacks = PileType.Hand.GetPile(Owner!).Cards.Where(c => c.Type == CardType.Attack).ToList();
        foreach (var card in attacks)
        {
            var soul = Owner.Creature.CombatState.CreateCard<Soul>(Owner);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(soul); // 升级后变为 灵魂+
            }
            await CardCmd.Transform(card, soul);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
