using System;
using Amiya.Cards;
using Amiya.Models;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;

namespace Amiya.Patches;

/// <summary>
/// 临时测试：初始牌组建好后，把平凡亦是喜乐升级 5 次（平凡亦是喜乐+5）。
/// StartingDeck 只能返回规范实例（游戏会对每张卡调用 ToMutable()，
/// 返回可变克隆会抛 MutableModelException 导致开局黑屏），所以升级放在
/// PopulateStartingDeck 的 Postfix 里对运行实例执行。
/// </summary>
[HarmonyPatch(typeof(Player), "PopulateStartingDeck")]
internal static class AmiyaStartingDeckPatch
{
    private static void Postfix(Player __instance)
    {
        try
        {
            if (__instance.Character is not AmiyaCharacter)
            {
                return;
            }
            foreach (var card in __instance.Deck.Cards)
            {
                if (card is not OrdinaryJoy joy)
                {
                    continue;
                }
                for (int i = 0; i < 5; i++)
                {
                    joy.UpgradeInternal();
                    joy.FinalizeUpgradeInternal();
                }
                Log.Info("[Amiya] starting deck: 平凡亦是喜乐 upgraded to +5");
            }
        }
        catch (Exception ex)
        {
            Log.Error("[Amiya] starting deck patch failed: " + ex);
        }
    }
}
