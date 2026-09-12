using System;
using Amiya.Models;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Amiya.Patches;

/// <summary>
/// 阿米娅战斗形象热替换（PNG 贴图版）：CreateVisuals 实例化生物场景后，
/// 隐藏铁甲军 SpineSprite，改由 AmiyaFormVisual 挂载四形态透明 PNG 贴图，
/// 跟随 FormManagerPower.Current.Form 切换。静态贴图方案，无 spine 动画。
/// </summary>
[HarmonyPatch(typeof(CharacterModel), "CreateVisuals")]
internal static class AmiyaVisualSwapPatch
{
    private static void Postfix(CharacterModel __instance, NCreatureVisuals __result)
    {
        if (__instance is not AmiyaCharacter || __result == null)
        {
            return;
        }
        try
        {
            var visuals = __result.GetNodeOrNull<Node>("%Visuals");
            if (visuals == null)
            {
                Log.Error("[Amiya] visual swap: %Visuals node not found");
                return;
            }

            // 移除铁甲战士专属节点：其 SpineSlotNode 引用的槽位在阿米娅身上不存在。
            foreach (var name in new[] { "NIroncladVfx", "SlashVfxSlot", "EyeSlot" })
            {
                visuals.GetNodeOrNull<Node>(name)?.QueueFree();
            }

            // 挂载四形态 PNG 贴图（内部会隐藏铁甲 SpineSprite）
            AmiyaFormVisual.Attach(visuals);

            Log.Info("[Amiya] visual swap: png form visuals applied");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] visual swap failed: {ex}");
        }
    }
}
