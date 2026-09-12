using System;
using System.Collections.Generic;
using System.Linq;
using Amiya.Models;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace Amiya.Patches;

/// <summary>
/// 自创遗物权重提升（仅阿米娅）：
/// 原版稀有度权重 50/33/17（普通/罕见/稀有），而自创遗物多为罕见与稀有，
/// 又被庞大的共享遗物池稀释，很难见到。这里两处增强：
///  1) 稀有度掷点偏向：阿米娅按 30/40/30 掷点（普通/罕见/稀有），用官方种子 Rng，可复现；
///  2) 抽袋前缀：若该稀有度队列前 5 个里有自创遗物（队列已官方洗牌），优先抽出它。
/// 两条合计约把"拿到自创遗物"的概率从 ~10% 提到 ~30%。
/// </summary>
[HarmonyPatch(typeof(RelicFactory), "RollRarity", new Type[] { typeof(Player) })]
internal static class AmiyaRelicRarityBiasPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Player player, ref RelicRarity __result)
    {
        try
        {
            if (player?.Character is not AmiyaCharacter)
            {
                return true;
            }
            float n = player.PlayerRng.Rewards.NextFloat();
            __result = n < 0.30f
                ? RelicRarity.Common
                : (n < 0.70f ? RelicRarity.Uncommon : RelicRarity.Rare);
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }
}

[HarmonyPatch(typeof(RelicGrabBag), "PullFromFront", new Type[] { typeof(RelicRarity), typeof(Func<RelicModel, bool>), typeof(IRunState) })]
internal static class AmiyaRelicBagBiasPatch
{
    private static HashSet<ModelId>? _amiyaRelicIds;

    private static HashSet<ModelId> AmiyaRelicIds => _amiyaRelicIds ??= ModelDb.RelicPool<AmiyaRelicPool>().AllRelicIds;

    [HarmonyPrefix]
    private static bool Prefix(RelicGrabBag __instance, RelicRarity rarity, Func<RelicModel, bool> filter, IRunState runState, ref RelicModel? __result)
    {
        try
        {
            if (rarity != RelicRarity.Common && rarity != RelicRarity.Uncommon && rarity != RelicRarity.Rare)
            {
                return true;
            }
            // 定位这个抽袋属于哪个玩家（多人联机只对阿米娅的袋子生效）
            Player? player = runState?.Players.FirstOrDefault(p => p.RelicGrabBag == __instance);
            if (player?.Character is not AmiyaCharacter)
            {
                return true;
            }
            var deques = Traverse.Create(__instance).Field("_deques").GetValue() as Dictionary<RelicRarity, List<RelicModel>>;
            if (deques == null || !deques.TryGetValue(rarity, out List<RelicModel> deque) || deque.Count == 0)
            {
                return true;
            }
            // 队列已被官方种子洗牌：前 5 个里有自创遗物就优先拿它（等效权重提升，且不会重复出现）
            int scan = System.Math.Min(5, deque.Count);
            for (int i = 0; i < scan; i++)
            {
                if (AmiyaRelicIds.Contains(deque[i].Id) && filter(deque[i]))
                {
                    __result = deque[i];
                    deque.RemoveAt(i);
                    return false;
                }
            }
        }
        catch (Exception)
        {
            // 权重补丁失败时退回原逻辑
        }
        return true;
    }
}
