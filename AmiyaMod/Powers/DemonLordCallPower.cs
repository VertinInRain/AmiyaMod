using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

public partial class DemonLordCallPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        AssertMutable();
        SetAmount(0, false);
    }

    public async Task<bool> AddStack(PlayerChoiceContext cc)
    {
        await PowerCmd.ModifyAmount(cc, this, 1m, Owner, null);
        bool entered = false;
        if (Amount >= 7 && !Owner.HasPower<BlackCrownPower>())
        {
            Flash();
            await PowerCmd.Apply<BlackCrownPower>(cc, new[] { Owner }, 1m, Owner, null);
            await PowerCmd.Apply<EmberPower>(cc, new[] { Owner }, 1m, Owner, null);
            await PowerCmd.Apply<JudgmentPower>(cc, new[] { Owner }, 1m, Owner, null);
            entered = true;
        }
        if (Owner.HasPower<JudgmentPower>())
            foreach (var e in CombatState.HittableEnemies)
                await CreatureCmd.Damage(cc, e, 9m, ValueProp.Move, Owner);
        return entered;
    }
}