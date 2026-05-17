using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace Amiya.Powers;

/// <summary>
/// 支援未来 - 记录打出的伤害值，战斗结束时获得等量金�?
/// 参�? RoyaltiesPower.AfterCombatEnd
/// </summary>
public partial class SupportFuturePower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public decimal GoldToGain { get; set; }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (GoldToGain > 0)
            room.AddExtraReward(Owner.Player, new GoldReward((int)GoldToGain, Owner.Player));
        return Task.CompletedTask;
    }
}