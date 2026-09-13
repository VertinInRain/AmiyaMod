using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Afflictions;

// 注意：AfflictionModel 是游戏原生类，本身不实现 BaseLib 的 ILocalizationProvider，
// 而悬停提示在构造 HoverTip 时就会解析 LocString（找不到 key 会直接抛 LocException）。
// 因此这两个纯视觉好感必须自带 title/description 文本。

/// <summary>魔王牌特效：黑色雾气（视觉层由补丁复用官方 smog 覆盖层）。</summary>
public sealed class AmiyaDemonLordAffliction : AfflictionModel, ILocalizationProvider
{
    public List<(string, string)>? Localization => new()
    {
        ("title", "魔王之气"),
        ("description", "魔王形态的黑色雾气缠绕着这张牌。")
    };
}

/// <summary>领袖牌生效特效：白色光芒（视觉层由补丁复用官方 galvanized 覆盖层）。</summary>
public sealed class AmiyaLeaderGlowAffliction : AfflictionModel, ILocalizationProvider
{
    public List<(string, string)>? Localization => new()
    {
        ("title", "领袖之光"),
        ("description", "领袖形态的白色光芒笼罩着这张牌。")
    };
}
