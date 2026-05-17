using System.Threading.Tasks;
using BaseLib.Abstracts;
using Amiya.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 相变不息 - 每获得一次格挡，触发一次形态转�?
/// 参�? JuggernautPower.AfterBlockGained
/// </summary>
public partial class PhaseChangePower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (amount <= 0m || creature != Owner) return;
        var fm = Owner.GetPower<FormManagerPower>();
        if (fm != null) await fm.TriggerFormSwitch(new ThrowingPlayerChoiceContext());
    }
}