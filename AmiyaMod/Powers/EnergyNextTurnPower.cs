using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Amiya.Powers;

/// <summary>
/// 下回合额外能�?- 下回合开始时获得能量并自�?
/// 参�? EnergyNextTurnPower
/// </summary>
public partial class EnergyNextTurnPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player == Owner.Player)
        {
            await PlayerCmd.GainEnergy(Amount, player);
            await PowerCmd.Remove(this);
        }
    }
}