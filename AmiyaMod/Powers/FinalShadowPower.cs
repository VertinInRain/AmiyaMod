using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Powers;

/// <summary>
/// 终焉之影 - 每消耗一张牌获得1层魔王之�?
/// </summary>
public partial class FinalShadowPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner?.Creature != Owner) return;
        var dlc = Owner.GetPower<DemonLordCallPower>();
        if (dlc != null) await dlc.AddStack(choiceContext);
    }
}