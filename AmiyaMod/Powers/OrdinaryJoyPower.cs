using System.Linq;
using Amiya.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 平凡亦是喜乐 - 所有打击防御数�?2
/// 参�? FastenPower.ModifyBlockAdditive (防御额外格挡)
/// 额外增加: ModifyDamageAdditive (打击额外伤害)
/// </summary>
public partial class OrdinaryJoyPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return 0m;
        if (!props.IsPoweredAttack()) return 0m;
        if (cardSource == null || !(cardSource is Strike)) return 0m;
        return Amount;
    }

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (Owner != target) return 0m;
        if (!props.IsPoweredCardOrMonsterMoveBlock()) return 0m;
        if (cardSource == null || !(cardSource is Defend)) return 0m;
        return Amount;
    }
}