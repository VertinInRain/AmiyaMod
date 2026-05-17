using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Cards;
/// <summary>慈悲愿景 - 手牌�?打击防御时获1/2无实�?消�?/summary>
[Pool(typeof(AmiyaCardPool))]

public partial class MercyVision : BaseAmiyaCard
{ public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
  public MercyVision() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    var handPile = PileType.Hand.GetPile(Owner);
    int count = handPile.Cards.Count(c => c is Strike || c is Defend);
    if (count >= 5)
      await PowerCmd.Apply<IntangiblePower>(cc, new[] { Owner.Creature }, IsUpgraded ? 2m : 1m, Owner.Creature, this);
  } protected override void OnUpgrade() { } }