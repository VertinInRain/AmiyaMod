using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Amiya;

/// <summary>
/// 阿米娅模组的自定义关键词
/// 参照游戏原版关键词（各种卡牌关键词.txt）和Typhon模组的实现方式
/// </summary>
public class AmiyaKeywords
{
    /// <summary>魔王 - 打出时获得1层魔王之唤</summary>
    [CustomEnum("DEMON_LORD")]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword DemonLord;

    /// <summary>感染 - 回合开始时若在弃牌堆，变为打击/防御</summary>
    [CustomEnum("INFECTION")]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Infection;

    /// <summary>领袖 - 打出时可选择放入抽牌/弃牌/消耗堆</summary>
    [CustomEnum("LEADER")]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Leader;

    /// <summary>墓园 - 战斗开始时放入弃牌堆</summary>
    [CustomEnum("GRAVEYARD")]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Graveyard;
}
