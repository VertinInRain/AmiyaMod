using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class BorrowTomorrow : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    public BorrowTomorrow() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        await PlayerCmd.GainEnergy(2m, Owner);
        int draw = IsUpgraded ? 4 : 3;
        await CardPileCmd.Draw(cc, draw, Owner);
        var results = new List<CardPileAddResult>();
        for (int i = 0; i < 2; i++)
        {
            var v = CombatState.CreateCard<Void>(Owner);
            results.Add(await CardPileCmd.AddGeneratedCardToCombat(v, PileType.Draw, Owner));
        }
        CardCmd.PreviewCardPileAdd(results);
    }
    protected override void OnUpgrade() { }
}