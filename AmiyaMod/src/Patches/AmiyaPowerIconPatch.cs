using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Patches;

/// <summary>
/// 兜底图标补丁：任何仍指向缺失的 amiya 贴图的能力图标，统一替换为官方占位贴图。
/// 覆盖非 CustomPowerModel 的 amiya 能力（如 AmiyaTempStrengthPower，无法覆写 CustomPackedIconPath）。
/// BaseLib 的 CustomPackedIconPath 前缀补丁已把自定义能力替换成真实官方贴图，本补丁只处理漏网之鱼。
/// </summary>
[HarmonyPatch(typeof(PowerModel), "PackedIconPath", MethodType.Getter)]
internal static class AmiyaPowerIconPatch
{
    private static void Postfix(ref string __result)
    {
        if (__result != null && __result.Contains("/amiya-"))
        {
            __result = Amiya.Art.PlaceholderArt.Power("ritual_power");
        }
    }
}
