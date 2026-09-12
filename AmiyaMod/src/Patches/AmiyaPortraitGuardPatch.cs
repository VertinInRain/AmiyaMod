using System;
using Amiya.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Amiya.Patches;

/// <summary>
/// 卡图守卫：新版 RitsuLib（2026-09-10 创意工坊自动更新后）会补丁 NCard.UpdatePortrait，
/// 对没有资产档案的自定义卡牌把 PortraitPath 解析成空串 → 引擎报
/// "No loader found for resource: res://"，并把卡面纹理清掉（表现为卡图时有时无/消失）。
/// 双保险：
///  1) PortraitPath getter 后置补丁：空路径替换为我们已注册进资源缓存的卡图路径；
///  2) UpdatePortrait 后置补丁：下一帧把阿米娅卡图重新写回卡面，
///     保证无论其它 mod 的同步补丁做什么，最终显示的都是我们的卡图。
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
internal static class AmiyaPortraitGuardPatch
{
    [HarmonyPostfix]
    private static void UpdatePortraitPostfix(NCard __instance)
    {
        try
        {
            if (__instance.Model is not BaseAmiyaCard card || !__instance.IsInsideTree())
            {
                return;
            }
            SceneTree? tree = __instance.GetTree();
            if (tree == null)
            {
                return;
            }
            // 下一帧再写回：晚于其它 mod 的同步补丁，成为最终状态
            void OnFrame()
            {
                try
                {
                    tree.ProcessFrame -= OnFrame;
                    if (!GodotObject.IsInstanceValid(__instance))
                    {
                        return;
                    }
                    Texture2D? tex = card.CustomPortrait;
                    if (tex == null)
                    {
                        return;
                    }
                    if (__instance.GetNodeOrNull<TextureRect>("%Portrait") is { } portrait)
                    {
                        portrait.Texture = tex;
                    }
                    if (__instance.GetNodeOrNull<TextureRect>("%AncientPortrait") is { } ancient)
                    {
                        ancient.Texture = tex;
                    }
                }
                catch (Exception)
                {
                    // 显示守卫不允许抛异常
                }
            }
            tree.ProcessFrame += OnFrame;
        }
        catch (Exception)
        {
            // 显示守卫不允许抛异常
        }
    }
}

/// <summary>把阿米娅卡牌被解析成空串的 PortraitPath 修回已注册的缓存路径（顺带消灭 res:// 报错）。</summary>
[HarmonyPatch(typeof(CardModel), "PortraitPath", MethodType.Getter)]
internal static class AmiyaPortraitPathGuardPatch
{
    [HarmonyPostfix]
    private static void PortraitPathPostfix(CardModel __instance, ref string __result)
    {
        if (__instance is BaseAmiyaCard && string.IsNullOrEmpty(__result))
        {
            __result = "res://Amiya/card_art/" + __instance.GetType().Name + ".png";
        }
    }
}
