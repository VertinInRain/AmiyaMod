using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Powers;

/// <summary>
/// 剑不�?- 下一张领袖牌耗能�?
/// 参�? VeilpiercerPower.TryModifyEnergyCostInCombat
/// </summary>
public partial class SwordNeverSleepsPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner) return false;
        if (!card.Keywords.Contains(AmiyaKeywords.Leader)) return false;
        if (card.Pile?.Type != PileType.Hand && card.Pile?.Type != PileType.Play) return false;
        modifiedCost = 0m;
        return true;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext cc, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == Owner && cardPlay.Card.Keywords.Contains(AmiyaKeywords.Leader))
            await PowerCmd.Remove(this);
    }
}