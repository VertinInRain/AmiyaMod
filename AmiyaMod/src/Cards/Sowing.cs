using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Amiya.Cards;

/// <summary>
/// 播种（稀有，费用 2→1）：战斗结束后，额外获得一次卡牌奖励。
/// 实现采用官方 TheHunt 卡同款机制：打出时直接给当前战斗房间追加一组卡牌奖励
/// （TheHuntPower 本身只是标记状态、不产出奖励——之前误用它导致无效）。
/// </summary>
public sealed class Sowing : BaseAmiyaCard
{
    public Sowing() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "播种"),
        ("description", "#战斗结束后，额外获得一次卡牌奖励。")
    };

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState?.RunState?.CurrentRoom is CombatRoom combatRoom)
        {
            combatRoom.AddExtraReward(Owner, new CardReward(CardCreationOptions.ForRoom(Owner, combatRoom.RoomType), 3, Owner));
        }
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 → 1
    }
}
