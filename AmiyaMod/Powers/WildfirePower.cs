using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Powers;

public partial class WildfirePower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    private int _strengthGainedThisTurn;

    public async Task OnFormSwitchTriggered(PlayerChoiceContext cc)
    {
        Flash();
        await PowerCmd.Apply<StrengthPower>(cc, new[] { Owner }, Amount, Owner, null);
        _strengthGainedThisTurn += Amount;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext cc, CombatSide side)
    {
        await base.AfterTurnEnd(cc, side);
        if (side == Owner.Side && _strengthGainedThisTurn > 0)
        {
            await PowerCmd.Apply<StrengthPower>(cc, new[] { Owner }, -_strengthGainedThisTurn, Owner, null);
            _strengthGainedThisTurn = 0;
        }
    }
}