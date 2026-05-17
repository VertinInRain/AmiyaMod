using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Amiya.Powers;

/// <summary>
/// 播种 - 战斗结束后额外获得卡牌奖�?
/// 参�? TheHuntPower (战斗结束后额外卡牌奖励，由游戏奖励系统处�?
/// </summary>
public partial class SowingPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    // TheHuntPower只是标记，战斗结束后额外卡牌奖励
    // 由游戏的combat reward系统检测此Power后增加奖�?
    // TODO: 确认sts2中TheHunt的实际奖励逻辑
}