using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 灵魂堡垒 - 每次形态转换后获得格挡
/// Amount存储每次获得的格挡�?
/// </summary>
public partial class SoulFortressPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>由FormManagerPower在形态转换后调用</summary>
    public async Task OnFormSwitchTriggered(PlayerChoiceContext choiceContext)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }
}