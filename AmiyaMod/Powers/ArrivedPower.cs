using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Amiya.Powers;

/// <summary>
/// 已至 - 回合开始时�?能量+额外�?张牌
/// 参�? DemesnePower (ModifyHandDraw + ModifyMaxEnergy)
/// </summary>
public partial class ArrivedPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner.Player) return count;
        return count + 1m;
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != Owner.Player) return amount;
        return amount + 1m;
    }
}