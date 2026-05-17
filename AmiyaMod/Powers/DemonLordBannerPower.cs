using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Powers;

/// <summary>
/// 魔王的旗�?- 打出魔王词条牌时抽牌
/// </summary>
public partial class DemonLordBannerPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (!cardPlay.Card.Keywords.Contains(AmiyaKeywords.DemonLord)) return;
        Flash();
        if (Owner.Player != null)
            await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
    }
}