using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Powers;

/// <summary>
/// 铁卫 - 每次进入近卫形态时获得多层覆甲(Plating)
/// Amount为每次获得的覆甲层数
/// </summary>
public partial class IronGuardPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>由FormManagerPower在形态转换后调用</summary>
    public async Task OnFormSwitchTriggered(PlayerChoiceContext choiceContext)
    {
        var fm = Owner.GetPower<FormManagerPower>();
        if (fm != null && fm.CurrentForm == 0) // 近卫形�?
        {
            Flash();
            await PowerCmd.Apply<PlatingPower>(choiceContext, new[] { Owner }, Amount, Owner, null);
        }
    }
}