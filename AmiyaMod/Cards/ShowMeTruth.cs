using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class ShowMeTruth : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? new[] { CardKeyword.Exhaust, CardKeyword.Retain } : new[] { CardKeyword.Exhaust };
    public ShowMeTruth() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        await PowerCmd.Apply<IntangiblePower>(cc, new[] { Owner.Creature }, 2m, Owner.Creature, this);
        var toxics = new[] { CombatState.CreateCard<Toxic>(Owner), CombatState.CreateCard<Toxic>(Owner), CombatState.CreateCard<Toxic>(Owner) };
        var r1 = await CardPileCmd.AddGeneratedCardToCombat(toxics[0], PileType.Draw, Owner);
        var r2 = await CardPileCmd.AddGeneratedCardToCombat(toxics[1], PileType.Discard, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(toxics[2], PileType.Hand, Owner);
        CardCmd.PreviewCardPileAdd(new[] { r1, r2 });
    }
    protected override void OnUpgrade() { }
}