using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Cards;
using Amiya.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Patches;

/// <summary>
/// 溢流消耗：每次抽牌结束后，把"本应抽到但超出手牌上限"的牌（仍留在抽牌堆顶）直接消耗。
/// DrawInternal 是 async 方法：Harmony 的异步 Postfix 在 Task 完成后执行，可以拿到抽牌结果。
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), "DrawInternal")]
internal static class AmiyaDrawOverflowPatch
{
    // 注意：直通式异步后置补丁的返回类型必须与第一参数（__result）类型一致，
    // 且必须把原结果返回——否则调用方 await 得到 null（CentennialPuzzle 对结果
    // 调 FirstOrDefault 直接抛异常，战斗回合循环死亡 → 游戏卡死）。
    private static async Task<IEnumerable<CardModel>> Postfix(Task<IEnumerable<CardModel>> __result, PlayerChoiceContext choiceContext, decimal count, Player player)
    {
        IEnumerable<CardModel> drawn = Array.Empty<CardModel>();
        try
        {
            drawn = await __result;
            if (FormManagerPower.Current?.Owner is { } owner && owner.Player == player && owner.HasPower<DrawOverflowExhaustPower>())
            {
                int requested = Math.Max(0, (int)Math.Ceiling(count));
                int remaining = requested - drawn.Count();
                if (remaining > 0)
                {
                    // 溢出 = 模拟"继续抽牌"：烧抽牌堆顶牌；抽牌堆空了就把弃牌堆洗回抽牌堆再继续烧，
                    // 直到烧满溢出数或整副牌打空。
                    int exhausted = 0;
                    while (remaining > 0)
                    {
                        var drawPile = PileType.Draw.GetPile(player);
                        if (drawPile.Cards.Count == 0)
                        {
                            await CardPileCmd.Shuffle(choiceContext, player);
                            if (drawPile.Cards.Count == 0)
                            {
                                break; // 抽牌堆与弃牌堆都没牌了
                            }
                        }
                        foreach (CardModel card in drawPile.Cards.Take(remaining).ToList())
                        {
                            await CardCmd.Exhaust(choiceContext, card);
                            exhausted++;
                            remaining--;
                        }
                    }
                    Log.Info($"[Amiya] draw overflow: {exhausted} cards exhausted (requested={requested}, drawn={drawn.Count()})");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("[Amiya] draw overflow patch failed: " + ex);
        }
        return drawn;
    }
}
