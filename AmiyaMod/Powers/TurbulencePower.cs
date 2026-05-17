using System.Linq;
using System.Threading.Tasks;
using Amiya.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Powers;

/// <summary>
/// 湍流 - 每次抽到打击/防御时抽一张牌
/// </summary>
public partial class TurbulencePower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner?.Creature != Owner) return;
        if (card is Strike || card is Defend)
        {
            Flash();
            if (Owner.Player != null)
                await CardPileCmd.Draw(choiceContext, 1m, Owner.Player);
        }
    }
}