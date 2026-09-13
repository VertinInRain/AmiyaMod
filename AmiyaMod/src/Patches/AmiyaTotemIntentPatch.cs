using System;
using System.Collections.Generic;
using Amiya.Powers;
using Amiya.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Amiya.Patches;

/// <summary>
/// 无名图腾：敌方攻击意图的显示值减半（实际伤害由 NamelessTotemPower.ModifyDamageMultiplicative 减半，两者一致）。
/// </summary>
[HarmonyPatch(typeof(SingleAttackIntent), "GetTotalDamage")]
[HarmonyPatch(typeof(MultiAttackIntent), "GetTotalDamage")]
internal static class AmiyaTotemIntentPatch
{
    private static void Postfix(ref int __result)
    {
        try
        {
            // 显示层：只按"本地玩家"的图腾减半（视觉表现，不参与结算）
            if (FormManagerPower.Local?.Owner != null
                && FormManagerPower.Local.Owner.HasPower<NamelessTotemPower>())
            {
                __result = Math.Max(1, __result / 2);
            }
        }
        catch (Exception)
        {
            // 意图显示不允许抛异常
        }
    }
}
