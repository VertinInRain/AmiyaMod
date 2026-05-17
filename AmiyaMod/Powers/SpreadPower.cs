using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Powers;

/// <summary>
/// 蔓延 - 每次形态转换时抽牌
/// Amount存储每次抽牌数量
/// </summary>
public partial class SpreadPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnFormSwitchTriggered(PlayerChoiceContext choiceContext)
    {
        if (Owner.Player == null) return;
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
    }
}