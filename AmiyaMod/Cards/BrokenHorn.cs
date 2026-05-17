using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class BrokenHorn : BaseAmiyaCard
{
    public BrokenHorn() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        bool upgraded = IsUpgraded;
        await CreatureCmd.GainBlock(Owner.Creature, upgraded ? 18m : 15m, ValueProp.Move, cp);
        // 检查手牌是否有攻击牌
        var handPile = PileType.Hand.GetPile(Owner);
        if (!handPile.Cards.Any(c => c.Type == CardType.Attack))
        {
            int bonus = upgraded ? 2 : 1;
            await PowerCmd.Apply<DrawCardsNextTurnPower>(cc, new[] { Owner.Creature }, bonus, Owner.Creature, this);
            await PowerCmd.Apply<EnergyNextTurnPower>(cc, new[] { Owner.Creature }, bonus, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() { }
}