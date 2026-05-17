using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 黑冠 - 魔王形态核心状�?
/// 造成的伤害翻倍，每出一张牌，受�?点伤�?
/// </summary>
public partial class BlackCrownPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    
    /// <summary>
    /// 卡牌打出�?- 每出一张牌，受�?点伤�?
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);
        
        // 只在持有者打出卡牌时触发
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        
        // 每出一张牌，受�?点伤�?
        await CreatureCmd.Damage(context, Owner, 3m, ValueProp.Move, Owner);
    }
    
    /// <summary>
    /// 修改伤害倍率 - 造成的伤害翻�?
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只修改由持有者造成的伤�?
        if (dealer == Owner)
        {
            return 2m;
        }
        return 1m;
    }
}
