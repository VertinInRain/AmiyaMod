using System;
using System.Collections.Generic;
using Amiya.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Patches;

/// <summary>
/// 实例词条卡面高亮：描述文本按卡牌类型静态注册，相信明天/不容拒绝动态添加的
/// InstanceTags 无法进入描述正文。此补丁在 GetDescriptionForPile 结果前按实例词条
/// 插入金色词条行（跳过天生词条，避免重复）。
/// </summary>
[HarmonyPatch(typeof(CardModel), "GetDescriptionForPile", new Type[] { typeof(PileType), typeof(Creature) })]
internal static class AmiyaKeywordDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not BaseAmiyaCard ami || ami.InstanceTags == AmiyaTag.None)
        {
            return;
        }

        var names = new List<string>();
        foreach (var tag in new[] { AmiyaTag.DemonLord, AmiyaTag.Infection, AmiyaTag.Leader, AmiyaTag.Graveyard })
        {
            // 只展示实例新增词条（天生词条已在静态描述里带金色行）
            if ((ami.AmiyaTags & tag) == 0 && ami.HasAmiyaTag(tag))
            {
                names.Add("[gold]" + BaseAmiyaCard.KeywordTitle(tag) + "[/gold]");
            }
        }
        if (names.Count > 0)
        {
            __result = string.Join(" ", names) + "\n" + __result;
            Log.Info($"[Amiya] keyword desc patch fired: {ami.Id.Entry} instanceTags={ami.InstanceTags} added={string.Join(" ", names)}");
        }
    }
}
