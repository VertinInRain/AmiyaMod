using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Powers;

/// <summary>
/// 洪流不息 - 追踪本回合已打出攻击牌数，打出时获得等量能量
/// 每回合开始时计数器清�?
/// </summary>
public partial class EndlessFloodCounterPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public int AttacksPlayedThisTurn { get; private set; }

    protected override object? InitInternalData()
    {
        AttacksPlayedThisTurn = 0;
        return null;
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext cc, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type == CardType.Attack)
            AttacksPlayedThisTurn++;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext cc, Player player)
    {
        if (player.Creature == Owner)
            AttacksPlayedThisTurn = 0;
    }
}