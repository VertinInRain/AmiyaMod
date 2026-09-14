using System;
using System.Linq;
using System.Reflection;
using Amiya.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace Amiya.Patches;

/// <summary>
/// 任务牌【路网】的计数：玩家「放弃一次卡牌奖励」时 +1。
///
/// 游戏里没有现成的"奖励被放弃"钩子（Hook 只有 AfterRewardTaken），
/// 但 <c>CardReward.OnSkipped()</c> 正好是那个点：奖励集合结束时，未被领取的奖励会被逐个调用
/// OnSkipped（见 RewardsSetSynchronizer.SkipRewardsSet）。它在每个客户端都会执行
/// （本地按钮 + RewardSetSkippedMessage 两条路径），所以写入是两端一致的。
///
/// 这里只做同步的计数写入；发遗物/移除卡牌等异步动作放在【路网】自己的房间钩子里 await 执行。
/// 注册方式与其它补丁一致：手工 Patch + 日志确认。
/// </summary>
internal static class AmiyaQuestPatch
{
    public static void Apply(Harmony harmony)
    {
        try
        {
            MethodInfo? target = AccessTools.Method(typeof(CardReward), "OnSkipped");
            if (target == null)
            {
                Log.Error("[Amiya] CardReward.OnSkipped 反射解析失败，路网任务无法计数");
                return;
            }
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(AmiyaQuestPatch), nameof(AfterCardRewardSkipped)));
            Log.Info("[Amiya] patched CardReward.OnSkipped -> 路网任务计数（放弃卡牌奖励）");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] patching CardReward.OnSkipped failed: {ex}");
        }
    }

    private static void AfterCardRewardSkipped(CardReward __instance)
    {
        try
        {
            Player? player = __instance.Player;
            if (player?.Deck == null)
            {
                return;
            }
            foreach (CardModel card in player.Deck.Cards.ToList())
            {
                if (card is RoadNetwork road)
                {
                    road.RegisterSkippedCardReward();
                    Log.Info($"[Amiya] road network: skipped card reward {road.Skips}/{RoadNetwork.RequiredSkips}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] road network counting failed: {ex}");
        }
    }
}
