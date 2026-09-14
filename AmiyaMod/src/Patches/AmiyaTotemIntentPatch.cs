using System;
using Amiya.Powers;
using Amiya.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Amiya.Patches;

/// <summary>
/// 无名图腾：敌方攻击意图的显示值减半（实际伤害由 NamelessTotemPower.ModifyDamageMultiplicative 减半，两者一致）。
///
/// 注意：Harmony 的类级 [HarmonyPatch] 特性是"合并"语义——同一个类上写多个 [HarmonyPatch(typeof(X), "M")]
/// 并不会打两个目标，后一个会覆盖前一个（这也是日志里一直出现
/// "SingleAttackIntent.GetTotalDamage: NOT ATTACHED" 的原因）。
/// 所以这里拆成两个补丁类，各自只声明一个目标。
/// </summary>
[HarmonyPatch(typeof(SingleAttackIntent), "GetTotalDamage")]
internal static class AmiyaTotemSingleIntentPatch
{
    private static void Postfix(ref int __result) => AmiyaTotemIntentHalving.Apply(ref __result);
}

[HarmonyPatch(typeof(MultiAttackIntent), "GetTotalDamage")]
internal static class AmiyaTotemMultiIntentPatch
{
    private static void Postfix(ref int __result) => AmiyaTotemIntentHalving.Apply(ref __result);
}

internal static class AmiyaTotemIntentHalving
{
    /// <summary>显示层：只按"本地玩家"的图腾减半（视觉表现，不参与结算）。</summary>
    internal static void Apply(ref int result)
    {
        try
        {
            if (FormManagerPower.Local?.Owner != null
                && FormManagerPower.Local.Owner.HasPower<NamelessTotemPower>())
            {
                result = Math.Max(1, result / 2);
            }
        }
        catch (Exception)
        {
            // 意图显示不允许抛异常
        }
    }
}
