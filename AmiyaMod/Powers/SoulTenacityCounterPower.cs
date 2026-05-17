using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 神魂坚韧之歌 - 追踪本场战斗中被完全格挡的攻击次�?
/// 使用AfterBlockGained作为代理：每次获得格挡时，计�?1
/// (近似方案：获得格挡≈格挡成功；战斗结束后清零)
/// </summary>
public partial class SoulTenacityCounterPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int BlocksPerformedThisCombat { get; private set; }

    protected override object? InitInternalData()
    {
        BlocksPerformedThisCombat = 0;
        return null;
    }

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner || amount <= 0m) return;
        // 从卡牌效果获得的格挡不算，但我们无法区分
        // 这是一个近似计数器
        BlocksPerformedThisCombat++;
    }
}