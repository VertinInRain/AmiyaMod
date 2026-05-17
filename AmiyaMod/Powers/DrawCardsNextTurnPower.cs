using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Powers;

/// <summary>
/// 下回合额外抽�?- 下回合开始时额外抽牌并自�?
/// 参�? DrawCardsNextTurnPower
/// </summary>
public partial class DrawCardsNextTurnPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner.Player) return count;
        if (AmountOnTurnStart == 0) return count;
        return count + Amount;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext cc, Player player)
    {
        if (player.Creature == Owner && AmountOnTurnStart != 0)
            await PowerCmd.Remove(this);
    }
}