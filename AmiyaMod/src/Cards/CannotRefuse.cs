using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Amiya.Cards;

/// <summary>不容拒绝（稀有，消耗）：为抽牌堆、手牌、弃牌堆、消耗堆中所有打击、防御添加魔王词条（费用 1/0）。</summary>
public sealed class CannotRefuse : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public CannotRefuse() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "不容拒绝"),
        ("description", "#为抽牌堆、手牌、弃牌堆、消耗堆中所有打击、防御添加魔王词条。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var pileType in new[] { PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust })
        {
            foreach (var card in pileType.GetPile(Owner!).Cards)
            {
                if (card is BaseAmiyaCard ac
                    && (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend)))
                {
                    ac.InstanceTags |= AmiyaTag.DemonLord;
                    // 带重试的卡面刷新（选牌容器/牌堆状态稳定后生效）
                    _ = BaseAmiyaCard.RefreshCardVisualsAsync(card);
                    // 手牌：交给奇偶特效逻辑（双词条领袖生效时白光优先）；非手牌：直接挂黑雾
                    if (card.Pile?.Type == PileType.Hand)
                    {
                        if (Amiya.Powers.FormManagerPower.Current is { } fm)
                        {
                            _ = fm.RefreshLeaderGlow(Owner!);
                        }
                    }
                    else if (card.Affliction == null)
                    {
                        await CardCmd.Afflict<Amiya.Afflictions.AmiyaDemonLordAffliction>(card, 1m);
                    }
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}
