using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 魔王的祭�?- 打出魔王词条牌时对随机敌人造成伤害
/// </summary>
public partial class DemonLordVesselPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (!cardPlay.Card.Keywords.Contains(AmiyaKeywords.DemonLord)) return;
        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0) return;
        var target = enemies[new System.Random().Next(enemies.Count)];
        Flash();
        await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered, Owner);
    }
}