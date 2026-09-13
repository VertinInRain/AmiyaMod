using Amiya.Afflictions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Patches;

/// <summary>
/// 自定义词条特效覆盖层：AfflictionModel.OverlayPath 按 Id.Entry 取场景且不可覆写，
/// 此补丁把我们的词条特效映射到官方现成覆盖层：
///   魔王黑色雾气 → smog（活雾同款烟雾）
///   领袖生效白光 → galvanized（镀锌银白光泽）
///   克雷松【污染】→ tainted（原版污染同款卡面覆盖层）
/// </summary>
[HarmonyPatch(typeof(AfflictionModel), "OverlayPath", MethodType.Getter)]
internal static class AmiyaAfflictionOverlayPatch
{
    private static void Postfix(AfflictionModel __instance, ref string __result)
    {
        if (__instance is AmiyaDemonLordAffliction)
        {
            __result = SceneHelper.GetScenePath("cards/overlays/afflictions/smog");
        }
        else if (__instance is AmiyaLeaderGlowAffliction)
        {
            __result = SceneHelper.GetScenePath("cards/overlays/afflictions/galvanized");
        }
        else if (__instance is Amiya.Boss.KresonTaint)
        {
            __result = SceneHelper.GetScenePath("cards/overlays/afflictions/tainted");
        }
    }
}
