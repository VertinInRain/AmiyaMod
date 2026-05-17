using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Powers;

/// <summary>
/// 虚空残片 - 每回合第一张牌免费
/// 参�? VoidFormPower.TryModifyEnergyCostInCombat
/// 维护内部计数器追踪每回合已打出牌�?
/// </summary>
public partial class VoidFragmentPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    private int _cardsPlayedThisTurn;

    protected override object? InitInternalData()
    {
        _cardsPlayedThisTurn = 0;
        return null;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (_cardsPlayedThisTurn > 0) return false;
        modifiedCost = 0m;
        return true;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == Owner)
            _cardsPlayedThisTurn++;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext cc, Player player)
    {
        if (player.Creature == Owner)
            _cardsPlayedThisTurn = 0;
        await Task.CompletedTask;
    }
}