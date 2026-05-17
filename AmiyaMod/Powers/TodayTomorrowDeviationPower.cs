using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 今时明日的偏�?- 打攻击牌→下回合获格�?
/// Amount为每张攻击牌获得的格挡�?
/// 维护内部计数器：累计攻击牌数，下回合开始时结算并清�?
/// </summary>
public partial class TodayTomorrowDeviationPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _pendingBlockCount;

    protected override object? InitInternalData()
    {
        _pendingBlockCount = 0;
        return null;
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext cc, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type == CardType.Attack)
        {
            _pendingBlockCount++;
            Flash();
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext cc, Player player)
    {
        if (player.Creature != Owner) return;
        if (_pendingBlockCount > 0)
        {
            await CreatureCmd.GainBlock(Owner, Amount * _pendingBlockCount, ValueProp.Move, null);
            _pendingBlockCount = 0;
        }
    }
}