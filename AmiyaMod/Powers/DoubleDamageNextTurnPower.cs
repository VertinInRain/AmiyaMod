using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Powers;

/// <summary>
/// 下回合伤害翻�?- 下回合开始时施加DoubleDamagePower并自�?
/// 参�? ShadowStepPower
/// </summary>
public partial class DoubleDamageNextTurnPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    private bool _applied = false;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext cc, Player player)
    {
        if (player.Creature != Owner) return;
        if (!_applied)
        {
            _applied = true;
            await PowerCmd.Apply<DoubleDamagePower>(cc, new[] { Owner }, Amount, Owner, null);
            await PowerCmd.Remove(this);
        }
    }
}