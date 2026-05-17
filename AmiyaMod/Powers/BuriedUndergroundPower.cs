using System.Linq;
using System.Threading.Tasks;
using Amiya.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 深埋地底 - 打击命中敌人时本回合减敌人力�?
/// 参�? MonarchsGazePower.AfterDamageGiven
/// 附加检测：只有打击(Strike标签)才触�?
/// </summary>
public partial class BuriedUndergroundPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageGiven(PlayerChoiceContext cc, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner) return;
        if (!props.IsPoweredAttack()) return;
        if (cardSource == null || !(cardSource is Strike)) return;
        await PowerCmd.Apply<StrengthPower>(cc, new[] { target }, -Amount, Owner, null);
    }
}