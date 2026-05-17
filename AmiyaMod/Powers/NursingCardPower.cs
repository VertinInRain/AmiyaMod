using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Powers;

/// <summary>
/// 疗养特供�?- 每打出一张感染牌获得能量
/// </summary>
public partial class NursingCardPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (!cardPlay.Card.Keywords.Contains(AmiyaKeywords.Infection)) return;
        Flash();
        // TODO: 获得能量API
    }
}