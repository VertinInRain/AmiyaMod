using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Boss;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>
/// 锚点（状态牌，1 费，可升级）：克雷松战的关键牌。
/// 打出后克雷松本回合解除无敌，覆盖到下一张牌的效果结束（升级后延长至两次出牌）；
/// 回合结束无条件恢复无敌。打出后进入弃牌堆（不消耗），会随洗牌循环。
/// 不入卡牌奖励池、不进图鉴（CardRarity.Status + showInCardLibrary: false）。
/// </summary>
public sealed class Anchor : BaseAmiyaCard
{
    public Anchor()
        : base(1, CardType.Status, CardRarity.Status, TargetType.None, showInCardLibrary: false, autoAdd: true)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "锚点"),
        ("description", "{IfUpgraded:show:打出后解除无敌效果延长至出两次牌。|打出后克雷松本回合解除无敌至下张牌效果结束。}")
    };

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature? boss = CombatState?.Enemies?.FirstOrDefault(c => c.GetPower<KresonInvinciblePower>() != null);
        if (boss?.GetPower<KresonInvinciblePower>() is { } invincible)
        {
            invincible.OpenAfterCard(cardPlay, IsUpgraded ? 2 : 1);
        }
        return Task.CompletedTask;
    }
}
