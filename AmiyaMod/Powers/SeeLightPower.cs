using System.Linq;
using System.Threading.Tasks;
using Amiya.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 得见光芒 - 打打击获格挡(6)，打防御对随机敌人造成伤害(6)
/// 参�? RagePower (攻击→格�? + SerpentFormPower (打牌→随机伤�?
/// </summary>
public partial class SeeLightPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    private const decimal BlockAmount = 6m;
    private const decimal DamageAmount = 6m;

    public override async Task AfterCardPlayedLate(PlayerChoiceContext cc, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        if (cardPlay.Card is Strike)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, BlockAmount, ValueProp.Unpowered, null);
        }
        else if (cardPlay.Card is Defend)
        {
            Flash();
            var enemies = CombatState.HittableEnemies.ToList();
            if (enemies.Count > 0)
            {
                var target = enemies[new System.Random().Next(enemies.Count)];
                await CreatureCmd.Damage(cc, target, DamageAmount, ValueProp.Unpowered, Owner);
            }
        }
    }
}