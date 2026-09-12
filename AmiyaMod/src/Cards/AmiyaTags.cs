using System;

namespace Amiya.Cards;

/// <summary>
/// 阿米娅自定义词条标签（与官方 CardKeyword 并存）。
/// 通过 BaseAmiyaCard.AmiyaTags 由各卡声明；实例级动态添加（不容拒绝/相信明天）后续接入。
/// </summary>
[Flags]
public enum AmiyaTag
{
    None = 0,
    DemonLord = 1, // 魔王
    Infection = 2, // 感染
    Leader = 4,    // 领袖
    Graveyard = 8  // 墓园
}
