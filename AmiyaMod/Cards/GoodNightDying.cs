using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class GoodNightDying : BaseAmiyaCard
{
    public GoodNightDying() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var results = new List<CardPileAddResult>();
        for (int i = 0; i < 2; i++)
        {
            var s = CombatState.CreateCard<Strike>(Owner);
            results.Add(await CardPileCmd.AddGeneratedCardToCombat(s, PileType.Draw, Owner));
        }
        CardCmd.PreviewCardPileAdd(results);
        await PowerCmd.Apply<DoubleDamageNextTurnPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() { UpgradeStarCostBy(-1); }
}